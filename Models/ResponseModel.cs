using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WeatherApplication.Models
{

    [Serializable]
    public class WeatherResponseViewModel
    {
        [JsonPropertyName("current")]
        public Current Current { get; set; }

        [JsonPropertyName("daily")]
        public List<Current> daily { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("requestType")]
        public int RequestType { get; set; }

        [JsonProperty("uom")]
        public string UOM { get; set; }

        [JsonProperty("cityname")]
        public string CityName { get; set; }
        public string WindUOM { get; set; }
    }
}
