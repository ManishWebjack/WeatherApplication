using System.Threading.Tasks;
using WeatherApplication.Models;

namespace WeatherApplication.Helpers.IRepository
{
    public interface IAPIHelper
    {
        public Task<WeatherResponseViewModel> GetWeatherFromZip(FormRequestModel model);

    }
}
