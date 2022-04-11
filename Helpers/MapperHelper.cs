using AutoMapper;
using WeatherApplication.Models;

namespace WeatherApplication.Helpers
{
    public class MapperHelper : Profile
    {
        public MapperHelper()
        {
            CreateMap<WeatherResponseViewModel, WeatherAPIResponseModel>().ReverseMap();
        }

    }
}
