using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VoteRightWebApp.Models;
using VoteRightWebApp.Services;

namespace VoteRightWebApp.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly DatabaseService _databaseService;
    private readonly IUserService _userService;

    public HomeController(IWebHostEnvironment environment, DatabaseService databaseService, IUserService userService)
    {
        _environment = environment;
        _databaseService = databaseService;
        _userService = userService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult SignIn()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SignIn(int phoneNumber)
    {
        if (phoneNumber == 0)
        {
            ViewBag.Error = "Please enter your phone number";
            return View();
        }

        // Check if user exists in database
        var user = await _userService.FindUserAsync(phoneNumber);
        if (user == null)
        {
            ViewBag.Error = "Phone number not found. Please sign up first.";
            return View();
        }

        // Store user info in session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetInt32("UserPhone", user.PhoneNumber);
        HttpContext.Session.SetString("UserDistrict", user.District);

        return RedirectToAction("Index", "FileDownload");
    }

    public async Task<IActionResult> SignUp()
    {
        var districts = await _databaseService.GetDistinctDistrictsAsync();
        ViewBag.Districts = districts;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SignUp(string name, int phoneNumber, int whatsAppNumber,
                                string district, string politicalPartyOrganization, string organizationalPosition)
    {

        if (string.IsNullOrEmpty(name) || phoneNumber == 0 ||
            string.IsNullOrEmpty(district) || string.IsNullOrEmpty(politicalPartyOrganization))
        {
            ViewBag.Error = "Please fill in all required fields";
            return View();
        }

        // Check if user already exists
        var existingUser = await _userService.FindUserAsync(phoneNumber);
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
        await _userService.AddUserAsync(user);

        // Store user info in session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", name);
        HttpContext.Session.SetInt32("UserPhone", phoneNumber);
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

    public async Task<IActionResult> FindVolunteers()
    {
        var districts = await _databaseService.GetDistinctDistrictsAsync();
        ViewBag.Districts = districts;
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
