using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using VoteRightWebApp.Models;
using VoteRightWebApp.Services;

namespace VoteRightWebApp.Controllers
{
    public class FileDownloadController : Controller
    {
        private readonly IS3Service _s3Service;
        private readonly ILogger<FileDownloadController> _logger;
        private readonly ILocationDataService _locationDataService;
        private readonly DatabaseService _databaseService;

        public FileDownloadController(IS3Service s3Service, ILogger<FileDownloadController> logger, ILocationDataService locationDataService, DatabaseService databaseService)
        {
            _s3Service = s3Service;
            _logger = logger;
            _databaseService = databaseService;
            _locationDataService = locationDataService;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is logged in
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("SignIn", "Home");
            }

            // Prevent caching of this page
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            var userDistrict = HttpContext.Session.GetString("UserDistrict")?? string.Empty;
            ViewBag.Assemblies = _locationDataService.GetAssembliesByDistrict(userDistrict);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Download([FromBody] FileDownloadRequest request)
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(userName) || userId == null)
                return Json(new { success = false, message = "Session expired. Please sign in again." });

            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.AssemblyNumber))
            {
                return Json(new { success = false, message = "Assembly selection is required. Please select an Assembly to download data." });
            }

            _logger.LogInformation("Starting CSV download:Assembly={Assembly}, Booths={Booths}",
                request.AssemblyNumber, request.BoothRange != null ? request.BoothRange : "All");

            // Validate assembly belongs to the user's district before proceeding
            var userDistrict = HttpContext.Session.GetString("UserDistrict") ?? string.Empty;
            var assemblies = _locationDataService.GetAssembliesByDistrict(userDistrict) ?? new List<Assembly>();
            if (assemblies == null || !assemblies.Any(a => a.Number == request.AssemblyNumber))
            {
                _logger.LogWarning("User {User} attempted download for invalid assembly {Assembly} in district {District}", userName, request.AssemblyNumber, userDistrict);
                return Json(new { success = false, message = "Selected assembly is not valid for your district." });
            }

            // Pre-parse and validate booth range if provided to catch format errors early
            List<int>? parsedBooths = null;
            if (!string.IsNullOrWhiteSpace(request.BoothRange))
            {
                try
                {
                    parsedBooths = ParseBoothNumbers(request.BoothRange);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("Invalid booth range provided by user {User}: {Range}", userName, request.BoothRange);
                    return Json(new { success = false, message = ex.Message });
                }

                // Validate requested booth numbers against assembly's known booth count
                var assemblyObj = assemblies.FirstOrDefault(a => a.Number == request.AssemblyNumber);
                if (assemblyObj != null && parsedBooths.Any())
                {
                    var maxBooth = assemblyObj.BoothCount;
                    var invalid = parsedBooths.Where(b => b < 1 || b > maxBooth).ToList();
                    if (invalid.Any())
                    {
                        _logger.LogInformation("User {User} requested booths outside available range for assembly {Assembly}. Requested: {Requested}, Available: 1-{Max}", userName, request.AssemblyNumber, string.Join(",", invalid), maxBooth);
                        return Json(new { success = false, message = $"Requested booth number(s) out of range. Available booths for assembly {request.AssemblyNumber} are 1 to {maxBooth}." });
                    }
                }
            }

            try
            {
                // Download CSV stream (filtered or full)
                Stream stream = (parsedBooths != null)
                    ? await _s3Service.DownloadAssemblyFileByBoothsAsync(request.AssemblyNumber, parsedBooths)
                    : await _s3Service.DownloadAssemblyFileAsync(request.AssemblyNumber);

                // Log download to database (minimal queries)
                var userPhone = HttpContext.Session.GetString("UserPhone");
                var deviceType = GetDeviceType(Request.Headers["User-Agent"].ToString());


                await _databaseService.AddDownloadAsync(new Download
                {
                    UserId = userId ?? 0,
                    Assembly = request.AssemblyNumber,
                    Booths = request.BoothRange,
                    DeviceType = deviceType,
                    DownloadedAt = DateTime.UtcNow
                });


                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("CSV download completed in {Duration}ms, Size={Size} bytes", duration, stream.Length);

                // Ensure stream is rewound before returning
                if (stream.CanSeek) stream.Position = 0;

                // Use AssemblyName if provided, otherwise fall back to AssemblyNumber for filename and logging
                var assemblyDisplayName = string.IsNullOrWhiteSpace(request.AssemblyName) ? request.AssemblyNumber : request.AssemblyName;

                // Build a filename (include booth range if provided) and do a simple sanitize
                var filename = request.BoothRange != null
                    ? $"{request.AssemblyNumber}_{assemblyDisplayName}_{request.BoothRange}.csv"
                    : $"{request.AssemblyNumber}_{assemblyDisplayName}.csv";
                filename = filename.Replace("\"", "").Replace(" ", "_");

                // Return the CSV and set Content-Disposition header with the filename
                return File(stream, "text/csv", filename);
            }
            catch (VoteRightWebApp.Services.BoothsNotFoundException ex)
            {
                // Friendly error when requested booth(s) were not found in the CSV
                _logger.LogInformation("No matching booths for Assembly {Assembly}: {Message}", request.AssemblyNumber, ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "Error downloading filtered CSV after {Duration}ms: {Message}", duration, ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }


        // Helper: Detect device type
        private static string GetDeviceType(string userAgent)
        {
            if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)) return "Mobile";
            if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)) return "Tablet";
            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Laptop/Desktop";
            return "Unknown";
        }

        private List<int> ParseBoothNumbers(string boothNumberRange)
        {
            if (string.IsNullOrWhiteSpace(boothNumberRange))
                throw new ArgumentException("Booth number range is required.", nameof(boothNumberRange));

            // Normalize and split (accept single number or range like "12-45")
            var parts = boothNumberRange.Split('-').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (parts.Length == 1)
            {
                if (!int.TryParse(parts[0], out var single))
                    throw new ArgumentException("Invalid booth number specified. Please provide numeric values.", nameof(boothNumberRange));
                return new List<int> { single };
            }
            else if (parts.Length == 2)
            {
                if (!int.TryParse(parts[0], out var start) || !int.TryParse(parts[1], out var end))
                    throw new ArgumentException("Invalid booth range specified. Please use the format 'start-end' with numeric values.", nameof(boothNumberRange));
                if (end < start)
                    throw new ArgumentException("Invalid booth range: end must be greater than or equal to start.", nameof(boothNumberRange));
                return Enumerable.Range(start, end - start + 1).ToList();
            }
            else
            {
                throw new ArgumentException("Invalid booth range format. Use 'start' or 'start-end'.", nameof(boothNumberRange));
            }
        }
    }
}
