using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using VoteRightWebApp.Models;

namespace VoteRightWebApp.Services
{
    public class LocationDataService : ILocationDataService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;
        private readonly string _excelPath;

        public LocationDataService(IWebHostEnvironment environment, IMemoryCache cache)
        {
            _environment = environment;
            _cache = cache;
            _excelPath = Path.Combine(_environment.ContentRootPath, "Data", "constituencies.xlsx");
        }

        public List<string> GetDistricts()
        {
            var cacheKey = "DistrictsCache";
            var lastWrite = File.Exists(_excelPath) ? File.GetLastWriteTimeUtc(_excelPath) : DateTime.MinValue;
            if (!_cache.TryGetValue<(List<string>, DateTime)>(cacheKey, out var cache) || cache.Item2 != lastWrite)
            {
                var districts = ReadAssembliesFromExcel().Select(a => a.District).Distinct().ToList();
                _cache.Set(cacheKey, (districts, lastWrite));
                return districts;
            }
            return cache.Item1;
        }

        public List<Assembly> GetAssembliesByDistrict(string district)
        {
            var cacheKey = $"AssembliesByDistrict{district}Cache";
            var lastWrite = File.Exists(_excelPath) ? File.GetLastWriteTimeUtc(_excelPath) : DateTime.MinValue;
            if (!_cache.TryGetValue<(List<Assembly>, DateTime)>(cacheKey, out var cache) || cache.Item2 != lastWrite)
            {
                var assemblies = ReadAssembliesFromExcel().Where(a => a.District == district).ToList();
                _cache.Set(cacheKey, (assemblies, lastWrite));
                return assemblies;
            }
            return cache.Item1;
        }

        // --- Excel reading helpers ---
        private List<Assembly> ReadAssembliesFromExcel()
        {
            var assemblies = new List<Assembly>();
            using (var workbook = new XLWorkbook(_excelPath))
            {
                var ws = workbook.Worksheets.First();
                var rows = ws.RangeUsed()?.RowsUsed().Skip(1);
                foreach (var row in rows?? Enumerable.Empty<IXLRangeRow>())
                {
                    var assemblyNumber = row.Cell(1).GetString();
                    var assembly = row.Cell(2).GetString();
                    var district = row.Cell(3).GetString();
                    var booth = row.Cell(4).GetValue<int>();
                    if (!string.IsNullOrWhiteSpace(assembly) && booth > 0)
                    {
                        assemblies.Add(new Assembly
                        {
                            Number = assemblyNumber,
                            Name = assembly,
                            District = district,
                            BoothCount = booth
                        });
                    }
                }
            }
            return assemblies;
        }
    }
}