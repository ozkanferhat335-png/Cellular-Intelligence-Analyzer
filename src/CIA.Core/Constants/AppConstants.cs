using System;

namespace CIA.Core.Constants
{
    public static class AppConstants
    {
        // Application Info
        public const string AppName = "Cellular Intelligence Analyzer";
        public const string AppVersion = "1.0.0";
        public const string AppAuthor = "CIA Platform";

        // Database
        public const string DatabaseFileName = "CIA_Database.db";
        public const string DatabaseVersion = "1.0";
        public const int DatabaseCommandTimeout = 300;
        public const int BulkInsertBatchSize = 5000;

        // Performance
        public const int MaxMapPoints = 100000;
        public const int ClusteringThreshold = 500;
        public const int QueryTimeoutSeconds = 30;
        public const int MaxHtsRecordsPerQuery = 1000000;
        public const int MinHtsRecordsForAnalysis = 5;
        public const int MinMinutesForAnalysis = 30;

        // RF Parameters
        public const double EarthRadiusKm = 6371.0;
        public const double DefaultAntennaHeightM = 30.0;
        public const double DefaultAntennaPowerDbm = 43.0;
        public const double DefaultFrequencyMhz = 1800.0;
        public const double MaxCoverageRadiusKm = 35.0;
        public const double MinCoverageRadiusKm = 0.1;

        // RSRP Thresholds (dBm)
        public const double RsrpExcellent = -80.0;
        public const double RsrpGood = -90.0;
        public const double RsrpFair = -100.0;
        public const double RsrpPoor = -110.0;

        // RSRQ Thresholds (dB)
        public const double RsrqExcellent = -10.0;
        public const double RsrqGood = -15.0;
        public const double RsrqFair = -20.0;

        // SINR Thresholds (dB)
        public const double SinrExcellent = 20.0;
        public const double SinrGood = 13.0;
        public const double SinrFair = 0.0;

        // Confidence Score Thresholds
        public const int ConfidenceScoreLow = 40;
        public const int ConfidenceScoreMedium = 70;
        public const int ConfidenceScoreHigh = 100;

        // Scoring Weights
        public const double WeightCellMatch = 0.30;
        public const double WeightSectorMatch = 0.20;
        public const double WeightMeasurementMatch = 0.20;
        public const double WeightTimeMatch = 0.15;
        public const double WeightDistanceFit = 0.10;
        public const double WeightMovementConsistency = 0.05;

        // Map
        public const double DefaultMapLatitude = 39.9334;
        public const double DefaultMapLongitude = 32.8597;
        public const int DefaultMapZoom = 7;
        public const int MaxMapZoom = 20;
        public const int MinMapZoom = 2;

        // File Import
        public const int CsvMaxFileSizeMb = 2048;
        public const string CsvDelimiter = ";";
        public const string CsvDateFormat = "yyyy-MM-dd HH:mm:ss";

        // Security
        public const int PasswordMinLength = 8;
        public const int SessionTimeoutMinutes = 480;
        public const int MaxLoginAttempts = 5;
        public const int LockoutDurationMinutes = 30;

        // Logging
        public const string LogDirectory = "Logs";
        public const string LogFilePattern = "CIA_{0:yyyy-MM-dd}.log";
        public const int LogRetentionDays = 90;

        // Reports
        public const string ReportDirectory = "Reports";
        public const string TempDirectory = "Temp";

        // Sector Beam Width
        public const double DefaultBeamWidthDegrees = 65.0;
        public const double MaxBeamWidthDegrees = 120.0;

        // Overshooting Detection
        public const double OvershootingDistanceMultiplier = 2.5;

        // PCI
        public const int PciMin = 0;
        public const int PciMax = 503;
        public const int PciConflictDistanceKm = 5;
    }

    public static class DatabaseTables
    {
        public const string Users = "Users";
        public const string Roles = "Roles";
        public const string UserRoles = "UserRoles";
        public const string Sites = "Sites";
        public const string Sectors = "Sectors";
        public const string Cells = "Cells";
        public const string DriveTests = "DriveTests";
        public const string HtsRecords = "HTSRecords";
        public const string ImportedFiles = "ImportedFiles";
        public const string AnalysisResults = "AnalysisResults";
        public const string CoverageModels = "CoverageModels";
        public const string Reports = "Reports";
        public const string SystemLogs = "SystemLogs";
        public const string Settings = "Settings";
    }

    public static class SettingKeys
    {
        public const string MapProvider = "MapProvider";
        public const string DefaultLatitude = "DefaultLatitude";
        public const string DefaultLongitude = "DefaultLongitude";
        public const string DefaultZoom = "DefaultZoom";
        public const string MaxQueryResults = "MaxQueryResults";
        public const string EnableAiAnalysis = "EnableAiAnalysis";
        public const string ReportOutputPath = "ReportOutputPath";
        public const string ImportBatchSize = "ImportBatchSize";
        public const string LogLevel = "LogLevel";
        public const string Theme = "Theme";
        public const string Language = "Language";
    }
}
