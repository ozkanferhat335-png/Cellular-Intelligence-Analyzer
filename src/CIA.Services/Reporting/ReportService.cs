using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Data.Entities;
using CIA.Data.Repositories;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using NLog;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CIA.Services.Reporting
{
    public interface IReportService
    {
        Task<ReportDto> GenerateHtsReportAsync(ReportRequestDto request, int userId);
        Task<ReportDto> GenerateDriveTestReportAsync(ReportRequestDto request, int userId);
        Task<ReportDto> GenerateNarrowedBaseReportAsync(ReportRequestDto request, NarrowedBaseAnalysisResultDto analysisResult, int userId);
        Task<ReportDto> GenerateExecutiveSummaryAsync(ReportRequestDto request, int userId);
        Task<bool> OpenReportAsync(string filePath);
    }

    public class ReportService : IReportService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        // Colors for PDF
        private static readonly BaseColor HeaderColor = new BaseColor(31, 73, 125);
        private static readonly BaseColor SubHeaderColor = new BaseColor(68, 114, 196);
        private static readonly BaseColor HighConfidenceColor = new BaseColor(0, 176, 80);
        private static readonly BaseColor MediumConfidenceColor = new BaseColor(255, 192, 0);
        private static readonly BaseColor LowConfidenceColor = new BaseColor(255, 0, 0);
        private static readonly BaseColor TableHeaderColor = new BaseColor(31, 73, 125);
        private static readonly BaseColor TableAltRowColor = new BaseColor(242, 242, 242);

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ReportDto> GenerateHtsReportAsync(ReportRequestDto request, int userId)
        {
            Logger.Info($"HTS raporu oluşturuluyor: {request.PhoneNumber}");

            var outputPath = GetOutputPath(request, "HTS_Raporu");
            var report = await CreateReportRecordAsync(request, userId, outputPath);

            try
            {
                var htsQuery = new HtsQueryDto
                {
                    PhoneNumber = request.PhoneNumber,
                    IMEI = request.IMEI,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    PageSize = 100000
                };

                var htsResult = await _unitOfWork.HtsRecords.QueryAsync(htsQuery);

                if (request.Format == ReportFormat.PDF)
                    await GenerateHtsPdfAsync(outputPath, request, htsResult);
                else if (request.Format == ReportFormat.Excel)
                    await GenerateHtsExcelAsync(outputPath, request, htsResult);

                await FinalizeReportAsync(report, outputPath);
                Logger.Info($"HTS raporu oluşturuldu: {outputPath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HTS raporu oluşturma hatası");
                await FailReportAsync(report, ex.Message);
                throw;
            }

            return MapToDto(report);
        }

        public async Task<ReportDto> GenerateDriveTestReportAsync(ReportRequestDto request, int userId)
        {
            Logger.Info($"Drive Test raporu oluşturuluyor: DriveTestId={request.DriveTestId}");

            var outputPath = GetOutputPath(request, "DriveTest_Raporu");
            var report = await CreateReportRecordAsync(request, userId, outputPath);

            try
            {
                if (request.Format == ReportFormat.PDF)
                    await GenerateDriveTestPdfAsync(outputPath, request);
                else if (request.Format == ReportFormat.Excel)
                    await GenerateDriveTestExcelAsync(outputPath, request);

                await FinalizeReportAsync(report, outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Drive Test raporu oluşturma hatası");
                await FailReportAsync(report, ex.Message);
                throw;
            }

            return MapToDto(report);
        }

        public async Task<ReportDto> GenerateNarrowedBaseReportAsync(
            ReportRequestDto request,
            NarrowedBaseAnalysisResultDto analysisResult,
            int userId)
        {
            Logger.Info($"Daraltılmış baz raporu oluşturuluyor");

            var outputPath = GetOutputPath(request, "DaraltilmisBaz_Raporu");
            var report = await CreateReportRecordAsync(request, userId, outputPath);

            try
            {
                if (request.Format == ReportFormat.PDF)
                    await GenerateNarrowedBasePdfAsync(outputPath, request, analysisResult);
                else if (request.Format == ReportFormat.Excel)
                    await GenerateNarrowedBaseExcelAsync(outputPath, request, analysisResult);

                await FinalizeReportAsync(report, outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Daraltılmış baz raporu oluşturma hatası");
                await FailReportAsync(report, ex.Message);
                throw;
            }

            return MapToDto(report);
        }

        public async Task<ReportDto> GenerateExecutiveSummaryAsync(ReportRequestDto request, int userId)
        {
            var outputPath = GetOutputPath(request, "Yonetici_Ozeti");
            var report = await CreateReportRecordAsync(request, userId, outputPath);

            try
            {
                var totalSites = await _unitOfWork.Sites.GetTotalCountAsync();
                var totalHts = await _unitOfWork.HtsRecords.GetTotalCountAsync();
                var recentAnalyses = (await _unitOfWork.AnalysisResults.GetRecentAsync(10)).ToList();

                if (request.Format == ReportFormat.PDF)
                    await GenerateExecutiveSummaryPdfAsync(outputPath, request, totalSites, totalHts, recentAnalyses);

                await FinalizeReportAsync(report, outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Yönetici özeti oluşturma hatası");
                await FailReportAsync(report, ex.Message);
                throw;
            }

            return MapToDto(report);
        }

        public Task<bool> OpenReportAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return Task.FromResult(false);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Rapor açma hatası: {filePath}");
                return Task.FromResult(false);
            }
        }

        private async Task GenerateHtsPdfAsync(string outputPath, ReportRequestDto request, HtsQueryResultDto data)
        {
            using (var doc = new Document(PageSize.A4, 36, 36, 54, 36))
            using (var writer = PdfWriter.GetInstance(doc, new FileStream(outputPath, FileMode.Create)))
            {
                doc.Open();

                // Header
                AddPdfHeader(doc, "HTS ANALİZ RAPORU", request);

                // Summary table
                var summaryTable = new PdfPTable(2) { WidthPercentage = 100 };
                summaryTable.SetWidths(new float[] { 1, 2 });
                AddTableRow(summaryTable, "Telefon Numarası", request.PhoneNumber ?? "-");
                AddTableRow(summaryTable, "IMEI", request.IMEI ?? "-");
                AddTableRow(summaryTable, "Başlangıç Tarihi", request.StartDate?.ToString("dd.MM.yyyy HH:mm") ?? "-");
                AddTableRow(summaryTable, "Bitiş Tarihi", request.EndDate?.ToString("dd.MM.yyyy HH:mm") ?? "-");
                AddTableRow(summaryTable, "Toplam Kayıt", data.TotalCount.ToString("N0"));
                AddTableRow(summaryTable, "Sorgu Süresi", data.QueryDuration.TotalSeconds.ToString("F2") + " saniye");
                doc.Add(summaryTable);

                doc.Add(new Paragraph("\n"));

                // Records table
                if (data.Records.Any())
                {
                    doc.Add(new Paragraph("HTS KAYITLARI", GetSectionFont()));
                    doc.Add(new Paragraph("\n"));

                    var recordTable = new PdfPTable(6) { WidthPercentage = 100 };
                    recordTable.SetWidths(new float[] { 2, 2, 2, 2, 1.5f, 1.5f });

                    AddTableHeader(recordTable, "Tarih/Saat", "Telefon", "Cell ID", "CGI", "Süre (sn)", "Tür");

                    foreach (var record in data.Records.Take(500))
                    {
                        AddTableRow(recordTable,
                            record.CallDateTime.ToString("dd.MM.yyyy HH:mm:ss"),
                            record.PhoneNumber ?? "-",
                            record.CellId ?? "-",
                            record.CGI ?? "-",
                            record.DurationSeconds.ToString(),
                            record.CallType ?? "-");
                    }

                    doc.Add(recordTable);

                    if (data.TotalCount > 500)
                        doc.Add(new Paragraph($"\n* Toplam {data.TotalCount:N0} kayıttan ilk 500 tanesi gösterilmektedir.", GetNoteFont()));
                }

                AddPdfFooter(doc);
                doc.Close();
            }

            await Task.CompletedTask;
        }

        private async Task GenerateHtsExcelAsync(string outputPath, ReportRequestDto request, HtsQueryResultDto data)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("HTS Kayıtları");

                // Header
                ws.Cells[1, 1].Value = "HTS ANALİZ RAPORU";
                ws.Cells[1, 1, 1, 8].Merge = true;
                ws.Cells[1, 1].Style.Font.Size = 16;
                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
                ws.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Column headers
                int row = 3;
                string[] headers = { "Tarih/Saat", "Telefon", "IMEI", "IMSI", "Cell ID", "CGI", "LAC", "Süre (sn)", "Tür", "Aranan" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[row, i + 1].Value = headers[i];
                    ws.Cells[row, i + 1].Style.Font.Bold = true;
                    ws.Cells[row, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                    ws.Cells[row, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                row++;
                foreach (var record in data.Records)
                {
                    ws.Cells[row, 1].Value = record.CallDateTime.ToString("dd.MM.yyyy HH:mm:ss");
                    ws.Cells[row, 2].Value = record.PhoneNumber;
                    ws.Cells[row, 3].Value = record.IMEI;
                    ws.Cells[row, 4].Value = record.IMSI;
                    ws.Cells[row, 5].Value = record.CellId;
                    ws.Cells[row, 6].Value = record.CGI;
                    ws.Cells[row, 7].Value = record.LAC;
                    ws.Cells[row, 8].Value = record.DurationSeconds;
                    ws.Cells[row, 9].Value = record.CallType;
                    ws.Cells[row, 10].Value = record.CalledNumber;

                    if (row % 2 == 0)
                    {
                        ws.Cells[row, 1, row, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, 1, row, 10].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(242, 242, 242));
                    }
                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                package.SaveAs(new FileInfo(outputPath));
            }

            await Task.CompletedTask;
        }

        private async Task GenerateDriveTestPdfAsync(string outputPath, ReportRequestDto request)
        {
            using (var doc = new Document(PageSize.A4.Rotate(), 36, 36, 54, 36))
            using (var writer = PdfWriter.GetInstance(doc, new FileStream(outputPath, FileMode.Create)))
            {
                doc.Open();
                AddPdfHeader(doc, "DRIVE TEST ANALİZ RAPORU", request);

                if (request.DriveTestId.HasValue)
                {
                    var stats = await _unitOfWork.DriveTests.GetStatisticsAsync(request.DriveTestId.Value);

                    var statsTable = new PdfPTable(2) { WidthPercentage = 100 };
                    statsTable.SetWidths(new float[] { 1, 2 });
                    AddTableRow(statsTable, "Toplam Ölçüm Noktası", stats.TotalPoints.ToString("N0"));
                    AddTableRow(statsTable, "Ortalama RSRP", $"{stats.AvgRSRP:F1} dBm");
                    AddTableRow(statsTable, "Min RSRP", $"{stats.MinRSRP:F1} dBm");
                    AddTableRow(statsTable, "Max RSRP", $"{stats.MaxRSRP:F1} dBm");
                    AddTableRow(statsTable, "Ortalama RSRQ", $"{stats.AvgRSRQ:F1} dB");
                    AddTableRow(statsTable, "Ortalama SINR", $"{stats.AvgSINR:F1} dB");
                    AddTableRow(statsTable, "Mükemmel Kapsama", $"{stats.ExcellentCoveragePercent:F1}%");
                    AddTableRow(statsTable, "İyi Kapsama", $"{stats.GoodCoveragePercent:F1}%");
                    AddTableRow(statsTable, "Orta Kapsama", $"{stats.FairCoveragePercent:F1}%");
                    AddTableRow(statsTable, "Zayıf Kapsama", $"{stats.PoorCoveragePercent:F1}%");
                    AddTableRow(statsTable, "Kapsama Yok", $"{stats.NoCoveragePercent:F1}%");
                    AddTableRow(statsTable, "Benzersiz PCI Sayısı", stats.UniquePCIs.ToString());
                    AddTableRow(statsTable, "Benzersiz Hücre Sayısı", stats.UniqueCells.ToString());
                    doc.Add(statsTable);
                }

                AddPdfFooter(doc);
                doc.Close();
            }

            await Task.CompletedTask;
        }

        private async Task GenerateDriveTestExcelAsync(string outputPath, ReportRequestDto request)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Drive Test");
                ws.Cells[1, 1].Value = "DRIVE TEST ANALİZ RAPORU";
                ws.Cells[1, 1, 1, 10].Merge = true;
                StyleHeaderCell(ws.Cells[1, 1]);

                if (request.DriveTestId.HasValue)
                {
                    var query = new DriveTestQueryDto { DriveTestId = request.DriveTestId, PageSize = 100000 };
                    var records = (await _unitOfWork.DriveTests.GetRecordsAsync(query)).ToList();

                    int row = 3;
                    string[] headers = { "Zaman", "Enlem", "Boylam", "RSRP", "RSRQ", "SINR", "RSSI", "PCI", "EARFCN", "Hız (km/h)", "Cell ID" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[row, i + 1].Value = headers[i];
                        StyleColumnHeader(ws.Cells[row, i + 1]);
                    }

                    row++;
                    foreach (var record in records)
                    {
                        ws.Cells[row, 1].Value = record.Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
                        ws.Cells[row, 2].Value = record.Latitude;
                        ws.Cells[row, 3].Value = record.Longitude;
                        ws.Cells[row, 4].Value = record.RSRP;
                        ws.Cells[row, 5].Value = record.RSRQ;
                        ws.Cells[row, 6].Value = record.SINR;
                        ws.Cells[row, 7].Value = record.RSSI;
                        ws.Cells[row, 8].Value = record.PCI;
                        ws.Cells[row, 9].Value = record.EARFCN;
                        ws.Cells[row, 10].Value = record.SpeedKmh;
                        ws.Cells[row, 11].Value = record.ServingCellId;
                        row++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                package.SaveAs(new FileInfo(outputPath));
            }

            await Task.CompletedTask;
        }

        private async Task GenerateNarrowedBasePdfAsync(
            string outputPath, ReportRequestDto request, NarrowedBaseAnalysisResultDto result)
        {
            using (var doc = new Document(PageSize.A4, 36, 36, 54, 36))
            using (var writer = PdfWriter.GetInstance(doc, new FileStream(outputPath, FileMode.Create)))
            {
                doc.Open();
                AddPdfHeader(doc, "DARALTILMIŞ BAZ ANALİZ RAPORU", request);

                // Confidence score
                var confidenceColor = result.ConfidenceLevel == ConfidenceLevel.High ? HighConfidenceColor
                    : result.ConfidenceLevel == ConfidenceLevel.Medium ? MediumConfidenceColor
                    : LowConfidenceColor;

                var confidenceTable = new PdfPTable(2) { WidthPercentage = 100 };
                confidenceTable.SetWidths(new float[] { 1, 2 });
                AddTableRow(confidenceTable, "Hedef", result.PhoneNumber ?? result.IMEI ?? "-");
                AddTableRow(confidenceTable, "Analiz Dönemi", $"{result.StartDate:dd.MM.yyyy HH:mm} - {result.EndDate:dd.MM.yyyy HH:mm}");
                AddTableRow(confidenceTable, "Toplam HTS Kaydı", result.TotalHtsRecords.ToString("N0"));
                AddTableRow(confidenceTable, "Güven Skoru", $"{result.ConfidenceScore}/100 ({result.ConfidenceLevel})");
                AddTableRow(confidenceTable, "Hareket Modeli", result.MovementPattern.ToString());
                AddTableRow(confidenceTable, "Toplam Mesafe", $"{result.TotalDistanceKm:F1} km");
                AddTableRow(confidenceTable, "Ortalama Hız", $"{result.AverageSpeedKmh:F1} km/h");
                AddTableRow(confidenceTable, "Analiz Süresi", $"{result.AnalysisDuration.TotalSeconds:F1} saniye");
                doc.Add(confidenceTable);

                doc.Add(new Paragraph("\n"));

                // Location estimates
                if (result.LocationEstimates.Any())
                {
                    doc.Add(new Paragraph("KONUM TAHMİNLERİ", GetSectionFont()));
                    doc.Add(new Paragraph("\n"));

                    var locTable = new PdfPTable(5) { WidthPercentage = 100 };
                    locTable.SetWidths(new float[] { 2, 1.5f, 1.5f, 1.5f, 1 });
                    AddTableHeader(locTable, "Site Kodu", "Enlem", "Boylam", "Yarıçap (km)", "Olasılık");

                    foreach (var est in result.LocationEstimates.Take(20))
                    {
                        AddTableRow(locTable,
                            est.SiteCode ?? "-",
                            est.CenterLatitude.ToString("F6"),
                            est.CenterLongitude.ToString("F6"),
                            est.RadiusKm.ToString("F2"),
                            $"{est.Probability:P0}");
                    }

                    doc.Add(locTable);
                }

                // Scoring details
                if (result.ScoringDetails.Any())
                {
                    doc.Add(new Paragraph("\n"));
                    doc.Add(new Paragraph("PUANLAMA DETAYLARI", GetSectionFont()));
                    doc.Add(new Paragraph("\n"));

                    var scoreTable = new PdfPTable(4) { WidthPercentage = 100 };
                    scoreTable.SetWidths(new float[] { 2, 1, 1, 1 });
                    AddTableHeader(scoreTable, "Parametre", "Ağırlık", "Ham Skor", "Ağırlıklı Skor");

                    foreach (var detail in result.ScoringDetails)
                    {
                        AddTableRow(scoreTable,
                            detail.Parameter,
                            $"{detail.Weight:P0}",
                            $"{detail.RawScore:F2}",
                            $"{detail.WeightedScore:F2}");
                    }

                    doc.Add(scoreTable);
                }

                // Warnings
                if (result.Warnings.Any())
                {
                    doc.Add(new Paragraph("\n"));
                    doc.Add(new Paragraph("UYARILAR", GetSectionFont()));
                    foreach (var warning in result.Warnings)
                        doc.Add(new Paragraph($"⚠ {warning}", GetNoteFont()));
                }

                // Disclaimer
                doc.Add(new Paragraph("\n\n"));
                doc.Add(new Paragraph(
                    "YASAL UYARI: Bu rapor yapay zeka destekli analiz sonuçlarını içermektedir. " +
                    "Sonuçlar kesin hüküm niteliği taşımaz; öneri, risk ve olasılık değerlendirmesi olarak değerlendirilmelidir.",
                    GetNoteFont()));

                AddPdfFooter(doc);
                doc.Close();
            }

            await Task.CompletedTask;
        }

        private async Task GenerateNarrowedBaseExcelAsync(
            string outputPath, ReportRequestDto request, NarrowedBaseAnalysisResultDto result)
        {
            using (var package = new ExcelPackage())
            {
                // Summary sheet
                var summaryWs = package.Workbook.Worksheets.Add("Özet");
                summaryWs.Cells[1, 1].Value = "DARALTILMIŞ BAZ ANALİZ RAPORU";
                summaryWs.Cells[1, 1, 1, 4].Merge = true;
                StyleHeaderCell(summaryWs.Cells[1, 1]);

                int row = 3;
                summaryWs.Cells[row, 1].Value = "Hedef"; summaryWs.Cells[row, 2].Value = result.PhoneNumber ?? result.IMEI; row++;
                summaryWs.Cells[row, 1].Value = "Güven Skoru"; summaryWs.Cells[row, 2].Value = result.ConfidenceScore; row++;
                summaryWs.Cells[row, 1].Value = "Güven Seviyesi"; summaryWs.Cells[row, 2].Value = result.ConfidenceLevel.ToString(); row++;
                summaryWs.Cells[row, 1].Value = "Toplam HTS Kaydı"; summaryWs.Cells[row, 2].Value = result.TotalHtsRecords; row++;
                summaryWs.Cells[row, 1].Value = "Hareket Modeli"; summaryWs.Cells[row, 2].Value = result.MovementPattern.ToString(); row++;
                summaryWs.Cells[row, 1].Value = "Toplam Mesafe (km)"; summaryWs.Cells[row, 2].Value = result.TotalDistanceKm; row++;

                // Location estimates sheet
                var locWs = package.Workbook.Worksheets.Add("Konum Tahminleri");
                locWs.Cells[1, 1].Value = "Site Kodu";
                locWs.Cells[1, 2].Value = "Enlem";
                locWs.Cells[1, 3].Value = "Boylam";
                locWs.Cells[1, 4].Value = "Yarıçap (km)";
                locWs.Cells[1, 5].Value = "Olasılık";
                locWs.Cells[1, 6].Value = "Drive Test Onayı";

                int locRow = 2;
                foreach (var est in result.LocationEstimates)
                {
                    locWs.Cells[locRow, 1].Value = est.SiteCode;
                    locWs.Cells[locRow, 2].Value = est.CenterLatitude;
                    locWs.Cells[locRow, 3].Value = est.CenterLongitude;
                    locWs.Cells[locRow, 4].Value = est.RadiusKm;
                    locWs.Cells[locRow, 5].Value = est.Probability;
                    locWs.Cells[locRow, 6].Value = est.HasDriveTestConfirmation ? "Evet" : "Hayır";
                    locRow++;
                }

                locWs.Cells[locWs.Dimension.Address].AutoFitColumns();
                summaryWs.Cells[summaryWs.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(outputPath));
            }

            await Task.CompletedTask;
        }

        private async Task GenerateExecutiveSummaryPdfAsync(
            string outputPath, ReportRequestDto request,
            int totalSites, long totalHts, List<AnalysisResult> recentAnalyses)
        {
            using (var doc = new Document(PageSize.A4, 36, 36, 54, 36))
            using (var writer = PdfWriter.GetInstance(doc, new FileStream(outputPath, FileMode.Create)))
            {
                doc.Open();
                AddPdfHeader(doc, "YÖNETİCİ ÖZETİ", request);

                var statsTable = new PdfPTable(2) { WidthPercentage = 100 };
                statsTable.SetWidths(new float[] { 1, 2 });
                AddTableRow(statsTable, "Toplam Baz İstasyonu", totalSites.ToString("N0"));
                AddTableRow(statsTable, "Toplam HTS Kaydı", totalHts.ToString("N0"));
                AddTableRow(statsTable, "Son Analiz Sayısı", recentAnalyses.Count.ToString());
                AddTableRow(statsTable, "Rapor Tarihi", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                doc.Add(statsTable);

                AddPdfFooter(doc);
                doc.Close();
            }

            await Task.CompletedTask;
        }

        // Helper methods
        private void AddPdfHeader(Document doc, string title, ReportRequestDto request)
        {
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, HeaderColor);
            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);

            var titlePara = new Paragraph(title, titleFont) { Alignment = Element.ALIGN_CENTER };
            doc.Add(titlePara);

            var orgName = request.OrganizationName ?? "Cellular Intelligence Analyzer";
            doc.Add(new Paragraph(orgName, subtitleFont) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph($"Oluşturma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}", subtitleFont) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new LineSeparator());
            doc.Add(new Paragraph("\n"));
        }

        private void AddPdfFooter(Document doc)
        {
            doc.Add(new Paragraph("\n\n"));
            doc.Add(new LineSeparator());
            var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
            doc.Add(new Paragraph($"Cellular Intelligence Analyzer v{AppConstants.AppVersion} | Gizlilik Derecesi: Gizli", footerFont)
            { Alignment = Element.ALIGN_CENTER });
        }

        private void AddTableHeader(PdfPTable table, params string[] headers)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, font))
                {
                    BackgroundColor = TableHeaderColor,
                    Padding = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };
                table.AddCell(cell);
            }
        }

        private void AddTableRow(PdfPTable table, params string[] values)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
            bool isHeader = table.Rows.Count % 2 == 0;

            foreach (var value in values)
            {
                var cell = new PdfPCell(new Phrase(value ?? "-", font))
                {
                    BackgroundColor = isHeader ? BaseColor.WHITE : TableAltRowColor,
                    Padding = 4
                };
                table.AddCell(cell);
            }
        }

        private Font GetSectionFont() => FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, SubHeaderColor);
        private Font GetNoteFont() => FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, BaseColor.GRAY);

        private void StyleHeaderCell(ExcelRange cell)
        {
            cell.Style.Font.Size = 14;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        private void StyleColumnHeader(ExcelRange cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        private string GetOutputPath(ReportRequestDto request, string prefix)
        {
            var dir = request.OutputPath ?? AppConstants.ReportDirectory;
            Directory.CreateDirectory(dir);
            var ext = request.Format == ReportFormat.PDF ? ".pdf"
                : request.Format == ReportFormat.Excel ? ".xlsx"
                : request.Format == ReportFormat.CSV ? ".csv" : ".docx";
            return Path.Combine(dir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
        }

        private async Task<Report> CreateReportRecordAsync(ReportRequestDto request, int userId, string outputPath)
        {
            var report = new Report
            {
                Title = request.Title ?? "Rapor",
                Description = request.Description,
                ReportType = (int)request.Type,
                ReportFormat = (int)request.Format,
                FilePath = outputPath,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Parameters = JsonConvert.SerializeObject(request),
                IsGenerated = false
            };

            await _unitOfWork.Reports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            return report;
        }

        private async Task FinalizeReportAsync(Report report, string outputPath)
        {
            report.IsGenerated = true;
            report.FileSizeBytes = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task FailReportAsync(Report report, string errorMessage)
        {
            report.IsGenerated = false;
            report.ErrorMessage = errorMessage;
            await _unitOfWork.SaveChangesAsync();
        }

        private ReportDto MapToDto(Report report)
        {
            return new ReportDto
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                Type = (ReportType)report.ReportType,
                Format = (ReportFormat)report.ReportFormat,
                FilePath = report.FilePath,
                FileSizeBytes = report.FileSizeBytes,
                CreatedAt = report.CreatedAt,
                CreatedByUserId = report.CreatedByUserId,
                IsGenerated = report.IsGenerated,
                ErrorMessage = report.ErrorMessage
            };
        }
    }
}
