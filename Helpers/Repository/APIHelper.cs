using AutoMapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WeatherApplication.Helpers.ENum;
using WeatherApplication.Helpers.IRepository;
using WeatherApplication.Models;

namespace WeatherApplication.Helpers
{
    public class APIHelper : IAPIHelper
    {
        #region variable

        private readonly string _locAPIParam = @"?zip={0},{1}&appid={2}";
        private readonly string _oneCallAPIParam = @"?lat={0}&lon={1}&exclude={2}&units={3}&appid={4}";
        private readonly WeatherApiConfiguration _apiConfiguration;
        private WeatherResponseViewModel _viewModel;
        private IMapper _mapper;

        #endregion

        #region Constructor

        public APIHelper(IOptions<WeatherApiConfiguration> options, IMapper mapper)
        {
            _apiConfiguration = options.Value;
            _mapper = mapper;
        }

        #endregion

        #region Properties

        private string LocationAPIParam { get { return _locAPIParam; } }
        private string OneCallAPIParam { get { return _oneCallAPIParam; } }
        private WeatherResponseViewModel WeatherResponse
        {
            get
            {
                if (_viewModel == null)
                {
                    _viewModel = new WeatherResponseViewModel();
                    _viewModel.daily = new List<Current>();
                }
                return _viewModel;
            }
            set
            {
                _viewModel = value;
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// The method is responsible for calling "RequestAPI" and on successful output, it modifies the response model and 
        /// sets units information along with the date-time conversion from Unix time to representable format and returns to calling functions
        /// </summary>
        /// <param name="model">The request model contains all information like UOM, request Type, and ZipCode</param>
        /// <returns></returns>
        public async Task<WeatherResponseViewModel> GetWeatherFromZip(FormRequestModel model)
        {
            try
            {
                await RequestAPI(model);

                //Sets Date property based on Dt (UnixTime)
                if (WeatherResponse.daily != null && model.RequestType == (int)eRequestType.daily)
                    foreach (var response in WeatherResponse.daily)
                        response.Date = DateTimeOffset.FromUnixTimeSeconds(response.Dt).LocalDateTime;
                else
                    WeatherResponse.Current.Date = DateTimeOffset.FromUnixTimeSeconds(WeatherResponse.Current.Dt).LocalDateTime;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                WeatherResponse.ErrorMessage = "Error in API response, Date not proper: ";
            }
            catch (Exception ex)
            {
                WeatherResponse.ErrorMessage = "Error Calling API : " + ex.Message;
            }

            //Sets UOM property based on selected Unit
            WeatherResponse.UOM = (model.UOM == 0 ? 1 : (int)model.UOM) == 1 ? "°C" : "°F";
            WeatherResponse.WindUOM = (model.UOM == 0 ? 1 : (int)model.UOM) == 1 ? "meter/sec" : "miles/hour";
            WeatherResponse.RequestType = model.RequestType;

            return WeatherResponse;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// The method deals with API calls based on request Type: 1 for Current and 2 for Daily(7 days); 
        /// first, it uses Zip to get Lat, Lon information from the server then, using this information calls weather API.
        /// </summary>
        /// <param name="model">The request model contains all information like UOM, request Type, and ZipCode </param>
        /// <param name="Country">By default, it is set to India</param>
        /// <returns></returns>
        private async Task RequestAPI(FormRequestModel model, string Country = "IN")
        {
            LoactionModel locationData = new LoactionModel();
            string uom = Enum.GetName(typeof(eUnit), (model.UOM == 0 ? 1 : model.UOM));

            using (HttpClient _client = new HttpClient())
            {
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //calling Location API : Fetch Lat/Lon from ZIP code
                _client.BaseAddress = new Uri(_apiConfiguration.LocationBaseURL);

                string locationParam = string.Format(LocationAPIParam, model.ZipCode, Country, _apiConfiguration.APIKey);

                try
                {
                    HttpResponseMessage response = await _client.GetAsync(locationParam);
                    response.EnsureSuccessStatusCode();

                    var dataObjects = await response.Content.ReadAsStringAsync();
                    locationData = JsonConvert.DeserializeObject<LoactionModel>(dataObjects);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR : ", ex.Message);
                    return;
                }
            }

            using (HttpClient _client = new HttpClient())
            {
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.BaseAddress = new Uri(_apiConfiguration.OneCallAPIBaseURL);

                string oneCallParam = string.Format(OneCallAPIParam, locationData.lat, locationData.lon, GetExcludeList(model.RequestType), uom, _apiConfiguration.APIKey);

                try
                {
                    HttpResponseMessage response = await _client.GetAsync(oneCallParam);

                    response.EnsureSuccessStatusCode();
                    var dataObjects = await response.Content.ReadAsStringAsync();

                    if (model.RequestType == (int)eRequestType.current)
                        dataObjects = dataObjects.Replace("temp", "CurrTemp");

                    var data = JsonConvert.DeserializeObject<WeatherAPIResponseModel>(dataObjects);

                    WeatherResponse = (_mapper.Map<WeatherResponseViewModel>(data));
                    WeatherResponse.CityName = locationData.name ?? "";
                }
                catch (Exception ex)
                {
                    WeatherResponse.ErrorMessage = string.Format("ERROR : ", ex.Message);
                }
            }
        }

        //To generate Exclude list except for selected one in request model.
        private string GetExcludeList(int i)
        {
            string excludeList = string.Empty;

            excludeList = string.Join(",", (Enum.GetNames(typeof(eRequestType)).Where(x => x != Enum.GetName(typeof(eRequestType), i))));

            return excludeList;
        }

        #endregion
    }
}