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
    public partial class DaysUC : UserControl
    {
        public DaysUC()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }
        public string Tytul
        {
            get { return this.titleDays.Text; }
            set { this.titleDays.Text=value; }
        }
        public string TempMax
        {
            get { return this.tempDays.Text; }
            set { this.tempDays.Text=value; }
        }
        public string TempMin
        {
            get { return this.tempLDays.Text; }
            set { this.tempLDays.Text=value; }
        }
        public string PrawdOpadow
        {
            get { return this.popDays.Text; }
            set { this.popDays.Text=value; }
        }
        public string IloscOpadow
        {
            get { return this.qpfDays.Text; }
            set { this.qpfDays.Text=value; }
        }
        public string Dzien
        {
            get { return this.dateDays.Text; }
            set { this.dateDays.Text=value; }
        }
        public string Warunki
        {
            get { return this.conditionDays.Text; }
            set { this.conditionDays.Text=value; }
        }
        public ImageSource Ikona
        {
            get { return this.ikonkaDays.Source; }
            set { this.ikonkaDays.Source=value; }
        }
        public string Wilgotnosc
        {
            get { return this.humDays.Text; }
            set { this.humDays.Text = value; }
        }
        public string Wiatr
        {
            get { return this.windDays.Text; }
            set { this.windDays.Text = value; }
        }
    }
}
