using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace PogodynkaWP8._0ver1
{
    public partial class Pogoda : PhoneApplicationPage, INotifyPropertyChanged
    {
        #region ZMIENNE
        public bool czyBylaJuzUzywana = false; //potrzebna przy powrocie z przeglądarki 
        public Thread t;
        //public IsolatedStorageSettings ustawienia = IsolatedStorageSettings.ApplicationSettings;

        public string miasto;
        public static string query; //zawiera przekazane z poprzedniej strony parametry zmodyfikowane do linka
        public bool czyToGPS; //czy lokacja jest z GPS czy jest to wpisana miejscowość
        public static ForecastDay curObs;
        public static List<ForecastDay> nastepneDni2 = new List<ForecastDay>(); //txt_forecast
        public static List<ForecastDay> nastepneDni = new List<ForecastDay>(); //SimpleForecast
        public static List<HourlyForecast> godzinowaPrognoza = new List<HourlyForecast>();
        public static List<String> listaSportow = new List<string>(); //lista ze sportami
        public static List<String> listaAktywnosci = new List<string>(); //lista z aktywnościami
        public static Astronomy astronomy;

        //do sportów i wypoczynku
        public double temperaturaOdczuwalna; //do przechowywania obliczonej w funkcji temperatury odczuwalnej
        public string pog = "";
        public string wiatr = "";
        public string wiatrPorywy="";
        public int godzina = 0;
        public int miesiac = 0;
        public string dzienTygodnia = "";
        public string temperatura = "";
        public char poraDnia = new char();
        public char poraRoku = new char();

        //do ubrań
        public WriteableBitmap wbFinalna = null;  //końcowa bitmapa - pogodynka wraz z ubraniem
        //public List<string> ubrania = new List<string>(); //lista z wybranymi ubraniami
        public bool czyBedziePadac = false;
        #endregion

        public Pogoda()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged; //handler do zdarzenia zmiany wartości właściwości dla list wypoczynku i sportów



        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) //następuje w momencie gdy rozpoczyna się nawigacja poza aktualną stronę 
        {
            base.OnNavigatingFrom(e);
            if (e.NavigationMode.Equals(NavigationMode.Back)) //sprawdzam, czy cofam się do strony głownej (wtedy true, bo chce zmienić miasto i muszę zatrzymać wątek dla aktualnego miasta))
            {
                t.Abort();
                czyBylaJuzUzywana = false;
                listaAktywnosci.Clear();
                listaSportow.Clear();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) //wywoływane w momencie gdy nastąpi nawigacja do danej strony
        {
            base.OnNavigatedTo(e);
            string msg;
            if (!(czyBylaJuzUzywana))
            {
                if (NavigationContext.QueryString.TryGetValue("msg", out msg))
                {
                    if (msg.Contains(","))
                    {
                        query = msg;
                        var temp = query.Split(','); //długość i szerokość z GPS podawana jest z przecinkami. Niestety aby wstawić wartości do zapytania(linka) przecinki trzeba zastąpić kropkami. 
                        if (temp.Length == 4)
                        {
                            query = temp[0] + "." + temp[1] + "," + temp[2] + "." + temp[3];
                        }
                        this.miasto = "GPS: " + msg;
                        Debug.WriteLine("GPS");
                        czyToGPS = true;
                    }
                    else
                    {
                        this.miasto = msg;
                        query = "Poland/" + msg;
                        Debug.WriteLine("MIASTO ");
                        czyToGPS = false;
                    }
                    this.miastoTB.Text = miasto;

                    t = new Thread(NewThread);
                    t.Start();
                }
                czyBylaJuzUzywana = true;
            }
        }

        public void NewThread()
        {
            string url = "http://api.wunderground.com/api/c9d15b10ff3ed303/forecast/forecast10day/hourly/conditions/astronomy/lang:PL/q/" + query + ".xml";

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += wc_DownloadStringCompleted; //uruchamiane kiedy pobierze się cała zawartość

            wc.DownloadStringAsync(new Uri(url));

        }

        public void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {

            string weather = "";
            try
            {
                weather = e.Result;
                XmlReader reader = XmlReader.Create(new StringReader(weather));
                XDocument doc = XDocument.Load(reader);

                obrabianieAstronomy(doc);

                obrabianieConditions(doc);

                obrabianieHourlyForecast(doc);

                temperaturaOdczuwalna = obliczTemperatureOdczuwalna(temperatura, wiatr);

                Debug.WriteLine(temperaturaOdczuwalna.ToString());

                wyborSportow();

                ubranie();

                wyborWypoczynku();



                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //ukrycie paska postępu
                    this.postep.Visibility=Visibility.Collapsed;
                    ///Wyświetlanie listy sportów i aktywności

                    this.sportyLB.ItemsSource = listaSportow;
                    this.sportyLB.SelectionChanged += sportyLB_SelectionChanged;

                    this.wypoczynekLB.ItemsSource = listaAktywnosci;
                    this.wypoczynekLB.SelectionChanged += wypoczynekLB_SelectionChanged;
                });


            }
            catch (NullReferenceException nrex)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    string informacja = "Pogoda dla wprowadzonej nazwy miejscowości nie została znaleziona. Niestety nie wszystkie mniejsze miasta mają swoje stacje meteorologiczne, dlatego sugerujemy użycie GPS w celu zlokalizowania najbliższej stacji pogodowej.";
                    MessageBox.Show(informacja, "Błędne miasto", MessageBoxButton.OK);

                    this.textBox1.Text = informacja;
                    this.ikonka.Source = null;
                });
                Debug.WriteLine(nrex.Message);
            }
            catch (Exception ex)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(ex.Message, "Nieznany błąd", MessageBoxButton.OK);
                    Debug.WriteLine(ex.GetType().ToString());
                    this.textBox1.Text = ex.Message;
                    this.ikonka.Source = null;
                });
            }
        }

        private void ApplicationBar_About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.RelativeOrAbsolute));
        }


        //WYPOCZYNEK
        #region WYPOCZYNEK
        public void wyborWypoczynku()
        {

            String pogoda2 = pog.ToLower();

            if (pogoda2.Equals("pogodnie"))
            {
                ladnaPogodaWyp(poraDnia);
            }
            else if (pogoda2.Equals("przewaga chmur"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("obłoki zanikające"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Contains("śnieg"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Contains("śnieżek"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("niewielkie zachmurzenie"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Contains("deszcz"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("pochmurno"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Contains("mżawka"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Contains("mgł"))//BO JESZCZE płatki mgły
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Contains("zamglenia"))
                ladnaPogodaWyp(poraDnia);

            //listaAktywnosci.Add("Nieznany rodzaj pogody");

            zalezneWyp();
        }

        private void deszczowaPogodaWyp(char poraDnia)
        {
            podDachemWyp();
        }

        private void ladnaPogodaWyp(char poraDnia)
        {
            //int temp = 0;
            //if (int.TryParse(temperatura, out temp))
            //    Debug.WriteLine(temp);
            if ((temperaturaOdczuwalna > -30) && (temperaturaOdczuwalna < 35))
            {
                podstawoweWyp();
                okazjonalneWyp();
            }

            podDachemWyp();
            //zalezneWyp();
        }

        private void zalezneWyp()
        {
            string pogoda2 = pog.ToLower();
            if ((pogoda2.Equals("pogodnie")) || (pogoda2.Equals("niewielkie zachmurzenie")) || (pogoda2.Equals("obłoki zanikające")))
            {
                TimeSpan wschod1 = new TimeSpan(astronomy.sunrise.Hour - 1, astronomy.sunrise.Minute, 0); //-1 godzina do wschodu
                TimeSpan zachod1 = new TimeSpan(astronomy.sunset.Hour - 1, astronomy.sunset.Minute, 0); //-1 godzina do zachodu
                TimeSpan wschod = new TimeSpan(astronomy.sunrise.Hour, astronomy.sunrise.Minute, 0);
                TimeSpan zachod = new TimeSpan(astronomy.sunset.Hour, astronomy.sunset.Minute, 0);
                TimeSpan teraz = DateTime.Now.TimeOfDay;
                if ((teraz < wschod) && (teraz > wschod1))
                {
                    listaAktywnosci.Add("Podziwiaj wschód słońca");
                    Debug.WriteLine("Wschód");
                }
                else if ((teraz < zachod) && (teraz > zachod1))
                {
                    listaAktywnosci.Add("Podziwiaj zachód słońca");
                    Debug.WriteLine("Zachód");
                }
                else if ((teraz > zachod) || (teraz < wschod1))
                {
                    Debug.WriteLine("Gwiazdy");
                    listaAktywnosci.Add("Podziwiaj gwiazdy");
                }
                else if ((teraz < zachod1) || (teraz > wschod))
                {
                    listaAktywnosci.Add("Podziwiaj niebo ");
                    Debug.WriteLine("Chmury");
                    if ((miesiac >= 3) && (miesiac <= 11))
                    {
                        Debug.WriteLine(wiatr);

                        double wind = 0.0;
                        if (double.TryParse(wiatr, out wind))
                        {
                            if ((wind >= 5) && (wind <= 20))
                                listaAktywnosci.Add("Puszczanie latawca");
                        }
                    }

                    listaAktywnosci.Add("Wyjazd na działkę/wieś");
                    listaAktywnosci.Add("Wyprawa do lasu");

                    if (pogoda2.Equals("pogodnie"))
                    {
                        listaAktywnosci.Add("Opalanie");
                        listaAktywnosci.Add("Piknik");
                    }
                }
                Debug.WriteLine(wschod.ToString() + " " + zachod.ToString() + " " + teraz.ToString());
            }
        }

        private void podDachemWyp()
        {
            if ((poraDnia.Equals('p')) || (poraDnia.Equals('o')) || (poraDnia.Equals('w')))
            {
                listaAktywnosci.Add("Pub");
                listaAktywnosci.Add("Kawiarnia");
                listaAktywnosci.Add("Restauracja");
                listaAktywnosci.Add("Kino");
                listaAktywnosci.Add("Kręgle");
                listaAktywnosci.Add("Bilard");
                listaAktywnosci.Add("Muzeum");
                listaAktywnosci.Add("Biblioteka");
                listaAktywnosci.Add("Teatr");
                listaAktywnosci.Add("Aquapark");
                listaAktywnosci.Add("Zakupy");
                listaAktywnosci.Add("Zajęcia plastyczne");
                listaAktywnosci.Add("Zajęcia muzyczne");
            }
            if ((poraDnia.Equals('w') || (poraDnia.Equals('n'))))
            {
                listaAktywnosci.Add("Impreza");
                listaAktywnosci.Add("Koncert");
                listaAktywnosci.Add("Randka w ciemno");
                listaAktywnosci.Add("Pub");
            }
        }

        private void okazjonalneWyp()
        {
            listaAktywnosci.Add("Wydarzenie w mieście");
        }


        private void podstawoweWyp()
        {
            listaAktywnosci.Add("Spacer");
            listaAktywnosci.Add("Spotkanie z przyjaciółmi");
            listaAktywnosci.Add("Spacer z psem");
            listaAktywnosci.Add("Fotografowanie");
            listaAktywnosci.Add("Rysowanie krajobrazu");
        }

        public void wypoczynekLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string query = "";

            var nazwaAktywnosci = (sender as ListBox).SelectedItem as String;

            if (listaAktywnosci.Count > 0)
            {
                if ((nazwaAktywnosci.Equals("Spacer")) || (nazwaAktywnosci.Equals("Spacer z psem")) || (nazwaAktywnosci.Equals("Fotografowanie")) || (nazwaAktywnosci.Equals("Rysowanie krajobrazu")) || (nazwaAktywnosci.Equals("Piknik"))) { query = "Park"; }
                else if ((nazwaAktywnosci.Equals("Impreza")) || (nazwaAktywnosci.Equals("Randka w ciemno")))
                {
                    query = "Club";
                }
                else if (nazwaAktywnosci.Equals("Zakupy"))
                {
                    query = "\"Centrum handlowe\"";
                }
                else if ((nazwaAktywnosci.Equals("Zajęcia plastyczne")) || (nazwaAktywnosci.Equals("Zajęcia muzyczne")))
                {
                    query = "\"Dom kultury\"";
                }
                else if (nazwaAktywnosci.Equals("Koncert"))
                {
                    var wbt2 = new WebBrowserTask();
                    wbt2.Uri = new Uri("https://www.google.pl/#q=" + miasto + "+Koncerty", UriKind.RelativeOrAbsolute);
                    wbt2.Show();
                    query = "";
                }
                else if (nazwaAktywnosci.Equals("Spotkanie z przyjaciółmi"))
                {
                    query = "Kawiarnia";//?
                }
                else if ((nazwaAktywnosci.Equals("Podziwiaj niebo ")) ||
                (nazwaAktywnosci.Equals("Podziwiaj gwiazdy")) ||
                (nazwaAktywnosci.Equals("Podziwiaj zachód słońca")) ||
                (nazwaAktywnosci.Equals("Podziwiaj wschód słońca")))
                {
                    query = "";
                }
                else
                {
                    query = "\"" + nazwaAktywnosci + "\"";
                }
                Debug.WriteLine("Działa to to?                 " + nazwaAktywnosci + "/" + query);
                if (!(query.Equals("")))
                {
                    var wbt = new WebBrowserTask();
                    Uri uri = new Uri("https://maps.google.pl/maps?q=" + miasto + "+" + query, UriKind.RelativeOrAbsolute);
                    wbt.Uri = uri;
                    wbt.Show();
                }
            }
        }

        #endregion WYPOCZYNEK

        //OBRABIANIE ASTRONOMII, POGODY GODZINOWEJ I NA NASTĘPNE DNI
        #region OBRABIANIE ASTRONOMII, POGODY GODZINOWEJ I NA NASTĘPNE DNI

        public static string PierwszaLiteraWielka(string zdanie)
        {
            if (String.IsNullOrEmpty(zdanie))
                return " ";
            return zdanie.First().ToString().ToUpper() + String.Join("", zdanie.Skip(1));
        }

        /// <summary>
        /// Funkcja obliczająca temperaturę odczuwalną wg wzoru 
        /// </summary>
        /// <param name="temp">Temperatura w st C</param>
        /// <param name="wiatr">Wiatr w km/h</param>
        /// <returns>Zwraca -999.9999 gdy nie udało się obliczyć</returns>
        public static double obliczTemperatureOdczuwalna(string temp, string wiatr)
        {
            double tempWC = 0.0, V = 0.0;
            double result = 0.0;
            //if (double.TryParse(temp, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out tempWC))
            if(true)
            {
                tempWC=Convert.ToDouble(temp);
                Debug.WriteLine(tempWC.ToString());

                if (double.TryParse(wiatr, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out V))
                {
                    Debug.WriteLine(V.ToString());
                    if (V!=0.0)
                    {
                        V = Math.Pow(V, 0.16);
                        result = 13.12 + (0.6215 * tempWC) - (11.37 * V) + (0.3965 * tempWC * V);
                    }
                    else
                    {
                        result=tempWC;
                    }
                }
            }
            else
                result = -999.9999;
            return Math.Round(result, 4);//result;
        }


        public static void obrabianieAstronomy(XDocument doc)
        {
            astronomy = new Astronomy();
            int hTmp = 0, mTmp = 0, hTmpDt = 0, mTmpDt = 0; //hTmpDt - hour temporary do DateTime

            var moon_phase = (from d in doc.Descendants()
                              where (d.Name.LocalName == "moon_phase")
                              select d).FirstOrDefault();
            astronomy.ageOfMoon = moon_phase.Element("ageOfMoon").Value;
            astronomy.percentIlluminated = moon_phase.Element("percentIlluminated").Value;
            var tmp = (from d in moon_phase.Descendants()
                       where (d.Name.LocalName == "sunset")
                       select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp)) && (int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                hTmpDt = hTmp;
                mTmpDt = mTmp;
                DateTime dtTmp = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hTmpDt, mTmpDt, 0);
                astronomy.moonset = dtTmp;
            }
            tmp = (from d in moon_phase.Descendants()
                   where (d.Name.LocalName == "sunrise")
                   select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp)) && (int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                hTmpDt = hTmp;
                mTmpDt = mTmp;
                DateTime dtTmp = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hTmpDt, mTmpDt, 0);
                astronomy.moonrise = dtTmp;
            }
            moon_phase = (from d in doc.Descendants()
                          where (d.Name.LocalName == "sun_phase")
                          select d).FirstOrDefault();
            tmp = (from d in moon_phase.Descendants()
                   where (d.Name.LocalName == "sunrise")
                   select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp)) && (int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                hTmpDt = hTmp;
                mTmpDt = mTmp;
                DateTime cos = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hTmpDt, mTmpDt, 0);
                astronomy.sunrise = cos;
            }
            tmp = (from d in moon_phase.Descendants()
                   where (d.Name.LocalName == "sunset")
                   select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp)) && (int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                hTmpDt = hTmp;
                mTmpDt = mTmp;
                DateTime cos = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hTmpDt, mTmpDt, 0);
                astronomy.sunset = cos;
            }


            //KONIEC ASTRONOMY
        }

        private void obrabianieHourlyForecast(XDocument doc)
        {
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;

            var hourly_forecast = (from d in doc.Descendants()
                                   where (d.Name.LocalName == "hourly_forecast")
                                   select d).ToList();

            var hourly = (from d in hourly_forecast.Descendants()
                          where (d.Name.LocalName == "forecast")
                          select d).ToList();
            int i = 0;
            foreach (var item in hourly)
            {
                int iTmp = 0;
                HourlyForecast hf = new HourlyForecast();
                var FCTTIME = (from d in item.Descendants()
                               where (d.Name.LocalName == "FCTTIME")
                               select d).FirstOrDefault();
                int h = 0, min = 0, s = 0, y = 0, mon = 0, mday = 0;
                if (int.TryParse(FCTTIME.Element("hour").Value, out iTmp))
                    h = iTmp;
                if (int.TryParse(FCTTIME.Element("min").Value, out iTmp))
                    min = iTmp;
                if (int.TryParse(FCTTIME.Element("year").Value, out iTmp))
                    y = iTmp;
                if (int.TryParse(FCTTIME.Element("mon").Value, out iTmp))
                    mon = iTmp;
                if (int.TryParse(FCTTIME.Element("mday").Value, out iTmp))
                    mday = iTmp;

                DateTime dt = new DateTime(y, mon, mday, h, min, s);
                hf.czas = dt;

                hf.monAbbrev = FCTTIME.Element("mon_abbrev").Value;
                hf.monthAbbrev = FCTTIME.Element("month_name_abbrev").Value;
                hf.pretty = FCTTIME.Element("pretty").Value;
                hf.weekdayNameAbbrev = FCTTIME.Element("weekday_name_abbrev").Value;
                hf.weekdayNameNight = FCTTIME.Element("weekday_name_night").Value;
                //Debug.WriteLine(hf.czas.ToLongDateString()+" "+hf.czas.ToLocalTime()+" "+hf.weekdayNameAbbrev+" "+hf.monthAbbrev);
                hf.condition = item.Element("condition").Value;
                hf.icon = item.Element("icon").Value;
                hf.iconUrl = item.Element("icon_url").Value;
                hf.sky = item.Element("sky").Value;
                hf.humidity = item.Element("humidity").Value;
                hf.pop = item.Element("pop").Value;
                hf.fctcode = item.Element("fctcode").Value;
                hf.tempC = ((from d in item.Descendants()
                             where d.Name.LocalName == "temp"
                             select d).FirstOrDefault()).Element("metric").Value;
                hf.dewpointC = ((from d in item.Descendants()
                                 where d.Name.LocalName == "dewpoint"
                                 select d).FirstOrDefault()).Element("metric").Value;
                hf.windKph = ((from d in item.Descendants()
                               where d.Name.LocalName == "wspd"
                               select d).FirstOrDefault()).Element("metric").Value;
                hf.windDir = (((from d in item.Descendants() where d.Name.LocalName == "wdir" select d).FirstOrDefault()).Element("dir").Value);
                hf.windDegrees = (((from d in item.Descendants() where d.Name.LocalName == "wdir" select d).FirstOrDefault()).Element("degrees").Value);

                if (int.TryParse((((from d in item.Descendants()
                                    where d.Name.LocalName == "windchill"
                                    select d).FirstOrDefault()).Element("metric").Value), out iTmp))
                {
                    if (iTmp < -100)
                        hf.windchill = "";
                    else
                        hf.windchill = iTmp.ToString();
                }

                if (int.TryParse((((from d in item.Descendants()
                                    where d.Name.LocalName == "heatindex"
                                    select d).FirstOrDefault()).Element("metric").Value), out iTmp))
                {
                    if (iTmp < -100)
                        hf.heatindex = "";
                    else
                        hf.heatindex = iTmp.ToString();
                }
                hf.feelslike = (((from d in item.Descendants()
                                  where d.Name.LocalName == "feelslike"
                                  select d).FirstOrDefault()).Element("metric").Value);
                hf.qpf = (((from d in item.Descendants()
                            where d.Name.LocalName == "qpf"
                            select d).FirstOrDefault()).Element("metric").Value);
                hf.snow = (((from d in item.Descendants()
                             where d.Name.LocalName == "snow"
                             select d).FirstOrDefault()).Element("metric").Value);
                hf.pressure = (((from d in item.Descendants()
                                 where d.Name.LocalName == "mslp"
                                 select d).FirstOrDefault()).Element("metric").Value);
                if (i == 0)
                {
                    dzienTygodnia = dt.DayOfWeek.ToString();
                    miesiac = dt.Month;
                    godzina = dt.Hour;
                    temperatura = hf.tempC;
                    //wiatr = hf.windKph;
                    if (pog.Equals(" "))
                        pog = hf.condition;
                    Debug.WriteLine("*"+pog+"*");
                    i++;
                }
                godzinowaPrognoza.Add(hf);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
                    //System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                    HourlyUC b = new HourlyUC();
                    b.Godz = hf.czas.ToString("HH:mm");
                    b.Dzien = hf.czas.ToString("d MMM yyyy");
                    b.Warunki = hf.condition;
                    b.Opady = "Opady: " + hf.qpf;
                    b.temperatura.Text = "Temp: " + hf.tempC + "C";
                    ImageSource imgSrc;
                    if (hf.czas.Hour <= astronomy.sunrise.Hour || hf.czas.Hour >= astronomy.sunset.Hour)
                    {
                        imgSrc = new BitmapImage(new Uri("Icons/nt_" + hf.icon + ".png", UriKind.Relative));
                    }
                    else
                    {
                        imgSrc = new BitmapImage(new Uri("Icons/" + hf.icon + ".png", UriKind.Relative));
                    }

                    b.ikonka.Source = imgSrc;
                    hStackPanel.Children.Add(b);
                });
            }
        }

        private void obrabianieConditions(XDocument doc)
        {
            var current_obs = (from d in doc.Descendants()
                               where (d.Name.LocalName == "current_observation")
                               select d).FirstOrDefault();
            if (czyToGPS)
            {

                Debug.WriteLine(current_obs.ToString());

                var disLoc = (from d in current_obs.Descendants()
                              where (d.Name.LocalName == "display_location")
                              select d).FirstOrDefault();
                //var place = (from d in disLoc.Descendants()
                //             where (d.Name.LocalName=="full")
                //             select d).FirstOrDefault();
                /*Pobieranie aktualnych danych */



                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //pobranie nazwy miejscowości wg GPS
                    this.miasto = disLoc.Element("full").Value;
                    this.miastoTB.Text = disLoc.Element("full").Value;
                    Debug.WriteLine(disLoc.ToString());
                });


            }
            //aktualne dane pogodowe
            curObs = new ForecastDay();
            curObs.conditions = current_obs.Element("weather").Value;

            //w niektórych miejscowościach nie ma aktualnej pogody
            if (curObs.conditions.Equals(null))
                pog = " ";
            else
                pog = curObs.conditions; //do sportów potrzebne
            pog = PierwszaLiteraWielka(pog);
            curObs.highTempC = current_obs.Element("temp_c").Value; //taka zwykła temperatura
            curObs.lowTempC = current_obs.Element("feelslike_c").Value; //odczuwalna
            if (temperatura != null)
                Debug.WriteLine(temperatura);
            temperatura = curObs.lowTempC;
            Debug.WriteLine(temperatura);
            curObs.icon = current_obs.Element("icon").Value;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                String cos = "";
                cos = pog + "\nTemperatura: " + curObs.highTempC + "C     Odczuwalna: " + curObs.lowTempC + "C\n" +
                        "Wiatr: " + current_obs.Element("wind_kph").Value + "km/h, ";
                wiatr = current_obs.Element("wind_kph").Value;
                if (((current_obs.Element("wind_gust_kph").Value).Equals(0.ToString())) || ((current_obs.Element("wind_gust_kph").Value).Equals((0.0).ToString())) || ((current_obs.Element("wind_gust_kph").Value).Equals(null)) || ((current_obs.Element("wind_gust_kph").Value).Equals(""))) //podmuchy mogą być zerem albo moze ich nie być
                    Debug.WriteLine("|" + current_obs.Element("wind_gust_kph").Value + "|");
                else
                {
                    cos += " porywy do " + current_obs.Element("wind_gust_kph").Value + "km/h  ";
                    wiatrPorywy =current_obs.Element("wind_gust_kph").Value;
                }
                var tmpWiatr = current_obs.Element("wind_dir").Value;
                Debug.WriteLine(tmpWiatr);
                if (tmpWiatr.Equals("East"))
                    tmpWiatr = "wschodni";
                else if (tmpWiatr.Equals("West"))
                    tmpWiatr = "zachodni";
                else if (tmpWiatr.Equals("North"))
                    tmpWiatr = "północny";
                else if (tmpWiatr.Equals("South"))
                    tmpWiatr = "południowy";
                else if (tmpWiatr.Equals("Variable"))
                    tmpWiatr = "zmienny";
                cos += tmpWiatr + "\n";
                //,   w porywach do: "+current_obs.Element("wind_gust_kph").Value+"km/h   "+current_obs.Element("wind_dir").Value+"\n"+
                cos += "Wilgotność: " + current_obs.Element("relative_humidity").Value +
                        "\nCiśnienie: " + current_obs.Element("pressure_mb").Value + "hPa, " + current_obs.Element("pressure_trend").Value + "\nWidoczność: ";
                if (!(current_obs.Element("visibility_km").Value).Equals("N/A"))
                    cos += current_obs.Element("visibility_km").Value + "km\nOpady (dzień/godz):";
                else
                    cos += " \nOpady (dzien/godz): ";
                //        "\nWidoczność: "+current_obs.Element("visibility_km").Value+"km\nOpady (dzień/godz):";
                if (!(current_obs.Element("precip_1hr_metric").Value).Contains('-'))
                    cos = cos + current_obs.Element("precip_1hr_metric").Value + " mm/";
                else
                    cos += " - /";
                if (!(current_obs.Element("precip_today_metric").Value).Contains('-'))
                    cos += current_obs.Element("precip_today_metric").Value + " mm";
                else
                    cos += " -";
                //        "\nOpady (godz/dzień): "+current_obs.Element("precip_1hr_metric").Value+"mm/"+current_obs.Element("precip_today_metric").Value+"mm";
                this.textBox1.Text = cos;
                Uri uri = null;
                if ((DateTime.Now < astronomy.sunrise) || (DateTime.Now > astronomy.sunset)) //po zachodzie słońca
                {
                    Debug.WriteLine("sunrise: " + astronomy.sunrise.Hour + " " + astronomy.sunset.Hour);
                    uri = new Uri("Icons/nt_" + curObs.icon + ".png", UriKind.Relative);
                }
                else
                {
                    uri = new Uri("Icons/" + curObs.icon + ".png", UriKind.Relative);
                }
                ImageSource imgSource = new BitmapImage(uri);
                this.ikonka.Source = imgSource;

            });

            var txt_forecast = (from d in doc.Descendants()
                                where (d.Name.LocalName == "txt_forecast")
                                select d).ToList();

            var forecast = (from d in txt_forecast.Descendants()
                            where (d.Name.LocalName == "forecastday")
                            select d).ToList();
            var simpleForecast = (from d in doc.Descendants()
                                  where (d.Name.LocalName == "simpleforecast")
                                  select d).ToList();

            var smplFrcstDay = (from d in simpleForecast.Descendants()
                                where (d.Name.LocalName == "forecastday")
                                select d).ToList();
            int simplePeriod = 1;


            foreach (var item in smplFrcstDay)
            {
                string sTmp = "";
                int iTmp = 0;
                Console.WriteLine("****" + item);
                ForecastDay fd = new ForecastDay();

                fd.period = item.Element("period").Value;
                fd.icon = item.Element("icon").Value;
                fd.iconUrl = item.Element("icon_url").Value;
                fd.conditions = item.Element("conditions").Value;
                fd.pop = item.Element("pop").Value;

                //DATA
                Date d = new Date();
                var data = (from x in item.Descendants()
                            where x.Name.LocalName == "date"
                            select x).FirstOrDefault();

                Console.WriteLine("XXXXXXX: " + data.Element("day").Value);
                d.day = data.Element("day").Value;
                d.epoch = data.Element("epoch").Value;
                d.hour = data.Element("hour").Value;
                d.min = data.Element("min").Value;
                d.month = data.Element("month").Value;
                d.monthName = data.Element("monthname").Value;
                d.weekDay = data.Element("weekday").Value; //albo weekday_short , czyli skrót nazwy dnia tygodnia
                d.pretty = data.Element("pretty").Value;
                d.yday = data.Element("yday").Value;
                d.year = data.Element("year").Value;
                d.prettyShort = data.Element("pretty_short").Value;

                fd.data = d;
                int h = 0, min = 0, s = 0, y = 0, mon = 0, mday = 0;
                if (int.TryParse(d.hour, out iTmp))
                    h = iTmp;
                if (int.TryParse(d.min, out iTmp))
                    min = iTmp;
                if (int.TryParse(d.year, out iTmp))
                    y = iTmp;
                if (int.TryParse(d.month, out iTmp))
                    mon = iTmp;
                if (int.TryParse(d.day, out iTmp))
                    mday = iTmp;

                DateTime dt = new DateTime(y, mon, mday, h, min, s);
                fd.data2 = dt;

                var wnd = (from x in item.Descendants()
                           where x.Name.LocalName == "maxwind"
                           select x).FirstOrDefault();
                sTmp = wnd.Element("mph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.maxwind_mph = iTmp;
                sTmp = wnd.Element("kph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.maxwind_kph = iTmp;
                fd.maxwind_dir = wnd.Element("dir").Value;
                fd.maxwind_degrees = wnd.Element("degrees").Value;

                //AVERAGE WIND
                wnd = (from x in item.Descendants()
                       where x.Name.LocalName == "avewind"
                       select x).FirstOrDefault();
                sTmp = wnd.Element("mph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.avewind_mph = iTmp;
                sTmp = wnd.Element("kph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.avewind_kph = iTmp;
                fd.avewind_dir = wnd.Element("dir").Value;
                fd.avewind_degrees = wnd.Element("degrees").Value;

                Console.WriteLine(sTmp);
                // HUMIDITY
                sTmp = item.Element("avehumidity").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.avehumidity = iTmp;
                sTmp = item.Element("maxhumidity").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.maxhumidity = iTmp;
                sTmp = item.Element("minhumidity").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.minhumidity = iTmp;

                //TEMPERATURE
                var temp2 = from m in item.Descendants()
                            where (m.Name.LocalName == "high")
                            select m;
                var temeperatura = from m in temp2.Descendants()
                                   where m.Name.LocalName == "celsius"
                                   select m.Value;

                sTmp = temeperatura.First();
                fd.highTempC = sTmp;
                temp2 = from m in item.Descendants()
                        where (m.Name.LocalName == "low")
                        select m;
                temeperatura = from m in temp2.Descendants()
                               where m.Name.LocalName == "celsius"
                               select m.Value;
                sTmp = temeperatura.First();
                fd.lowTempC = sTmp;
                nastepneDni.Add(fd);
            }

            foreach (var item in forecast)
            {
                Console.WriteLine(item);
                ForecastDay d = new ForecastDay();
                d.period = item.Element("period").Value;
                d.icon = item.Element("icon").Value;
                d.iconUrl = item.Element("icon_url").Value;
                d.fcttext = item.Element("fcttext").Value;
                d.fcttextMetric = item.Element("fcttext_metric").Value;
                d.title = item.Element("title").Value;
                d.pop = item.Element("pop").Value;
                int p = 0;
                if (int.TryParse(d.period, out p))
                {

                    if (p % 2 == 0)
                    {
                        ForecastDay demo = nastepneDni.Find(df => df.period.Equals(simplePeriod.ToString()));
                        d.avehumidity = demo.avehumidity;
                        d.avewind_degrees = demo.avewind_degrees;
                        d.avewind_dir = demo.avewind_dir;
                        d.avewind_kph = demo.avewind_kph;
                        d.avewind_mph = demo.avewind_mph;
                        d.conditions = PierwszaLiteraWielka(demo.conditions);
                        d.data2 = demo.data2;
                        d.highTempC = demo.highTempC;
                        d.lowTempC = demo.lowTempC;
                        d.maxhumidity = demo.maxhumidity;
                        d.maxwind_degrees = demo.maxwind_degrees;
                        d.maxwind_dir = demo.maxwind_dir;
                        d.maxwind_kph = demo.maxwind_kph;
                        d.maxwind_mph = demo.maxwind_mph;
                        d.minhumidity = demo.minhumidity;
                        d.qpfAllDay = demo.qpfAllDay;
                        d.skyicon = demo.skyicon;

                        simplePeriod++;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
                            System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                            DaysUC b = new DaysUC();
                            ImageSource imgSrc;
                            b.Tytul = d.title;
                            b.Wilgotnosc = d.avehumidity.ToString() + "%";
                            b.Warunki = d.conditions;
                            b.Dzien = d.data2.ToString("d MMM yyyy");
                            if (d.snowAllDay != null && d.qpfAllDay != null)
                                b.IloscOpadow = d.qpfAllDay.ToString() + "mm(D)," + d.snowAllDay.ToString() + "mm(Ś)";
                            else if (d.snowAllDay != null && d.qpfAllDay == null)
                                b.IloscOpadow = "0mm(D)," + d.snowAllDay.ToString() + "(Ś)";
                            else if (d.snowAllDay == null && d.qpfAllDay != null)
                                b.IloscOpadow = d.qpfAllDay.ToString() + "mm(D),0mm(Ś)";
                            else if (d.snowAllDay == null && d.qpfAllDay == null)
                                b.IloscOpadow = "0 mm";
                            b.PrawdOpadow = d.pop + "%";
                            b.TempMin = d.lowTempC.ToString() + "C";
                            b.TempMax = d.highTempC.ToString() + "C";
                            b.Wiatr = d.avewind_kph.ToString() + "km/h";

                            imgSrc = new BitmapImage(new Uri("Icons/" + d.icon + ".png", UriKind.Relative));


                            b.Ikona = imgSrc;
                            ndStackPanel.Children.Add(b);
                        });


                    }
                    else
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
                            System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                            DaysUC b = new DaysUC();
                            ImageSource imgSrc;
                            b.Tytul = d.title;
                            var strings = d.fcttextMetric.Split('.', ':');
                            int i = strings.Length;
                            switch (i)
                            {
                                case 6:
                                    b.Warunki = strings[0];
                                    b.TempMin = strings[3];
                                    b.TempMax = strings[3];
                                    b.Wiatr = strings[5];
                                    b.PrawdOpadow = "";
                                    b.IloscOpadow = "";
                                    break;
                                case 9:
                                    b.Warunki = strings[0];
                                    b.TempMax = strings[3];
                                    b.TempMin = strings[3];
                                    b.Wiatr = strings[5];
                                    b.PrawdOpadow = "";
                                    b.IloscOpadow = "";
                                    b.Wilgotnosc = strings[7];
                                    break;
                                case 11:
                                    b.Warunki = strings[0];
                                    b.TempMax = strings[3];
                                    b.TempMin = strings[3];
                                    b.Wiatr = strings[5];
                                    b.PrawdOpadow = (strings[9].Split(' ')).Last();
                                    b.IloscOpadow = "";
                                    b.Wilgotnosc = strings[7];
                                    break;
                                default:
                                    b.Warunki = strings[0];
                                    b.TempMax = strings[3];
                                    b.Wiatr = strings[5];
                                    b.PrawdOpadow = "";
                                    b.IloscOpadow = "";
                                    b.Wilgotnosc = "";
                                    break;
                            }
                            b.Dzien = "";
                            b.Warunki = PierwszaLiteraWielka(b.Warunki);
                            imgSrc = new BitmapImage(new Uri("Icons/" + d.icon + ".png", UriKind.Relative));


                            b.Ikona = imgSrc;
                            ndStackPanel.Children.Add(b);
                        });
                    }

                }
                nastepneDni2.Add(d);

            }
            var dzien = (from d in nastepneDni where d.period == "1" select d).FirstOrDefault();
            // var dzien2 = (from d in dni2 where d.period=="0" select d).FirstOrDefault();
            if (!(dzien == null))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Debug.WriteLine("dzien" + dzien.ToString());
                    TextBlock oDniu = this.oDniu;
                    oDniu.TextWrapping = TextWrapping.Wrap;
                    oDniu.Text = "Dzisiaj jest " + dzien.data.day + " " + dzien.data.monthName + " " + dzien.data.year + ", " + dzien.data.weekDay + ". To " + dzien.data.yday+1 + " dzień roku.";
                    oDniu.Visibility = Visibility.Visible;
                });
            }
        }
        #endregion  OBRABIANIE ASTRONOMII, POGODY GODZINOWEJ I NA NASTĘPNE DNI

        //UBRANIA
        #region UBRANIA
        public List<string> pogodaDlaUbran()
        {
            List<string> ubrania = new List<string>();
            string pogodaZaGodzine = "";
            for (int i = 0; i <= 2; i++)
            {
                //pogodaZaGodzine = nastepneDni2.ElementAt(i).fcttextMetric;
                pogodaZaGodzine = godzinowaPrognoza.ElementAt(i).condition;
                //var s = pogodaZaGodzine.Split('.');
                //string zapowiedz = s[0];

                if ((pogodaZaGodzine.Contains("deszcz")) || (pogodaZaGodzine.Contains("burz")))
                    czyBedziePadac = true;
            }
            if ((pog.Contains("deszcz")) || (pog.Contains("mżawka")) || (czyBedziePadac))
                ubrania.Add("parasolka_k.png");

            //double temp = 0, t = 0; 
            //if (double.TryParse(temperatura, out t))
            //    temp = t;
            if (temperaturaOdczuwalna <= 0)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("kurtka_zimowa_czapka_k.png");
            }
            else if (temperaturaOdczuwalna < 5)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("kurtka_rekawiczki_k.png");
                ubrania.Add("czapka_k.png");

            }
            else if (temperaturaOdczuwalna < 10)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("kurtka_k.png"); //tu dałabym cieplejszą kurtkę
            }
            else if (temperaturaOdczuwalna < 18)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("plaszcz_k.png");
            }
            else if (temperaturaOdczuwalna < 23)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("dlrekaw.png");
            }
            else if (temperaturaOdczuwalna < 28)
            {
                ubrania.Add("sandalki_k.png");
                ubrania.Add("spodniekr_k.png");
                ubrania.Add("tshirt_k.png");
            }
            else
            {
                ubrania.Add("sandalki_k.png");
                ubrania.Add("spodniekr_k.png");
                ubrania.Add("podkoszulek_k.png");
            }

            return ubrania;
        }

        public void ubranie()
        {

            List<string> ubrania = pogodaDlaUbran();

            List<BitmapImage> images = new List<BitmapImage>();
            int width = 0; // Final width.
            int height = 0; // Final height.
            Debug.WriteLine("TUTAJ, w ubraniu na początku");
            Dispatcher.BeginInvoke(() =>
            {
                WriteableBitmap wb = null;
                try
                {
                    foreach (string image in ubrania)
                    {
                        // Create a Bitmap from the file and add it to the list    

                        BitmapImage img = new BitmapImage(new Uri("Ubrania/" + image, UriKind.RelativeOrAbsolute));

                        StreamResourceInfo r = System.Windows.Application.GetResourceStream(new Uri("Ubrania/" + image, UriKind.RelativeOrAbsolute));
                        img.SetSource(r.Stream);
                        //wb = new WriteableBitmap(img);
                        //Update the size of the final bitmap
                        //width = wb.PixelWidth > width ? wb.PixelWidth : width;
                        //height = wb.PixelHeight > height ? wb.PixelHeight : height;

                        images.Add(img);

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                // Create a bitmap to hold the combined image 
                BitmapImage finalImage = new BitmapImage();

                StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri("Ubrania/kobieta.png",
                    UriKind.RelativeOrAbsolute));
                finalImage.SetSource(sri.Stream);
                wbFinalna = new WriteableBitmap(finalImage);

                width = finalImage.PixelWidth;
                height = finalImage.PixelHeight;

                using (MemoryStream mem = new MemoryStream())
                {

                    foreach (BitmapImage item in images)
                    {
                        Image image = new Image();
                        image.Height = height;
                        image.Width = width;
                        image.Source = item;

                        // TranslateTransform                      
                        TranslateTransform tf = new TranslateTransform();
                        tf.X = 0;
                        tf.Y = 0;
                        wbFinalna.Render(image, tf);

                        // tempHeight += item.PixelHeight;
                    }

                    wbFinalna.Invalidate();
                    wbFinalna.SaveJpeg(mem, width, height, 0, 100);
                    mem.Seek(0, System.IO.SeekOrigin.Begin);

                    // Show image. 
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ubranieIMG.Source = wbFinalna;
                    });
                }

            });

        }
        #endregion UBRANIA

        // SPORTY
        #region SPORTY
        public void sportyLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // throw new NotImplementedException();
            var nazwaSportu = (sender as ListBox).SelectedItem as String;
            if (nazwaSportu.Equals("Zostań w domu"))
            {
                Debug.WriteLine("Nieznany rodzaj pogody, dla którego nie ma żadnych sportów :C");
            }
            else
            {
                string query = "";
                if (nazwaSportu.Equals("Rower"))
                    query = "\"Ścieżka rowerowa\"";
                else if (nazwaSportu.Equals("Siatkówka"))
                    query = "\"Boisko siatkówka\"";
                else if (nazwaSportu.Equals("Koszykówka"))
                    query = "\"Boisko koszykówka\"";
                else if (nazwaSportu.Equals("Piłka nożna"))
                    query = "\"Boisko do piłki nożnej orlik\"";
                else if (nazwaSportu.Equals("Jazda konna"))
                    query = "\"Stadnina koni\"";
                else if (nazwaSportu.Equals("Tenis"))
                    query = "\"Kort tenisowy\"";
                else if ((nazwaSportu.Equals("Łyżwy")) || (nazwaSportu.Equals("Hokej")))
                    query = "\"Lodowisko\"";
                else if ((nazwaSportu.Equals("Narciarstwo")) || (nazwaSportu.Equals("Snowboard")))
                    query = "\"Stok narciarski\"";
                else if (nazwaSportu.Equals("Trening sztuk walki"))
                    query = "\"Szkoła sztuk walki\"";
                else
                    query = nazwaSportu;


                Debug.WriteLine("Działa to to?                 " + nazwaSportu + "/" + query);
                var wbt = new WebBrowserTask();
                Uri uri = new Uri("https://maps.google.pl/maps?q=" + miasto + "+" + query, UriKind.RelativeOrAbsolute);
                wbt.Uri = uri;
                //sportyLB.SelectedItem=null;
                wbt.Show();
            }
        }


        public void wyborSportow()
        {
            poraDnia = getPoraDnia();
            poraRoku = getPoraRoku();

            String pogoda2 = pog.ToLower();
            if (pogoda2.Equals("pogodnie"))
            {
                ladnaPogoda(poraDnia);
            }
            else if (pogoda2.Equals("przewaga chmur"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("obłoki zanikające"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("gęsty śnieg"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Contains("śnieg"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Contains("śnieżek"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("niewielkie zachmurzenie"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Contains("deszcz"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Equals("pochmurno"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Contains("mgły"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Contains("mżawka"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Contains("mgła"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Contains("zamglenia"))
                ladnaPogoda(poraDnia);
            else
                listaSportow.Add("Zostań w domu");

        }

        public char getPoraDnia()
        {
            char pora;

            if ((godzina >= 6) && (godzina < 10))
                pora = 'r'; // ranek
            else if ((godzina >= 10) && (godzina < 14))
                pora = 'p'; // "poludnie";
            else if ((godzina >= 14) && (godzina < 18))
                pora = 'o'; // "popołudnie";
            else if ((godzina >= 18) && (godzina < 22))
                pora = 'w'; // "wieczór";
            else if ((godzina >= 22) && (godzina < 1))
                pora = 'n'; // "noc";
            else
                pora = 'g'; // "głęboka noc";

            return pora;
        }

        public void ladnaPogoda(char poraDnia)
        {
            //int temp = 0;
            //if (int.TryParse(temperatura, out temp))
            //    Debug.WriteLine(temp);
            if ((temperaturaOdczuwalna > -30) && (temperaturaOdczuwalna < 35))
            {
                standardowe();
                naHali();
                naDworze();
            }
        }

        public void deszczowaPogoda(char poraDnia)
        {
            naHali();
        }

        public char getPoraRoku()
        {
            // można dodać np. przedwiośnie
            char c;

            if ((miesiac == 4) || (miesiac == 5))
            { // kwiecien-maj
                c = 'w';
            }
            else if ((miesiac >= 6) && (miesiac <= 8))
            { // czerwiec-sierpien
                c = 'l';
            }
            else if ((miesiac >= 9) && (miesiac <= 11))
            { // wrzesien-list
                c = 'j';
            }
            else // grudzien-marzec
            {
                c = 'z';
            }

            return c;

        }

        public void standardowe()
        {
            try
            {
                listaSportow.Add("Bieganie");
                listaSportow.Add("Rower");
                listaSportow.Add("Joga");
                listaSportow.Add("Nordic walking");
                listaSportow.Add("Rolki");
                listaSportow.Add("Wrotki");
                listaSportow.Add("Deskorolka");
            }
            catch (Exception ccc)
            {
                Debug.WriteLine(ccc.Message);
            }
        }

        public void naHali()
        {
            try
            {
                char poraDnia = getPoraDnia();
                if (poraDnia.Equals('p') || (poraDnia.Equals('o')) || (poraDnia.Equals('w')))
                {
                    listaSportow.Add("Siatkówka");
                    listaSportow.Add("Koszykówka");
                    listaSportow.Add("Piłka nożna");
                    listaSportow.Add("Badminton");
                    listaSportow.Add("Squash");
                    listaSportow.Add("Siłownia");
                    listaSportow.Add("Szermierka");
                    listaSportow.Add("Łucznictwo");
                    listaSportow.Add("Strzelnica");
                    listaSportow.Add("Ściana wspinaczkowa");
                    listaSportow.Add("Trening sztuk walki");
                    listaSportow.Add("Basen");
                    listaSportow.Add("Ping-pong");
                }
            }
            catch (Exception ccc)
            {
                Debug.WriteLine(ccc.Message);
            }

        }

        public void naDworze()
        {
            try
            {
                if ((poraDnia.Equals('p')) || (poraDnia.Equals('o')) || (poraDnia.Equals('w')))
                {
                    if (!(poraRoku.Equals('z')))
                    {
                        listaSportow.Add("BMX");
                        listaSportow.Add("Quady");
                        listaSportow.Add("Gokarty");
                        listaSportow.Add("Golf");
                        listaSportow.Add("Jazda konna");
                        listaSportow.Add("Paintball");
                        listaSportow.Add("Tenis");
                    }
                    else
                    {
                        listaSportow.Add("Łyżwy");
                        listaSportow.Add("Snowboard");
                        listaSportow.Add("Narciarstwo");
                        listaSportow.Add("Hokej");
                        listaSportow.Add("Sanki");
                    }
                }
            }
            catch (Exception ccc)
            {
                Debug.WriteLine(ccc.Message);
            }
        }

        public void okazjonalne()
        {
            try
            {
                listaSportow.Add("Pływanie");
                listaSportow.Add("Kajaki");
                listaSportow.Add("Pływanie łódką"); // pontonem?
                listaSportow.Add("Nurkowanie");
                listaSportow.Add("Narty wodne");
                listaSportow.Add("Piłka wodna");
                listaSportow.Add("Serfowanie");
                listaSportow.Add("Siatkówka plażowa");
            }
            catch (Exception ccc)
            {
                Debug.WriteLine(ccc.Message);
            }
        }



        #endregion SPORTY

    }
}