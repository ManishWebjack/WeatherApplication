using System;
using System.ComponentModel.DataAnnotations;
using WeatherApplication.Helpers.ENum;

namespace WeatherApplication.Models
{
    public class FormRequestModel
    {
        [Required(ErrorMessage = "Unit Required")]
        [Range(1,2)]
        public eUnit UOM { get; set; }

        [Required(ErrorMessage = "Zip Code Required")]
        [StringLength(6)]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "Request Type Required")]
        [Range(1, 4)]
        public int RequestType { get; set; }
    }
}