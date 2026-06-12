using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIA.Core.DTOs;
using CIA.Data.Context;
using CIA.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CIA.Data.Repositories.Implementations
{
    public class SiteRepository : BaseRepository<Site>, ISiteRepository
    {
        public SiteRepository(CiaDbContext context) : base(context) { }

        public async Task<IEnumerable<Site>> GetSitesWithSectorsAsync(SiteFilterDto filter)
        {
            var query = Context.Sites
                .Include(s => s.Sectors)
                .ThenInclude(sec => sec.Cells)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.ToLower();
                query = query.Where(s =>
                    s.SiteCode.ToLower().Contains(search) ||
                    s.SiteName.ToLower().Contains(search) ||
                    s.City.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(filter.Region))
                query = query.Where(s => s.Region == filter.Region);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(s => s.City == filter.City);

            if (filter.Status.HasValue)
                query = query.Where(s => s.Status == (int)filter.Status.Value);

            if (filter.Technology.HasValue)
                query = query.Where(s => s.Sectors.Any(sec => sec.Technology == (int)filter.Technology.Value));

            if (filter.Band.HasValue)
                query = query.Where(s => s.Sectors.Any(sec => sec.Band == (int)filter.Band.Value));

            if (filter.MinLatitude.HasValue) query = query.Where(s => s.Latitude >= filter.MinLatitude.Value);
            if (filter.MaxLatitude.HasValue) query = query.Where(s => s.Latitude <= filter.MaxLatitude.Value);
            if (filter.MinLongitude.HasValue) query = query.Where(s => s.Longitude >= filter.MinLongitude.Value);
            if (filter.MaxLongitude.HasValue) query = query.Where(s => s.Longitude <= filter.MaxLongitude.Value);

            return await query
                .OrderBy(s => s.SiteCode)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        public async Task<Site> GetSiteByCodeAsync(string siteCode)
        {
            return await Context.Sites
                .Include(s => s.Sectors)
                .ThenInclude(sec => sec.Cells)
                .FirstOrDefaultAsync(s => s.SiteCode == siteCode);
        }

        public async Task<IEnumerable<Site>> GetSitesInBoundingBoxAsync(
            double minLat, double maxLat, double minLon, double maxLon)
        {
            return await Context.Sites
                .Where(s => s.Latitude >= minLat && s.Latitude <= maxLat &&
                            s.Longitude >= minLon && s.Longitude <= maxLon)
                .Include(s => s.Sectors)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await Context.Sites.CountAsync();
        }

        public async Task<IEnumerable<string>> GetRegionsAsync()
        {
            return await Context.Sites
                .Where(s => !string.IsNullOrEmpty(s.Region))
                .Select(s => s.Region)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetCitiesAsync(string region = null)
        {
            var query = Context.Sites.Where(s => !string.IsNullOrEmpty(s.City));
            if (!string.IsNullOrEmpty(region))
                query = query.Where(s => s.Region == region);

            return await query
                .Select(s => s.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }

    public class SectorRepository : BaseRepository<Sector>, ISectorRepository
    {
        public SectorRepository(CiaDbContext context) : base(context) { }

        public async Task<IEnumerable<Sector>> GetSectorsBySiteIdAsync(int siteId)
        {
            return await Context.Sectors
                .Where(s => s.SiteId == siteId)
                .OrderBy(s => s.SectorIndex)
                .ToListAsync();
        }

        public async Task<IEnumerable<Sector>> GetSectorsWithCellsAsync(int siteId)
        {
            return await Context.Sectors
                .Where(s => s.SiteId == siteId)
                .Include(s => s.Cells)
                .OrderBy(s => s.SectorIndex)
                .ToListAsync();
        }

        public async Task<Sector> GetSectorWithCellsAsync(int sectorId)
        {
            return await Context.Sectors
                .Include(s => s.Cells)
                .Include(s => s.Site)
                .FirstOrDefaultAsync(s => s.Id == sectorId);
        }
    }

    public class CellRepository : BaseRepository<Cell>, ICellRepository
    {
        public CellRepository(CiaDbContext context) : base(context) { }

        public async Task<Cell> GetCellByCellIdAsync(string cellId)
        {
            return await Context.Cells
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .FirstOrDefaultAsync(c => c.CellId == cellId);
        }

        public async Task<Cell> GetCellByCgiAsync(string cgi)
        {
            return await Context.Cells
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .FirstOrDefaultAsync(c => c.CGI == cgi);
        }

        public async Task<IEnumerable<Cell>> GetCellsByPciAsync(int pci)
        {
            return await Context.Cells
                .Where(c => c.PCI == pci)
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cell>> GetCellsBySectorIdAsync(int sectorId)
        {
            return await Context.Cells
                .Where(c => c.SectorId == sectorId)
                .ToListAsync();
        }

        public async Task<Cell> GetCellWithSectorAndSiteAsync(string cellId)
        {
            return await Context.Cells
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .FirstOrDefaultAsync(c => c.CellId == cellId || c.CGI == cellId);
        }

        public async Task<IEnumerable<Cell>> GetAllCellsWithLocationAsync()
        {
            return await Context.Cells
                .Include(c => c.Sector)
                .ThenInclude(s => s.Site)
                .Where(c => c.IsActive)
                .ToListAsync();
        }
    }
}
