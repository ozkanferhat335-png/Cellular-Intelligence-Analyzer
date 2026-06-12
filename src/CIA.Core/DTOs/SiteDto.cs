using System;
using System.Collections.Generic;
using CIA.Core.Enums;

namespace CIA.Core.DTOs
{
    public class SiteDto
    {
        public int Id { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public SiteStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public double? AntennaHeightM { get; set; }
        public string Operator { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SectorDto> Sectors { get; set; } = new List<SectorDto>();
        public int SectorCount { get; set; }
    }

    public class SectorDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string SiteCode { get; set; }
        public string SectorName { get; set; }
        public int SectorIndex { get; set; }
        public double Azimuth { get; set; }
        public double MechanicalTilt { get; set; }
        public double ElectricalTilt { get; set; }
        public double TotalTilt => MechanicalTilt + ElectricalTilt;
        public double BeamWidthDegrees { get; set; }
        public TechnologyType Technology { get; set; }
        public BandType Band { get; set; }
        public double FrequencyMhz { get; set; }
        public double TxPowerDbm { get; set; }
        public bool IsActive { get; set; }
        public List<CellDto> Cells { get; set; } = new List<CellDto>();
    }

    public class CellDto
    {
        public int Id { get; set; }
        public int SectorId { get; set; }
        public string CellId { get; set; }
        public string CGI { get; set; }
        public int? PCI { get; set; }
        public int? TAC { get; set; }
        public int? EARFCN { get; set; }
        public long? ENBId { get; set; }
        public int? LocalCellId { get; set; }
        public TechnologyType Technology { get; set; }
        public BandType Band { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public double SiteLatitude { get; set; }
        public double SiteLongitude { get; set; }
        public double Azimuth { get; set; }
        public string SiteCode { get; set; }
        public string SectorName { get; set; }
    }

    public class SiteFilterDto
    {
        public string SearchText { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public SiteStatus? Status { get; set; }
        public TechnologyType? Technology { get; set; }
        public BandType? Band { get; set; }
        public double? MinLatitude { get; set; }
        public double? MaxLatitude { get; set; }
        public double? MinLongitude { get; set; }
        public double? MaxLongitude { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }
}
