using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace BlazorWebView.Wpf
{
    public sealed class BlazorWebView : HwndHost, IBlazorWebView
    {
        #region Imports
        const string DllName = "BlazorWebViewNative";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void BlazorWebViewNative_Register(IntPtr hInstance);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr BlazorWebViewNative_Ctor(IntPtr parent);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr BlazorWebViewNative_GetHWND(IntPtr blazorWebView);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void BlazorWebViewNative_Dtor(IntPtr blazorWebView);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void BlazorWebViewNative_Initialize(IntPtr blazorWebView);
        #endregion

        private IntPtr blazorWebView;

        public event EventHandler<string> OnWebMessageReceived;

        static BlazorWebView()
        {
            var hInstance = Marshal.GetHINSTANCE(typeof(BlazorWebView).Module);
            BlazorWebViewNative_Register(hInstance);
        }

        public void Initialize(Action<WebViewOptions> configure)
        {
            BlazorWebViewNative_Initialize(this.blazorWebView);
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
            blazorWebView = BlazorWebViewNative_Ctor(hwndParent.Handle);
            var hwnd = BlazorWebViewNative_GetHWND(blazorWebView);
            return new HandleRef(this, hwnd);
        }
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            BlazorWebViewNative_Dtor(blazorWebView);
        }
    }
}
