using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PogodynkaWP8._0ver1
{
    public class ForecastDay
    {
        public string period { get; set; }
        public string icon { get; set; }
        public string iconUrl { get; set; }
        public string title { get; set; }
        public string fcttext { get; set; }
        public string fcttextMetric { get; set; }
        public string pop { get; set; } //Probability of Precipitation -> prawdopodobieństwo opadów atmosferycznych
        public Date data { get; set; }

        //dla forecast z SIMPLE FORECAST
        public string highTempC { get; set; } //a może zamienic na int?
        public string lowTempC { get; set; } //temp w C
        public string conditions { get; set; } //warunki pogodowe
        public string skyicon { get; set; } //różne od icon, nie wiem  czemu
        public string qpfAllDay { get; set; } //quantitative precipitation forecast. - ilościowa prognoza opadów w przeciągu 3 następnych godzin
        //może double, wynik albo w in(cal) albo mm
        public string snowAllDay { get; set; } //śnieg w dzień, w cm albo calach
        public int avehumidity { get; set; } //średnia wilgotność
        public int maxhumidity { get; set; }
        public int minhumidity { get; set; }

        //public class MaxWind //maksymalny wiatr. Nie wiem co zrobić ze średnim wiatrem, bo pola są takie same...

        public int maxwind_mph { get; set; } //metry na godzinę
        public int maxwind_kph { get; set; } //km/h
        public string maxwind_dir { get; set; } //kierunek wiatru
        public string maxwind_degrees { get; set; } //stopnie w kierunku wiatru

        //śrdni wiatr. Nie wiem co zrobić ze średnim wiatrem, bo pola są takie same...

        public int avewind_mph { get; set; } //metry na godzinę
        public int avewind_kph { get; set; } //km/h
        public string avewind_dir { get; set; } //kierunek wiatru
        public string avewind_degrees { get; set; } //stopnie w kierunku wiatru

    }
}
