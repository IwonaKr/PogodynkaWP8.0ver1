﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
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
using Microsoft.Xna.Framework.Media;

namespace PogodynkaWP8._0ver1
{
    public partial class Pogoda : PhoneApplicationPage
    {
        #region ZMIENNE
        public bool czyBylaJuzUzywana=false;
        public IsolatedStorageSettings ustawienia = IsolatedStorageSettings.ApplicationSettings;

        public string miasto;
        public static string mess; //potrzebne do linka
        public bool czyToGPS;
        public static List<ForecastDay> dni2= new List<ForecastDay>(); //txt_forecast
        public static List<ForecastDay> SFDay = new List<ForecastDay>(); //SimpleForecast
        public static List<HourlyForecast> HourlyForecast = new List<HourlyForecast>();
        public static ObservableCollection<String> listaSportow = new ObservableCollection<string>(); //lista ze sportami
        public static ObservableCollection<String> listaAktywnosci = new ObservableCollection<string>();
        public static Astronomy astronomy;

        //do sportów i wypoczynku
        public string pog="";
        public string wiatr="";
        public int godzina=0;
        public int miesiac=0;
        public string dzienTygodnia="";
        public string temperatura="";
        public char poraDnia= new char();
        public char poraRoku= new char();

        //do ubrań
        public WriteableBitmap wbFinal=null;
        public List<string> ubrania = new List<string>(); //lista z wybranymi ubraniami
        public bool czyBedziePadac = false;
        #endregion

        public Pogoda()
        {
            InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string msg;
            if (!(czyBylaJuzUzywana))
            {
                if (NavigationContext.QueryString.TryGetValue("msg", out msg))
                {
                    if (msg.Contains(","))
                    {
                        mess=msg;
                        var cos = mess.Split(',');
                        if (cos.Length==4)
                        {
                            mess=cos[0]+"."+cos[1]+","+cos[2]+"."+cos[3];
                        }
                        this.miasto="GPS: "+msg;
                        Debug.WriteLine("GPS");
                        czyToGPS=true;
                    }
                    else
                    {
                        this.miasto=msg;
                        mess="Poland/"+msg;
                        Debug.WriteLine("MIASTO ");
                        czyToGPS=false;
                    }
                    this.miastoTB.Text=miasto;

                    Thread t = new Thread(NewThread);
                    t.Start();
                }
                czyBylaJuzUzywana=true;
            }
        }

        public void NewThread()
        {
            string url = "http://api.wunderground.com/api/c9d15b10ff3ed303/forecast/forecast10day/hourly/conditions/astronomy/lang:PL/q/"+mess+".xml";

            WebClient wc = new WebClient();
            //wc.DownloadStringCompleted += HttpsCompleted;
            wc.DownloadStringCompleted +=wc_DownloadStringCompleted; //dodane, bez tej funkcji też działa!!

            wc.DownloadStringAsync(new Uri(url));

        }

        public void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string weather="";
            //try
            //{
            weather = e.Result;
            XmlReader reader = XmlReader.Create(new StringReader(weather));
            XDocument doc = XDocument.Load(reader);

            obrabianieAstronomy(doc);

            obrabianieConditions(doc);

            obrabianieHourlyForecast(doc);

            wyborSportow();

            ubranie();

            wyborWypoczynku();

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ///Wyświetlanie listy sportów i aktywności

                this.sportyLB.ItemsSource=listaSportow;
                this.sportyLB.SelectionChanged+=sportyLB_SelectionChanged;

                this.wypoczynekLB.ItemsSource=listaAktywnosci;
                this.wypoczynekLB.SelectionChanged+=wypoczynekLB_SelectionChanged;
            });


            //}
            //catch (Exception ex)
            //{
            //    Deployment.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK);
            //        this.textBox1.Text=ex.Message;
            //        this.ikonka.Source=null;
            //    });
            //}
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
            else if (pogoda2.Equals("śnieg"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("niewielkie zachmurzenie"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("deszcz"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("lekki deszcz"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("pochmurno"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("płatki mgły"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("lekkie przelotne deszcze"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("lekka mżawka"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("zamglenia"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("mżawka"))
                deszczowaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("mgła"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("lekka mgła"))
                ladnaPogodaWyp(poraDnia);
            else if (pogoda2.Equals("częściowe zamglenia"))
                ladnaPogodaWyp(poraDnia);
            else
            {
                //listaAktywnosci.Add("Nieznany rodzaj pogody");
                zalezneWyp();
                podDachemWyp();
                podstawoweWyp();

            }
        }

        private void deszczowaPogodaWyp(char poraDnia)
        {
            podDachemWyp();
        }

        private void ladnaPogodaWyp(char poraDnia)
        {
            int temp=0;
            if (int.TryParse(temperatura, out temp))
                Debug.WriteLine(temp);
            if ((temp > -30) && (temp < 35))
            {
                podstawoweWyp();
                niePadaWyp();
                okazjonalneWyp();
            }

            podDachemWyp();
            zalezneWyp();
        }

        private void zalezneWyp()
        {
            string pogoda2 = pog.ToLower();
            if ((pogoda2.Equals("pogodnie"))||(pogoda2.Equals("niewielkie zachmurzenie"))||(pogoda2.Equals("obłoki zanikające")))
            {
                TimeSpan wschod1 = new TimeSpan(astronomy.sunrise.Hour-1, astronomy.sunrise.Minute, 0); //-1 godzina do wschodu
                TimeSpan zachod1 = new TimeSpan(astronomy.sunset.Hour-1, astronomy.sunset.Minute, 0); //-1 godzina do zachodu
                TimeSpan wschod = new TimeSpan(astronomy.sunrise.Hour, astronomy.sunrise.Minute, 0);
                TimeSpan zachod = new TimeSpan(astronomy.sunset.Hour, astronomy.sunset.Minute, 0);
                TimeSpan teraz = DateTime.Now.TimeOfDay;
                if ((teraz<wschod)&&(teraz>wschod1))
                {
                    listaAktywnosci.Add("Podziwiaj wschód słońca");
                    Debug.WriteLine("Wschód");
                }
                else if ((teraz<zachod)&&(teraz>zachod1))
                {
                    listaAktywnosci.Add("Podziwiaj zachód słońca");
                    Debug.WriteLine("Zachód");
                }
                else if ((teraz>zachod) ||(teraz<wschod1))
                {
                    Debug.WriteLine("Gwiazdy");
                    listaAktywnosci.Add("Podziwiaj gwiazdy");
                }
                else if ((teraz<zachod1)||(teraz>wschod))
                {
                    listaAktywnosci.Add("Podziwiaj chmury / niebo ");
                    Debug.WriteLine("Chmury");
                }
                Debug.WriteLine(wschod.ToString()+" "+zachod.ToString()+" "+teraz.ToString());
            }
        }

        private void podDachemWyp()
        {
            if ((poraDnia.Equals('p'))||(poraDnia.Equals('o'))||(poraDnia.Equals('w')))
            {
                listaAktywnosci.Add("Pub");
                listaAktywnosci.Add("Kawiarnia");
                listaAktywnosci.Add("Restauracja");
                listaAktywnosci.Add("Kino");
                listaAktywnosci.Add("Kręgle");
                listaAktywnosci.Add("Muzeum");
                listaAktywnosci.Add("Biblioteka");
                listaAktywnosci.Add("Teatr");
                listaAktywnosci.Add("Aquapark");
                listaAktywnosci.Add("Zakupy");
                listaAktywnosci.Add("Zajęcia plastyczne");
                listaAktywnosci.Add("Zajęcia muzyczne");
            }
            if ((poraDnia.Equals('w')||(poraDnia.Equals('n'))))
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

        private void niePadaWyp()
        {
            listaAktywnosci.Add("Ogród zoologiczny");

        }

        private void podstawoweWyp()
        {
            listaAktywnosci.Add("Spacer");
            listaAktywnosci.Add("Spotkanie z przyjaciółmi");
            listaAktywnosci.Add("Spacer z psem");
            listaAktywnosci.Add("Fotografowanie");
            listaAktywnosci.Add("Rysowanie");
        }

        public void wypoczynekLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string query="";

            var nazwaAktywnosci= (sender as ListBox).SelectedItem as String;
            if (!(nazwaAktywnosci.Equals(null)))
            {
                if ((nazwaAktywnosci.Equals("Spacer")) ||(nazwaAktywnosci.Equals("Spacer z psem")) || (nazwaAktywnosci.Equals("Fotografowanie"))) { query="Park"; }
                else if ((nazwaAktywnosci.Equals("Impreza"))||(nazwaAktywnosci.Equals("Randka w ciemno")))
                {
                    query="Club";
                }
                else if (nazwaAktywnosci.Equals("Zakupy"))
                {
                    query="\"Centrum handlowe\"";
                }
                else if ((nazwaAktywnosci.Equals("Zajęcia plastyczne"))||(nazwaAktywnosci.Equals("Zajęcia muzyczne")))
                {
                    query="\"Dom kultury\"";
                }
                else if (nazwaAktywnosci.Equals("Koncert"))
                {
                    var wbt2 = new WebBrowserTask();
                    wbt2.Uri=new Uri("https://www.google.pl/#q="+miasto+"+Koncerty", UriKind.RelativeOrAbsolute);
                    wbt2.Show();
                    query="";
                }
                else if (nazwaAktywnosci.Equals("Spotkanie z przyjaciółmi"))
                {
                    query=""; //?
                }
                else if ((nazwaAktywnosci.Equals("Podziwiaj chmury / niebo "))||
                (nazwaAktywnosci.Equals("Podziwiaj gwiazdy"))||
                (nazwaAktywnosci.Equals("Podziwiaj zachód słońca"))||
                (nazwaAktywnosci.Equals("Podziwiaj wschód słońca")))
                {
                    query="";
                }
                else
                {
                    query="\""+nazwaAktywnosci+"\"";
                }
                Debug.WriteLine("Działa to to?                 "+ nazwaAktywnosci+"/"+query);
                if (!(query.Equals("")))
                {
                    var wbt = new WebBrowserTask();
                    Uri uri = new Uri("https://maps.google.pl/maps?q=" + miasto + "+" + query, UriKind.RelativeOrAbsolute);
                    wbt.Uri=uri;
                    wbt.Show();
                }

            }
        }

        #endregion WYPOCZYNEK

        //OBRABIANIE ASTRONOMII, POGODY GODZINOWEJ I NA NASTĘPNE DNI
        #region OBRABIANIE ASTRONOMII, POGODY GODZINOWEJ I NA NASTĘPNE DNI

        public static void obrabianieAstronomy(XDocument doc)
        {
            astronomy = new Astronomy();
            int hTmp=0, mTmp=0, ho=0, m=0;

            var moon_phase = (from d in doc.Descendants()
                              where (d.Name.LocalName=="moon_phase")
                              select d).FirstOrDefault();
            astronomy.ageOfMoon=moon_phase.Element("ageOfMoon").Value;
            astronomy.percentIlluminated=moon_phase.Element("percentIlluminated").Value;
            var tmp = (from d in moon_phase.Descendants()
                       where (d.Name.LocalName=="sunset")
                       select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp))&&(int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                ho=hTmp;
                m=mTmp;
                DateTime cos=new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, ho, m, 0);
                astronomy.moonset=cos;
            }
            tmp = (from d in moon_phase.Descendants()
                   where (d.Name.LocalName=="sunrise")
                   select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp))&&(int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                ho=hTmp;
                m=mTmp;
                DateTime cos=new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, ho, m, 0);
                astronomy.moonrise=cos;
            }
            moon_phase = (from d in doc.Descendants()
                          where (d.Name.LocalName=="sun_phase")
                          select d).FirstOrDefault();
            tmp = (from d in moon_phase.Descendants()
                   where (d.Name.LocalName=="sunrise")
                   select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp))&&(int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                ho=hTmp;
                m=mTmp;
                DateTime cos=new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, ho, m, 0);
                astronomy.sunrise=cos;
            }
            tmp = (from d in moon_phase.Descendants()
                   where (d.Name.LocalName=="sunset")
                   select d).FirstOrDefault();
            if ((int.TryParse(tmp.Element("hour").Value, out hTmp))&&(int.TryParse(tmp.Element("minute").Value, out mTmp)))
            {
                ho=hTmp;
                m=mTmp;
                DateTime cos=new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, ho, m, 0);
                astronomy.sunset=cos;
            }


            //KONIEC ASTRONOMY
        }

        private void obrabianieHourlyForecast(XDocument doc)
        {
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;

            var hourly_forecast = (from d in doc.Descendants()
                                   where (d.Name.LocalName=="hourly_forecast")
                                   select d).ToList();
            // Debug.WriteLine("HF!! "+hourly_forecast.ToString());

            var hourly = (from d in hourly_forecast.Descendants()
                          where (d.Name.LocalName=="forecast")
                          select d).ToList();
            int i=0;
            foreach (var item in hourly)
            {
                int iTmp=0;
                HourlyForecast hf = new HourlyForecast();
                var FCTTIME = (from d in item.Descendants()
                               where (d.Name.LocalName=="FCTTIME")
                               select d).FirstOrDefault();
                int h=0, min=0, s=0, y=0, mon=0, mday=0;
                if (int.TryParse(FCTTIME.Element("hour").Value, out iTmp))
                    h=iTmp;
                if (int.TryParse(FCTTIME.Element("min").Value, out iTmp))
                    min=iTmp;
                if (int.TryParse(FCTTIME.Element("year").Value, out iTmp))
                    y=iTmp;
                if (int.TryParse(FCTTIME.Element("mon").Value, out iTmp))
                    mon=iTmp;
                if (int.TryParse(FCTTIME.Element("mday").Value, out iTmp))
                    mday=iTmp;

                DateTime dt = new DateTime(y, mon, mday, h, min, s);
                hf.czas=dt;

                hf.monAbbrev = FCTTIME.Element("mon_abbrev").Value;
                hf.monthAbbrev=FCTTIME.Element("month_name_abbrev").Value;
                hf.pretty=FCTTIME.Element("pretty").Value;
                hf.weekdayNameAbbrev=FCTTIME.Element("weekday_name_abbrev").Value;
                hf.weekdayNameNight=FCTTIME.Element("weekday_name_night").Value;
                //Debug.WriteLine(hf.czas.ToLongDateString()+" "+hf.czas.ToLocalTime()+" "+hf.weekdayNameAbbrev+" "+hf.monthAbbrev);
                hf.condition=item.Element("condition").Value;
                hf.icon=item.Element("icon").Value;
                hf.iconUrl=item.Element("icon_url").Value;
                hf.sky=item.Element("sky").Value;
                hf.humidity=item.Element("humidity").Value;
                hf.pop=item.Element("pop").Value;
                hf.fctcode=item.Element("fctcode").Value;
                hf.tempC=((from d in item.Descendants()
                           where d.Name.LocalName=="temp"
                           select d).FirstOrDefault()).Element("metric").Value;
                hf.dewpointC=((from d in item.Descendants()
                               where d.Name.LocalName=="dewpoint"
                               select d).FirstOrDefault()).Element("metric").Value;
                hf.windKph=((from d in item.Descendants()
                             where d.Name.LocalName=="wspd"
                             select d).FirstOrDefault()).Element("metric").Value;
                hf.windDir=(((from d in item.Descendants() where d.Name.LocalName=="wdir" select d).FirstOrDefault()).Element("dir").Value);
                hf.windDegrees=(((from d in item.Descendants() where d.Name.LocalName=="wdir" select d).FirstOrDefault()).Element("degrees").Value);

                if (int.TryParse((((from d in item.Descendants()
                                    where d.Name.LocalName=="windchill"
                                    select d).FirstOrDefault()).Element("metric").Value), out iTmp))
                {
                    if (iTmp<-100)
                        hf.windchill="";
                    else
                        hf.windchill=iTmp.ToString();
                }

                if (int.TryParse((((from d in item.Descendants()
                                    where d.Name.LocalName=="heatindex"
                                    select d).FirstOrDefault()).Element("metric").Value), out iTmp))
                {
                    if (iTmp<-100)
                        hf.heatindex="";
                    else
                        hf.heatindex=iTmp.ToString();
                }
                hf.feelslike=(((from d in item.Descendants()
                                where d.Name.LocalName=="feelslike"
                                select d).FirstOrDefault()).Element("metric").Value);
                hf.qpf=(((from d in item.Descendants()
                          where d.Name.LocalName=="qpf"
                          select d).FirstOrDefault()).Element("metric").Value);
                hf.snow=(((from d in item.Descendants()
                           where d.Name.LocalName=="snow"
                           select d).FirstOrDefault()).Element("metric").Value);
                hf.pressure=(((from d in item.Descendants()
                               where d.Name.LocalName=="mslp"
                               select d).FirstOrDefault()).Element("metric").Value);
                if (i==0)
                {
                    dzienTygodnia=dt.DayOfWeek.ToString();
                    miesiac=dt.Month;
                    godzina=dt.Hour;
                    temperatura=hf.tempC;
                    wiatr=hf.windKph;
                    if (pog.Equals(""))
                        pog=hf.condition;
                    i++;
                }
                HourlyForecast.Add(hf);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
                    //System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                    HourlyUC b = new HourlyUC();
                    b.Godz=hf.czas.ToString("HH:mm");
                    b.Dzien=hf.czas.ToString("d MMM yyyy");
                    b.Warunki=hf.condition;
                    b.opady.Text="Opady: "+hf.qpf;
                    b.temperatura.Text="Temp: "+hf.tempC+"C";
                    ImageSource imgSrc;
                    if (hf.czas.Hour<=astronomy.sunrise.Hour || hf.czas.Hour>=astronomy.sunset.Hour)
                    {
                        imgSrc = new BitmapImage(new Uri("Icons/nt_"+hf.icon+".png", UriKind.Relative));
                    }
                    else
                    {
                        imgSrc = new BitmapImage(new Uri("Icons/"+hf.icon+".png", UriKind.Relative));
                    }

                    b.ikonka.Source=imgSrc;
                    hStackPanel.Children.Add(b);
                });
            }
        }

        private void obrabianieConditions(XDocument doc)
        {
            var current_obs = (from d in doc.Descendants()
                               where (d.Name.LocalName=="current_observation")
                               select d).FirstOrDefault();
            if (czyToGPS)
            {

                Debug.WriteLine(current_obs.ToString());

                var disLoc = (from d in current_obs.Descendants()
                              where (d.Name.LocalName=="display_location")
                              select d).FirstOrDefault();
                //var place = (from d in disLoc.Descendants()
                //             where (d.Name.LocalName=="full")
                //             select d).FirstOrDefault();
                /*Pobieranie aktualnych danych */



                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.miasto=disLoc.Element("full").Value;
                    this.miastoTB.Text=disLoc.Element("full").Value;
                    Debug.WriteLine(disLoc.ToString());
                });


            }
            ForecastDay curObs = new ForecastDay();
            curObs.conditions=current_obs.Element("weather").Value;
            if (curObs.conditions.Equals(null))
                pog=" ";
            else
                pog=curObs.conditions; //do sportów potrzebne
            curObs.highTempC=current_obs.Element("temp_c").Value; //taka zwykła temperatura
            curObs.lowTempC=current_obs.Element("feelslike_c").Value; //odczuwalna
            curObs.icon=current_obs.Element("icon").Value;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                String cos = "";
                cos =pog+"\nTemperatura: "+curObs.highTempC+"C     Odczuwalna: "+curObs.lowTempC+"C\n"+
                        "Wiatr: "+current_obs.Element("wind_kph").Value+"km/h,   w porywach do: "+current_obs.Element("wind_gust_kph").Value+"km/h   "+current_obs.Element("wind_dir").Value+"\n"+
                        "Wilgotność: "+current_obs.Element("relative_humidity").Value+
                        "\nCiśnienie: "+current_obs.Element("pressure_mb").Value+"hPa, "+current_obs.Element("pressure_trend").Value+"\nWidoczność: ";
                if (!(current_obs.Element("visibility_km").Value).Equals("N/A"))
                    cos+=current_obs.Element("visibility_km").Value+"km\nOpady (dzień/godz):";
                else
                    cos+=" \nOpady (dzien/godz): ";
                //        "\nWidoczność: "+current_obs.Element("visibility_km").Value+"km\nOpady (dzień/godz):";
                if (!(current_obs.Element("precip_1hr_metric").Value).Contains('-'))
                    cos=cos+current_obs.Element("precip_1hr_metric").Value+" mm/";
                else
                    cos+=" - /";
                if (!(current_obs.Element("precip_today_metric").Value).Contains('-'))
                    cos+=current_obs.Element("precip_today_metric").Value+" mm";
                else
                    cos+=" -";
                //        "\nOpady (godz/dzień): "+current_obs.Element("precip_1hr_metric").Value+"mm/"+current_obs.Element("precip_today_metric").Value+"mm";
                this.textBox1.Text=cos;
                Uri uri =null;
                if ((DateTime.Now<astronomy.sunrise)||(DateTime.Now>astronomy.sunset)) //po zachodzie słońca
                {
                    Debug.WriteLine("sunrise: "+astronomy.sunrise.Hour+" "+astronomy.sunset.Hour);
                    uri  =  new Uri("Icons/nt_"+curObs.icon+".png", UriKind.Relative);
                }
                else
                {
                    uri = new Uri("Icons/"+curObs.icon+".png", UriKind.Relative);
                }
                ImageSource imgSource = new BitmapImage(uri);
                this.ikonka.Source = imgSource;

            });

            var txt_forecast = (from d in doc.Descendants()
                                where (d.Name.LocalName == "txt_forecast")
                                select d).ToList();

            var forecast = (from d in txt_forecast.Descendants()
                            where (d.Name.LocalName=="forecastday")
                            select d).ToList();
            var simpleForecast = (from d in doc.Descendants()
                                  where (d.Name.LocalName=="simpleforecast")
                                  select d).ToList();

            var smplFrcstDay = (from d in simpleForecast.Descendants()
                                where (d.Name.LocalName=="forecastday")
                                select d).ToList();
            int simplePeriod =1;


            foreach (var item in smplFrcstDay)
            {
                string sTmp="";
                int iTmp=0;
                Console.WriteLine("****"+item);
                ForecastDay fd = new ForecastDay();

                fd.period = item.Element("period").Value;
                fd.icon=item.Element("icon").Value;
                fd.iconUrl=item.Element("icon_url").Value;
                fd.conditions=item.Element("conditions").Value;
                fd.pop=item.Element("pop").Value;

                //DATA
                Date d = new Date();
                var data = (from x in item.Descendants()
                            where x.Name.LocalName=="date"
                            select x).FirstOrDefault();

                Console.WriteLine("XXXXXXX: "+data.Element("day").Value);
                d.day=data.Element("day").Value;
                d.epoch=data.Element("epoch").Value;
                d.hour=data.Element("hour").Value;
                d.min=data.Element("min").Value;
                d.month=data.Element("month").Value;
                d.monthName=data.Element("monthname").Value;
                d.weekDay=data.Element("weekday").Value; //albo weekday_short , czyli skrót nazwy dnia tygodnia
                d.pretty=data.Element("pretty").Value;
                d.yday=data.Element("yday").Value;
                d.year=data.Element("year").Value;
                d.prettyShort=data.Element("pretty_short").Value;

                fd.data=d;
                int h=0, min=0, s=0, y=0, mon=0, mday=0;
                if (int.TryParse(d.hour, out iTmp))
                    h=iTmp;
                if (int.TryParse(d.min, out iTmp))
                    min=iTmp;
                if (int.TryParse(d.year, out iTmp))
                    y=iTmp;
                if (int.TryParse(d.month, out iTmp))
                    mon=iTmp;
                if (int.TryParse(d.day, out iTmp))
                    mday=iTmp;

                DateTime dt = new DateTime(y, mon, mday, h, min, s);
                fd.data2=dt;

                var wnd = (from x in item.Descendants()
                           where x.Name.LocalName=="maxwind"
                           select x).FirstOrDefault();
                sTmp=wnd.Element("mph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.maxwind_mph=iTmp;
                sTmp=wnd.Element("kph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.maxwind_kph=iTmp;
                fd.maxwind_dir=wnd.Element("dir").Value;
                fd.maxwind_degrees=wnd.Element("degrees").Value;

                //AVERAGE WIND
                wnd = (from x in item.Descendants()
                       where x.Name.LocalName=="avewind"
                       select x).FirstOrDefault();
                sTmp=wnd.Element("mph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.avewind_mph=iTmp;
                sTmp=wnd.Element("kph").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.avewind_kph=iTmp;
                fd.avewind_dir=wnd.Element("dir").Value;
                fd.avewind_degrees=wnd.Element("degrees").Value;

                Console.WriteLine(sTmp);
                // HUMIDITY
                sTmp=item.Element("avehumidity").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.avehumidity=iTmp;
                sTmp=item.Element("maxhumidity").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.maxhumidity=iTmp;
                sTmp=item.Element("minhumidity").Value;
                if (int.TryParse(sTmp, out iTmp))
                    fd.minhumidity=iTmp;

                //TEMPERATURE
                var temp2 = from m in item.Descendants()
                            where (m.Name.LocalName=="high")
                            select m;
                var temeperatura = from m in temp2.Descendants()
                                   where m.Name.LocalName=="celsius"
                                   select m.Value;

                sTmp = temeperatura.First();
                fd.highTempC=sTmp;
                temp2 = from m in item.Descendants()
                        where (m.Name.LocalName=="low")
                        select m;
                temeperatura = from m in temp2.Descendants()
                               where m.Name.LocalName=="celsius"
                               select m.Value;
                sTmp = temeperatura.First();
                fd.lowTempC=sTmp;
                SFDay.Add(fd);
            }

            foreach (var item in forecast)
            {
                Console.WriteLine(item);
                ForecastDay d = new ForecastDay();
                d.period = item.Element("period").Value;
                d.icon=item.Element("icon").Value;
                d.iconUrl=item.Element("icon_url").Value;
                d.fcttext=item.Element("fcttext").Value;
                d.fcttextMetric=item.Element("fcttext_metric").Value;
                d.title=item.Element("title").Value;
                d.pop=item.Element("pop").Value;
                int p=0;
                if (int.TryParse(d.period, out p))
                {

                    if (p%2==0)
                    {
                        ForecastDay demo = SFDay.Find(df => df.period.Equals(simplePeriod.ToString()));
                        d.avehumidity=demo.avehumidity;
                        d.avewind_degrees=demo.avewind_degrees;
                        d.avewind_dir=demo.avewind_dir;
                        d.avewind_kph=demo.avewind_kph;
                        d.avewind_mph=demo.avewind_mph;
                        d.conditions=demo.conditions;
                        d.data2=demo.data2;
                        d.highTempC=demo.highTempC;
                        d.lowTempC=demo.lowTempC;
                        d.maxhumidity=demo.maxhumidity;
                        d.maxwind_degrees=demo.maxwind_degrees;
                        d.maxwind_dir=demo.maxwind_dir;
                        d.maxwind_kph=demo.maxwind_kph;
                        d.maxwind_mph=demo.maxwind_mph;
                        d.minhumidity=demo.minhumidity;
                        d.qpfAllDay=demo.qpfAllDay;
                        d.skyicon=demo.skyicon;

                        simplePeriod++;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
                            System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                            DaysUC b = new DaysUC();
                            ImageSource imgSrc;
                            b.Tytul=d.title;
                            b.Wilgotnosc=d.avehumidity.ToString()+"%";
                            b.Warunki=d.conditions;
                            b.Dzien=d.data2.ToString("d MMM yyyy");
                            if (d.snowAllDay!=null && d.qpfAllDay!=null)
                                b.IloscOpadow=d.qpfAllDay.ToString()+"mm(D),"+d.snowAllDay.ToString()+"mm(Ś)";
                            else if (d.snowAllDay!=null && d.qpfAllDay==null)
                                b.IloscOpadow="0mm(D),"+d.snowAllDay.ToString()+"(Ś)";
                            else if (d.snowAllDay==null && d.qpfAllDay!=null)
                                b.IloscOpadow=d.qpfAllDay.ToString()+"mm(D),0mm(Ś)";
                            else if (d.snowAllDay==null&&d.qpfAllDay==null)
                                b.IloscOpadow="0 mm";
                            b.PrawdOpadow=d.pop+"%";
                            b.TempMin=d.lowTempC.ToString()+"C";
                            b.TempMax=d.highTempC.ToString()+"C";
                            b.Wiatr=d.avewind_kph.ToString()+"km/h";

                            imgSrc = new BitmapImage(new Uri("Icons/"+d.icon+".png", UriKind.Relative));


                            b.Ikona=imgSrc;
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
                            b.Tytul=d.title;
                            var strings = d.fcttextMetric.Split('.', ':');
                            int i=strings.Length;
                            switch (i)
                            {
                                case 6:
                                    b.Warunki=strings[0];
                                    b.TempMin=strings[3];
                                    b.TempMax=strings[3];
                                    b.Wiatr=strings[5];
                                    b.PrawdOpadow="";
                                    b.IloscOpadow="";
                                    break;
                                case 9:
                                    b.Warunki=strings[0];
                                    b.TempMax=strings[3];
                                    b.TempMin=strings[3];
                                    b.Wiatr=strings[5];
                                    b.PrawdOpadow="";
                                    b.IloscOpadow="";
                                    b.Wilgotnosc=strings[7];
                                    break;
                                case 11:
                                    b.Warunki=strings[0];
                                    b.TempMax=strings[3];
                                    b.TempMin=strings[3];
                                    b.Wiatr=strings[5];
                                    b.PrawdOpadow=(strings[9].Split(' ')).Last();
                                    b.IloscOpadow="";
                                    b.Wilgotnosc=strings[7];
                                    break;
                                default:
                                    b.Warunki=strings[0];
                                    b.TempMax=strings[3];
                                    b.Wiatr=strings[5];
                                    b.PrawdOpadow="";
                                    b.IloscOpadow="";
                                    b.Wilgotnosc="";
                                    break;
                            }
                            b.Dzien="";

                            imgSrc = new BitmapImage(new Uri("Icons/"+d.icon+".png", UriKind.Relative));


                            b.Ikona=imgSrc;
                            ndStackPanel.Children.Add(b);
                        });
                    }

                }
                dni2.Add(d);

            }
            var dzien = (from d in SFDay where d.period=="1" select d).FirstOrDefault();
            // var dzien2 = (from d in dni2 where d.period=="0" select d).FirstOrDefault();
            if (!(dzien==null))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Debug.WriteLine("dzien"+dzien.ToString());
                    TextBlock oDniu = this.oDniu;
                    oDniu.TextWrapping=TextWrapping.Wrap;
                    oDniu.Text="Dzisiaj jest "+dzien.data.day+" "+dzien.data.monthName+" "+dzien.data.year+", "+dzien.data.weekDay+". To "+dzien.data.yday+" dzień roku.";
                    oDniu.Visibility=Visibility.Visible;
                });
            }
        }
        #endregion  OBRABIANIE ASTRONOMII, POGODY GODZINOWEJ I NA NASTĘPNE DNI

        //UBRANIA
        #region UBRANIA
        public void pogodaDlaUbran()
        {
            string pogodaZaGodzine = "";
            for (int i = 0; i < 2; i++)
            {
                pogodaZaGodzine=dni2.ElementAt(i).fcttextMetric;
                var s=pogodaZaGodzine.Split('.');
                string zapowiedz = s[0];

                if ((zapowiedz.Equals("Możliwy deszcz"))||(zapowiedz.Equals("Możliwe burze")))
                    czyBedziePadac=true;
            }
            if ((pog.Equals("deszcz"))||(pog.Equals("lekki deszcz"))||(pog.Equals("lekkie przelotne deszcze"))||(pog.Equals("mżawka"))||(czyBedziePadac))
                ubrania.Add("parasolka_k.png");

            int temp=0, t=0; ;
            if (int.TryParse(temperatura, out t))
                temp = t;

            if (temp<2)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("kurtka_k.png");
                ubrania.Add("czapka_k.png");
            }
            else if (temp<10)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("kurtka_k.png"); //tu dałabym cieplejszą kurtkę
            }
            else if (temp<18)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("kurtka_k.png"); //lżejsza kurtka bądź płaszczyk
            }
            else if (temp<23)
            {
                ubrania.Add("buty_k.png");
                ubrania.Add("spodniedl_k.png");
                ubrania.Add("dlrekaw.png");
            }
            else if (temp<28)
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


        }

        public void ubranie()
        {

            pogodaDlaUbran();
            //ubrania.Add("spodniedl_k.png");
            //ubrania.Add("bluza_k.png");
            //ubrania.Add("buty_k.png");

            //string[] files = new string[] { "bluza_k.png", "buty_k.png", "czapka_k.png", "dlspodnie_k.png" };

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

                        BitmapImage img = new BitmapImage(new Uri("Ubrania/"+image, UriKind.RelativeOrAbsolute));

                        StreamResourceInfo r = System.Windows.Application.GetResourceStream(new Uri("Ubrania/"+image, UriKind.RelativeOrAbsolute));
                        img.SetSource(r.Stream);
                        wb = new WriteableBitmap(img);
                        //Update the size of the final bitmap
                        width = wb.PixelWidth > width ? wb.PixelWidth : width;
                        height = wb.PixelHeight > height ? wb.PixelHeight : height;

                        images.Add(img);

                    }
                }
                catch (Exception poooo)
                {
                    Debug.WriteLine(poooo.Message);
                }
                // Create a bitmap to hold the combined image 
                BitmapImage finalImage = new BitmapImage();

                StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri("Ubrania/kobieta.png",
                    UriKind.RelativeOrAbsolute));
                finalImage.SetSource(sri.Stream);
                wbFinal= new WriteableBitmap(finalImage);

                width=finalImage.PixelWidth;
                height=finalImage.PixelHeight;

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
                        wbFinal.Render(image, tf);

                        // tempHeight += item.PixelHeight;
                    }

                    wbFinal.Invalidate();
                    wbFinal.SaveJpeg(mem, width, height, 0, 100);
                    mem.Seek(0, System.IO.SeekOrigin.Begin);

                    // Show image. 
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ubranieIMG.Source = wbFinal;
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
            if (nazwaSportu.Equals("Nieznany rodzaj pogody"))
            {
                Debug.WriteLine("Nieznany rodzaj pogody, dla którego nie ma żadnych sportów :C");
            }
            else
            {
                string query = "";
                if (nazwaSportu.Equals("Rower"))
                    query="\"Ścieżka rowerowa\"";
                else if (nazwaSportu.Equals("Siatkówka"))
                    query="\"Boisko siatkówka\"";
                else if (nazwaSportu.Equals("Koszykówka"))
                    query="\"Boisko koszykówka\"";
                else if (nazwaSportu.Equals("Piłka nożna"))
                    query="\"Boisko do piłki nożnej orlik\"";
                else if (nazwaSportu.Equals("Jazda konna"))
                    query="\"Stadnina koni\"";
                else if ((nazwaSportu.Equals("Łyżwy"))||(nazwaSportu.Equals("Hokej")))
                    query="\"Lodowisko\"";
                else if ((nazwaSportu.Equals("Narciarstwo"))||(nazwaSportu.Equals("Snowboard")))
                    query="\"Stok narciarski\"";
                else if (nazwaSportu.Equals("Trening sztuk walki"))
                    query="\"Szkoła sztuk walki\"";
                else
                    query=nazwaSportu;


                Debug.WriteLine("Działa to to?                 "+ nazwaSportu+"/"+query);
                var wbt = new WebBrowserTask();
                Uri uri = new Uri("https://maps.google.pl/maps?q=" + miasto + "+" + query, UriKind.RelativeOrAbsolute);
                wbt.Uri=uri;
                //sportyLB.SelectedItem=null;
                wbt.Show();
            }
        }


        public void wyborSportow()
        {
            poraDnia = getPoraDnia();
            poraRoku= getPoraRoku();

            String pogoda2 = pog.ToLower();
            if (pogoda2.Equals("pogodnie"))
            {
                ladnaPogoda(poraDnia);
            }
            else if (pogoda2.Equals("przewaga chmur"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("obłoki zanikające"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("śnieg"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Equals("niewielkie zachmurzenie"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("deszcz"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Equals("lekki deszcz"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Equals("pochmurno"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("płatki mgły"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("lekkie przelotne deszcze"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Equals("lekka mżawka"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("zamglenia"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("mżawka"))
                deszczowaPogoda(poraDnia);
            else if (pogoda2.Equals("mgła"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("lekka mgła"))
                ladnaPogoda(poraDnia);
            else if (pogoda2.Equals("częściowe zamglenia"))
                ladnaPogoda(poraDnia);
            else
                listaSportow.Add("Nieznany rodzaj pogody");

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
            int temp=0;
            if (int.TryParse(temperatura, out temp))
                Debug.WriteLine(temp);
            if ((temp>-30)&&(temp<35))
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
                char poraDnia=getPoraDnia();
                if (poraDnia.Equals('p')||(poraDnia.Equals('o'))||(poraDnia.Equals('w')))
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
                if ((poraDnia.Equals('p'))||(poraDnia.Equals('o'))||(poraDnia.Equals('w')))
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

        public void ekstremalne()
        {
            try
            {
                listaSportow.Add("Parkour");
                listaSportow.Add("Bungee");
                listaSportow.Add("Paralotnia");
                listaSportow.Add("Skok ze spadochronem");
                listaSportow.Add("Windsurfing");
                // polowanie jednak nie
                listaSportow.Add("Lot balonem");
            }
            catch (Exception ccc)
            {
                Debug.WriteLine(ccc.Message);
            }
        }

        #endregion SPORTY

        private void ApplicationBar_About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.RelativeOrAbsolute));

        }



    }
}