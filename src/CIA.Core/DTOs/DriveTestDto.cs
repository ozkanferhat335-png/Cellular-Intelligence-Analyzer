using System;
using System.Collections.Generic;
using CIA.Core.Enums;

namespace CIA.Core.DTOs
{
    public class DriveTestRecordDto
    {
        public long Id { get; set; }
        public int DriveTestId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? RSRP { get; set; }
        public double? RSRQ { get; set; }
        public double? SINR { get; set; }
        public double? RSSI { get; set; }
        public int? PCI { get; set; }
        public int? EARFCN { get; set; }
        public double? SpeedKmh { get; set; }
        public string ServingCellId { get; set; }
        public string CGI { get; set; }
        public TechnologyType? Technology { get; set; }
        public SignalQuality SignalQuality { get; set; }
        public bool IsAnomalous { get; set; }
        public string AnomalyDescription { get; set; }
    }

    public class DriveTestDto
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string Description { get; set; }
        public DateTime TestDate { get; set; }
        public string Operator { get; set; }
        public string Region { get; set; }
        public string Vehicle { get; set; }
        public string Engineer { get; set; }
        public int TotalRecords { get; set; }
        public double TotalDistanceKm { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? MinLatitude { get; set; }
        public double? MaxLatitude { get; set; }
        public double? MinLongitude { get; set; }
        public double? MaxLongitude { get; set; }
        public double? AvgRSRP { get; set; }
        public double? AvgRSRQ { get; set; }
        public double? AvgSINR { get; set; }
        public int ImportedFileId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DriveTestAnalysisDto
    {
        public int DriveTestId { get; set; }
        public string TestName { get; set; }
        public DateTime AnalysisDate { get; set; }
        public List<CoverageHoleDto> CoverageHoles { get; set; } = new List<CoverageHoleDto>();
        public List<OvershootingDto> OvershootingCells { get; set; } = new List<OvershootingDto>();
        public List<PciConflictDto> PciConflicts { get; set; } = new List<PciConflictDto>();
        public List<MissingNeighborDto> MissingNeighbors { get; set; } = new List<MissingNeighborDto>();
        public DriveTestStatisticsDto Statistics { get; set; }
        public List<AnomalyDto> Anomalies { get; set; } = new List<AnomalyDto>();
    }

    public class DriveTestStatisticsDto
    {
        public int TotalPoints { get; set; }
        public double AvgRSRP { get; set; }
        public double MinRSRP { get; set; }
        public double MaxRSRP { get; set; }
        public double AvgRSRQ { get; set; }
        public double AvgSINR { get; set; }
        public double ExcellentCoveragePercent { get; set; }
        public double GoodCoveragePercent { get; set; }
        public double FairCoveragePercent { get; set; }
        public double PoorCoveragePercent { get; set; }
        public double NoCoveragePercent { get; set; }
        public int UniquePCIs { get; set; }
        public int UniqueCells { get; set; }
        public double TotalDistanceKm { get; set; }
    }

    public class CoverageHoleDto
    {
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public double AvgRSRP { get; set; }
        public int PointCount { get; set; }
        public double SeverityScore { get; set; }
    }

    public class OvershootingDto
    {
        public string CellId { get; set; }
        public string SiteCode { get; set; }
        public double SiteLatitude { get; set; }
        public double SiteLongitude { get; set; }
        public double MaxDetectedDistanceKm { get; set; }
        public double ExpectedMaxDistanceKm { get; set; }
        public double OvershootRatio { get; set; }
        public int AffectedPoints { get; set; }
    }

    public class PciConflictDto
    {
        public int PCI { get; set; }
        public List<string> ConflictingCells { get; set; } = new List<string>();
        public double MinDistanceBetweenCellsKm { get; set; }
        public string Severity { get; set; }
    }

    public class MissingNeighborDto
    {
        public string ServingCellId { get; set; }
        public string PotentialNeighborCellId { get; set; }
        public double DistanceKm { get; set; }
        public double AvgRSRP { get; set; }
        public int DetectionCount { get; set; }
    }

    public class AnomalyDto
    {
        public AnomalyType Type { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Severity { get; set; }
        public string AffectedCell { get; set; }
        public string Recommendation { get; set; }
        public double Probability { get; set; }
    }

    public class DriveTestQueryDto
    {
        public int? DriveTestId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? MinLatitude { get; set; }
        public double? MaxLatitude { get; set; }
        public double? MinLongitude { get; set; }
        public double? MaxLongitude { get; set; }
        public double? MaxRSRP { get; set; }
        public double? MinRSRP { get; set; }
        public int? PCI { get; set; }
        public string CellId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10000;
    }
}
