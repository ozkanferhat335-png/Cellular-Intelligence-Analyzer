using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIA.Data.Context;
using CIA.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CIA.Data.Repositories.Implementations
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(CiaDbContext context) : base(context) { }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await Context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await Context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetWithRolesAsync(int userId)
        {
            return await Context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IEnumerable<User>> GetAllWithRolesAsync()
        {
            return await Context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await Context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await Context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await Context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
                await Context.SaveChangesAsync();
            }
        }

        public async Task IncrementFailedLoginAsync(int userId)
        {
            var user = await Context.Users.FindAsync(userId);
            if (user != null)
            {
                user.FailedLoginAttempts++;
                await Context.SaveChangesAsync();
            }
        }

        public async Task ResetFailedLoginAsync(int userId)
        {
            var user = await Context.Users.FindAsync(userId);
            if (user != null)
            {
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
                await Context.SaveChangesAsync();
            }
        }

        public async Task LockUserAsync(int userId, DateTime lockUntil)
        {
            var user = await Context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LockedUntil = lockUntil;
                await Context.SaveChangesAsync();
            }
        }
    }

    public class SettingRepository : BaseRepository<Setting>, ISettingRepository
    {
        public SettingRepository(CiaDbContext context) : base(context) { }

        public async Task<Setting> GetByKeyAsync(string key)
        {
            return await Context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        }

        public async Task<string> GetValueAsync(string key, string defaultValue = null)
        {
            var setting = await Context.Settings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public async Task SetValueAsync(string key, string value)
        {
            var setting = await Context.Settings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new Setting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
                await Context.Settings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }
            await Context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Setting>> GetByCategoryAsync(string category)
        {
            return await Context.Settings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();
        }
    }

    public class ImportedFileRepository : BaseRepository<ImportedFile>, IImportedFileRepository
    {
        public ImportedFileRepository(CiaDbContext context) : base(context) { }

        public async Task<IEnumerable<ImportedFile>> GetByTypeAsync(string fileType)
        {
            return await Context.ImportedFiles
                .Where(f => f.FileType == fileType)
                .OrderByDescending(f => f.ImportedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ImportedFile>> GetRecentAsync(int count = 20)
        {
            return await Context.ImportedFiles
                .OrderByDescending(f => f.ImportedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(int fileId, int status, int processedRows = 0, int failedRows = 0, string errorMessage = null)
        {
            var file = await Context.ImportedFiles.FindAsync(fileId);
            if (file != null)
            {
                file.Status = status;
                if (processedRows > 0) file.ProcessedRows = processedRows;
                if (failedRows > 0) file.FailedRows = failedRows;
                if (errorMessage != null) file.ErrorMessage = errorMessage;
                await Context.SaveChangesAsync();
            }
        }
    }

    public class SystemLogRepository : BaseRepository<SystemLog>, ISystemLogRepository
    {
        public SystemLogRepository(CiaDbContext context) : base(context) { }

        public async Task<IEnumerable<SystemLog>> GetRecentAsync(int count = 100)
        {
            return await Context.SystemLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetByLevelAsync(string level, int count = 100)
        {
            return await Context.SystemLogs
                .Where(l => l.Level == level)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await Context.SystemLogs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task DeleteOldLogsAsync(int retentionDays)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var oldLogs = await Context.SystemLogs
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync();
            Context.SystemLogs.RemoveRange(oldLogs);
            await Context.SaveChangesAsync();
        }

        public async Task<long> GetTotalCountAsync()
        {
            return await Context.SystemLogs.LongCountAsync();
        }
    }

    public class AnalysisRepository : BaseRepository<AnalysisResult>, IAnalysisRepository
    {
        public AnalysisRepository(CiaDbContext context) : base(context) { }

        public async Task<IEnumerable<AnalysisResult>> GetByTypeAsync(int analysisType)
        {
            return await Context.AnalysisResults
                .Where(a => a.AnalysisType == analysisType && !a.IsArchived)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AnalysisResult>> GetRecentAsync(int count = 10)
        {
            return await Context.AnalysisResults
                .Where(a => !a.IsArchived)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<AnalysisResult>> GetByUserAsync(int userId)
        {
            return await Context.AnalysisResults
                .Where(a => a.CreatedByUserId == userId && !a.IsArchived)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }

    public class ReportRepository : BaseRepository<Report>, IReportRepository
    {
        public ReportRepository(CiaDbContext context) : base(context) { }

        public async Task<IEnumerable<Report>> GetByTypeAsync(int reportType)
        {
            return await Context.Reports
                .Where(r => r.ReportType == reportType)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetRecentAsync(int count = 10)
        {
            return await Context.Reports
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetByUserAsync(int userId)
        {
            return await Context.Reports
                .Where(r => r.CreatedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }

    public class CoverageModelRepository : BaseRepository<CoverageModel>, ICoverageModelRepository
    {
        public CoverageModelRepository(CiaDbContext context) : base(context) { }

        public async Task<CoverageModel> GetBySectorIdAsync(int sectorId)
        {
            return await Context.CoverageModels
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .FirstOrDefaultAsync(c => c.SectorId == sectorId);
        }

        public async Task<IEnumerable<CoverageModel>> GetAllWithSectorInfoAsync()
        {
            return await Context.CoverageModels
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .ToListAsync();
        }

        public async Task UpsertAsync(CoverageModel model)
        {
            var existing = await Context.CoverageModels
                .FirstOrDefaultAsync(c => c.SectorId == model.SectorId);

            if (existing == null)
            {
                await Context.CoverageModels.AddAsync(model);
            }
            else
            {
                existing.EstimatedRadiusKm = model.EstimatedRadiusKm;
                existing.CoveragePolygonJson = model.CoveragePolygonJson;
                existing.EstimatedRsrpAtEdge = model.EstimatedRsrpAtEdge;
                existing.TerrainType = model.TerrainType;
                existing.ModeledAt = DateTime.UtcNow;
                existing.IsValidatedByDriveTest = model.IsValidatedByDriveTest;
                existing.ValidationAccuracy = model.ValidationAccuracy;
            }

            await Context.SaveChangesAsync();
        }
    }
}
