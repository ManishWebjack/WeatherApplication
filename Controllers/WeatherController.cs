using Microsoft.AspNetCore.Mvc;
using WeatherApplication.Helpers.IRepository;
using WeatherApplication.Models;

namespace WeatherApplication.Controllers
{
    public class WeatherController : Controller
    {
        public readonly IAPIHelper _aPIHelper;

        public WeatherController(IAPIHelper aPIHelper)
        {
            _aPIHelper = aPIHelper;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ActionName("GetCurrentWeather")]
        public IActionResult GetWeather(FormRequestModel req)
        {
            if (ModelState.IsValid)
            {
                var result = _aPIHelper.GetWeatherFromZip(req).Result;
                ViewBag.Result = result;
            }

            return View("Index");
        }
    }
}
