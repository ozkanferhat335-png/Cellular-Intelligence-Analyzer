using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIA.Core.DTOs;
using CIA.Data.Entities;

namespace CIA.Data.Repositories
{
    public interface ISiteRepository : IRepository<Site>
    {
        Task<IEnumerable<Site>> GetSitesWithSectorsAsync(SiteFilterDto filter);
        Task<Site> GetSiteByCodeAsync(string siteCode);
        Task<IEnumerable<Site>> GetSitesInBoundingBoxAsync(double minLat, double maxLat, double minLon, double maxLon);
        Task<int> GetTotalCountAsync();
        Task<IEnumerable<string>> GetRegionsAsync();
        Task<IEnumerable<string>> GetCitiesAsync(string region = null);
    }

    public interface ISectorRepository : IRepository<Sector>
    {
        Task<IEnumerable<Sector>> GetSectorsBySiteIdAsync(int siteId);
        Task<IEnumerable<Sector>> GetSectorsWithCellsAsync(int siteId);
        Task<Sector> GetSectorWithCellsAsync(int sectorId);
    }

    public interface ICellRepository : IRepository<Cell>
    {
        Task<Cell> GetCellByCellIdAsync(string cellId);
        Task<Cell> GetCellByCgiAsync(string cgi);
        Task<IEnumerable<Cell>> GetCellsByPciAsync(int pci);
        Task<IEnumerable<Cell>> GetCellsBySectorIdAsync(int sectorId);
        Task<Cell> GetCellWithSectorAndSiteAsync(string cellId);
        Task<IEnumerable<Cell>> GetAllCellsWithLocationAsync();
    }

    public interface IHtsRepository : IRepository<HtsRecord>
    {
        Task<HtsQueryResultDto> QueryAsync(HtsQueryDto query);
        Task<IEnumerable<HtsRecord>> GetMovementHistoryAsync(string phoneNumber, DateTime startDate, DateTime endDate);
        Task<IEnumerable<HtsRecord>> GetByImeiAsync(string imei, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<HtsRecord>> GetByImsiAsync(string imsi, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<HtsRecord>> GetByCellIdAsync(string cellId, DateTime? startDate = null, DateTime? endDate = null);
        Task<long> GetTotalCountAsync();
        Task<long> BulkInsertAsync(IEnumerable<HtsRecord> records, IProgress<int> progress = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetDistinctPhoneNumbersAsync(int limit = 1000);
        Task<IEnumerable<string>> GetDistinctCellIdsAsync();
        Task DeleteByImportedFileIdAsync(int importedFileId);
    }

    public interface IDriveTestRepository : IRepository<DriveTest>
    {
        Task<IEnumerable<DriveTest>> GetAllWithStatsAsync();
        Task<DriveTest> GetWithRecordsAsync(int driveTestId);
        Task<IEnumerable<DriveTestRecord>> GetRecordsAsync(DriveTestQueryDto query);
        Task<long> GetRecordCountAsync(int driveTestId);
        Task<long> BulkInsertRecordsAsync(IEnumerable<DriveTestRecord> records, IProgress<int> progress = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<DriveTestRecord>> GetRecordsInBoundingBoxAsync(int driveTestId, double minLat, double maxLat, double minLon, double maxLon);
        Task<IEnumerable<DriveTestRecord>> GetRecordsByCellIdAsync(string cellId);
        Task<IEnumerable<DriveTestRecord>> GetRecordsByPciAsync(int pci);
        Task<DriveTestStatisticsDto> GetStatisticsAsync(int driveTestId);
    }

    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetWithRolesAsync(int userId);
        Task<IEnumerable<User>> GetAllWithRolesAsync();
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
        Task UpdateLastLoginAsync(int userId);
        Task IncrementFailedLoginAsync(int userId);
        Task ResetFailedLoginAsync(int userId);
        Task LockUserAsync(int userId, DateTime lockUntil);
    }

    public interface IAnalysisRepository : IRepository<AnalysisResult>
    {
        Task<IEnumerable<AnalysisResult>> GetByTypeAsync(int analysisType);
        Task<IEnumerable<AnalysisResult>> GetRecentAsync(int count = 10);
        Task<IEnumerable<AnalysisResult>> GetByUserAsync(int userId);
    }

    public interface IReportRepository : IRepository<Report>
    {
        Task<IEnumerable<Report>> GetByTypeAsync(int reportType);
        Task<IEnumerable<Report>> GetRecentAsync(int count = 10);
        Task<IEnumerable<Report>> GetByUserAsync(int userId);
    }

    public interface ISettingRepository : IRepository<Setting>
    {
        Task<Setting> GetByKeyAsync(string key);
        Task<string> GetValueAsync(string key, string defaultValue = null);
        Task SetValueAsync(string key, string value);
        Task<IEnumerable<Setting>> GetByCategoryAsync(string category);
    }

    public interface IImportedFileRepository : IRepository<ImportedFile>
    {
        Task<IEnumerable<ImportedFile>> GetByTypeAsync(string fileType);
        Task<IEnumerable<ImportedFile>> GetRecentAsync(int count = 20);
        Task UpdateStatusAsync(int fileId, int status, int processedRows = 0, int failedRows = 0, string errorMessage = null);
    }

    public interface ISystemLogRepository : IRepository<SystemLog>
    {
        Task<IEnumerable<SystemLog>> GetRecentAsync(int count = 100);
        Task<IEnumerable<SystemLog>> GetByLevelAsync(string level, int count = 100);
        Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task DeleteOldLogsAsync(int retentionDays);
        Task<long> GetTotalCountAsync();
    }

    public interface ICoverageModelRepository : IRepository<CoverageModel>
    {
        Task<CoverageModel> GetBySectorIdAsync(int sectorId);
        Task<IEnumerable<CoverageModel>> GetAllWithSectorInfoAsync();
        Task UpsertAsync(CoverageModel model);
    }
}
