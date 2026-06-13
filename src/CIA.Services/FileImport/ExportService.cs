using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CIA.Core.DTOs;
using CIA.Data.Entities;
using NLog;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace CIA.Services.FileImport
{
    public interface IExportService
    {
        Task<string> ExportHtsRecordsToCsvAsync(IEnumerable<HtsRecordDto> records, string outputPath);
        Task<string> ExportHtsRecordsToExcelAsync(IEnumerable<HtsRecordDto> records, string outputPath);
        Task<string> ExportDriveTestRecordsToCsvAsync(IEnumerable<DriveTestRecord> records, string outputPath);
        Task<string> ExportDriveTestRecordsToExcelAsync(IEnumerable<DriveTestRecord> records, string outputPath);
        Task<string> ExportImportHistoryToExcelAsync(IEnumerable<ImportedFile> files, string outputPath);
        Task<string> ExportAnalysisResultToExcelAsync(NarrowedBaseAnalysisResultDto result, string outputPath);
    }

    public class ExportService : IExportService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public ExportService()
        {
            // EPPlus 4.x does not require license context
        }

        public async Task<string> ExportHtsRecordsToCsvAsync(IEnumerable<HtsRecordDto> records, string outputPath)
        {
            return await Task.Run(() =>
            {
                Logger.Info($"HTS kayıtları CSV olarak dışa aktarılıyor: {outputPath}");

                var sb = new StringBuilder();
                // Header
                sb.AppendLine("PhoneNumber;IMEI;IMSI;CellId;CGI;LAC;MCC;MNC;CallDateTime;DurationSeconds;CallType;CalledNumber;Latitude;Longitude");

                foreach (var r in records)
                {
                    sb.AppendLine(string.Join(";", new[]
                    {
                        Escape(r.PhoneNumber),
                        Escape(r.IMEI),
                        Escape(r.IMSI),
                        Escape(r.CellId),
                        Escape(r.CGI),
                        Escape(r.LAC),
                        Escape(r.MCC),
                        Escape(r.MNC),
                        r.CallDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        r.DurationSeconds.ToString(),
                        Escape(r.CallType),
                        Escape(r.CalledNumber),
                        r.Latitude?.ToString("F6") ?? "",
                        r.Longitude?.ToString("F6") ?? ""
                    }));
                }

                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Logger.Info($"CSV dışa aktarma tamamlandı: {outputPath}");
                return outputPath;
            });
        }

        public async Task<string> ExportHtsRecordsToExcelAsync(IEnumerable<HtsRecordDto> records, string outputPath)
        {
            return await Task.Run(() =>
            {
                Logger.Info($"HTS kayıtları Excel olarak dışa aktarılıyor: {outputPath}");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("HTS Kayıtları");

                    // Headers
                    var headers = new[] { "Telefon No", "IMEI", "IMSI", "Cell ID", "CGI", "LAC", "MCC", "MNC",
                        "Tarih/Saat", "Süre (sn)", "Arama Türü", "Aranan No", "Enlem", "Boylam" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[1, i + 1].Value = headers[i];
                    }

                    // Style header row
                    using (var range = ws.Cells[1, 1, 1, headers.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                        range.Style.Font.Color.SetColor(Color.White);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Data rows
                    int row = 2;
                    foreach (var r in records)
                    {
                        ws.Cells[row, 1].Value = r.PhoneNumber;
                        ws.Cells[row, 2].Value = r.IMEI;
                        ws.Cells[row, 3].Value = r.IMSI;
                        ws.Cells[row, 4].Value = r.CellId;
                        ws.Cells[row, 5].Value = r.CGI;
                        ws.Cells[row, 6].Value = r.LAC;
                        ws.Cells[row, 7].Value = r.MCC;
                        ws.Cells[row, 8].Value = r.MNC;
                        ws.Cells[row, 9].Value = r.CallDateTime.ToString("dd.MM.yyyy HH:mm:ss");
                        ws.Cells[row, 10].Value = r.DurationSeconds;
                        ws.Cells[row, 11].Value = r.CallType;
                        ws.Cells[row, 12].Value = r.CalledNumber;
                        ws.Cells[row, 13].Value = r.Latitude;
                        ws.Cells[row, 14].Value = r.Longitude;

                        // Alternate row color
                        if (row % 2 == 0)
                        {
                            using (var range = ws.Cells[row, 1, row, headers.Length])
                            {
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 245, 255));
                            }
                        }

                        row++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    // Add summary sheet
                    var wsSummary = package.Workbook.Worksheets.Add("Özet");
                    wsSummary.Cells[1, 1].Value = "HTS Kayıt Dışa Aktarım Özeti";
                    wsSummary.Cells[1, 1].Style.Font.Bold = true;
                    wsSummary.Cells[1, 1].Style.Font.Size = 14;
                    wsSummary.Cells[2, 1].Value = "Dışa Aktarım Tarihi:";
                    wsSummary.Cells[2, 2].Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    wsSummary.Cells[3, 1].Value = "Toplam Kayıt:";
                    wsSummary.Cells[3, 2].Value = row - 2;
                    wsSummary.Cells.AutoFitColumns();

                    package.SaveAs(new FileInfo(outputPath));
                }

                Logger.Info($"Excel dışa aktarma tamamlandı: {outputPath}");
                return outputPath;
            });
        }

        public async Task<string> ExportDriveTestRecordsToCsvAsync(IEnumerable<DriveTestRecord> records, string outputPath)
        {
            return await Task.Run(() =>
            {
                Logger.Info($"Drive Test kayıtları CSV olarak dışa aktarılıyor: {outputPath}");

                var sb = new StringBuilder();
                sb.AppendLine("Timestamp;Latitude;Longitude;RSRP;RSRQ;SINR;RSSI;PCI;EARFCN;SpeedKmh;ServingCellId;CGI");

                foreach (var r in records)
                {
                    sb.AppendLine(string.Join(";", new[]
                    {
                        r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        r.Latitude.ToString("F6"),
                        r.Longitude.ToString("F6"),
                        r.RSRP?.ToString("F2") ?? "",
                        r.RSRQ?.ToString("F2") ?? "",
                        r.SINR?.ToString("F2") ?? "",
                        r.RSSI?.ToString("F2") ?? "",
                        r.PCI?.ToString() ?? "",
                        r.EARFCN?.ToString() ?? "",
                        r.SpeedKmh?.ToString("F1") ?? "",
                        Escape(r.ServingCellId),
                        Escape(r.CGI)
                    }));
                }

                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Logger.Info($"Drive Test CSV dışa aktarma tamamlandı: {outputPath}");
                return outputPath;
            });
        }

        public async Task<string> ExportDriveTestRecordsToExcelAsync(IEnumerable<DriveTestRecord> records, string outputPath)
        {
            return await Task.Run(() =>
            {
                Logger.Info($"Drive Test kayıtları Excel olarak dışa aktarılıyor: {outputPath}");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Drive Test Kayıtları");

                    var headers = new[] { "Zaman Damgası", "Enlem", "Boylam", "RSRP (dBm)", "RSRQ (dB)",
                        "SINR (dB)", "RSSI (dBm)", "PCI", "EARFCN", "Hız (km/h)", "Serving Cell ID", "CGI" };

                    for (int i = 0; i < headers.Length; i++)
                        ws.Cells[1, i + 1].Value = headers[i];

                    using (var range = ws.Cells[1, 1, 1, headers.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                        range.Style.Font.Color.SetColor(Color.White);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    int row = 2;
                    foreach (var r in records)
                    {
                        ws.Cells[row, 1].Value = r.Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
                        ws.Cells[row, 2].Value = r.Latitude;
                        ws.Cells[row, 3].Value = r.Longitude;
                        ws.Cells[row, 4].Value = r.RSRP;
                        ws.Cells[row, 5].Value = r.RSRQ;
                        ws.Cells[row, 6].Value = r.SINR;
                        ws.Cells[row, 7].Value = r.RSSI;
                        ws.Cells[row, 8].Value = r.PCI;
                        ws.Cells[row, 9].Value = r.EARFCN;
                        ws.Cells[row, 10].Value = r.SpeedKmh;
                        ws.Cells[row, 11].Value = r.ServingCellId;
                        ws.Cells[row, 12].Value = r.CGI;

                        // Color-code RSRP values
                        if (r.RSRP.HasValue)
                        {
                            Color rsrpColor;
                            if (r.RSRP.Value >= -80) rsrpColor = Color.FromArgb(200, 255, 200);
                            else if (r.RSRP.Value >= -90) rsrpColor = Color.FromArgb(220, 255, 220);
                            else if (r.RSRP.Value >= -100) rsrpColor = Color.FromArgb(255, 255, 200);
                            else if (r.RSRP.Value >= -110) rsrpColor = Color.FromArgb(255, 220, 180);
                            else rsrpColor = Color.FromArgb(255, 200, 200);

                            ws.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(rsrpColor);
                        }

                        if (row % 2 == 0 && !r.RSRP.HasValue)
                        {
                            using (var range = ws.Cells[row, 1, row, headers.Length])
                            {
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245, 248, 255));
                            }
                        }

                        row++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    // Summary sheet
                    var wsSummary = package.Workbook.Worksheets.Add("Özet");
                    wsSummary.Cells[1, 1].Value = "Drive Test Dışa Aktarım Özeti";
                    wsSummary.Cells[1, 1].Style.Font.Bold = true;
                    wsSummary.Cells[1, 1].Style.Font.Size = 14;
                    wsSummary.Cells[2, 1].Value = "Dışa Aktarım Tarihi:";
                    wsSummary.Cells[2, 2].Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    wsSummary.Cells[3, 1].Value = "Toplam Kayıt:";
                    wsSummary.Cells[3, 2].Value = row - 2;

                    var recordsList = records as IList<DriveTestRecord> ?? new List<DriveTestRecord>(records);
                    var rsrpVals = recordsList.Where(r => r.RSRP.HasValue).Select(r => r.RSRP.Value).ToList();
                    if (rsrpVals.Any())
                    {
                        wsSummary.Cells[4, 1].Value = "Ortalama RSRP:";
                        wsSummary.Cells[4, 2].Value = rsrpVals.Average().ToString("F2") + " dBm";
                        wsSummary.Cells[5, 1].Value = "Min RSRP:";
                        wsSummary.Cells[5, 2].Value = rsrpVals.Min().ToString("F2") + " dBm";
                        wsSummary.Cells[6, 1].Value = "Max RSRP:";
                        wsSummary.Cells[6, 2].Value = rsrpVals.Max().ToString("F2") + " dBm";
                    }

                    wsSummary.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                }

                Logger.Info($"Drive Test Excel dışa aktarma tamamlandı: {outputPath}");
                return outputPath;
            });
        }

        public async Task<string> ExportImportHistoryToExcelAsync(IEnumerable<ImportedFile> files, string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("İçe Aktarım Geçmişi");

                    var headers = new[] { "Dosya Adı", "Tür", "Durum", "Toplam Kayıt", "İşlenen", "Hatalı",
                        "Süre", "Aktarım Tarihi", "Dosya Boyutu" };

                    for (int i = 0; i < headers.Length; i++)
                        ws.Cells[1, i + 1].Value = headers[i];

                    using (var range = ws.Cells[1, 1, 1, headers.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                        range.Style.Font.Color.SetColor(Color.White);
                    }

                    int row = 2;
                    foreach (var f in files)
                    {
                        ws.Cells[row, 1].Value = f.FileName;
                        ws.Cells[row, 2].Value = f.FileType;
                        ws.Cells[row, 3].Value = ((CIA.Core.Enums.ImportStatus)f.Status).ToString();
                        ws.Cells[row, 4].Value = f.TotalRows;
                        ws.Cells[row, 5].Value = f.ProcessedRows;
                        ws.Cells[row, 6].Value = f.FailedRows;
                        ws.Cells[row, 7].Value = f.ImportDurationMs.HasValue ? $"{f.ImportDurationMs.Value / 1000.0:F1}s" : "-";
                        ws.Cells[row, 8].Value = f.ImportedAt.ToString("dd.MM.yyyy HH:mm:ss");
                        ws.Cells[row, 9].Value = $"{f.FileSizeBytes / 1024.0 / 1024.0:F2} MB";
                        row++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                }

                return outputPath;
            });
        }

        public async Task<string> ExportAnalysisResultToExcelAsync(NarrowedBaseAnalysisResultDto result, string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    // Summary sheet
                    var wsSummary = package.Workbook.Worksheets.Add("Analiz Özeti");
                    wsSummary.Cells[1, 1].Value = "Daraltılmış Baz Analiz Raporu";
                    wsSummary.Cells[1, 1].Style.Font.Bold = true;
                    wsSummary.Cells[1, 1].Style.Font.Size = 16;

                    var summaryData = new[]
                    {
                        ("Analiz ID:", result.AnalysisId),
                        ("Telefon No:", result.PhoneNumber),
                        ("IMEI:", result.IMEI),
                        ("Analiz Tarihi:", result.AnalysisDate.ToString("dd.MM.yyyy HH:mm:ss")),
                        ("Başlangıç:", result.StartDate.ToString("dd.MM.yyyy HH:mm:ss")),
                        ("Bitiş:", result.EndDate.ToString("dd.MM.yyyy HH:mm:ss")),
                        ("Toplam HTS Kayıt:", result.TotalHtsRecords.ToString()),
                        ("Güven Skoru:", $"{result.ConfidenceScore}/100"),
                        ("Güven Seviyesi:", result.ConfidenceLevel.ToString()),
                        ("Hareket Deseni:", result.MovementPattern.ToString()),
                        ("Toplam Mesafe:", $"{result.TotalDistanceKm:F2} km"),
                        ("Ortalama Hız:", $"{result.AverageSpeedKmh:F1} km/h"),
                        ("Özet:", result.Summary)
                    };

                    int row = 3;
                    foreach (var (label, value) in summaryData)
                    {
                        wsSummary.Cells[row, 1].Value = label;
                        wsSummary.Cells[row, 1].Style.Font.Bold = true;
                        wsSummary.Cells[row, 2].Value = value;
                        row++;
                    }
                    wsSummary.Cells.AutoFitColumns();

                    // Location estimates sheet
                    if (result.LocationEstimates?.Count > 0)
                    {
                        var wsLoc = package.Workbook.Worksheets.Add("Konum Tahminleri");
                        var locHeaders = new[] { "Enlem", "Boylam", "Yarıçap (km)", "Olasılık", "Tahmini Zaman", "Cell ID", "Site Kodu", "Azimut" };
                        for (int i = 0; i < locHeaders.Length; i++)
                            wsLoc.Cells[1, i + 1].Value = locHeaders[i];

                        using (var range = wsLoc.Cells[1, 1, 1, locHeaders.Length])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                            range.Style.Font.Color.SetColor(Color.White);
                        }

                        row = 2;
                        foreach (var loc in result.LocationEstimates)
                        {
                            wsLoc.Cells[row, 1].Value = loc.CenterLatitude;
                            wsLoc.Cells[row, 2].Value = loc.CenterLongitude;
                            wsLoc.Cells[row, 3].Value = loc.RadiusKm;
                            wsLoc.Cells[row, 4].Value = $"{loc.Probability:P1}";
                            wsLoc.Cells[row, 5].Value = loc.EstimatedTime.ToString("dd.MM.yyyy HH:mm:ss");
                            wsLoc.Cells[row, 6].Value = loc.BasedOnCellId;
                            wsLoc.Cells[row, 7].Value = loc.SiteCode;
                            wsLoc.Cells[row, 8].Value = loc.Azimuth;
                            row++;
                        }
                        wsLoc.Cells.AutoFitColumns();
                    }

                    // Movement history sheet
                    if (result.MovementHistory?.Count > 0)
                    {
                        var wsMove = package.Workbook.Worksheets.Add("Hareket Geçmişi");
                        var moveHeaders = new[] { "Zaman", "Enlem", "Boylam", "Cell ID", "CGI", "Site Kodu", "Sektör", "Azimut", "Sıra No" };
                        for (int i = 0; i < moveHeaders.Length; i++)
                            wsMove.Cells[1, i + 1].Value = moveHeaders[i];

                        using (var range = wsMove.Cells[1, 1, 1, moveHeaders.Length])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                            range.Style.Font.Color.SetColor(Color.White);
                        }

                        row = 2;
                        foreach (var pt in result.MovementHistory)
                        {
                            wsMove.Cells[row, 1].Value = pt.Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
                            wsMove.Cells[row, 2].Value = pt.Latitude;
                            wsMove.Cells[row, 3].Value = pt.Longitude;
                            wsMove.Cells[row, 4].Value = pt.CellId;
                            wsMove.Cells[row, 5].Value = pt.CGI;
                            wsMove.Cells[row, 6].Value = pt.SiteCode;
                            wsMove.Cells[row, 7].Value = pt.SectorName;
                            wsMove.Cells[row, 8].Value = pt.Azimuth?.ToString("F0") ?? "";
                            wsMove.Cells[row, 9].Value = pt.SequenceNumber;
                            row++;
                        }
                        wsMove.Cells.AutoFitColumns();
                    }

                    // Scoring details sheet
                    if (result.ScoringDetails?.Count > 0)
                    {
                        var wsScore = package.Workbook.Worksheets.Add("Puanlama Detayları");
                        var scoreHeaders = new[] { "Parametre", "Ağırlık", "Ham Puan", "Ağırlıklı Puan", "Açıklama" };
                        for (int i = 0; i < scoreHeaders.Length; i++)
                            wsScore.Cells[1, i + 1].Value = scoreHeaders[i];

                        using (var range = wsScore.Cells[1, 1, 1, scoreHeaders.Length])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                            range.Style.Font.Color.SetColor(Color.White);
                        }

                        row = 2;
                        foreach (var sd in result.ScoringDetails)
                        {
                            wsScore.Cells[row, 1].Value = sd.Parameter;
                            wsScore.Cells[row, 2].Value = sd.Weight;
                            wsScore.Cells[row, 3].Value = sd.RawScore;
                            wsScore.Cells[row, 4].Value = sd.WeightedScore;
                            wsScore.Cells[row, 5].Value = sd.Description;
                            row++;
                        }
                        wsScore.Cells.AutoFitColumns();
                    }

                    package.SaveAs(new FileInfo(outputPath));
                }

                Logger.Info($"Analiz sonucu Excel dışa aktarma tamamlandı: {outputPath}");
                return outputPath;
            });
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
