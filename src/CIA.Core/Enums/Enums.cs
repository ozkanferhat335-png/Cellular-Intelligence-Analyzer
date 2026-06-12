using System;

namespace CIA.Core.Enums
{
    public enum UserRole
    {
        Admin = 1,
        Analyst = 2,
        Viewer = 3,
        Operator = 4
    }

    public enum SiteStatus
    {
        Active = 1,
        Inactive = 2,
        UnderMaintenance = 3,
        Planned = 4,
        Decommissioned = 5
    }

    public enum TechnologyType
    {
        GSM = 1,
        UMTS = 2,
        LTE = 3,
        NR5G = 4
    }

    public enum BandType
    {
        Band700 = 700,
        Band800 = 800,
        Band900 = 900,
        Band1800 = 1800,
        Band2100 = 2100,
        Band2600 = 2600,
        Band3500 = 3500
    }

    public enum ConfidenceLevel
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum AnalysisType
    {
        HTSAnalysis = 1,
        DriveTestAnalysis = 2,
        CoverageAnalysis = 3,
        NarrowedBaseAnalysis = 4,
        RFOptimization = 5,
        AIAnalysis = 6
    }

    public enum ImportStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        PartiallyCompleted = 5
    }

    public enum ReportType
    {
        HTSReport = 1,
        DriveTestReport = 2,
        CoverageReport = 3,
        NarrowedBaseReport = 4,
        ExecutiveSummary = 5
    }

    public enum ReportFormat
    {
        PDF = 1,
        Excel = 2,
        CSV = 3,
        Word = 4
    }

    public enum MapLayerType
    {
        BaseStations = 1,
        Sectors = 2,
        HTSRecords = 3,
        DriveTestPoints = 4,
        CoverageHeatmap = 5,
        EstimatedCoverage = 6,
        AnalysisResults = 7
    }

    public enum SignalQuality
    {
        Excellent = 1,
        Good = 2,
        Fair = 3,
        Poor = 4,
        NoSignal = 5
    }

    public enum AnomalyType
    {
        CoverageHole = 1,
        Overshooting = 2,
        PCIConflict = 3,
        MissingNeighbor = 4,
        TiltError = 5,
        CapacityProblem = 6,
        AbnormalMovement = 7
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    public enum TerrainType
    {
        Urban = 1,
        SubUrban = 2,
        Rural = 3,
        OpenArea = 4,
        DenseUrban = 5
    }

    public enum MovementPattern
    {
        Stationary = 1,
        SlowMoving = 2,
        FastMoving = 3,
        Erratic = 4,
        Unknown = 5
    }
}
