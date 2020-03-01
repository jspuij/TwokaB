using System;

using AppKit;
using Foundation;
using WebWindows.Blazor;

namespace BlazorApp.Mac
{
    public partial class ViewController : NSViewController
    {
        private IDisposable run;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            run = ComponentsDesktop.Run<Startup>(BlazorWebView, "wwwroot/index.html");
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
