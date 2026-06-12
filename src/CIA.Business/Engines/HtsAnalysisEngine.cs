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
    public interface IHtsAnalysisEngine
    {
        Task<HtsMovementDto> AnalyzeMovementAsync(string phoneNumber, DateTime startDate, DateTime endDate);
        Task<HtsSubscriberRelationDto> AnalyzeSubscriberRelationsAsync(string phoneNumber, DateTime startDate, DateTime endDate);
        Task<List<HtsTransitionDto>> DetectSuspiciousTransitionsAsync(List<HtsMovementPointDto> movementPoints);
        Task<MovementPattern> ClassifyMovementPatternAsync(HtsMovementDto movement);
    }

    public class HtsAnalysisEngine : IHtsAnalysisEngine
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        // Speed thresholds in km/h
        private const double SlowMovingThreshold = 30.0;
        private const double FastMovingThreshold = 120.0;
        private const double SuspiciousSpeedThreshold = 200.0;
        private const double TeleportationThreshold = 500.0;

        public HtsAnalysisEngine(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<HtsMovementDto> AnalyzeMovementAsync(
            string phoneNumber, DateTime startDate, DateTime endDate)
        {
            Logger.Info($"HTS hareket analizi başlatılıyor: {phoneNumber}, {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");

            var records = (await _unitOfWork.HtsRecords.GetMovementHistoryAsync(
                phoneNumber, startDate, endDate)).ToList();

            if (!records.Any())
            {
                Logger.Warn($"HTS kaydı bulunamadı: {phoneNumber}");
                return null;
            }

            var movementPoints = new List<HtsMovementPointDto>();
            int sequence = 0;

            foreach (var record in records)
            {
                var cell = await _unitOfWork.Cells.GetCellWithSectorAndSiteAsync(record.CellId ?? record.CGI);

                var point = new HtsMovementPointDto
                {
                    Timestamp = record.CallDateTime,
                    CellId = record.CellId,
                    CGI = record.CGI,
                    Latitude = record.Latitude ?? cell?.Sector?.Site?.Latitude,
                    Longitude = record.Longitude ?? cell?.Sector?.Site?.Longitude,
                    SiteCode = cell?.Sector?.Site?.SiteCode,
                    SectorName = cell?.Sector?.SectorName,
                    Azimuth = cell?.Sector?.Azimuth ?? 0,
                    SequenceNumber = ++sequence
                };

                movementPoints.Add(point);
            }

            var transitions = await DetectSuspiciousTransitionsAsync(movementPoints);
            var movement = BuildMovementDto(phoneNumber, movementPoints, transitions);
            movement.Pattern = await ClassifyMovementPatternAsync(movement);

            Logger.Info($"HTS hareket analizi tamamlandı: {records.Count} kayıt, {movement.UniqueBasesCount} benzersiz baz");
            return movement;
        }

        public async Task<HtsSubscriberRelationDto> AnalyzeSubscriberRelationsAsync(
            string phoneNumber, DateTime startDate, DateTime endDate)
        {
            var query = new HtsQueryDto
            {
                PhoneNumber = phoneNumber,
                StartDate = startDate,
                EndDate = endDate,
                PageSize = 100000
            };

            var result = await _unitOfWork.HtsRecords.QueryAsync(query);
            var records = result.Records;

            var contactFrequency = new Dictionary<string, int>();
            foreach (var record in records.Where(r => !string.IsNullOrEmpty(r.CalledNumber)))
            {
                var number = r.CalledNumber;
                if (contactFrequency.ContainsKey(number))
                    contactFrequency[number]++;
                else
                    contactFrequency[number] = 1;
            }

            var commonLocations = records
                .Where(r => !string.IsNullOrEmpty(r.CellId))
                .GroupBy(r => r.CellId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            return new HtsSubscriberRelationDto
            {
                PhoneNumber = phoneNumber,
                ContactedNumbers = contactFrequency.Keys.ToList(),
                ContactFrequency = contactFrequency,
                CommonLocations = commonLocations,
                AnalysisStartDate = startDate,
                AnalysisEndDate = endDate
            };
        }

        public async Task<List<HtsTransitionDto>> DetectSuspiciousTransitionsAsync(
            List<HtsMovementPointDto> movementPoints)
        {
            var transitions = new List<HtsTransitionDto>();

            for (int i = 1; i < movementPoints.Count; i++)
            {
                var from = movementPoints[i - 1];
                var to = movementPoints[i];

                var timeDelta = to.Timestamp - from.Timestamp;
                double? distanceKm = null;
                double? speedKmh = null;
                bool isSuspicious = false;
                string suspicionReason = null;

                if (from.Latitude.HasValue && from.Longitude.HasValue &&
                    to.Latitude.HasValue && to.Longitude.HasValue)
                {
                    distanceKm = GeoHelper.CalculateDistanceKm(
                        from.Latitude.Value, from.Longitude.Value,
                        to.Latitude.Value, to.Longitude.Value);

                    if (timeDelta.TotalHours > 0)
                    {
                        speedKmh = distanceKm / timeDelta.TotalHours;

                        if (speedKmh > TeleportationThreshold)
                        {
                            isSuspicious = true;
                            suspicionReason = $"Olağandışı hız: {speedKmh:F0} km/h (Teleportasyon şüphesi)";
                        }
                        else if (speedKmh > SuspiciousSpeedThreshold)
                        {
                            isSuspicious = true;
                            suspicionReason = $"Yüksek hız: {speedKmh:F0} km/h";
                        }
                    }

                    // Same cell but different location - data inconsistency
                    if (from.CellId == to.CellId && distanceKm > 5.0)
                    {
                        isSuspicious = true;
                        suspicionReason = "Aynı Cell ID, farklı konum - veri tutarsızlığı";
                    }
                }

                // Very short time between calls from different cells
                if (from.CellId != to.CellId && timeDelta.TotalSeconds < 30)
                {
                    isSuspicious = true;
                    suspicionReason = suspicionReason ?? "Çok kısa sürede baz değişimi";
                }

                transitions.Add(new HtsTransitionDto
                {
                    From = from,
                    To = to,
                    TimeDelta = timeDelta,
                    DistanceKm = distanceKm,
                    EstimatedSpeedKmh = speedKmh,
                    IsSuspicious = isSuspicious,
                    SuspicionReason = suspicionReason
                });
            }

            return await Task.FromResult(transitions);
        }

        public async Task<MovementPattern> ClassifyMovementPatternAsync(HtsMovementDto movement)
        {
            if (movement == null || !movement.MovementPoints.Any())
                return MovementPattern.Unknown;

            if (movement.UniqueBasesCount <= 1)
                return MovementPattern.Stationary;

            if (movement.AverageSpeedKmh < SlowMovingThreshold)
                return MovementPattern.SlowMoving;

            if (movement.AverageSpeedKmh > FastMovingThreshold)
                return MovementPattern.FastMoving;

            // Check for erratic movement
            var suspiciousCount = movement.Transitions.Count(t => t.IsSuspicious);
            if (suspiciousCount > movement.Transitions.Count * 0.3)
                return MovementPattern.Erratic;

            return await Task.FromResult(MovementPattern.SlowMoving);
        }

        private HtsMovementDto BuildMovementDto(
            string phoneNumber,
            List<HtsMovementPointDto> movementPoints,
            List<HtsTransitionDto> transitions)
        {
            double totalDistance = 0;
            foreach (var t in transitions.Where(t => t.DistanceKm.HasValue))
                totalDistance += t.DistanceKm.Value;

            var duration = movementPoints.Any()
                ? movementPoints.Last().Timestamp - movementPoints.First().Timestamp
                : TimeSpan.Zero;

            double avgSpeed = duration.TotalHours > 0 ? totalDistance / duration.TotalHours : 0;

            return new HtsMovementDto
            {
                PhoneNumber = phoneNumber,
                StartTime = movementPoints.FirstOrDefault()?.Timestamp ?? DateTime.MinValue,
                EndTime = movementPoints.LastOrDefault()?.Timestamp ?? DateTime.MinValue,
                MovementPoints = movementPoints,
                Transitions = transitions,
                TotalDistanceKm = totalDistance,
                AverageSpeedKmh = avgSpeed,
                UniqueBasesCount = movementPoints.Select(p => p.CellId).Distinct().Count()
            };
        }
    }
}
