using System;

namespace WeatherApplication.Models
{
    public class LoactionModel
    {
        public string name { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public string country{get;set;}
        public DateTime date { get; set; }
    }
}
