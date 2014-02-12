using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace PogodynkaWP8._0ver1
{
    public partial class About : PhoneApplicationPage
    {
        public About()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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
            //oNasTB.Text="System wspomagania organizowania wolnego czasu dla systemów WP i Android\n";
            //oNasTB.Text+="Pogodynka ver 1.0.0\nAutorki\\dyplomantki: Anna Mazur & Iwona Krocz\nPromotor: dr inż. Piotr Kopniak";
            oNasTB.Text += "Pogodynka 1.0.0\nAutorki: AnnaeM & IwonaKr.\n";
            oNasTB.Text+="\n\nPowstałe dzięki serwisowi pogodowemu WeatherUnderground";
        }
        private void logo_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var wbt = new WebBrowserTask();
            Uri uri = new Uri("http://www.wunderground.com/?apiref=5eb71539bdb4d721", UriKind.RelativeOrAbsolute);
            wbt.Uri=uri;
            wbt.Show();
        }
    }
}