using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;
using VoteRightWebApp.Models;
using VoteRightWebApp.Services;
using VoteRightWebApp.Utility;
using UtilHelper = VoteRightWebApp.Utility.DownloadHelper;

namespace VoteRightWebApp.Controllers;

public class FileDownloadController : Controller
{
    private readonly DatabaseService _databaseService;
    private readonly ICsvExportService _csvExportService;
    private readonly IVoterExportService _voterExportService;

    public FileDownloadController(DatabaseService databaseService, IConfiguration configuration, ICsvExportService csvExportService, IVoterExportService voterExportService)
    {
        _databaseService = databaseService;
        _csvExportService = csvExportService;
        _voterExportService = voterExportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var assemblies = await _databaseService.GetAssembliesAsync();
        ViewBag.Assemblies = assemblies;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Download([FromBody] FileDownloadRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AssemblyName))
        {
            return BadRequest(new { message = "Assembly is required" });
        }

        var baseName = request.AssemblyNumber ?? request.AssemblyName ?? "download";
        var utf8FileName = UtilHelper.EnsureCsvExtension(UtilHelper.SanitizeFileName(baseName));
        var asciiFileName = UtilHelper.EnsureCsvExtension(UtilHelper.ToAsciiFallback(utf8FileName));

        Response.ContentType = "text/csv";
        Response.Headers[HeaderNames.CacheControl] = "no-store";

        var disposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = asciiFileName,
            FileNameStar = utf8FileName
        };

        Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();

        // Register completion callback to record successful downloads without delaying response
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            var entry = new FileDownloadEntry
            {
                UserId = userId.Value,
                Assembly = request.AssemblyNumber ?? request.AssemblyName ?? string.Empty,
                Booths = string.IsNullOrWhiteSpace(request.BoothRange) ? null : request.BoothRange,
                DeviceType = UtilHelper.MapDeviceType(Request.Headers["User-Agent"].ToString()),
                DownloadedAt = DateTime.UtcNow
            };
            Response.OnCompleted(async () =>
            {
                try { await _databaseService.AddFileDownloadEntryAsync(entry); } catch { }
            });
        }

        try
        {
            await _voterExportService.StreamVotersToCsvAsync(request.AssemblyName!, request.BoothRange, Response.Body, HttpContext.RequestAborted);
            return new EmptyResult();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to generate CSV." });
        }
    }

    // Helper methods moved to DownloadHelper
}
