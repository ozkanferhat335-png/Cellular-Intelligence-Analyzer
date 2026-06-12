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
    public interface IAiAnalysisEngine
    {
        Task<List<RfOptimizationRecommendationDto>> GenerateRfOptimizationRecommendationsAsync(int driveTestId);
        Task<List<AnomalyDto>> DetectAnomaliesAsync(int driveTestId);
        Task<List<AnomalyDto>> DetectHtsAnomaliesAsync(string phoneNumber, DateTime startDate, DateTime endDate);
        Task<string> GenerateNaturalLanguageSummaryAsync(NarrowedBaseAnalysisResultDto result);
    }

    public class AiAnalysisEngine : IAiAnalysisEngine
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDriveTestAnalysisEngine _driveTestEngine;

        public AiAnalysisEngine(IUnitOfWork unitOfWork, IDriveTestAnalysisEngine driveTestEngine)
        {
            _unitOfWork = unitOfWork;
            _driveTestEngine = driveTestEngine;
        }

        public async Task<List<RfOptimizationRecommendationDto>> GenerateRfOptimizationRecommendationsAsync(
            int driveTestId)
        {
            Logger.Info($"RF optimizasyon önerileri oluşturuluyor: DriveTestId={driveTestId}");

            var analysis = await _driveTestEngine.AnalyzeAsync(driveTestId);
            if (analysis == null) return new List<RfOptimizationRecommendationDto>();

            var recommendations = new List<RfOptimizationRecommendationDto>();
            int priority = 1;

            // Overshooting recommendations
            foreach (var overshoot in analysis.OvershootingCells)
            {
                var cell = await _unitOfWork.Cells.GetCellWithSectorAndSiteAsync(overshoot.CellId);
                if (cell?.Sector == null) continue;

                double currentTilt = cell.Sector.MechanicalTilt + cell.Sector.ElectricalTilt;
                double recommendedTiltIncrease = Math.Min(10, (overshoot.OvershootRatio - 1) * 3);

                recommendations.Add(new RfOptimizationRecommendationDto
                {
                    SiteCode = overshoot.SiteCode,
                    SectorName = cell.Sector.SectorName,
                    CellId = overshoot.CellId,
                    ProblemType = AnomalyType.Overshooting,
                    ProblemDescription = $"Overshooting tespit edildi. Maksimum mesafe: {overshoot.MaxDetectedDistanceKm:F1} km, Beklenen: {overshoot.ExpectedMaxDistanceKm:F1} km",
                    Recommendation = $"Elektriksel tilt {recommendedTiltIncrease:F1}° artırılması önerilir.",
                    ParameterName = "Electrical Tilt",
                    CurrentValue = cell.Sector.ElectricalTilt,
                    RecommendedValue = cell.Sector.ElectricalTilt + recommendedTiltIncrease,
                    ExpectedImprovementPercent = Math.Min(40, recommendedTiltIncrease * 4),
                    Probability = 0.85,
                    Risk = "Düşük - Komşu hücre kapsamasını etkileyebilir",
                    Priority = priority++
                });
            }

            // PCI conflict recommendations
            foreach (var conflict in analysis.PciConflicts)
            {
                recommendations.Add(new RfOptimizationRecommendationDto
                {
                    ProblemType = AnomalyType.PCIConflict,
                    ProblemDescription = $"PCI {conflict.PCI} çakışması: {conflict.ConflictingCells.Count} hücre etkileniyor",
                    Recommendation = $"PCI {conflict.PCI} için yeniden planlama yapılmalıdır. Önerilen yeni PCI: {GenerateAlternativePci(conflict.PCI)}",
                    ParameterName = "PCI",
                    CurrentValue = conflict.PCI,
                    RecommendedValue = GenerateAlternativePci(conflict.PCI),
                    ExpectedImprovementPercent = 25,
                    Probability = 0.90,
                    Risk = conflict.Severity == "Kritik" ? "Yüksek - Handover başarısızlıklarına neden olabilir" : "Orta",
                    Priority = priority++
                });
            }

            // Coverage hole recommendations
            foreach (var hole in analysis.CoverageHoles.Take(5))
            {
                recommendations.Add(new RfOptimizationRecommendationDto
                {
                    ProblemType = AnomalyType.CoverageHole,
                    ProblemDescription = $"Kapsama deliği: {hole.RadiusKm:F1} km yarıçap, Ortalama RSRP: {hole.AvgRSRP:F1} dBm",
                    Recommendation = $"Koordinat ({hole.CenterLatitude:F4}, {hole.CenterLongitude:F4}) yakınına yeni baz istasyonu kurulumu veya mevcut baz parametrelerinin optimizasyonu önerilir.",
                    ParameterName = "Coverage",
                    CurrentValue = hole.AvgRSRP,
                    RecommendedValue = AppConstants.RsrpGood,
                    ExpectedImprovementPercent = 30,
                    Probability = 0.75,
                    Risk = "Düşük",
                    Priority = priority++
                });
            }

            // Missing neighbor recommendations
            foreach (var missing in analysis.MissingNeighbors.Take(5))
            {
                recommendations.Add(new RfOptimizationRecommendationDto
                {
                    CellId = missing.ServingCellId,
                    ProblemType = AnomalyType.MissingNeighbor,
                    ProblemDescription = $"Eksik komşu: {missing.ServingCellId} → {missing.PotentialNeighborCellId}",
                    Recommendation = $"{missing.PotentialNeighborCellId} hücresi {missing.ServingCellId} hücresinin komşu listesine eklenmeli.",
                    ParameterName = "Neighbor List",
                    CurrentValue = 0,
                    RecommendedValue = 1,
                    ExpectedImprovementPercent = 15,
                    Probability = 0.80,
                    Risk = "Çok Düşük",
                    Priority = priority++
                });
            }

            Logger.Info($"RF optimizasyon önerileri oluşturuldu: {recommendations.Count} öneri");
            return recommendations.OrderBy(r => r.Priority).ToList();
        }

        public async Task<List<AnomalyDto>> DetectAnomaliesAsync(int driveTestId)
        {
            var analysis = await _driveTestEngine.AnalyzeAsync(driveTestId);
            return analysis?.Anomalies ?? new List<AnomalyDto>();
        }

        public async Task<List<AnomalyDto>> DetectHtsAnomaliesAsync(
            string phoneNumber, DateTime startDate, DateTime endDate)
        {
            var anomalies = new List<AnomalyDto>();

            var query = new HtsQueryDto
            {
                PhoneNumber = phoneNumber,
                StartDate = startDate,
                EndDate = endDate,
                PageSize = 100000
            };

            var result = await _unitOfWork.HtsRecords.QueryAsync(query);
            var records = result.Records;

            if (!records.Any()) return anomalies;

            // Detect abnormal movement patterns
            var sortedRecords = records.OrderBy(r => r.CallDateTime).ToList();

            for (int i = 1; i < sortedRecords.Count; i++)
            {
                var prev = sortedRecords[i - 1];
                var curr = sortedRecords[i];

                if (prev.Latitude.HasValue && prev.Longitude.HasValue &&
                    curr.Latitude.HasValue && curr.Longitude.HasValue)
                {
                    var timeDelta = curr.CallDateTime - prev.CallDateTime;
                    var distance = GeoHelper.CalculateDistanceKm(
                        prev.Latitude.Value, prev.Longitude.Value,
                        curr.Latitude.Value, curr.Longitude.Value);

                    if (timeDelta.TotalHours > 0)
                    {
                        double speed = distance / timeDelta.TotalHours;
                        if (speed > 300) // Physically impossible speed
                        {
                            anomalies.Add(new AnomalyDto
                            {
                                Type = AnomalyType.AbnormalMovement,
                                Description = $"Olağandışı hareket: {speed:F0} km/h hız tespit edildi",
                                Latitude = curr.Latitude.Value,
                                Longitude = curr.Longitude.Value,
                                Severity = Math.Min(100, speed / 5),
                                Probability = 0.90,
                                Recommendation = "Manuel inceleme gereklidir."
                            });
                        }
                    }
                }
            }

            // Detect unusual call patterns
            var callsPerHour = records
                .GroupBy(r => new { r.CallDateTime.Date, r.CallDateTime.Hour })
                .Select(g => g.Count())
                .ToList();

            if (callsPerHour.Any())
            {
                double avgCallsPerHour = callsPerHour.Average();
                double maxCallsPerHour = callsPerHour.Max();

                if (maxCallsPerHour > avgCallsPerHour * 5)
                {
                    anomalies.Add(new AnomalyDto
                    {
                        Type = AnomalyType.AbnormalMovement,
                        Description = $"Olağandışı arama yoğunluğu: Saatte {maxCallsPerHour} arama (ortalama: {avgCallsPerHour:F1})",
                        Severity = 60,
                        Probability = 0.70,
                        Recommendation = "Arama örüntüsü incelenmelidir."
                    });
                }
            }

            return anomalies;
        }

        public async Task<string> GenerateNaturalLanguageSummaryAsync(NarrowedBaseAnalysisResultDto result)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== YAPAY ZEKA ANALİZ RAPORU ===");
            sb.AppendLine();

            // Confidence assessment
            string confidenceText = result.ConfidenceLevel switch
            {
                ConfidenceLevel.High => "Yüksek güvenilirlik seviyesinde",
                ConfidenceLevel.Medium => "Orta güvenilirlik seviyesinde",
                _ => "Düşük güvenilirlik seviyesinde"
            };

            sb.AppendLine($"Bu analiz {confidenceText} gerçekleştirilmiştir (Skor: {result.ConfidenceScore}/100).");
            sb.AppendLine();

            // Movement analysis
            if (result.MovementPattern != MovementPattern.Unknown)
            {
                string patternText = result.MovementPattern switch
                {
                    MovementPattern.Stationary => "Analiz döneminde hedef kişi büyük ölçüde sabit bir konumda kalmıştır.",
                    MovementPattern.SlowMoving => $"Hedef kişi yavaş hareket etmiş, ortalama hız {result.AverageSpeedKmh:F1} km/h olarak hesaplanmıştır.",
                    MovementPattern.FastMoving => $"Hedef kişi hızlı hareket etmiş, ortalama hız {result.AverageSpeedKmh:F1} km/h olarak hesaplanmıştır.",
                    MovementPattern.Erratic => "Hedef kişinin hareketi düzensiz bir örüntü sergilemiştir. Bu durum veri kalitesi sorununa veya olağandışı bir duruma işaret edebilir.",
                    _ => "Hareket örüntüsü belirlenememiştir."
                };
                sb.AppendLine(patternText);
                sb.AppendLine();
            }

            // Location estimates
            if (result.LocationEstimates.Any())
            {
                sb.AppendLine("KONUM TAHMİNLERİ:");
                foreach (var estimate in result.LocationEstimates.Take(3))
                {
                    sb.AppendLine($"• {estimate.SiteCode}: %{estimate.Probability * 100:F0} olasılık" +
                                 (estimate.HasDriveTestConfirmation ? " (Drive Test ile doğrulandı)" : ""));
                }
                sb.AppendLine();
            }

            // Warnings
            if (result.Warnings.Any())
            {
                sb.AppendLine("UYARILAR:");
                foreach (var warning in result.Warnings)
                    sb.AppendLine($"⚠ {warning}");
                sb.AppendLine();
            }

            sb.AppendLine("NOT: Bu analiz yapay zeka destekli bir değerlendirmedir. Kesin hüküm niteliği taşımaz.");
            sb.AppendLine("Sonuçlar öneri, risk ve olasılık değerlendirmesi olarak değerlendirilmelidir.");

            return await Task.FromResult(sb.ToString());
        }

        private int GenerateAlternativePci(int currentPci)
        {
            // Simple PCI reuse distance calculation
            int alternative = (currentPci + 168) % 504; // PCI reuse distance
            return alternative;
        }
    }
}
