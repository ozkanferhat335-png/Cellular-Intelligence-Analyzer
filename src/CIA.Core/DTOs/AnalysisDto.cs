using System;
using System.Collections.Generic;
using CIA.Core.Enums;

namespace CIA.Core.DTOs
{
    public class NarrowedBaseAnalysisRequestDto
    {
        public string PhoneNumber { get; set; }
        public string IMEI { get; set; }
        public string IMSI { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool UsedriveTestData { get; set; } = true;
        public bool UseCoverageModels { get; set; } = true;
        public int MinHtsRecords { get; set; } = 5;
        public int MinAnalysisMinutes { get; set; } = 30;
    }

    public class NarrowedBaseAnalysisResultDto
    {
        public string AnalysisId { get; set; }
        public string PhoneNumber { get; set; }
        public string IMEI { get; set; }
        public DateTime AnalysisDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalHtsRecords { get; set; }
        public int ConfidenceScore { get; set; }
        public ConfidenceLevel ConfidenceLevel { get; set; }
        public string ConfidenceLevelText => ConfidenceLevel.ToString();
        public List<LocationEstimateDto> LocationEstimates { get; set; } = new List<LocationEstimateDto>();
        public List<HtsMovementPointDto> MovementHistory { get; set; } = new List<HtsMovementPointDto>();
        public List<HtsTransitionDto> Transitions { get; set; } = new List<HtsTransitionDto>();
        public MovementPattern MovementPattern { get; set; }
        public double TotalDistanceKm { get; set; }
        public double AverageSpeedKmh { get; set; }
        public List<ScoringDetailDto> ScoringDetails { get; set; } = new List<ScoringDetailDto>();
        public string Summary { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan AnalysisDuration { get; set; }
    }

    public class LocationEstimateDto
    {
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public double Probability { get; set; }
        public DateTime EstimatedTime { get; set; }
        public string BasedOnCellId { get; set; }
        public string SiteCode { get; set; }
        public double Azimuth { get; set; }
        public List<GeoPointDto> PolygonPoints { get; set; } = new List<GeoPointDto>();
        public bool HasDriveTestConfirmation { get; set; }
        public double DriveTestConfidenceBoost { get; set; }
    }

    public class ScoringDetailDto
    {
        public string Parameter { get; set; }
        public double Weight { get; set; }
        public double RawScore { get; set; }
        public double WeightedScore { get; set; }
        public string Description { get; set; }
    }

    public class CoverageModelDto
    {
        public int Id { get; set; }
        public int SectorId { get; set; }
        public string SiteCode { get; set; }
        public string SectorName { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double Azimuth { get; set; }
        public double BeamWidthDegrees { get; set; }
        public double EstimatedRadiusKm { get; set; }
        public double AntennaHeightM { get; set; }
        public double TxPowerDbm { get; set; }
        public double FrequencyMhz { get; set; }
        public double MechanicalTilt { get; set; }
        public double ElectricalTilt { get; set; }
        public TerrainType TerrainType { get; set; }
        public List<GeoPointDto> CoveragePolygon { get; set; } = new List<GeoPointDto>();
        public double EstimatedRsrpAtEdge { get; set; }
        public DateTime ModeledAt { get; set; }
        public bool IsValidatedByDriveTest { get; set; }
        public double ValidationAccuracy { get; set; }
    }

    public class GeoPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }

        public GeoPointDto() { }

        public GeoPointDto(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public class RfOptimizationRecommendationDto
    {
        public string SiteCode { get; set; }
        public string SectorName { get; set; }
        public string CellId { get; set; }
        public AnomalyType ProblemType { get; set; }
        public string ProblemDescription { get; set; }
        public string Recommendation { get; set; }
        public double CurrentValue { get; set; }
        public double RecommendedValue { get; set; }
        public string ParameterName { get; set; }
        public double ExpectedImprovementPercent { get; set; }
        public double Probability { get; set; }
        public string Risk { get; set; }
        public int Priority { get; set; }
    }

    public class AnalysisResultDto
    {
        public int Id { get; set; }
        public AnalysisType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; }
        public string ResultJson { get; set; }
        public int ConfidenceScore { get; set; }
        public bool IsArchived { get; set; }
    }
}
