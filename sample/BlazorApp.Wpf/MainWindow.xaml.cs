using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        }

        bool initialized = false;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (!initialized)
            {
                initialized = true;
                run = ComponentsDesktop.Run<Startup>(this.BlazorWebView, "wwwroot/index.html");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Environment.Exit(0);
        }
    }
}
