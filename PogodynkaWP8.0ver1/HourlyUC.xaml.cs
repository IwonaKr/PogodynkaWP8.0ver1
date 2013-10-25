using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace PogodynkaWP8._0ver1
{
    public partial class HourlyUC : UserControl
    {
        public HourlyUC()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }
        public string Godz
        {
            get { return this.godzina.Text; }
            set { this.godzina.Text=value; }
        }
        public string Temp
        {
            get { return this.temperatura.Text; }
            set { this.temperatura.Text=value; }
        }
        public string Opady
        {
            get { return this.opady.Text; }
            set { this.opady.Text=value; }
        }
        public string Dzien
        {
            get { return this.dzien.Text; }
            set { this.dzien.Text=value; }
        }
        public string Warunki
        {
            get { return this.warunki.Text; }
            set { this.warunki.Text=value; }
        }
        public ImageSource Ikona
        {
            get { return this.ikonka.Source; }
            set { this.ikonka.Source=value; }
        }
    }
}
