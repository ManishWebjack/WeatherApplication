using AutoMapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public async Task<WeatherResponseViewModel> GetWeatherFromZip(FormRequestModel model)
        {
            try
            {
                await RequestAPI(model);
            }
            catch (Exception ex)
            {
                WeatherResponse.ErrorMessage = "Error Calling API : " + ex.Message;
            }

            //Sets Date property based on Dt (UnixTime)
            if (WeatherResponse.daily != null)
                foreach (var response in WeatherResponse.daily)
                    response.Date = UnixTimeToDateTime(response.Dt);
            else
                WeatherResponse.Current.Date = UnixTimeToDateTime(WeatherResponse.Current.Dt);

            //Sets UOM property based on selected Unit
            WeatherResponse.UOM = (model.UOM == 0 ? 1 : (int)model.UOM) == 1 ? "°C" : "°F";
            WeatherResponse.RequestType = model.RequestType;

            return WeatherResponse;
        }

        #endregion

        #region Helper Methods

        private async Task RequestAPI(FormRequestModel model, string Country = "IN")
        {
            LoactionModel locationData = new LoactionModel();
            string uom = Enum.GetName(typeof(eUnit), (model.UOM == 0 ? 1 : model.UOM));

            using (HttpClient _client = new HttpClient())
            {
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //calling Location API : Fetch Lat/Lon from ZIP
                _client.BaseAddress = new Uri(_apiConfiguration.LocationBaseURL);

                string locationParam = string.Format(LocationAPIParam, model.ZipCode, Country, _apiConfiguration.APIKey);

                try
                {
                    HttpResponseMessage response = await _client.GetAsync(locationParam);
                    if (response.IsSuccessStatusCode)
                    {
                        var dataObjects = await response.Content.ReadAsStringAsync();
                        var i = JObject.Parse(dataObjects)["coord"];
                        locationData = JsonConvert.DeserializeObject<LoactionModel>(dataObjects);
                    }
                    else
                    {
                        Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR : ", ex.Message);
                }

            }
            //Call Weather API when City Lon/lat Information is present
            if (locationData != null && !string.IsNullOrEmpty(locationData.name))
            {
                using (HttpClient _client = new HttpClient())
                {
                    _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    _client.BaseAddress = new Uri(_apiConfiguration.OneCallAPIBaseURL);

                    string oneCallParam = string.Format(OneCallAPIParam, locationData.lat, locationData.lon, GetExcludeList(model.RequestType), uom, _apiConfiguration.APIKey);

                    try
                    {
                        HttpResponseMessage response = await _client.GetAsync(oneCallParam);
                        if (response.IsSuccessStatusCode)
                        {
                            var dataObjects = await response.Content.ReadAsStringAsync();

                            if (model.RequestType == (int)eRequestType.current)
                                dataObjects = dataObjects.Replace("temp", "CurrTemp");

                            var data = JsonConvert.DeserializeObject<WeatherAPIResponseModel>(dataObjects);

                            WeatherResponse = (_mapper.Map<WeatherResponseViewModel>(data));
                            WeatherResponse.CityName = locationData.name ?? "";
                        }
                        else
                        {
                            WeatherResponse.ErrorMessage = string.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        }
                    }
                    catch (Exception ex)
                    {
                        WeatherResponse.ErrorMessage = string.Format("ERROR : ", ex.Message);
                    }
                }
            }
            else
                WeatherResponse.ErrorMessage = "ZIP Code not valid";
        }

        //To generate Exclude list except for selected One.
        private string GetExcludeList(int i)
        {
            string excludeList = string.Empty;

            excludeList = string.Join(",", (Enum.GetNames(typeof(eRequestType)).Where(x => x != Enum.GetName(typeof(eRequestType), i))));

            return excludeList;
        }

        //To convert unix DateTime to Normal Date Time
        public DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
            return dtDateTime;
        }

        #endregion
    }
}