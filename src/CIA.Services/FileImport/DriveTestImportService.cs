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
using CIA.Services.FileImport;
using NLog;

namespace CIA.Services.FileImport
{
    public interface IDriveTestImportService
    {
        Task<ImportedFileDto> ImportAsync(
            string filePath,
            string testName,
            string description,
            int userId,
            IProgress<ImportProgressDto> progress = null,
            CancellationToken cancellationToken = default);
    }

    public class DriveTestImportService : IDriveTestImportService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        // Drive test column aliases
        private static readonly Dictionary<string, string[]> ColumnAliases = new Dictionary<string, string[]>
        {
            { "Timestamp", new[] { "time", "timestamp", "datetime", "date_time", "zaman", "tarih" } },
            { "Latitude", new[] { "lat", "latitude", "enlem", "y" } },
            { "Longitude", new[] { "lon", "lng", "longitude", "boylam", "x" } },
            { "RSRP", new[] { "rsrp", "lte_rsrp", "rsrp_dbm" } },
            { "RSRQ", new[] { "rsrq", "lte_rsrq", "rsrq_db" } },
            { "SINR", new[] { "sinr", "lte_sinr", "sinr_db" } },
            { "RSSI", new[] { "rssi", "lte_rssi" } },
            { "PCI", new[] { "pci", "physical_cell_id", "lte_pci" } },
            { "EARFCN", new[] { "earfcn", "lte_earfcn", "dl_earfcn" } },
            { "SpeedKmh", new[] { "speed", "speed_kmh", "hiz", "velocity" } },
            { "ServingCellId", new[] { "cell_id", "cellid", "serving_cell", "cell", "ci" } },
            { "CGI", new[] { "cgi", "global_cell_id" } }
        };

        public DriveTestImportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ImportedFileDto> ImportAsync(
            string filePath,
            string testName,
            string description,
            int userId,
            IProgress<ImportProgressDto> progress = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            Logger.Info($"Drive Test içe aktarma başlatılıyor: {filePath}");

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

            var importedFile = new ImportedFile
            {
                FileName = fileInfo.Name,
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                FileType = "DriveTest",
                Status = (int)ImportStatus.Processing,
                ImportedAt = DateTime.UtcNow,
                ImportedByUserId = userId
            };

            await _unitOfWork.ImportedFiles.AddAsync(importedFile);
            await _unitOfWork.SaveChangesAsync();

            int processedRows = 0;
            int failedRows = 0;

            try
            {
                // Create DriveTest header
                var driveTest = new DriveTest
                {
                    TestName = testName,
                    Description = description,
                    TestDate = DateTime.Today,
                    ImportedFileId = importedFile.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DriveTests.AddAsync(driveTest);
                await _unitOfWork.SaveChangesAsync();

                var records = new List<DriveTestRecord>(AppConstants.BulkInsertBatchSize);
                var columnMapping = await AutoDetectColumnMappingAsync(filePath);

                double minLat = double.MaxValue, maxLat = double.MinValue;
                double minLon = double.MaxValue, maxLon = double.MinValue;
                DateTime? startTime2 = null, endTime = null;
                var rsrpValues = new List<double>();
                var rsrqValues = new List<double>();
                var sinrValues = new List<double>();

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    var headerLine = await reader.ReadLineAsync();
                    var headers = headerLine?.Split(';') ?? new string[0];

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var record = ParseDriveTestRecord(line, headers, columnMapping, driveTest.Id);
                            if (record != null)
                            {
                                records.Add(record);
                                processedRows++;

                                // Update stats
                                if (record.Latitude < minLat) minLat = record.Latitude;
                                if (record.Latitude > maxLat) maxLat = record.Latitude;
                                if (record.Longitude < minLon) minLon = record.Longitude;
                                if (record.Longitude > maxLon) maxLon = record.Longitude;

                                if (!startTime2.HasValue || record.Timestamp < startTime2.Value)
                                    startTime2 = record.Timestamp;
                                if (!endTime.HasValue || record.Timestamp > endTime.Value)
                                    endTime = record.Timestamp;

                                if (record.RSRP.HasValue) rsrpValues.Add(record.RSRP.Value);
                                if (record.RSRQ.HasValue) rsrqValues.Add(record.RSRQ.Value);
                                if (record.SINR.HasValue) sinrValues.Add(record.SINR.Value);
                            }
                            else
                            {
                                failedRows++;
                            }
                        }
                        catch
                        {
                            failedRows++;
                        }

                        if (records.Count >= AppConstants.BulkInsertBatchSize)
                        {
                            await _unitOfWork.DriveTests.BulkInsertRecordsAsync(records, null, cancellationToken);
                            records.Clear();

                            progress?.Report(new ImportProgressDto
                            {
                                ProcessedRows = processedRows,
                                FailedRows = failedRows,
                                Elapsed = DateTime.UtcNow - startTime,
                                CurrentStatus = $"{processedRows:N0} kayıt işlendi"
                            });
                        }
                    }

                    if (records.Any())
                        await _unitOfWork.DriveTests.BulkInsertRecordsAsync(records, null, cancellationToken);
                }

                // Update DriveTest stats
                driveTest.TotalRecords = processedRows;
                driveTest.StartTime = startTime2;
                driveTest.EndTime = endTime;
                driveTest.MinLatitude = minLat == double.MaxValue ? (double?)null : minLat;
                driveTest.MaxLatitude = maxLat == double.MinValue ? (double?)null : maxLat;
                driveTest.MinLongitude = minLon == double.MaxValue ? (double?)null : minLon;
                driveTest.MaxLongitude = maxLon == double.MinValue ? (double?)null : maxLon;
                driveTest.AvgRSRP = rsrpValues.Any() ? rsrpValues.Average() : (double?)null;
                driveTest.AvgRSRQ = rsrqValues.Any() ? rsrqValues.Average() : (double?)null;
                driveTest.AvgSINR = sinrValues.Any() ? sinrValues.Average() : (double?)null;

                await _unitOfWork.SaveChangesAsync();

                importedFile.Status = (int)ImportStatus.Completed;
                importedFile.TotalRows = processedRows;
                importedFile.ProcessedRows = processedRows;
                importedFile.FailedRows = failedRows;
                importedFile.ImportDurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                await _unitOfWork.SaveChangesAsync();

                Logger.Info($"Drive Test içe aktarma tamamlandı: {processedRows:N0} kayıt");

                return new ImportedFileDto
                {
                    Id = importedFile.Id,
                    FileName = importedFile.FileName,
                    Status = ImportStatus.Completed,
                    TotalRows = processedRows,
                    ProcessedRows = processedRows,
                    FailedRows = failedRows,
                    ImportedAt = importedFile.ImportedAt
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Drive Test içe aktarma hatası");
                importedFile.Status = (int)ImportStatus.Failed;
                importedFile.ErrorMessage = ex.Message;
                await _unitOfWork.SaveChangesAsync();
                throw;
            }
        }

        private async Task<Dictionary<string, string>> AutoDetectColumnMappingAsync(string filePath)
        {
            var mapping = new Dictionary<string, string>();

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine)) return mapping;

                var headers = headerLine.Split(';');
                foreach (var header in headers)
                {
                    var normalized = header.Trim().ToLower().Replace(" ", "_");
                    foreach (var kvp in ColumnAliases)
                    {
                        if (kvp.Value.Any(alias => normalized.Contains(alias)))
                        {
                            if (!mapping.ContainsValue(kvp.Key))
                                mapping[header.Trim()] = kvp.Key;
                            break;
                        }
                    }
                }
            }

            return mapping;
        }

        private DriveTestRecord ParseDriveTestRecord(
            string line, string[] headers,
            Dictionary<string, string> columnMapping, int driveTestId)
        {
            var values = line.Split(';');
            if (values.Length < 3) return null;

            var record = new DriveTestRecord { DriveTestId = driveTestId };

            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                var header = headers[i].Trim();
                var value = values[i].Trim().Trim('"');

                if (!columnMapping.TryGetValue(header, out var fieldName)) continue;

                switch (fieldName)
                {
                    case "Timestamp":
                        if (DateTime.TryParse(value, out var ts)) record.Timestamp = ts;
                        break;
                    case "Latitude":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                            record.Latitude = lat;
                        break;
                    case "Longitude":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                            record.Longitude = lon;
                        break;
                    case "RSRP":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rsrp))
                            record.RSRP = rsrp;
                        break;
                    case "RSRQ":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rsrq))
                            record.RSRQ = rsrq;
                        break;
                    case "SINR":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sinr))
                            record.SINR = sinr;
                        break;
                    case "RSSI":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rssi))
                            record.RSSI = rssi;
                        break;
                    case "PCI":
                        if (int.TryParse(value, out var pci)) record.PCI = pci;
                        break;
                    case "EARFCN":
                        if (int.TryParse(value, out var earfcn)) record.EARFCN = earfcn;
                        break;
                    case "SpeedKmh":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var speed))
                            record.SpeedKmh = speed;
                        break;
                    case "ServingCellId": record.ServingCellId = value; break;
                    case "CGI": record.CGI = value; break;
                }
            }

            // Validate required fields
            if (record.Latitude == 0 && record.Longitude == 0) return null;
            if (record.Timestamp == default) return null;

            return record;
        }
    }
}
