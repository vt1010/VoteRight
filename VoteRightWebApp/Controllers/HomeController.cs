using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VoteRightWebApp.Models;
using VoteRightWebApp.Services;

namespace VoteRightWebApp.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILocationDataService _locationDataService;
    private readonly DatabaseService _databaseService;

    public HomeController(IWebHostEnvironment environment, ILocationDataService locationDataService, DatabaseService databaseService)
    {
        _environment = environment;
        _locationDataService = locationDataService;
        _databaseService = databaseService;
    }

    public IActionResult Index()
    {
        ViewBag.Districts = _locationDataService.GetDistricts();
        return View();
    }

    public IActionResult SignIn()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SignIn(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
        {
            ViewBag.Error = "Please enter your phone number";
            return View();
        }

        // Check if user exists in database
        var user = await _databaseService.FindUserAsync(phoneNumber);
        if (user == null)
        {
            ViewBag.Error = "Phone number not found. Please sign up first.";
            return View();
        }

        // Store user info in session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserPhone", user.PhoneNumber);
        HttpContext.Session.SetString("UserDistrict", user.District);

        return RedirectToAction("Index", "FileDownload");
    }

    public IActionResult SignUp()
    {
        // Use LocationDataService to load districts
        ViewBag.Districts = _locationDataService.GetDistricts();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SignUp(string name, string phoneNumber, string whatsAppNumber,
                                string district, string politicalPartyOrganization, string organizationalPosition)
    {
        ViewBag.Districts = _locationDataService.GetDistricts();
        
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phoneNumber) ||
            string.IsNullOrEmpty(district) || string.IsNullOrEmpty(politicalPartyOrganization))
        {
            ViewBag.Error = "Please fill in all required fields";
            return View();
        }

        // Check if user already exists
        var existingUser = await _databaseService.FindUserAsync(phoneNumber);
        if (existingUser != null)
        {
            ViewBag.Error = "Phone number already registered. Please sign in instead.";
            return View();
        }

        // Create new user
        var user = new User
        {
            Name = name,
            PhoneNumber = phoneNumber,
            WhatsAppNumber = whatsAppNumber,
            District = district,
            PoliticalPartyOrganization = politicalPartyOrganization,
            OrganizationalPosition = organizationalPosition,
            RegisteredAt = DateTime.UtcNow
        };

        // Save to database
        await _databaseService.AddUserAsync(user);

        // Store user info in session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", name);
        HttpContext.Session.SetString("UserPhone", phoneNumber);
        HttpContext.Session.SetString("UserDistrict", district);

        return RedirectToAction("Index", "FileDownload");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Faq()
    {
        return View();
    }

    public IActionResult FindVolunteers()
    {
        return View();
    }

    public IActionResult EducativeVideos()
    {
        return View();
    }

    public IActionResult VoteChoriVideos()
    {
        return View();
    }

    public IActionResult PressMedia()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
