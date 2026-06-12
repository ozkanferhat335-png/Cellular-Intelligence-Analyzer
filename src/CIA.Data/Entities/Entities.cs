using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CIA.Core.Enums;

namespace CIA.Data.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Username { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        [MaxLength(200)]
        public string Email { get; set; }
        [MaxLength(200)]
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    [Table("Roles")]
    public class Role
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    [Table("UserRoles")]
    public class UserRole
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }
    }

    [Table("Sites")]
    public class Site
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string SiteCode { get; set; }
        [MaxLength(200)]
        public string SiteName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [MaxLength(100)]
        public string Region { get; set; }
        [MaxLength(100)]
        public string City { get; set; }
        [MaxLength(100)]
        public string District { get; set; }
        public int Status { get; set; } = (int)SiteStatus.Active;
        public double? AntennaHeightM { get; set; }
        [MaxLength(100)]
        public string Operator { get; set; }
        [MaxLength(500)]
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual ICollection<Sector> Sectors { get; set; } = new List<Sector>();
    }

    [Table("Sectors")]
    public class Sector
    {
        [Key]
        public int Id { get; set; }
        public int SiteId { get; set; }
        [Required, MaxLength(100)]
        public string SectorName { get; set; }
        public int SectorIndex { get; set; }
        public double Azimuth { get; set; }
        public double MechanicalTilt { get; set; }
        public double ElectricalTilt { get; set; }
        public double BeamWidthDegrees { get; set; } = 65.0;
        public int Technology { get; set; }
        public int Band { get; set; }
        public double FrequencyMhz { get; set; }
        public double TxPowerDbm { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("SiteId")]
        public virtual Site Site { get; set; }
        public virtual ICollection<Cell> Cells { get; set; } = new List<Cell>();
    }

    [Table("Cells")]
    public class Cell
    {
        [Key]
        public int Id { get; set; }
        public int SectorId { get; set; }
        [Required, MaxLength(100)]
        public string CellId { get; set; }
        [MaxLength(50)]
        public string CGI { get; set; }
        public int? PCI { get; set; }
        public int? TAC { get; set; }
        public int? EARFCN { get; set; }
        public long? ENBId { get; set; }
        public int? LocalCellId { get; set; }
        public int Technology { get; set; }
        public int Band { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("SectorId")]
        public virtual Sector Sector { get; set; }
    }

    [Table("DriveTests")]
    public class DriveTest
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string TestName { get; set; }
        [MaxLength(1000)]
        public string Description { get; set; }
        public DateTime TestDate { get; set; }
        [MaxLength(100)]
        public string Operator { get; set; }
        [MaxLength(100)]
        public string Region { get; set; }
        [MaxLength(100)]
        public string Vehicle { get; set; }
        [MaxLength(200)]
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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<DriveTestRecord> Records { get; set; } = new List<DriveTestRecord>();
    }

    [Table("DriveTestRecords")]
    public class DriveTestRecord
    {
        [Key]
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
        [MaxLength(100)]
        public string ServingCellId { get; set; }
        [MaxLength(50)]
        public string CGI { get; set; }
        public int? Technology { get; set; }
        [ForeignKey("DriveTestId")]
        public virtual DriveTest DriveTest { get; set; }
    }

    [Table("HTSRecords")]
    public class HtsRecord
    {
        [Key]
        public long Id { get; set; }
        [MaxLength(20)]
        public string PhoneNumber { get; set; }
        [MaxLength(20)]
        public string IMEI { get; set; }
        [MaxLength(20)]
        public string IMSI { get; set; }
        [MaxLength(100)]
        public string CellId { get; set; }
        [MaxLength(50)]
        public string CGI { get; set; }
        [MaxLength(20)]
        public string LAC { get; set; }
        [MaxLength(10)]
        public string MCC { get; set; }
        [MaxLength(10)]
        public string MNC { get; set; }
        public DateTime CallDateTime { get; set; }
        public int DurationSeconds { get; set; }
        [MaxLength(50)]
        public string CallType { get; set; }
        [MaxLength(20)]
        public string CalledNumber { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int ImportedFileId { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("ImportedFiles")]
    public class ImportedFile
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(500)]
        public string FileName { get; set; }
        [MaxLength(1000)]
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        [MaxLength(50)]
        public string FileType { get; set; }
        public int Status { get; set; } = (int)ImportStatus.Pending;
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int FailedRows { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public int ImportedByUserId { get; set; }
        [MaxLength(2000)]
        public string ErrorMessage { get; set; }
        public long? ImportDurationMs { get; set; }
    }

    [Table("AnalysisResults")]
    public class AnalysisResult
    {
        [Key]
        public int Id { get; set; }
        public int AnalysisType { get; set; }
        [Required, MaxLength(500)]
        public string Title { get; set; }
        [MaxLength(2000)]
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedByUserId { get; set; }
        public string ResultJson { get; set; }
        public int ConfidenceScore { get; set; }
        public bool IsArchived { get; set; } = false;
    }

    [Table("CoverageModels")]
    public class CoverageModel
    {
        [Key]
        public int Id { get; set; }
        public int SectorId { get; set; }
        public double EstimatedRadiusKm { get; set; }
        public string CoveragePolygonJson { get; set; }
        public double EstimatedRsrpAtEdge { get; set; }
        public int TerrainType { get; set; }
        public DateTime ModeledAt { get; set; } = DateTime.UtcNow;
        public bool IsValidatedByDriveTest { get; set; } = false;
        public double ValidationAccuracy { get; set; }
        [ForeignKey("SectorId")]
        public virtual Sector Sector { get; set; }
    }

    [Table("Reports")]
    public class Report
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(500)]
        public string Title { get; set; }
        [MaxLength(2000)]
        public string Description { get; set; }
        public int ReportType { get; set; }
        public int ReportFormat { get; set; }
        [MaxLength(1000)]
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedByUserId { get; set; }
        public string Parameters { get; set; }
        public bool IsGenerated { get; set; } = false;
        [MaxLength(2000)]
        public string ErrorMessage { get; set; }
    }

    [Table("SystemLogs")]
    public class SystemLog
    {
        [Key]
        public long Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        [MaxLength(20)]
        public string Level { get; set; }
        [MaxLength(200)]
        public string Module { get; set; }
        [MaxLength(4000)]
        public string Message { get; set; }
        public string Exception { get; set; }
        public int? UserId { get; set; }
        [MaxLength(100)]
        public string MachineName { get; set; }
        [MaxLength(50)]
        public string ThreadId { get; set; }
    }

    [Table("Settings")]
    public class Setting
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Key { get; set; }
        public string Value { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }
        [MaxLength(100)]
        public string Category { get; set; }
        public bool IsEncrypted { get; set; } = false;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
