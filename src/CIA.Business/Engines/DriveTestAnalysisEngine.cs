using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Core.Helpers;
using CIA.Data.Repositories;
using NLog;

namespace CIA.Business.Engines
{
    public interface IDriveTestAnalysisEngine
    {
        Task<DriveTestAnalysisDto> AnalyzeAsync(int driveTestId);
        Task<List<CoverageHoleDto>> DetectCoverageHolesAsync(int driveTestId);
        Task<List<OvershootingDto>> DetectOvershootingAsync(int driveTestId);
        Task<List<PciConflictDto>> DetectPciConflictsAsync(int driveTestId);
        Task<List<MissingNeighborDto>> DetectMissingNeighborsAsync(int driveTestId);
        SignalQuality ClassifySignalQuality(double? rsrp);
    }

    public class DriveTestAnalysisEngine : IDriveTestAnalysisEngine
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        // Coverage hole detection parameters
        private const double CoverageHoleMinRadius = 0.2;
        private const double CoverageHoleRsrpThreshold = -110.0;
        private const int CoverageHoleMinPoints = 5;

        // Overshooting detection
        private const double OvershootingMinDistance = 3.0;

        // PCI conflict detection
        private const double PciConflictMaxDistance = 5.0;

        public DriveTestAnalysisEngine(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DriveTestAnalysisDto> AnalyzeAsync(int driveTestId)
        {
            Logger.Info($"Drive Test analizi başlatılıyor: DriveTestId={driveTestId}");

            var driveTest = await _unitOfWork.DriveTests.GetAllWithStatsAsync();
            var dt = driveTest.FirstOrDefault(d => d.Id == driveTestId);

            if (dt == null)
            {
                Logger.Warn($"Drive Test bulunamadı: {driveTestId}");
                return null;
            }

            var analysis = new DriveTestAnalysisDto
            {
                DriveTestId = driveTestId,
                TestName = dt.TestName,
                AnalysisDate = DateTime.Now
            };

            // Run all analyses in parallel
            var coverageHolesTask = DetectCoverageHolesAsync(driveTestId);
            var overshootingTask = DetectOvershootingAsync(driveTestId);
            var pciConflictsTask = DetectPciConflictsAsync(driveTestId);
            var missingNeighborsTask = DetectMissingNeighborsAsync(driveTestId);
            var statisticsTask = _unitOfWork.DriveTests.GetStatisticsAsync(driveTestId);

            await Task.WhenAll(coverageHolesTask, overshootingTask, pciConflictsTask, missingNeighborsTask, statisticsTask);

            analysis.CoverageHoles = await coverageHolesTask;
            analysis.OvershootingCells = await overshootingTask;
            analysis.PciConflicts = await pciConflictsTask;
            analysis.MissingNeighbors = await missingNeighborsTask;
            analysis.Statistics = await statisticsTask;

            // Compile anomalies
            analysis.Anomalies = CompileAnomalies(analysis);

            Logger.Info($"Drive Test analizi tamamlandı: {analysis.CoverageHoles.Count} kapsama deliği, " +
                       $"{analysis.OvershootingCells.Count} overshooting, {analysis.PciConflicts.Count} PCI çakışması");

            return analysis;
        }

        public async Task<List<CoverageHoleDto>> DetectCoverageHolesAsync(int driveTestId)
        {
            var query = new DriveTestQueryDto
            {
                DriveTestId = driveTestId,
                MaxRSRP = CoverageHoleRsrpThreshold,
                PageSize = 1000000
            };

            var records = (await _unitOfWork.DriveTests.GetRecordsAsync(query)).ToList();
            var holes = new List<CoverageHoleDto>();

            if (!records.Any()) return holes;

            // Cluster weak signal points
            var clusters = ClusterPoints(records, 0.5); // 500m clustering radius

            foreach (var cluster in clusters.Where(c => c.Count >= CoverageHoleMinPoints))
            {
                var centerLat = cluster.Average(r => r.Latitude);
                var centerLon = cluster.Average(r => r.Longitude);
                var avgRsrp = cluster.Where(r => r.RSRP.HasValue).Average(r => r.RSRP.Value);

                double maxRadius = 0;
                foreach (var point in cluster)
                {
                    var dist = GeoHelper.CalculateDistanceKm(centerLat, centerLon, point.Latitude, point.Longitude);
                    if (dist > maxRadius) maxRadius = dist;
                }

                if (maxRadius >= CoverageHoleMinRadius)
                {
                    holes.Add(new CoverageHoleDto
                    {
                        CenterLatitude = centerLat,
                        CenterLongitude = centerLon,
                        RadiusKm = maxRadius,
                        AvgRSRP = avgRsrp,
                        PointCount = cluster.Count,
                        SeverityScore = CalculateCoverageHoleSeverity(avgRsrp, maxRadius, cluster.Count)
                    });
                }
            }

            return holes.OrderByDescending(h => h.SeverityScore).ToList();
        }

        public async Task<List<OvershootingDto>> DetectOvershootingAsync(int driveTestId)
        {
            var query = new DriveTestQueryDto { DriveTestId = driveTestId, PageSize = 1000000 };
            var records = (await _unitOfWork.DriveTests.GetRecordsAsync(query)).ToList();
            var overshooting = new List<OvershootingDto>();

            var cellGroups = records
                .Where(r => !string.IsNullOrEmpty(r.ServingCellId))
                .GroupBy(r => r.ServingCellId);

            foreach (var group in cellGroups)
            {
                var cell = await _unitOfWork.Cells.GetCellWithSectorAndSiteAsync(group.Key);
                if (cell?.Sector?.Site == null) continue;

                var site = cell.Sector.Site;
                double maxDetectedDistance = 0;

                foreach (var record in group)
                {
                    var dist = GeoHelper.CalculateDistanceKm(
                        site.Latitude, site.Longitude,
                        record.Latitude, record.Longitude);
                    if (dist > maxDetectedDistance) maxDetectedDistance = dist;
                }

                double expectedMaxDistance = GeoHelper.EstimateCoverageRadiusKm(
                    cell.Sector.TxPowerDbm,
                    site.AntennaHeightM ?? AppConstants.DefaultAntennaHeightM,
                    cell.Sector.FrequencyMhz > 0 ? cell.Sector.FrequencyMhz : AppConstants.DefaultFrequencyMhz);

                double overshootRatio = maxDetectedDistance / expectedMaxDistance;

                if (overshootRatio > AppConstants.OvershootingDistanceMultiplier &&
                    maxDetectedDistance > OvershootingMinDistance)
                {
                    overshooting.Add(new OvershootingDto
                    {
                        CellId = group.Key,
                        SiteCode = site.SiteCode,
                        SiteLatitude = site.Latitude,
                        SiteLongitude = site.Longitude,
                        MaxDetectedDistanceKm = maxDetectedDistance,
                        ExpectedMaxDistanceKm = expectedMaxDistance,
                        OvershootRatio = overshootRatio,
                        AffectedPoints = group.Count()
                    });
                }
            }

            return overshooting.OrderByDescending(o => o.OvershootRatio).ToList();
        }

        public async Task<List<PciConflictDto>> DetectPciConflictsAsync(int driveTestId)
        {
            var allCells = (await _unitOfWork.Cells.GetAllCellsWithLocationAsync()).ToList();
            var conflicts = new List<PciConflictDto>();

            var pciGroups = allCells
                .Where(c => c.PCI.HasValue)
                .GroupBy(c => c.PCI.Value)
                .Where(g => g.Count() > 1);

            foreach (var group in pciGroups)
            {
                var cellList = group.ToList();
                double minDistance = double.MaxValue;
                var conflictingCells = new List<string>();

                for (int i = 0; i < cellList.Count; i++)
                {
                    for (int j = i + 1; j < cellList.Count; j++)
                    {
                        var dist = GeoHelper.CalculateDistanceKm(
                            cellList[i].SiteLatitude, cellList[i].SiteLongitude,
                            cellList[j].SiteLatitude, cellList[j].SiteLongitude);

                        if (dist < minDistance) minDistance = dist;

                        if (dist < PciConflictMaxDistance)
                        {
                            if (!conflictingCells.Contains(cellList[i].CellId))
                                conflictingCells.Add(cellList[i].CellId);
                            if (!conflictingCells.Contains(cellList[j].CellId))
                                conflictingCells.Add(cellList[j].CellId);
                        }
                    }
                }

                if (conflictingCells.Any())
                {
                    string severity = minDistance < 1.0 ? "Kritik" : minDistance < 3.0 ? "Yüksek" : "Orta";
                    conflicts.Add(new PciConflictDto
                    {
                        PCI = group.Key,
                        ConflictingCells = conflictingCells,
                        MinDistanceBetweenCellsKm = minDistance,
                        Severity = severity
                    });
                }
            }

            return conflicts.OrderBy(c => c.MinDistanceBetweenCellsKm).ToList();
        }

        public async Task<List<MissingNeighborDto>> DetectMissingNeighborsAsync(int driveTestId)
        {
            var query = new DriveTestQueryDto { DriveTestId = driveTestId, PageSize = 1000000 };
            var records = (await _unitOfWork.DriveTests.GetRecordsAsync(query)).ToList();
            var missingNeighbors = new List<MissingNeighborDto>();

            var servingCellGroups = records
                .Where(r => !string.IsNullOrEmpty(r.ServingCellId))
                .GroupBy(r => r.ServingCellId);

            foreach (var servingGroup in servingCellGroups)
            {
                var servingCell = await _unitOfWork.Cells.GetCellWithSectorAndSiteAsync(servingGroup.Key);
                if (servingCell?.Sector?.Site == null) continue;

                // Find nearby cells that appear in drive test but are not neighbors
                var nearbyRecords = records
                    .Where(r => r.ServingCellId != servingGroup.Key &&
                                !string.IsNullOrEmpty(r.ServingCellId))
                    .ToList();

                var potentialNeighborGroups = nearbyRecords
                    .GroupBy(r => r.ServingCellId)
                    .Where(g => g.Count() >= 3);

                foreach (var neighborGroup in potentialNeighborGroups)
                {
                    var neighborCell = await _unitOfWork.Cells.GetCellWithSectorAndSiteAsync(neighborGroup.Key);
                    if (neighborCell?.Sector?.Site == null) continue;

                    var distance = GeoHelper.CalculateDistanceKm(
                        servingCell.Sector.Site.Latitude, servingCell.Sector.Site.Longitude,
                        neighborCell.Sector.Site.Latitude, neighborCell.Sector.Site.Longitude);

                    if (distance < 5.0) // Within 5km
                    {
                        var avgRsrp = neighborGroup.Where(r => r.RSRP.HasValue).Average(r => r.RSRP ?? -120);

                        if (!missingNeighbors.Any(mn =>
                            mn.ServingCellId == servingGroup.Key &&
                            mn.PotentialNeighborCellId == neighborGroup.Key))
                        {
                            missingNeighbors.Add(new MissingNeighborDto
                            {
                                ServingCellId = servingGroup.Key,
                                PotentialNeighborCellId = neighborGroup.Key,
                                DistanceKm = distance,
                                AvgRSRP = avgRsrp,
                                DetectionCount = neighborGroup.Count()
                            });
                        }
                    }
                }
            }

            return missingNeighbors.OrderByDescending(mn => mn.DetectionCount).ToList();
        }

        public SignalQuality ClassifySignalQuality(double? rsrp)
        {
            if (!rsrp.HasValue) return SignalQuality.NoSignal;
            if (rsrp >= AppConstants.RsrpExcellent) return SignalQuality.Excellent;
            if (rsrp >= AppConstants.RsrpGood) return SignalQuality.Good;
            if (rsrp >= AppConstants.RsrpFair) return SignalQuality.Fair;
            if (rsrp >= AppConstants.RsrpPoor) return SignalQuality.Poor;
            return SignalQuality.NoSignal;
        }

        private List<List<CIA.Data.Entities.DriveTestRecord>> ClusterPoints(
            List<CIA.Data.Entities.DriveTestRecord> records, double radiusKm)
        {
            var clusters = new List<List<CIA.Data.Entities.DriveTestRecord>>();
            var assigned = new HashSet<long>();

            foreach (var record in records)
            {
                if (assigned.Contains(record.Id)) continue;

                var cluster = new List<CIA.Data.Entities.DriveTestRecord> { record };
                assigned.Add(record.Id);

                foreach (var other in records)
                {
                    if (assigned.Contains(other.Id)) continue;
                    var dist = GeoHelper.CalculateDistanceKm(
                        record.Latitude, record.Longitude,
                        other.Latitude, other.Longitude);
                    if (dist <= radiusKm)
                    {
                        cluster.Add(other);
                        assigned.Add(other.Id);
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        private double CalculateCoverageHoleSeverity(double avgRsrp, double radiusKm, int pointCount)
        {
            double rsrpFactor = Math.Max(0, (-avgRsrp - 100) / 40.0); // Normalize -100 to -140
            double radiusFactor = Math.Min(1.0, radiusKm / 2.0);
            double countFactor = Math.Min(1.0, pointCount / 50.0);
            return (rsrpFactor * 0.5 + radiusFactor * 0.3 + countFactor * 0.2) * 100;
        }

        private List<AnomalyDto> CompileAnomalies(DriveTestAnalysisDto analysis)
        {
            var anomalies = new List<AnomalyDto>();

            foreach (var hole in analysis.CoverageHoles.Take(10))
            {
                anomalies.Add(new AnomalyDto
                {
                    Type = AnomalyType.CoverageHole,
                    Description = $"Kapsama deliği tespit edildi. Ortalama RSRP: {hole.AvgRSRP:F1} dBm",
                    Latitude = hole.CenterLatitude,
                    Longitude = hole.CenterLongitude,
                    Severity = hole.SeverityScore,
                    Recommendation = "Yeni baz istasyonu kurulumu veya mevcut baz parametrelerinin optimizasyonu önerilir.",
                    Probability = 0.85
                });
            }

            foreach (var overshoot in analysis.OvershootingCells.Take(10))
            {
                anomalies.Add(new AnomalyDto
                {
                    Type = AnomalyType.Overshooting,
                    Description = $"Overshooting tespit edildi. Hücre: {overshoot.CellId}, Oran: {overshoot.OvershootRatio:F1}x",
                    Latitude = overshoot.SiteLatitude,
                    Longitude = overshoot.SiteLongitude,
                    Severity = Math.Min(100, overshoot.OvershootRatio * 20),
                    AffectedCell = overshoot.CellId,
                    Recommendation = $"Elektriksel tilt artırılması önerilir. Mevcut kapsama yarıçapı: {overshoot.MaxDetectedDistanceKm:F1} km",
                    Probability = 0.90
                });
            }

            foreach (var conflict in analysis.PciConflicts.Take(10))
            {
                anomalies.Add(new AnomalyDto
                {
                    Type = AnomalyType.PCIConflict,
                    Description = $"PCI çakışması: PCI={conflict.PCI}, {conflict.ConflictingCells.Count} hücre etkileniyor",
                    Severity = conflict.Severity == "Kritik" ? 90 : conflict.Severity == "Yüksek" ? 70 : 50,
                    Recommendation = "PCI yeniden planlaması gereklidir.",
                    Probability = 0.95
                });
            }

            return anomalies.OrderByDescending(a => a.Severity).ToList();
        }
    }
}
