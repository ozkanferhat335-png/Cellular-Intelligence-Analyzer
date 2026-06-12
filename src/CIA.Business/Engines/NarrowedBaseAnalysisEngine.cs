using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Core.Helpers;
using CIA.Data.Repositories;
using Newtonsoft.Json;
using NLog;

namespace CIA.Business.Engines
{
    public interface INarrowedBaseAnalysisEngine
    {
        Task<NarrowedBaseAnalysisResultDto> AnalyzeAsync(NarrowedBaseAnalysisRequestDto request);
        Task<int> CalculateConfidenceScoreAsync(NarrowedBaseAnalysisResultDto result, List<ScoringDetailDto> details);
    }

    public class NarrowedBaseAnalysisEngine : INarrowedBaseAnalysisEngine
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHtsAnalysisEngine _htsEngine;
        private readonly ICoverageModelingEngine _coverageEngine;

        public NarrowedBaseAnalysisEngine(
            IUnitOfWork unitOfWork,
            IHtsAnalysisEngine htsEngine,
            ICoverageModelingEngine coverageEngine)
        {
            _unitOfWork = unitOfWork;
            _htsEngine = htsEngine;
            _coverageEngine = coverageEngine;
        }

        public async Task<NarrowedBaseAnalysisResultDto> AnalyzeAsync(NarrowedBaseAnalysisRequestDto request)
        {
            var startTime = DateTime.UtcNow;
            Logger.Info($"Daraltılmış baz analizi başlatılıyor: {request.PhoneNumber ?? request.IMEI}");

            var result = new NarrowedBaseAnalysisResultDto
            {
                AnalysisId = Guid.NewGuid().ToString("N"),
                PhoneNumber = request.PhoneNumber,
                IMEI = request.IMEI,
                AnalysisDate = DateTime.Now,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            // Step 1: Get HTS records
            var htsQuery = new HtsQueryDto
            {
                PhoneNumber = request.PhoneNumber,
                IMEI = request.IMEI,
                IMSI = request.IMSI,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PageSize = AppConstants.MaxHtsRecordsPerQuery
            };

            var htsResult = await _unitOfWork.HtsRecords.QueryAsync(htsQuery);
            result.TotalHtsRecords = htsResult.TotalCount;

            // Minimum record check
            if (htsResult.TotalCount < request.MinHtsRecords)
            {
                result.Warnings.Add($"Yetersiz HTS kaydı: {htsResult.TotalCount} kayıt bulundu, minimum {request.MinHtsRecords} gerekli.");
                result.ConfidenceScore = 0;
                result.ConfidenceLevel = ConfidenceLevel.Low;
                result.Summary = "Analiz için yeterli HTS kaydı bulunamadı.";
                return result;
            }

            // Minimum duration check
            var timeSpan = request.EndDate - request.StartDate;
            if (timeSpan.TotalMinutes < request.MinAnalysisMinutes)
            {
                result.Warnings.Add($"Analiz süresi çok kısa: {timeSpan.TotalMinutes:F0} dakika, minimum {request.MinAnalysisMinutes} dakika gerekli.");
            }

            // Step 2: Analyze movement
            var movement = await _htsEngine.AnalyzeMovementAsync(
                request.PhoneNumber ?? request.IMEI,
                request.StartDate,
                request.EndDate);

            if (movement != null)
            {
                result.MovementHistory = movement.MovementPoints;
                result.Transitions = movement.Transitions;
                result.MovementPattern = movement.Pattern;
                result.TotalDistanceKm = movement.TotalDistanceKm;
                result.AverageSpeedKmh = movement.AverageSpeedKmh;
            }

            // Step 3: Build location estimates
            var locationEstimates = await BuildLocationEstimatesAsync(htsResult.Records, request);
            result.LocationEstimates = locationEstimates;

            // Step 4: Calculate confidence score
            var scoringDetails = new List<ScoringDetailDto>();
            result.ConfidenceScore = await CalculateConfidenceScoreAsync(result, scoringDetails);
            result.ScoringDetails = scoringDetails;

            // Step 5: Determine confidence level
            if (result.ConfidenceScore <= AppConstants.ConfidenceScoreLow)
                result.ConfidenceLevel = ConfidenceLevel.Low;
            else if (result.ConfidenceScore <= AppConstants.ConfidenceScoreMedium)
                result.ConfidenceLevel = ConfidenceLevel.Medium;
            else
                result.ConfidenceLevel = ConfidenceLevel.High;

            // Step 6: Generate summary
            result.Summary = GenerateSummary(result);
            result.AnalysisDuration = DateTime.UtcNow - startTime;

            Logger.Info($"Daraltılmış baz analizi tamamlandı. Güven skoru: {result.ConfidenceScore}, Süre: {result.AnalysisDuration.TotalSeconds:F1}s");
            return result;
        }

        public async Task<int> CalculateConfidenceScoreAsync(
            NarrowedBaseAnalysisResultDto result,
            List<ScoringDetailDto> details)
        {
            double totalScore = 0;

            // 1. Cell Match Score (30%)
            double cellMatchScore = CalculateCellMatchScore(result);
            double cellWeighted = cellMatchScore * AppConstants.WeightCellMatch * 100;
            totalScore += cellWeighted;
            details.Add(new ScoringDetailDto
            {
                Parameter = "Cell Eşleşmesi",
                Weight = AppConstants.WeightCellMatch,
                RawScore = cellMatchScore,
                WeightedScore = cellWeighted,
                Description = $"HTS kayıtlarındaki Cell ID'lerin baz istasyonu veritabanıyla eşleşme oranı"
            });

            // 2. Sector Match Score (20%)
            double sectorMatchScore = CalculateSectorMatchScore(result);
            double sectorWeighted = sectorMatchScore * AppConstants.WeightSectorMatch * 100;
            totalScore += sectorWeighted;
            details.Add(new ScoringDetailDto
            {
                Parameter = "Sektör Eşleşmesi",
                Weight = AppConstants.WeightSectorMatch,
                RawScore = sectorMatchScore,
                WeightedScore = sectorWeighted,
                Description = "Sektör yönü ve kapsama alanı eşleşme kalitesi"
            });

            // 3. Measurement Match Score (20%)
            double measurementScore = CalculateMeasurementMatchScore(result);
            double measurementWeighted = measurementScore * AppConstants.WeightMeasurementMatch * 100;
            totalScore += measurementWeighted;
            details.Add(new ScoringDetailDto
            {
                Parameter = "Ölçüm Eşleşmesi",
                Weight = AppConstants.WeightMeasurementMatch,
                RawScore = measurementScore,
                WeightedScore = measurementWeighted,
                Description = "Drive Test ölçümleriyle korelasyon"
            });

            // 4. Time Match Score (15%)
            double timeScore = CalculateTimeMatchScore(result);
            double timeWeighted = timeScore * AppConstants.WeightTimeMatch * 100;
            totalScore += timeWeighted;
            details.Add(new ScoringDetailDto
            {
                Parameter = "Zaman Eşleşmesi",
                Weight = AppConstants.WeightTimeMatch,
                RawScore = timeScore,
                WeightedScore = timeWeighted,
                Description = "HTS kayıtları ile ölçüm zamanlarının uyumu"
            });

            // 5. Distance Fitness Score (10%)
            double distanceScore = CalculateDistanceFitnessScore(result);
            double distanceWeighted = distanceScore * AppConstants.WeightDistanceFit * 100;
            totalScore += distanceWeighted;
            details.Add(new ScoringDetailDto
            {
                Parameter = "Mesafe Uygunluğu",
                Weight = AppConstants.WeightDistanceFit,
                RawScore = distanceScore,
                WeightedScore = distanceWeighted,
                Description = "Tahmin edilen konumun baz istasyonu kapsama alanına uygunluğu"
            });

            // 6. Movement Consistency Score (5%)
            double movementScore = CalculateMovementConsistencyScore(result);
            double movementWeighted = movementScore * AppConstants.WeightMovementConsistency * 100;
            totalScore += movementWeighted;
            details.Add(new ScoringDetailDto
            {
                Parameter = "Hareket Tutarlılığı",
                Weight = AppConstants.WeightMovementConsistency,
                RawScore = movementScore,
                WeightedScore = movementWeighted,
                Description = "Ardışık baz değişimlerinin fiziksel olarak mümkün olup olmadığı"
            });

            return await Task.FromResult((int)Math.Min(100, Math.Max(0, totalScore)));
        }

        private async Task<List<LocationEstimateDto>> BuildLocationEstimatesAsync(
            List<HtsRecordDto> records,
            NarrowedBaseAnalysisRequestDto request)
        {
            var estimates = new List<LocationEstimateDto>();

            var cellGroups = records
                .Where(r => !string.IsNullOrEmpty(r.CellId))
                .GroupBy(r => r.CellId)
                .OrderByDescending(g => g.Count());

            foreach (var group in cellGroups.Take(20))
            {
                var cell = await _unitOfWork.Cells.GetCellWithSectorAndSiteAsync(group.Key);
                if (cell?.Sector?.Site == null) continue;

                var site = cell.Sector.Site;
                var sector = cell.Sector;

                double radiusKm = GeoHelper.EstimateCoverageRadiusKm(
                    sector.TxPowerDbm,
                    site.AntennaHeightM ?? AppConstants.DefaultAntennaHeightM,
                    sector.FrequencyMhz > 0 ? sector.FrequencyMhz : AppConstants.DefaultFrequencyMhz);

                var polygon = GeoHelper.GenerateSectorPolygon(
                    site.Latitude, site.Longitude,
                    sector.Azimuth, sector.BeamWidthDegrees,
                    radiusKm);

                double probability = (double)group.Count() / records.Count;

                var estimate = new LocationEstimateDto
                {
                    CenterLatitude = site.Latitude,
                    CenterLongitude = site.Longitude,
                    RadiusKm = radiusKm,
                    Probability = probability,
                    EstimatedTime = group.OrderBy(r => r.CallDateTime).First().CallDateTime,
                    BasedOnCellId = group.Key,
                    SiteCode = site.SiteCode,
                    Azimuth = sector.Azimuth,
                    PolygonPoints = polygon
                };

                // Check for Drive Test confirmation
                if (request.UsedriveTestData)
                {
                    var driveTestRecords = await _unitOfWork.DriveTests.GetRecordsByCellIdAsync(group.Key);
                    if (driveTestRecords.Any())
                    {
                        estimate.HasDriveTestConfirmation = true;
                        estimate.DriveTestConfidenceBoost = 0.15;
                        estimate.Probability = Math.Min(1.0, estimate.Probability + estimate.DriveTestConfidenceBoost);
                    }
                }

                estimates.Add(estimate);
            }

            return estimates.OrderByDescending(e => e.Probability).ToList();
        }

        private double CalculateCellMatchScore(NarrowedBaseAnalysisResultDto result)
        {
            if (result.TotalHtsRecords == 0) return 0;
            var matchedCount = result.LocationEstimates.Count(e => !string.IsNullOrEmpty(e.SiteCode));
            return Math.Min(1.0, (double)matchedCount / Math.Max(1, result.LocationEstimates.Count));
        }

        private double CalculateSectorMatchScore(NarrowedBaseAnalysisResultDto result)
        {
            if (!result.LocationEstimates.Any()) return 0;
            var withPolygon = result.LocationEstimates.Count(e => e.PolygonPoints.Any());
            return (double)withPolygon / result.LocationEstimates.Count;
        }

        private double CalculateMeasurementMatchScore(NarrowedBaseAnalysisResultDto result)
        {
            var withDriveTest = result.LocationEstimates.Count(e => e.HasDriveTestConfirmation);
            if (!result.LocationEstimates.Any()) return 0.3; // Base score without drive test
            return 0.3 + (0.7 * (double)withDriveTest / result.LocationEstimates.Count);
        }

        private double CalculateTimeMatchScore(NarrowedBaseAnalysisResultDto result)
        {
            if (result.TotalHtsRecords < AppConstants.MinHtsRecordsForAnalysis) return 0.2;
            if (result.TotalHtsRecords >= 20) return 1.0;
            return 0.2 + (0.8 * (result.TotalHtsRecords - AppConstants.MinHtsRecordsForAnalysis) /
                         (20.0 - AppConstants.MinHtsRecordsForAnalysis));
        }

        private double CalculateDistanceFitnessScore(NarrowedBaseAnalysisResultDto result)
        {
            if (!result.Transitions.Any()) return 0.5;
            var validTransitions = result.Transitions.Count(t => !t.IsSuspicious);
            return (double)validTransitions / result.Transitions.Count;
        }

        private double CalculateMovementConsistencyScore(NarrowedBaseAnalysisResultDto result)
        {
            if (result.MovementPattern == MovementPattern.Unknown) return 0.3;
            if (result.MovementPattern == MovementPattern.Erratic) return 0.2;
            if (result.MovementPattern == MovementPattern.Stationary) return 0.9;
            return 0.7;
        }

        private string GenerateSummary(NarrowedBaseAnalysisResultDto result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Analiz Özeti - {result.PhoneNumber ?? result.IMEI}");
            sb.AppendLine($"Dönem: {result.StartDate:dd.MM.yyyy HH:mm} - {result.EndDate:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"Toplam HTS Kaydı: {result.TotalHtsRecords:N0}");
            sb.AppendLine($"Güven Skoru: {result.ConfidenceScore}/100 ({result.ConfidenceLevel})");
            sb.AppendLine($"Hareket Modeli: {result.MovementPattern}");
            sb.AppendLine($"Toplam Mesafe: {result.TotalDistanceKm:F1} km");
            sb.AppendLine($"Ortalama Hız: {result.AverageSpeedKmh:F1} km/h");
            sb.AppendLine($"Tespit Edilen Konum Sayısı: {result.LocationEstimates.Count}");

            if (result.LocationEstimates.Any())
            {
                var topLocation = result.LocationEstimates.First();
                sb.AppendLine($"En Olası Konum: {topLocation.SiteCode} (Olasılık: {topLocation.Probability:P0})");
            }

            return sb.ToString();
        }
    }
}
