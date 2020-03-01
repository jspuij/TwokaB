using Foundation;
using System;
using UIKit;
using WebWindows.Blazor;

namespace BlazorApp.iOS
{
    public partial class ViewController : UIViewController
    {
        private IDisposable run;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            run = ComponentsDesktop.Run<Startup>(BlazorWebView, "wwwroot/index.html");
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}