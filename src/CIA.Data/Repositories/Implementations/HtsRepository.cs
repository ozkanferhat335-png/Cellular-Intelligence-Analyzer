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
    public class HtsRepository : BaseRepository<HtsRecord>, IHtsRepository
    {
        private readonly string _connectionString;

        public HtsRepository(CiaDbContext context, string connectionString) : base(context)
        {
            _connectionString = connectionString;
        }

        public async Task<HtsQueryResultDto> QueryAsync(HtsQueryDto query)
        {
            var dbQuery = Context.HtsRecords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                dbQuery = dbQuery.Where(h => h.PhoneNumber == query.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(query.IMEI))
                dbQuery = dbQuery.Where(h => h.IMEI == query.IMEI);

            if (!string.IsNullOrWhiteSpace(query.IMSI))
                dbQuery = dbQuery.Where(h => h.IMSI == query.IMSI);

            if (!string.IsNullOrWhiteSpace(query.CellId))
                dbQuery = dbQuery.Where(h => h.CellId == query.CellId);

            if (!string.IsNullOrWhiteSpace(query.CGI))
                dbQuery = dbQuery.Where(h => h.CGI == query.CGI);

            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(h => h.CallDateTime >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(h => h.CallDateTime <= query.EndDate.Value);

            var startTime = DateTime.UtcNow;
            var totalCount = await dbQuery.CountAsync();

            var records = await dbQuery
                .OrderBy(h => h.CallDateTime)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var duration = DateTime.UtcNow - startTime;

            var dtos = records.Select(r => MapToDto(r)).ToList();

            return new HtsQueryResultDto
            {
                Records = dtos,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                QueryDuration = duration
            };
        }

        public async Task<IEnumerable<HtsRecord>> GetMovementHistoryAsync(
            string phoneNumber, DateTime startDate, DateTime endDate)
        {
            return await Context.HtsRecords
                .Where(h => h.PhoneNumber == phoneNumber &&
                            h.CallDateTime >= startDate &&
                            h.CallDateTime <= endDate)
                .OrderBy(h => h.CallDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<HtsRecord>> GetByImeiAsync(
            string imei, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = Context.HtsRecords.Where(h => h.IMEI == imei);
            if (startDate.HasValue) query = query.Where(h => h.CallDateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(h => h.CallDateTime <= endDate.Value);
            return await query.OrderBy(h => h.CallDateTime).ToListAsync();
        }

        public async Task<IEnumerable<HtsRecord>> GetByImsiAsync(
            string imsi, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = Context.HtsRecords.Where(h => h.IMSI == imsi);
            if (startDate.HasValue) query = query.Where(h => h.CallDateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(h => h.CallDateTime <= endDate.Value);
            return await query.OrderBy(h => h.CallDateTime).ToListAsync();
        }

        public async Task<IEnumerable<HtsRecord>> GetByCellIdAsync(
            string cellId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = Context.HtsRecords.Where(h => h.CellId == cellId || h.CGI == cellId);
            if (startDate.HasValue) query = query.Where(h => h.CallDateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(h => h.CallDateTime <= endDate.Value);
            return await query.OrderBy(h => h.CallDateTime).ToListAsync();
        }

        public async Task<long> GetTotalCountAsync()
        {
            return await Context.HtsRecords.LongCountAsync();
        }

        public async Task<long> BulkInsertAsync(
            IEnumerable<HtsRecord> records,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            long insertedCount = 0;
            var batch = new List<HtsRecord>(AppConstants.BulkInsertBatchSize);
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
                            INSERT INTO HTSRecords 
                            (PhoneNumber, IMEI, IMSI, CellId, CGI, LAC, MCC, MNC, 
                             CallDateTime, DurationSeconds, CallType, CalledNumber, 
                             Latitude, Longitude, ImportedFileId, ImportedAt)
                            VALUES 
                            (@PhoneNumber, @IMEI, @IMSI, @CellId, @CGI, @LAC, @MCC, @MNC,
                             @CallDateTime, @DurationSeconds, @CallType, @CalledNumber,
                             @Latitude, @Longitude, @ImportedFileId, @ImportedAt)";

                        var pPhoneNumber = cmd.CreateParameter(); pPhoneNumber.ParameterName = "@PhoneNumber"; cmd.Parameters.Add(pPhoneNumber);
                        var pIMEI = cmd.CreateParameter(); pIMEI.ParameterName = "@IMEI"; cmd.Parameters.Add(pIMEI);
                        var pIMSI = cmd.CreateParameter(); pIMSI.ParameterName = "@IMSI"; cmd.Parameters.Add(pIMSI);
                        var pCellId = cmd.CreateParameter(); pCellId.ParameterName = "@CellId"; cmd.Parameters.Add(pCellId);
                        var pCGI = cmd.CreateParameter(); pCGI.ParameterName = "@CGI"; cmd.Parameters.Add(pCGI);
                        var pLAC = cmd.CreateParameter(); pLAC.ParameterName = "@LAC"; cmd.Parameters.Add(pLAC);
                        var pMCC = cmd.CreateParameter(); pMCC.ParameterName = "@MCC"; cmd.Parameters.Add(pMCC);
                        var pMNC = cmd.CreateParameter(); pMNC.ParameterName = "@MNC"; cmd.Parameters.Add(pMNC);
                        var pCallDateTime = cmd.CreateParameter(); pCallDateTime.ParameterName = "@CallDateTime"; cmd.Parameters.Add(pCallDateTime);
                        var pDuration = cmd.CreateParameter(); pDuration.ParameterName = "@DurationSeconds"; cmd.Parameters.Add(pDuration);
                        var pCallType = cmd.CreateParameter(); pCallType.ParameterName = "@CallType"; cmd.Parameters.Add(pCallType);
                        var pCalledNumber = cmd.CreateParameter(); pCalledNumber.ParameterName = "@CalledNumber"; cmd.Parameters.Add(pCalledNumber);
                        var pLatitude = cmd.CreateParameter(); pLatitude.ParameterName = "@Latitude"; cmd.Parameters.Add(pLatitude);
                        var pLongitude = cmd.CreateParameter(); pLongitude.ParameterName = "@Longitude"; cmd.Parameters.Add(pLongitude);
                        var pImportedFileId = cmd.CreateParameter(); pImportedFileId.ParameterName = "@ImportedFileId"; cmd.Parameters.Add(pImportedFileId);
                        var pImportedAt = cmd.CreateParameter(); pImportedAt.ParameterName = "@ImportedAt"; cmd.Parameters.Add(pImportedAt);

                        foreach (var record in records)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            pPhoneNumber.Value = (object)record.PhoneNumber ?? DBNull.Value;
                            pIMEI.Value = (object)record.IMEI ?? DBNull.Value;
                            pIMSI.Value = (object)record.IMSI ?? DBNull.Value;
                            pCellId.Value = (object)record.CellId ?? DBNull.Value;
                            pCGI.Value = (object)record.CGI ?? DBNull.Value;
                            pLAC.Value = (object)record.LAC ?? DBNull.Value;
                            pMCC.Value = (object)record.MCC ?? DBNull.Value;
                            pMNC.Value = (object)record.MNC ?? DBNull.Value;
                            pCallDateTime.Value = record.CallDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            pDuration.Value = record.DurationSeconds;
                            pCallType.Value = (object)record.CallType ?? DBNull.Value;
                            pCalledNumber.Value = (object)record.CalledNumber ?? DBNull.Value;
                            pLatitude.Value = record.Latitude.HasValue ? (object)record.Latitude.Value : DBNull.Value;
                            pLongitude.Value = record.Longitude.HasValue ? (object)record.Longitude.Value : DBNull.Value;
                            pImportedFileId.Value = record.ImportedFileId;
                            pImportedAt.Value = record.ImportedAt.ToString("yyyy-MM-dd HH:mm:ss");

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

        public async Task<IEnumerable<string>> GetDistinctPhoneNumbersAsync(int limit = 1000)
        {
            return await Context.HtsRecords
                .Where(h => !string.IsNullOrEmpty(h.PhoneNumber))
                .Select(h => h.PhoneNumber)
                .Distinct()
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctCellIdsAsync()
        {
            return await Context.HtsRecords
                .Where(h => !string.IsNullOrEmpty(h.CellId))
                .Select(h => h.CellId)
                .Distinct()
                .ToListAsync();
        }

        public async Task DeleteByImportedFileIdAsync(int importedFileId)
        {
            var records = await Context.HtsRecords
                .Where(h => h.ImportedFileId == importedFileId)
                .ToListAsync();
            Context.HtsRecords.RemoveRange(records);
            await Context.SaveChangesAsync();
        }

        private HtsRecordDto MapToDto(HtsRecord record)
        {
            return new HtsRecordDto
            {
                Id = record.Id,
                PhoneNumber = record.PhoneNumber,
                IMEI = record.IMEI,
                IMSI = record.IMSI,
                CellId = record.CellId,
                CGI = record.CGI,
                LAC = record.LAC,
                MCC = record.MCC,
                MNC = record.MNC,
                CallDateTime = record.CallDateTime,
                DurationSeconds = record.DurationSeconds,
                CallType = record.CallType,
                CalledNumber = record.CalledNumber,
                Latitude = record.Latitude,
                Longitude = record.Longitude,
                ImportedFileId = record.ImportedFileId,
                ImportedAt = record.ImportedAt
            };
        }
    }
}
