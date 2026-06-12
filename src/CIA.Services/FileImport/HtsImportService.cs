using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Data.Entities;
using CIA.Data.Repositories;
using NLog;

namespace CIA.Services.FileImport
{
    public interface IHtsImportService
    {
        Task<ImportedFileDto> ImportAsync(
            HtsImportConfigDto config,
            int userId,
            IProgress<ImportProgressDto> progress = null,
            CancellationToken cancellationToken = default);

        Task<List<string>> DetectColumnsAsync(string filePath, string delimiter = ";");
        Task<List<Dictionary<string, string>>> PreviewAsync(string filePath, string delimiter = ";", int rowCount = 10);
    }

    public class ImportProgressDto
    {
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int FailedRows { get; set; }
        public double ProgressPercent => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
        public string CurrentStatus { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan? EstimatedRemaining { get; set; }
    }

    public class HtsImportService : IHtsImportService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        // Standard HTS column names (Turkish telecom format)
        private static readonly Dictionary<string, string[]> ColumnAliases = new Dictionary<string, string[]>
        {
            { "PhoneNumber", new[] { "telefon", "msisdn", "phone", "phonenumber", "abone_no", "numara", "calling_number" } },
            { "IMEI", new[] { "imei", "cihaz_no", "device_id" } },
            { "IMSI", new[] { "imsi", "abone_kimlik" } },
            { "CellId", new[] { "cell_id", "cellid", "hucre_id", "baz_id", "cell", "ci" } },
            { "CGI", new[] { "cgi", "global_cell_id" } },
            { "LAC", new[] { "lac", "location_area_code", "konum_alan_kodu" } },
            { "MCC", new[] { "mcc", "mobile_country_code" } },
            { "MNC", new[] { "mnc", "mobile_network_code" } },
            { "CallDateTime", new[] { "tarih", "date", "datetime", "call_date", "arama_tarihi", "timestamp", "zaman" } },
            { "DurationSeconds", new[] { "sure", "duration", "duration_sec", "konusma_suresi" } },
            { "CallType", new[] { "tur", "type", "call_type", "arama_turu" } },
            { "CalledNumber", new[] { "aranan", "called", "called_number", "b_number" } },
            { "Latitude", new[] { "lat", "latitude", "enlem" } },
            { "Longitude", new[] { "lon", "lng", "longitude", "boylam" } }
        };

        public HtsImportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ImportedFileDto> ImportAsync(
            HtsImportConfigDto config,
            int userId,
            IProgress<ImportProgressDto> progress = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            Logger.Info($"HTS içe aktarma başlatılıyor: {config.FilePath}");

            var fileInfo = new FileInfo(config.FilePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Dosya bulunamadı: {config.FilePath}");

            // Create import record
            var importedFile = new ImportedFile
            {
                FileName = fileInfo.Name,
                FilePath = config.FilePath,
                FileSizeBytes = fileInfo.Length,
                FileType = "HTS",
                Status = (int)ImportStatus.Processing,
                ImportedAt = DateTime.UtcNow,
                ImportedByUserId = userId
            };

            await _unitOfWork.ImportedFiles.AddAsync(importedFile);
            await _unitOfWork.SaveChangesAsync();

            int totalRows = 0;
            int processedRows = 0;
            int failedRows = 0;

            try
            {
                // Count total rows
                totalRows = await CountRowsAsync(config.FilePath, config.HasHeader);
                importedFile.TotalRows = totalRows;
                await _unitOfWork.SaveChangesAsync();

                var progressDto = new ImportProgressDto
                {
                    TotalRows = totalRows,
                    CurrentStatus = "Dosya okunuyor..."
                };

                var records = new List<HtsRecord>(config.BatchSize);
                var columnMapping = config.ColumnMapping.Any()
                    ? config.ColumnMapping
                    : await AutoDetectColumnMappingAsync(config.FilePath, config.Delimiter);

                using (var reader = new StreamReader(config.FilePath, Encoding.UTF8))
                {
                    string headerLine = null;
                    string[] headers = null;

                    if (config.HasHeader)
                    {
                        headerLine = await reader.ReadLineAsync();
                        headers = headerLine?.Split(config.Delimiter[0]);
                    }

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var record = ParseHtsRecord(line, headers, columnMapping, config.DateFormat, importedFile.Id);
                            if (record != null)
                            {
                                records.Add(record);
                                processedRows++;
                            }
                            else
                            {
                                failedRows++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug(ex, $"Satır ayrıştırma hatası: {line}");
                            failedRows++;
                        }

                        if (records.Count >= config.BatchSize)
                        {
                            await _unitOfWork.HtsRecords.BulkInsertAsync(records, null, cancellationToken);
                            records.Clear();

                            progressDto.ProcessedRows = processedRows;
                            progressDto.FailedRows = failedRows;
                            progressDto.Elapsed = DateTime.UtcNow - startTime;
                            if (processedRows > 0 && totalRows > 0)
                            {
                                double rate = processedRows / progressDto.Elapsed.TotalSeconds;
                                int remaining = totalRows - processedRows;
                                progressDto.EstimatedRemaining = TimeSpan.FromSeconds(remaining / Math.Max(1, rate));
                            }
                            progressDto.CurrentStatus = $"{processedRows:N0} / {totalRows:N0} kayıt işlendi";
                            progress?.Report(progressDto);
                        }
                    }

                    // Insert remaining records
                    if (records.Any())
                    {
                        await _unitOfWork.HtsRecords.BulkInsertAsync(records, null, cancellationToken);
                    }
                }

                importedFile.Status = (int)ImportStatus.Completed;
                importedFile.ProcessedRows = processedRows;
                importedFile.FailedRows = failedRows;
                importedFile.ImportDurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                await _unitOfWork.SaveChangesAsync();

                Logger.Info($"HTS içe aktarma tamamlandı: {processedRows:N0} kayıt, {failedRows} hata, " +
                           $"Süre: {(DateTime.UtcNow - startTime).TotalSeconds:F1}s");

                return MapToDto(importedFile);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HTS içe aktarma hatası");
                importedFile.Status = (int)ImportStatus.Failed;
                importedFile.ErrorMessage = ex.Message;
                importedFile.ProcessedRows = processedRows;
                importedFile.FailedRows = failedRows;
                await _unitOfWork.SaveChangesAsync();
                throw;
            }
        }

        public async Task<List<string>> DetectColumnsAsync(string filePath, string delimiter = ";")
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine)) return new List<string>();
                return headerLine.Split(delimiter[0]).Select(h => h.Trim()).ToList();
            }
        }

        public async Task<List<Dictionary<string, string>>> PreviewAsync(
            string filePath, string delimiter = ";", int rowCount = 10)
        {
            var result = new List<Dictionary<string, string>>();

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine)) return result;

                var headers = headerLine.Split(delimiter[0]).Select(h => h.Trim()).ToArray();
                int count = 0;

                string line;
                while ((line = await reader.ReadLineAsync()) != null && count < rowCount)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var values = line.Split(delimiter[0]);
                    var row = new Dictionary<string, string>();

                    for (int i = 0; i < headers.Length && i < values.Length; i++)
                        row[headers[i]] = values[i].Trim();

                    result.Add(row);
                    count++;
                }
            }

            return result;
        }

        private async Task<Dictionary<string, string>> AutoDetectColumnMappingAsync(
            string filePath, string delimiter)
        {
            var headers = await DetectColumnsAsync(filePath, delimiter);
            var mapping = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                var normalizedHeader = header.ToLower().Replace(" ", "_").Replace("-", "_");

                foreach (var kvp in ColumnAliases)
                {
                    if (kvp.Value.Any(alias => normalizedHeader.Contains(alias)))
                    {
                        if (!mapping.ContainsValue(kvp.Key))
                            mapping[header] = kvp.Key;
                        break;
                    }
                }
            }

            return mapping;
        }

        private HtsRecord ParseHtsRecord(
            string line, string[] headers,
            Dictionary<string, string> columnMapping,
            string dateFormat, int importedFileId)
        {
            var values = line.Split(';');
            if (values.Length < 2) return null;

            var record = new HtsRecord
            {
                ImportedFileId = importedFileId,
                ImportedAt = DateTime.UtcNow
            };

            for (int i = 0; i < (headers?.Length ?? values.Length) && i < values.Length; i++)
            {
                var header = headers?[i]?.Trim() ?? i.ToString();
                var value = values[i].Trim().Trim('"');

                if (!columnMapping.TryGetValue(header, out var fieldName)) continue;

                switch (fieldName)
                {
                    case "PhoneNumber": record.PhoneNumber = value; break;
                    case "IMEI": record.IMEI = value; break;
                    case "IMSI": record.IMSI = value; break;
                    case "CellId": record.CellId = value; break;
                    case "CGI": record.CGI = value; break;
                    case "LAC": record.LAC = value; break;
                    case "MCC": record.MCC = value; break;
                    case "MNC": record.MNC = value; break;
                    case "CallDateTime":
                        if (DateTime.TryParseExact(value, dateFormat,
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                            record.CallDateTime = dt;
                        else if (DateTime.TryParse(value, out dt))
                            record.CallDateTime = dt;
                        break;
                    case "DurationSeconds":
                        if (int.TryParse(value, out var dur)) record.DurationSeconds = dur;
                        break;
                    case "CallType": record.CallType = value; break;
                    case "CalledNumber": record.CalledNumber = value; break;
                    case "Latitude":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                            record.Latitude = lat;
                        break;
                    case "Longitude":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                            record.Longitude = lon;
                        break;
                }
            }

            // Validate minimum required fields
            if (string.IsNullOrEmpty(record.PhoneNumber) && string.IsNullOrEmpty(record.IMEI) &&
                string.IsNullOrEmpty(record.IMSI))
                return null;

            if (record.CallDateTime == default)
                return null;

            return record;
        }

        private async Task<int> CountRowsAsync(string filePath, bool hasHeader)
        {
            int count = 0;
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                if (hasHeader) await reader.ReadLineAsync(); // Skip header
                while (await reader.ReadLineAsync() != null)
                    count++;
            }
            return count;
        }

        private ImportedFileDto MapToDto(ImportedFile file)
        {
            return new ImportedFileDto
            {
                Id = file.Id,
                FileName = file.FileName,
                FilePath = file.FilePath,
                FileSizeBytes = file.FileSizeBytes,
                FileType = file.FileType,
                Status = (ImportStatus)file.Status,
                TotalRows = file.TotalRows,
                ProcessedRows = file.ProcessedRows,
                FailedRows = file.FailedRows,
                ImportedAt = file.ImportedAt,
                ImportedByUserId = file.ImportedByUserId,
                ErrorMessage = file.ErrorMessage,
                ImportDuration = file.ImportDurationMs.HasValue
                    ? TimeSpan.FromMilliseconds(file.ImportDurationMs.Value)
                    : (TimeSpan?)null
            };
        }
    }
}
