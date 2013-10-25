using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PogodynkaWP8._0ver1.Resources;

namespace PogodynkaWP8._0ver1
{
    public partial class MainPage : PhoneApplicationPage
    {
        public class Sample
        {
            public string Miasto { get; set; }   
        }
       
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            List<Sample> dataSource = new List<Sample>();
            dataSource.Add(new Sample() { Miasto="Lublin" });
            dataSource.Add(new Sample() { Miasto="Warszawa" });
            dataSource.Add(new Sample() { Miasto ="Puławy" });
            dataSource.Add(new Sample() { Miasto="Wrocław" });
            dataSource.Add(new Sample() { Miasto="Kielce" });
            dataSource.Add(new Sample() { Miasto="Poznań" });
            dataSource.Add(new Sample() { Miasto ="Kraków" });
            dataSource.Add(new Sample() { Miasto="Gdańsk" });
            this.listBox.ItemsSource=dataSource;
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Sample data = (sender as ListBox).SelectedItem as Sample;

            //Get the selected ListBoxItem container instance   
            ListBoxItem selectedItem = this.listBox.ItemContainerGenerator.ContainerFromItem(data) as ListBoxItem;

            // MessageBox.Show(data.Miasto);
            NavigationService.Navigate(new Uri("/Pogoda.xaml?msg="+data.Miasto, UriKind.RelativeOrAbsolute));
        }
        
    }
}