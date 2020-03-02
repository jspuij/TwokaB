using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebWindows.Blazor;

namespace BlazorApp.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IDisposable run;

        public MainWindow()
        {
            InitializeComponent();

            this.BlazorWebView.Loaded += BlazorWebView_Loaded;

        }

        private void BlazorWebView_Loaded(object sender, RoutedEventArgs e)
        {
            ComponentsDesktop.Run<Startup>(this.BlazorWebView, "wwwroot/index.html");
        }
    }
}
