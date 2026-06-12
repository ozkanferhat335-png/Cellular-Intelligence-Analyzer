using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Data.Context;
using CIA.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CIA.Data.Repositories.Implementations
{
    public class DriveTestRepository : BaseRepository<DriveTest>, IDriveTestRepository
    {
        private readonly string _connectionString;

        public DriveTestRepository(CiaDbContext context, string connectionString) : base(context)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<DriveTest>> GetAllWithStatsAsync()
        {
            return await Context.DriveTests
                .OrderByDescending(d => d.TestDate)
                .ToListAsync();
        }

        public async Task<DriveTest> GetWithRecordsAsync(int driveTestId)
        {
            return await Context.DriveTests
                .Include(d => d.Records)
                .FirstOrDefaultAsync(d => d.Id == driveTestId);
        }

        public async Task<IEnumerable<DriveTestRecord>> GetRecordsAsync(DriveTestQueryDto query)
        {
            var dbQuery = Context.DriveTestRecords.AsQueryable();

            if (query.DriveTestId.HasValue)
                dbQuery = dbQuery.Where(r => r.DriveTestId == query.DriveTestId.Value);

            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(r => r.Timestamp >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(r => r.Timestamp <= query.EndDate.Value);

            if (query.MinLatitude.HasValue) dbQuery = dbQuery.Where(r => r.Latitude >= query.MinLatitude.Value);
            if (query.MaxLatitude.HasValue) dbQuery = dbQuery.Where(r => r.Latitude <= query.MaxLatitude.Value);
            if (query.MinLongitude.HasValue) dbQuery = dbQuery.Where(r => r.Longitude >= query.MinLongitude.Value);
            if (query.MaxLongitude.HasValue) dbQuery = dbQuery.Where(r => r.Longitude <= query.MaxLongitude.Value);

            if (query.MinRSRP.HasValue) dbQuery = dbQuery.Where(r => r.RSRP >= query.MinRSRP.Value);
            if (query.MaxRSRP.HasValue) dbQuery = dbQuery.Where(r => r.RSRP <= query.MaxRSRP.Value);

            if (query.PCI.HasValue) dbQuery = dbQuery.Where(r => r.PCI == query.PCI.Value);

            if (!string.IsNullOrWhiteSpace(query.CellId))
                dbQuery = dbQuery.Where(r => r.ServingCellId == query.CellId);

            return await dbQuery
                .OrderBy(r => r.Timestamp)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<long> GetRecordCountAsync(int driveTestId)
        {
            return await Context.DriveTestRecords
                .LongCountAsync(r => r.DriveTestId == driveTestId);
        }

        public async Task<long> BulkInsertRecordsAsync(
            IEnumerable<DriveTestRecord> records,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            long insertedCount = 0;
            int totalProcessed = 0;

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            INSERT INTO DriveTestRecords 
                            (DriveTestId, Timestamp, Latitude, Longitude, RSRP, RSRQ, SINR, RSSI, 
                             PCI, EARFCN, SpeedKmh, ServingCellId, CGI, Technology)
                            VALUES 
                            (@DriveTestId, @Timestamp, @Latitude, @Longitude, @RSRP, @RSRQ, @SINR, @RSSI,
                             @PCI, @EARFCN, @SpeedKmh, @ServingCellId, @CGI, @Technology)";

                        var pDriveTestId = cmd.CreateParameter(); pDriveTestId.ParameterName = "@DriveTestId"; cmd.Parameters.Add(pDriveTestId);
                        var pTimestamp = cmd.CreateParameter(); pTimestamp.ParameterName = "@Timestamp"; cmd.Parameters.Add(pTimestamp);
                        var pLatitude = cmd.CreateParameter(); pLatitude.ParameterName = "@Latitude"; cmd.Parameters.Add(pLatitude);
                        var pLongitude = cmd.CreateParameter(); pLongitude.ParameterName = "@Longitude"; cmd.Parameters.Add(pLongitude);
                        var pRSRP = cmd.CreateParameter(); pRSRP.ParameterName = "@RSRP"; cmd.Parameters.Add(pRSRP);
                        var pRSRQ = cmd.CreateParameter(); pRSRQ.ParameterName = "@RSRQ"; cmd.Parameters.Add(pRSRQ);
                        var pSINR = cmd.CreateParameter(); pSINR.ParameterName = "@SINR"; cmd.Parameters.Add(pSINR);
                        var pRSSI = cmd.CreateParameter(); pRSSI.ParameterName = "@RSSI"; cmd.Parameters.Add(pRSSI);
                        var pPCI = cmd.CreateParameter(); pPCI.ParameterName = "@PCI"; cmd.Parameters.Add(pPCI);
                        var pEARFCN = cmd.CreateParameter(); pEARFCN.ParameterName = "@EARFCN"; cmd.Parameters.Add(pEARFCN);
                        var pSpeed = cmd.CreateParameter(); pSpeed.ParameterName = "@SpeedKmh"; cmd.Parameters.Add(pSpeed);
                        var pCellId = cmd.CreateParameter(); pCellId.ParameterName = "@ServingCellId"; cmd.Parameters.Add(pCellId);
                        var pCGI = cmd.CreateParameter(); pCGI.ParameterName = "@CGI"; cmd.Parameters.Add(pCGI);
                        var pTech = cmd.CreateParameter(); pTech.ParameterName = "@Technology"; cmd.Parameters.Add(pTech);

                        foreach (var record in records)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            pDriveTestId.Value = record.DriveTestId;
                            pTimestamp.Value = record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            pLatitude.Value = record.Latitude;
                            pLongitude.Value = record.Longitude;
                            pRSRP.Value = record.RSRP.HasValue ? (object)record.RSRP.Value : DBNull.Value;
                            pRSRQ.Value = record.RSRQ.HasValue ? (object)record.RSRQ.Value : DBNull.Value;
                            pSINR.Value = record.SINR.HasValue ? (object)record.SINR.Value : DBNull.Value;
                            pRSSI.Value = record.RSSI.HasValue ? (object)record.RSSI.Value : DBNull.Value;
                            pPCI.Value = record.PCI.HasValue ? (object)record.PCI.Value : DBNull.Value;
                            pEARFCN.Value = record.EARFCN.HasValue ? (object)record.EARFCN.Value : DBNull.Value;
                            pSpeed.Value = record.SpeedKmh.HasValue ? (object)record.SpeedKmh.Value : DBNull.Value;
                            pCellId.Value = (object)record.ServingCellId ?? DBNull.Value;
                            pCGI.Value = (object)record.CGI ?? DBNull.Value;
                            pTech.Value = record.Technology.HasValue ? (object)record.Technology.Value : DBNull.Value;

                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                            insertedCount++;
                            totalProcessed++;

                            if (totalProcessed % AppConstants.BulkInsertBatchSize == 0)
                            {
                                await transaction.CommitAsync(cancellationToken);
                                progress?.Report(totalProcessed);
                            }
                        }

                        await transaction.CommitAsync(cancellationToken);
                        progress?.Report(totalProcessed);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                }
            }

            return insertedCount;
        }

        public async Task<IEnumerable<DriveTestRecord>> GetRecordsInBoundingBoxAsync(
            int driveTestId, double minLat, double maxLat, double minLon, double maxLon)
        {
            return await Context.DriveTestRecords
                .Where(r => r.DriveTestId == driveTestId &&
                            r.Latitude >= minLat && r.Latitude <= maxLat &&
                            r.Longitude >= minLon && r.Longitude <= maxLon)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriveTestRecord>> GetRecordsByCellIdAsync(string cellId)
        {
            return await Context.DriveTestRecords
                .Where(r => r.ServingCellId == cellId || r.CGI == cellId)
                .OrderBy(r => r.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriveTestRecord>> GetRecordsByPciAsync(int pci)
        {
            return await Context.DriveTestRecords
                .Where(r => r.PCI == pci)
                .OrderBy(r => r.Timestamp)
                .ToListAsync();
        }

        public async Task<DriveTestStatisticsDto> GetStatisticsAsync(int driveTestId)
        {
            var records = await Context.DriveTestRecords
                .Where(r => r.DriveTestId == driveTestId && r.RSRP.HasValue)
                .ToListAsync();

            if (!records.Any())
                return new DriveTestStatisticsDto();

            var rsrpValues = records.Where(r => r.RSRP.HasValue).Select(r => r.RSRP.Value).ToList();
            var rsrqValues = records.Where(r => r.RSRQ.HasValue).Select(r => r.RSRQ.Value).ToList();
            var sinrValues = records.Where(r => r.SINR.HasValue).Select(r => r.SINR.Value).ToList();

            int total = records.Count;
            int excellent = rsrpValues.Count(v => v >= AppConstants.RsrpExcellent);
            int good = rsrpValues.Count(v => v >= AppConstants.RsrpGood && v < AppConstants.RsrpExcellent);
            int fair = rsrpValues.Count(v => v >= AppConstants.RsrpFair && v < AppConstants.RsrpGood);
            int poor = rsrpValues.Count(v => v >= AppConstants.RsrpPoor && v < AppConstants.RsrpFair);
            int noSignal = rsrpValues.Count(v => v < AppConstants.RsrpPoor);

            return new DriveTestStatisticsDto
            {
                TotalPoints = total,
                AvgRSRP = rsrpValues.Any() ? rsrpValues.Average() : 0,
                MinRSRP = rsrpValues.Any() ? rsrpValues.Min() : 0,
                MaxRSRP = rsrpValues.Any() ? rsrpValues.Max() : 0,
                AvgRSRQ = rsrqValues.Any() ? rsrqValues.Average() : 0,
                AvgSINR = sinrValues.Any() ? sinrValues.Average() : 0,
                ExcellentCoveragePercent = total > 0 ? (double)excellent / total * 100 : 0,
                GoodCoveragePercent = total > 0 ? (double)good / total * 100 : 0,
                FairCoveragePercent = total > 0 ? (double)fair / total * 100 : 0,
                PoorCoveragePercent = total > 0 ? (double)poor / total * 100 : 0,
                NoCoveragePercent = total > 0 ? (double)noSignal / total * 100 : 0,
                UniquePCIs = records.Where(r => r.PCI.HasValue).Select(r => r.PCI.Value).Distinct().Count(),
                UniqueCells = records.Where(r => !string.IsNullOrEmpty(r.ServingCellId)).Select(r => r.ServingCellId).Distinct().Count()
            };
        }
    }
}
