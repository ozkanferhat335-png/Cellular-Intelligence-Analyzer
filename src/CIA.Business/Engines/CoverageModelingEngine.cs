using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Core.Helpers;
using CIA.Data.Entities;
using CIA.Data.Repositories;
using Newtonsoft.Json;
using NLog;

namespace CIA.Business.Engines
{
    public interface ICoverageModelingEngine
    {
        Task<CoverageModelDto> ModelSectorCoverageAsync(int sectorId, TerrainType terrainType = TerrainType.Urban);
        Task<List<CoverageModelDto>> ModelAllSectorsAsync(TerrainType terrainType = TerrainType.Urban, IProgress<int> progress = null);
        Task<CoverageModelDto> ValidateWithDriveTestAsync(int sectorId, int driveTestId);
        Task<double> EstimateRsrpAtPointAsync(int sectorId, double latitude, double longitude);
    }

    public class CoverageModelingEngine : ICoverageModelingEngine
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        public CoverageModelingEngine(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CoverageModelDto> ModelSectorCoverageAsync(
            int sectorId, TerrainType terrainType = TerrainType.Urban)
        {
            var sector = await _unitOfWork.Sectors.GetSectorWithCellsAsync(sectorId);
            if (sector?.Site == null)
            {
                Logger.Warn($"Sektör bulunamadı: {sectorId}");
                return null;
            }

            var site = sector.Site;
            double antennaHeight = site.AntennaHeightM ?? AppConstants.DefaultAntennaHeightM;
            double frequency = sector.FrequencyMhz > 0 ? sector.FrequencyMhz : AppConstants.DefaultFrequencyMhz;
            double txPower = sector.TxPowerDbm > 0 ? sector.TxPowerDbm : AppConstants.DefaultAntennaPowerDbm;

            double radiusKm = GeoHelper.EstimateCoverageRadiusKm(
                txPower, antennaHeight, frequency,
                AppConstants.RsrpPoor, terrainType);

            // Apply tilt correction
            double totalTilt = sector.MechanicalTilt + sector.ElectricalTilt;
            if (totalTilt > 0)
            {
                double tiltFactor = 1.0 - (totalTilt / 90.0) * 0.5;
                radiusKm *= tiltFactor;
            }

            var polygon = GeoHelper.GenerateSectorPolygon(
                site.Latitude, site.Longitude,
                sector.Azimuth, sector.BeamWidthDegrees,
                radiusKm, 36);

            double rsrpAtEdge = GeoHelper.EstimateRsrpAtDistance(
                txPower, antennaHeight, frequency, radiusKm, terrainType);

            var model = new CoverageModelDto
            {
                SectorId = sectorId,
                SiteCode = site.SiteCode,
                SectorName = sector.SectorName,
                CenterLatitude = site.Latitude,
                CenterLongitude = site.Longitude,
                Azimuth = sector.Azimuth,
                BeamWidthDegrees = sector.BeamWidthDegrees,
                EstimatedRadiusKm = radiusKm,
                AntennaHeightM = antennaHeight,
                TxPowerDbm = txPower,
                FrequencyMhz = frequency,
                MechanicalTilt = sector.MechanicalTilt,
                ElectricalTilt = sector.ElectricalTilt,
                TerrainType = terrainType,
                CoveragePolygon = polygon,
                EstimatedRsrpAtEdge = rsrpAtEdge,
                ModeledAt = DateTime.Now
            };

            // Save to database
            var dbModel = new CoverageModel
            {
                SectorId = sectorId,
                EstimatedRadiusKm = radiusKm,
                CoveragePolygonJson = JsonConvert.SerializeObject(polygon),
                EstimatedRsrpAtEdge = rsrpAtEdge,
                TerrainType = (int)terrainType,
                ModeledAt = DateTime.UtcNow
            };

            await _unitOfWork.CoverageModels.UpsertAsync(dbModel);

            return model;
        }

        public async Task<List<CoverageModelDto>> ModelAllSectorsAsync(
            TerrainType terrainType = TerrainType.Urban,
            IProgress<int> progress = null)
        {
            var allSectors = await _unitOfWork.Sectors.GetAllAsync();
            var sectorList = allSectors.ToList();
            var models = new List<CoverageModelDto>();
            int processed = 0;

            foreach (var sector in sectorList)
            {
                try
                {
                    var model = await ModelSectorCoverageAsync(sector.Id, terrainType);
                    if (model != null) models.Add(model);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Sektör modelleme hatası: {sector.Id}");
                }

                processed++;
                progress?.Report(processed);
            }

            Logger.Info($"Tüm sektörler modellendi: {models.Count}/{sectorList.Count}");
            return models;
        }

        public async Task<CoverageModelDto> ValidateWithDriveTestAsync(int sectorId, int driveTestId)
        {
            var model = await ModelSectorCoverageAsync(sectorId);
            if (model == null) return null;

            var driveTestRecords = await _unitOfWork.DriveTests.GetRecordsByCellIdAsync(
                model.SiteCode);

            if (!driveTestRecords.Any())
            {
                Logger.Info($"Drive Test verisi bulunamadı: SectorId={sectorId}");
                return model;
            }

            var recordList = driveTestRecords.ToList();
            int pointsInModel = 0;
            int totalPoints = recordList.Count;

            foreach (var record in recordList)
            {
                bool inSector = GeoHelper.IsPointInSector(
                    record.Latitude, record.Longitude,
                    model.CenterLatitude, model.CenterLongitude,
                    model.Azimuth, model.BeamWidthDegrees,
                    model.EstimatedRadiusKm);

                if (inSector) pointsInModel++;
            }

            double accuracy = totalPoints > 0 ? (double)pointsInModel / totalPoints : 0;
            model.IsValidatedByDriveTest = true;
            model.ValidationAccuracy = accuracy;

            // Update database
            var dbModel = await _unitOfWork.CoverageModels.GetBySectorIdAsync(sectorId);
            if (dbModel != null)
            {
                dbModel.IsValidatedByDriveTest = true;
                dbModel.ValidationAccuracy = accuracy;
                await _unitOfWork.SaveChangesAsync();
            }

            Logger.Info($"Kapsama modeli doğrulandı: SectorId={sectorId}, Doğruluk={accuracy:P1}");
            return model;
        }

        public async Task<double> EstimateRsrpAtPointAsync(int sectorId, double latitude, double longitude)
        {
            var sector = await _unitOfWork.Sectors.GetSectorWithCellsAsync(sectorId);
            if (sector?.Site == null) return -140;

            var site = sector.Site;
            double distance = GeoHelper.CalculateDistanceKm(
                site.Latitude, site.Longitude, latitude, longitude);

            double antennaHeight = site.AntennaHeightM ?? AppConstants.DefaultAntennaHeightM;
            double frequency = sector.FrequencyMhz > 0 ? sector.FrequencyMhz : AppConstants.DefaultFrequencyMhz;
            double txPower = sector.TxPowerDbm > 0 ? sector.TxPowerDbm : AppConstants.DefaultAntennaPowerDbm;

            return GeoHelper.EstimateRsrpAtDistance(txPower, antennaHeight, frequency, distance);
        }
    }
}
