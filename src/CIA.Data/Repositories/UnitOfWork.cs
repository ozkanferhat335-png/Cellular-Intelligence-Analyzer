using System;
using System.Threading.Tasks;
using CIA.Data.Context;
using CIA.Data.Repositories.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace CIA.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CiaDbContext _context;
        private readonly string _connectionString;
        private IDbContextTransaction _transaction;

        private ISiteRepository _sites;
        private ISectorRepository _sectors;
        private ICellRepository _cells;
        private IHtsRepository _htsRecords;
        private IDriveTestRepository _driveTests;
        private IUserRepository _users;
        private IAnalysisRepository _analysisResults;
        private IReportRepository _reports;
        private ISettingRepository _settings;
        private IImportedFileRepository _importedFiles;
        private ISystemLogRepository _systemLogs;
        private ICoverageModelRepository _coverageModels;

        public UnitOfWork(CiaDbContext context, string connectionString)
        {
            _context = context;
            _connectionString = connectionString;
        }

        public ISiteRepository Sites => _sites ?? (_sites = new SiteRepository(_context));
        public ISectorRepository Sectors => _sectors ?? (_sectors = new SectorRepository(_context));
        public ICellRepository Cells => _cells ?? (_cells = new CellRepository(_context));
        public IHtsRepository HtsRecords => _htsRecords ?? (_htsRecords = new HtsRepository(_context, _connectionString));
        public IDriveTestRepository DriveTests => _driveTests ?? (_driveTests = new DriveTestRepository(_context, _connectionString));
        public IUserRepository Users => _users ?? (_users = new UserRepository(_context));
        public IAnalysisRepository AnalysisResults => _analysisResults ?? (_analysisResults = new AnalysisRepository(_context));
        public IReportRepository Reports => _reports ?? (_reports = new ReportRepository(_context));
        public ISettingRepository Settings => _settings ?? (_settings = new SettingRepository(_context));
        public IImportedFileRepository ImportedFiles => _importedFiles ?? (_importedFiles = new ImportedFileRepository(_context));
        public ISystemLogRepository SystemLogs => _systemLogs ?? (_systemLogs = new SystemLogRepository(_context));
        public ICoverageModelRepository CoverageModels => _coverageModels ?? (_coverageModels = new CoverageModelRepository(_context));

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
