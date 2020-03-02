using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace BlazorWebView.Wpf
{
    public sealed class BlazorWebView : HwndHost, IBlazorWebView
    {
        const string DllName = "BlazorWebViewNative";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] 
        private static extern IntPtr BlazorWebViewNative_Ctor(HandleRef parent);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr BlazorWebViewNative_GetHWND(IntPtr blazorWebViewNative);

        private IntPtr blazorWebViewNative;

        public event EventHandler<string> OnWebMessageReceived;

        public void Initialize(Action<WebViewOptions> configure)
        {
        }

        public void Invoke(Action callback)
        {
        }

        public void NavigateToUrl(string url)
        {
        }

        public void SendMessage(string message)
        {
        }

        public void ShowMessage(string title, string message)
        {
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this.blazorWebViewNative = BlazorWebViewNative_Ctor(hwndParent);
            var hwnd = BlazorWebViewNative_GetHWND(this.blazorWebViewNative);
            if (hwnd == IntPtr.Zero)
            {
                
            }
            return new HandleRef(this, hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}
