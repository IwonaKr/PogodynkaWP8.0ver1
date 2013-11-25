using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PogodynkaWP8._0ver1.Resources;
using Windows.Devices.Geolocation;
using Windows.System;

namespace PogodynkaWP8._0ver1
{
    public partial class MainPage : PhoneApplicationPage
    {
        Geolocator geolocator;
        Geoposition geoposition=null;
        bool GPSorWybor=true; //true  to wpisywanie, false to gps
        static bool haveLocation=false;
        public String miasto=null;

        public class Sample
        {
            public string Miasto { get; set; }   
        }
       
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            geolocator = new Geolocator();
            //List<Sample> dataSource = new List<Sample>();
            //dataSource.Add(new Sample() { Miasto="Lublin" });
            //dataSource.Add(new Sample() { Miasto="Warszawa" });
            //dataSource.Add(new Sample() { Miasto ="Puławy" });
            //dataSource.Add(new Sample() { Miasto="Wrocław" });
            //dataSource.Add(new Sample() { Miasto="Kielce" });
            //dataSource.Add(new Sample() { Miasto="Poznań" });
            //dataSource.Add(new Sample() { Miasto ="Kraków" });
            //dataSource.Add(new Sample() { Miasto="Gdańsk" });
            //this.listBox.ItemsSource=dataSource;
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private void OKbtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.GPSorWybor) //czyli wybrany GPS
            {
                if (haveLocation)
                {
                    NavigationService.Navigate(new Uri("/Pogoda.xaml?msg="+this.miasto, UriKind.RelativeOrAbsolute));
                    Debug.WriteLine(this.miasto);
                }
                else
                {
                    MessageBox.Show("Lokacja nie została jeszcze odnaleziona", "Brak lokacji", MessageBoxButton.OK);
                }
            }
            else
            {
                this.miasto=this.miastoTB.Text;
                NavigationService.Navigate(new Uri("/Pogoda.xaml?msg="+this.miasto, UriKind.RelativeOrAbsolute));
            }
        }

        private async void GPSbtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(10)
                    );

                Debug.WriteLine(geoposition.Coordinate.Latitude.ToString()+" "+geoposition.Coordinate.Longitude.ToString());
                this.miasto=geoposition.Coordinate.Latitude.ToString("0.0000")+","+geoposition.Coordinate.Longitude.ToString("0.0000");
                this.GPSTB.Text=geoposition.Coordinate.Latitude.ToString("0.0000")+" "+geoposition.Coordinate.Longitude.ToString("0.0000");
                if (geoposition!=null)
                    haveLocation=true;

            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have the right capability or the location master switch is off
                    //statusTextBlock.Text = "location  is disabled in phone settings.";
                    MessageBoxResult result = MessageBox.Show("Usługa lokalizacji jest wyłączona. ", "GPS wyłączony", MessageBoxButton.OKCancel);
                    if (result==MessageBoxResult.OK)
                        Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
                    haveLocation=false;
                }
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            this.OKbtn.Visibility=Visibility.Visible;
            ApplicationBarIconButton temp = (sender as ApplicationBarIconButton);
            //ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            if (temp.Text=="gps")
            {
                Debug.WriteLine("GiePeEs z appbaru guziczkowego");
                this.wlaczGPS.Visibility=Visibility.Visible;
                this.GPSbtn.Visibility=Visibility.Visible;
                this.GPSTB.Visibility=Visibility.Visible;
                this.podajMiasto.Visibility=Visibility.Collapsed;
                this.miastoTB.Visibility=Visibility.Collapsed;
                if (this.GPSorWybor==false)
                    this.GPSorWybor=true;

            }
            else if (temp.Text=="miasto")
            {
                Debug.WriteLine("Miasto z appbaru guziczkowego");
                this.podajMiasto.Visibility=Visibility.Visible;
                this.miastoTB.Visibility=Visibility.Visible;
                this.wlaczGPS.Visibility=Visibility.Collapsed;
                this.GPSbtn.Visibility=Visibility.Collapsed;
                this.GPSTB.Visibility=Visibility.Collapsed;
                if (this.GPSorWybor)
                    this.GPSorWybor=false;
            }
        }

        private void ApplicationBar_About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.RelativeOrAbsolute));

        }

        //private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    Sample data = (sender as ListBox).SelectedItem as Sample;

        //    //Get the selected ListBoxItem container instance   
        //    //ListBoxItem selectedItem = this.listBox.ItemContainerGenerator.ContainerFromItem(data) as ListBoxItem;

        //    // MessageBox.Show(data.Miasto);
        //    NavigationService.Navigate(new Uri("/Pogoda.xaml?msg="+data.Miasto, UriKind.RelativeOrAbsolute));
        //}
        
    }
}