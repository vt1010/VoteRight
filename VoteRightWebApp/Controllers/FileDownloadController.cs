using Microsoft.AspNetCore.Mvc;
using System.Text;
using VoteRightWebApp.Models;
using VoteRightWebApp.Services;

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

        var fileName = (request.AssemblyNumber ?? request.AssemblyName ?? "download") + ".csv";
        Response.ContentType = "text/csv";
        Response.Headers.ContentDisposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        {
            FileNameStar = fileName
        }.ToString();

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
}
