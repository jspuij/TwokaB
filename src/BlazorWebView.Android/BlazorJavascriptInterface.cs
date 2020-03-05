using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Interop;

namespace BlazorWebView.Android
{
    public class BlazorJavascriptInterface :Java.Lang.Object
    {
        private readonly BlazorWebView blazorWebView;

        public BlazorJavascriptInterface(BlazorWebView blazorWebView)
        {
            this.blazorWebView = blazorWebView;
        }

        [Export]
        [JavascriptInterface]
        public void PostMessage(string message)
        {
            this.blazorWebView.OnReceiveWebMessage(message);
        }
    }
}