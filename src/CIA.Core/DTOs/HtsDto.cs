using System;
using System.Collections.Generic;
using CIA.Core.Enums;

namespace CIA.Core.DTOs
{
    public class HtsRecordDto
    {
        public long Id { get; set; }
        public string PhoneNumber { get; set; }
        public string IMEI { get; set; }
        public string IMSI { get; set; }
        public string CellId { get; set; }
        public string CGI { get; set; }
        public string LAC { get; set; }
        public string MCC { get; set; }
        public string MNC { get; set; }
        public DateTime CallDateTime { get; set; }
        public int DurationSeconds { get; set; }
        public string CallType { get; set; }
        public string CalledNumber { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int ImportedFileId { get; set; }
        public DateTime ImportedAt { get; set; }
        public SiteDto MatchedSite { get; set; }
        public SectorDto MatchedSector { get; set; }
    }

    public class HtsQueryDto
    {
        public string PhoneNumber { get; set; }
        public string IMEI { get; set; }
        public string IMSI { get; set; }
        public string CellId { get; set; }
        public string CGI { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 1000;
        public bool IncludeSiteInfo { get; set; } = true;
    }

    public class HtsQueryResultDto
    {
        public List<HtsRecordDto> Records { get; set; } = new List<HtsRecordDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public TimeSpan QueryDuration { get; set; }
    }

    public class HtsMovementDto
    {
        public string PhoneNumber { get; set; }
        public string IMEI { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;
        public List<HtsMovementPointDto> MovementPoints { get; set; } = new List<HtsMovementPointDto>();
        public List<HtsTransitionDto> Transitions { get; set; } = new List<HtsTransitionDto>();
        public MovementPattern Pattern { get; set; }
        public double TotalDistanceKm { get; set; }
        public double AverageSpeedKmh { get; set; }
        public int UniqueBasesCount { get; set; }
    }

    public class HtsMovementPointDto
    {
        public DateTime Timestamp { get; set; }
        public string CellId { get; set; }
        public string CGI { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string SiteCode { get; set; }
        public string SectorName { get; set; }
        public double? Azimuth { get; set; }
        public int SequenceNumber { get; set; }
    }

    public class HtsTransitionDto
    {
        public HtsMovementPointDto From { get; set; }
        public HtsMovementPointDto To { get; set; }
        public TimeSpan TimeDelta { get; set; }
        public double? DistanceKm { get; set; }
        public double? EstimatedSpeedKmh { get; set; }
        public bool IsSuspicious { get; set; }
        public string SuspicionReason { get; set; }
    }

    public class HtsSubscriberRelationDto
    {
        public string PhoneNumber { get; set; }
        public List<string> ContactedNumbers { get; set; } = new List<string>();
        public Dictionary<string, int> ContactFrequency { get; set; } = new Dictionary<string, int>();
        public List<string> CommonLocations { get; set; } = new List<string>();
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
    }

    public class HtsImportConfigDto
    {
        public string FilePath { get; set; }
        public string Delimiter { get; set; } = ";";
        public bool HasHeader { get; set; } = true;
        public Dictionary<string, string> ColumnMapping { get; set; } = new Dictionary<string, string>();
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public int BatchSize { get; set; } = 5000;
    }
}
