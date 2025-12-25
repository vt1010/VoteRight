using Microsoft.AspNetCore.Mvc;
using VoteRightWebApp.Models;
using VoteRightWebApp.Services;
using System.Collections.Generic;
using System.Dynamic;

namespace VoteRightWebApp.Controllers
{
    public class FindVolunteersController : Controller
    {
        private readonly ILocationDataService _locationDataService;
        private readonly DatabaseService _databaseService;

        public FindVolunteersController(ILocationDataService locationDataService, DatabaseService databaseService)
        {
            _locationDataService = locationDataService;
            _databaseService = databaseService;
        }

        public IActionResult Index()
        {
            // Check if user is logged in (same as in FileDownloadController)
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("SignIn", "Home");
            }
            // Prevent caching of this page
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            ViewBag.Districts = _locationDataService.GetDistricts();
            ViewBag.Assemblies = new List<Assembly>(); // Start empty, populate via AJAX

            return View();
        }

        [HttpPost]
        public IActionResult Search(string district, string assemblyNumber)
        {
            ViewBag.Districts = _locationDataService.GetDistricts();
            ViewBag.Assemblies = _locationDataService.GetAssembliesByDistrict(district);

            // Preserve the selected district and assembly so the dropdowns keep their values after search
            ViewBag.SelectedDistrict = district;
            ViewBag.SelectedAssemblyNumber = assemblyNumber;

            var users = _databaseService.GetUsers(district, assemblyNumber);

            ViewBag.Users = users;
            return View("Index");
        }

        [HttpGet]
        public IActionResult GetAssembliesByDistrict(string district)
        {
            if (string.IsNullOrEmpty(district))
                return Json(new List<Assembly>());
            var assemblies = _locationDataService.GetAssembliesByDistrict(district);
            return Json(assemblies);
        }
    }
}
