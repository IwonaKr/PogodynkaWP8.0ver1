using System;
using System.Collections.Generic;
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
using System.Xml;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace PogodynkaWP8._0ver1
{
    public partial class pogoda : PhoneApplicationPage
    {
        public static string miasto;
        string place = "";
        string obs_time = "";
        string weather1 = "";
        string temperature_string = "";
        string relative_humidity = "";
        string wind_string = "";
        string pressure_mb = "";
        string dewpoint_string = "";
        string visibility_km = "";
        string latitude = "";
        string longitude = "";
        string icon="";
        //public static List<ForecastDay> dni1= new List<ForecastDay>();
        public static List<ForecastDay> dni2= new List<ForecastDay>(); //txt_forecast
        public static List<ForecastDay> SFDay = new List<ForecastDay>(); //SimpleForecast
        public static List<HourlyForecast> HourlyForecast = new List<HourlyForecast>();

        public pogoda()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string msg;
            if (NavigationContext.QueryString.TryGetValue("msg", out msg))
            {
                miasto=msg;
                this.miastoTB.Text=miasto;
                Thread t = new Thread(NewThread);
                t.Start();
            }
        }
        void NewThread()
        {
            string url = "http://api.wunderground.com/api/c9d15b10ff3ed303/forecast/forecast10day/hourly/conditions/astronomy/lang:PL/q/Poland/"+miasto+".xml";

            WebClient wc = new WebClient();
            //wc.DownloadStringCompleted += HttpsCompleted;
            wc.DownloadStringCompleted +=wc_DownloadStringCompleted; //dodane, bez tej funkcji też działa!!

            wc.DownloadStringAsync(new Uri(url));

        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            //Console.WriteLine("Zabawa z XDocument");
            string weather="";
            try
            {
                weather = e.Result;
                XmlReader reader = XmlReader.Create(new StringReader(weather));
                XDocument doc = XDocument.Load(reader);
                obrabianieConditions(doc);

                var hourly_forecast = (from d in doc.Descendants()
                                       where (d.Name.LocalName=="hourly_forecast")
                                       select d).ToList();
                Debug.WriteLine("HF!! "+hourly_forecast.ToString());

                var hourly = (from d in hourly_forecast.Descendants()
                              where (d.Name.LocalName=="forecast")
                              select d).ToList();
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
                        s=iTmp;
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
                    Debug.WriteLine(hf.czas.ToLongDateString()+" "+hf.czas.ToLocalTime()+" "+hf.weekdayNameAbbrev+" "+hf.monthAbbrev);
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
                    HourlyForecast.Add(hf);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("pl-PL");
                        System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                        HourlyUC b = new HourlyUC();
                        b.Godz=hf.czas.ToString("HH:mm");
                        b.Dzien=hf.czas.ToString("d MMM yyyy");
                        b.Warunki=hf.condition;
                        b.opady.Text="Opady: "+hf.qpf;
                        b.temperatura.Text="Temp: "+hf.tempC+"C";
                        ImageSource imgSrc = new BitmapImage(new Uri("Icons/"+hf.icon+".png", UriKind.Relative));
                        b.ikonka.Source=imgSrc;
                        hStackPanel.Children.Add(b);
                    });
                }

            }
            catch (Exception ex)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK);
                    this.textBox1.Text=ex.Message;
                    this.ikonka.Source=null;
                });
            }
        }

        private void obrabianieConditions(XDocument doc)
        {
            var txt_forecast = (from d in doc.Descendants()
                                where (d.Name.LocalName == "txt_forecast")
                                select d).ToList();

            var forecast = (from d in txt_forecast.Descendants()
                            where (d.Name.LocalName=="forecastday")
                            select d).ToList();

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
                dni2.Add(d);
            }
            var simpleForecast = (from d in doc.Descendants()
                                  where (d.Name.LocalName=="simpleforecast")
                                  select d).ToList();

            var smplFrcstDay = (from d in simpleForecast.Descendants()
                                where (d.Name.LocalName=="forecastday")
                                select d).ToList();
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


                //WIND NIE DZIAŁA, DOKOŃCZYĆ

                //MAX WIND
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

            var dzien = (from d in SFDay where d.period=="1" select d).FirstOrDefault();
            var dzien2 = (from d in dni2 where d.period=="0" select d).FirstOrDefault();
            if (!(dzien==null))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.textBox1.Text = "Period:             " + dzien.period+
                            "\nIconUri: " + dzien.iconUrl+
                            "\nPogoda:          " + dzien2.fcttext+
                            "\nfcttextMetric:     " + dzien2.fcttextMetric+
                            "\ntitle:           " + dzien2.title;
                    Uri uri = new Uri("Icons/"+dzien.icon+".png", UriKind.Relative);
                    ImageSource imgSource = new BitmapImage(uri);
                    this.ikonka.Source = imgSource;
                    TextBlock tb = new TextBlock();
                    tb.TextWrapping=TextWrapping.Wrap; //zawijanie tekstu
                    tb.Text = "Temp: "+dzien.lowTempC+"C-"+dzien.highTempC+"C\nWarunki: "+dzien.conditions+"\nWilgotność (min,max,śr): "+dzien.minhumidity+", "+dzien.maxhumidity.ToString()+","+dzien.avehumidity.ToString()+"\nWiatr (mile/h, km/h,kierunek): "+dzien.maxwind_mph.ToString()+","+dzien.maxwind_kph.ToString()+","+dzien.maxwind_dir;
                    TextBlock oDniu = new TextBlock();
                    oDniu.TextWrapping=TextWrapping.Wrap;
                    oDniu.Text="Dzisiaj jest "+dzien.data.day+" "+dzien.data.monthName+" "+dzien.data.year+", "+dzien.data.weekDay+". To "+dzien.data.yday+" dzień roku.";

                    this.glownyStackPanel.Children.Add(oDniu);
                    this.glownyStackPanel.Children.Add(tb);
                });
            }
        }

        private void HttpsCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(e.Result)))
            {
                // Parse the file and display each of the nodes.
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name.Equals("full"))
                            {
                                reader.Read();
                                place = reader.Value;
                            }
                            else if (reader.Name.Equals("observation_time"))
                            {
                                reader.Read();
                                obs_time = reader.Value;
                            }
                            else if (reader.Name.Equals("weather"))
                            {
                                reader.Read();
                                weather1 = reader.Value;
                            }
                            else if (reader.Name.Equals("temperature_string"))
                            {
                                reader.Read();
                                temperature_string = reader.Value;
                            }
                            else if (reader.Name.Equals("relative_humidity"))
                            {
                                reader.Read();
                                relative_humidity = reader.Value;
                            }
                            else if (reader.Name.Equals("wind_string"))
                            {
                                reader.Read();
                                wind_string = reader.Value;
                            }
                            else if (reader.Name.Equals("pressure_mb"))
                            {
                                reader.Read();
                                pressure_mb = reader.Value;
                            }
                            else if (reader.Name.Equals("dewpoint_string"))
                            {
                                reader.Read();
                                dewpoint_string = reader.Value;
                            }
                            else if (reader.Name.Equals("visibility_km"))
                            {
                                reader.Read();
                                visibility_km = reader.Value;
                            }
                            else if (reader.Name.Equals("latitude"))
                            {
                                reader.Read();
                                latitude = reader.Value;
                            }
                            else if (reader.Name.Equals("longitude"))
                            {
                                reader.Read();
                                longitude = reader.Value;
                            }
                            else if (reader.Name.Equals("icon"))
                            {
                                reader.Read();
                                icon=reader.Value;
                            }

                            break;
                    }
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.textBox1.Text = "Miejsce:             " + place+
                            "\nCzas obserwacji: " + obs_time+
                            "\nPogoda:          " + weather1+
                            "\nTemperatura:     " + temperature_string+
                            "\nWiatr:           " + wind_string+
                            "\nLokacja:         " + longitude + ", " + latitude;
                    Uri uri = new Uri("Icons/"+icon+".png", UriKind.Relative);
                    ImageSource imgSource = new BitmapImage(uri);
                    this.ikonka.Source = imgSource;
                    //this.ikonka.Source = new BitmapImage(new Uri("pack://application:,,,/PogodynkaWP7.1ver1;component/Icons/"+icon"+.png"));
                });

                //throw new NotImplementedException();
                /*if (e.Error == null)
                {
                    XDocument xdoc = XDocument.Parse(e.Result, LoadOptions.None);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.textBox1.Text = xdoc.Root.ToString();
                        });

                }*/
            }
        }
    }
}