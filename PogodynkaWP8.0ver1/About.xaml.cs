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
                //textBlock1.Text = "background = dark";
                imgSrc=new BitmapImage(new Uri("Logo/wundergroundLogo_white.png", UriKind.Relative));
                this.logo.Source= imgSrc;
            }
            else
            {
                //textBlock1.Text = "background = light";
                imgSrc=new BitmapImage(new Uri("Logo/wundergroundLogo_black.png", UriKind.Relative));
                this.logo.Source=imgSrc;
            }
        }
    }
}