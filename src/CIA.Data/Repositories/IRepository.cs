using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CIA.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task<int> SaveChangesAsync();
    }

    public interface IUnitOfWork : IDisposable
    {
        ISiteRepository Sites { get; }
        ISectorRepository Sectors { get; }
        ICellRepository Cells { get; }
        IHtsRepository HtsRecords { get; }
        IDriveTestRepository DriveTests { get; }
        IUserRepository Users { get; }
        IAnalysisRepository AnalysisResults { get; }
        IReportRepository Reports { get; }
        ISettingRepository Settings { get; }
        IImportedFileRepository ImportedFiles { get; }
        ISystemLogRepository SystemLogs { get; }
        ICoverageModelRepository CoverageModels { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
