using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
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

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            geolocator = new Geolocator();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Debug.WriteLine("MAIN: "+e.Content.ToString()+" "+e.NavigationMode+" "+e.Uri.ToString()+" "+e.GetType().ToString());
            Debug.WriteLine("");
            Visibility darkBackgroundVisibility = 
            (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            ImageSource imgSrc;
            // Write the theme background value.
            if (darkBackgroundVisibility == Visibility.Visible)
            {
                imgSrc=new BitmapImage(new Uri("Logo/wundergroundLogo_white.png", UriKind.Relative));
                this.logo.Source= imgSrc;
            }
            else
            {
                imgSrc=new BitmapImage(new Uri("Logo/wundergroundLogo_black.png", UriKind.Relative));
                this.logo.Source=imgSrc;
            }
        }

        private void OKbtn_Click(object sender, RoutedEventArgs e)
        {

            bool isNetwork=NetworkInterface.GetIsNetworkAvailable();
            if (!isNetwork)
            {
                MessageBoxResult result = MessageBox.Show("Nie wykryto połączenia z Internetem. Włączyć WiFi?", "Brak połączenia z Internetem", MessageBoxButton.OKCancel);
                if (result.Equals(MessageBoxResult.OK))
                {
                    Launcher.LaunchUriAsync(new Uri("ms-settings-wifi:"));
                }
                else
                {
                    MessageBox.Show("Aplikacja nie może działać bez aktywnego połączenia z Internetem. ", "", MessageBoxButton.OK);
                }
            }
            else
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
                    if (this.miastoTB.Text.Length>2)
                    {
                        this.miasto=this.miastoTB.Text;
                        NavigationService.Navigate(new Uri("/Pogoda.xaml?msg="+this.miasto, UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        MessageBox.Show("Miasto nie zostało wpisane", "Brak miasta", MessageBoxButton.OK);
                    }
                }
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

        private void logo_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var wbt = new WebBrowserTask();
            Uri uri = new Uri("http://www.wunderground.com/?apiref=5eb71539bdb4d721", UriKind.RelativeOrAbsolute);
            wbt.Uri=uri;
            wbt.Show();
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