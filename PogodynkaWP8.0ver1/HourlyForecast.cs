using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PogodynkaWP8._0ver1
{
    public class HourlyForecast
    {
        /// <summary>
        /// Przechowuje datę oraz czas
        /// </summary>
        public DateTime czas { get; set; }
        /// <summary>
        /// Skrót nazwy miesiąca po angielsku
        /// </summary>
        public string monAbbrev { get; set; }
        /// <summary>
        /// skrót nazwy miesiąca po polsku
        /// </summary>
        public string monthAbbrev { get; set; } //skrót nazwy miesiąca po polsku
        public string pretty { get; set; }
        /// <summary>
        /// Skrót nazwy dnia tygodnia
        /// </summary>
        public string weekdayNameAbbrev { get; set; } //skrót nazwy dnia tygodnia
        /// <summary>
        /// Nazwa dnia tygodnia + noc
        /// </summary>
        public string weekdayNameNight { get; set; } //nazwa dnia na wieczór? 
        //public string tz { get; set; }//timezone
        //public string ampm { get; set; } 
        ///// <summary>
        ///// Czas uniksowy
        ///// </summary>
        //public string epoch { get; set; } //czas uniksowy
        public string tempC { get; set; } //moze lepiej w int?
        /// <summary>
        /// Punkt rosy
        /// </summary>
        public string dewpointC { get; set; } //punkt rosy
        /// <summary>
        /// Krótki opis warunków pogodowych
        /// </summary>
        public string condition { get; set; } //krótki opis
        public string icon { get; set; }
        public string iconUrl { get; set; }
        public string fctcode { get; set; }
        public string sky { get; set; }
        public string windKph { get; set; }
        public string windDir { get; set; } //kierunek wiatru
        public string windDegrees { get; set; } //stopnie wiatru
        //public string uvi { get; set; } //ultraviolet index (1-16, where 16 is extreme) 
        public string humidity { get; set; }
        /// <summary>
        /// Błędne wartości <-100
        /// </summary>
        public string windchill { get; set; }
        /// <summary>
        /// Błędne wartości <-100
        /// </summary>
        public string heatindex { get; set; }
        public string feelslike { get; set; }
        /// <summary>
        /// Quantitative Precipitation Forecast. - ilościowa prognoza opadów w przeciągu 3 następnych godzin. double. in / mm.
        /// </summary>
        public string qpf { get; set; }
        public string snow { get; set; }
        public string pop { get; set; }
        /// <summary>
        /// MSLP - mean sea level pressure- ciśnienie
        /// </summary>
        public string pressure { get; set; }

    }
}
