using System;
using System.Collections.Generic;
using CIA.Core.Enums;

namespace CIA.Core.DTOs
{
    public class ReportDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ReportType Type { get; set; }
        public ReportFormat Format { get; set; }
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; }
        public string Parameters { get; set; }
        public bool IsGenerated { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ReportRequestDto
    {
        public ReportType Type { get; set; }
        public ReportFormat Format { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string PhoneNumber { get; set; }
        public string IMEI { get; set; }
        public int? DriveTestId { get; set; }
        public int? AnalysisResultId { get; set; }
        public string OutputPath { get; set; }
        public Dictionary<string, string> AdditionalParameters { get; set; } = new Dictionary<string, string>();
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeMaps { get; set; } = true;
        public string LogoPath { get; set; }
        public string OrganizationName { get; set; }
    }

    public class ImportedFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; }
        public ImportStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int FailedRows { get; set; }
        public DateTime ImportedAt { get; set; }
        public int ImportedByUserId { get; set; }
        public string ImportedByUsername { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan? ImportDuration { get; set; }
        public double ProgressPercent => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    }

    public class SystemLogDto
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; }
        public string MachineName { get; set; }
        public string ThreadId { get; set; }
    }

    public class SettingDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalSites { get; set; }
        public int ActiveSites { get; set; }
        public int TotalSectors { get; set; }
        public int TotalCells { get; set; }
        public long TotalHtsRecords { get; set; }
        public int TotalDriveTests { get; set; }
        public long TotalDriveTestPoints { get; set; }
        public int TotalAnalyses { get; set; }
        public int TotalReports { get; set; }
        public DateTime LastHtsImport { get; set; }
        public DateTime LastDriveTestImport { get; set; }
        public List<RecentActivityDto> RecentActivities { get; set; } = new List<RecentActivityDto>();
        public List<AnomalyDto> RecentAnomalies { get; set; } = new List<AnomalyDto>();
    }

    public class RecentActivityDto
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string Username { get; set; }
        public string Icon { get; set; }
    }
}
