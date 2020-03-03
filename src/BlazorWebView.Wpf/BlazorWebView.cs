using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace BlazorWebView.Wpf
{
    public sealed class BlazorWebView : HwndHost, IBlazorWebView
    {
        #region Imports

        const int WM_SIZE = 0x0005;

        const string DllName = "BlazorWebViewNative";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        delegate void WebMessageReceivedCallback(string message);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] 
        delegate IntPtr WebResourceRequestedCallback(string url, out int numBytes, out string contentType);
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void BlazorWebViewNative_Register(IntPtr hInstance);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr BlazorWebViewNative_Ctor(IntPtr parent, WebMessageReceivedCallback webMessageReceivedCallback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr BlazorWebViewNative_GetHWND(IntPtr blazorWebView);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void BlazorWebViewNative_Dtor(IntPtr blazorWebView);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void BlazorWebViewNative_Initialize(IntPtr blazorWebView);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        static extern void BlazorWebViewNative_AddCustomScheme(IntPtr instance, string scheme, WebResourceRequestedCallback requestHandler);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] 
        static extern void BlazorWebViewNative_NavigateToUrl(IntPtr instance, string url);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        static extern void BlazorWebViewNative_SendMessage(IntPtr instance, string message);
        #endregion

        private IntPtr blazorWebView;
        private readonly List<GCHandle> gcHandlesToFree = new List<GCHandle>();
        private int ownerThreadId;

        public event EventHandler<string> OnWebMessageReceived;

        static BlazorWebView()
        {
            var hInstance = Marshal.GetHINSTANCE(typeof(BlazorWebView).Module);
            BlazorWebViewNative_Register(hInstance);
        }

        public void Initialize(Action<WebViewOptions> configure)
        {
            var options = new WebViewOptions();
            configure.Invoke(options);

            foreach (var (schemeName, handler) in options.SchemeHandlers)
            {
                this.AddCustomScheme(schemeName, handler);
            }

            BlazorWebViewNative_Initialize(this.blazorWebView);
        }

        private void AddCustomScheme(string scheme, ResolveWebResourceDelegate requestHandler)
        {
            // Because of WKWebView limitations, this can only be called during the constructor
            // before the first call to Show. To enforce this, it's private and is only called
            // in response to the constructor options.
            WebResourceRequestedCallback callback = (string url, out int numBytes, out string contentType) =>
            {
                var responseStream = requestHandler(url, out contentType);
                if (responseStream == null)
                {
                    // Webview should pass through request to normal handlers (e.g., network)
                    // or handle as 404 otherwise
                    numBytes = 0;
                    return default;
                }

                // Read the stream into memory and serve the bytes
                // In the future, it would be possible to pass the stream through into C++
                using (responseStream)
                using (var ms = new MemoryStream())
                {
                    responseStream.CopyTo(ms);

                    numBytes = (int)ms.Position;
                    var buffer = Marshal.AllocHGlobal(numBytes);
                    Marshal.Copy(ms.GetBuffer(), 0, buffer, numBytes);
                    return buffer;
                }
            };

            gcHandlesToFree.Add(GCHandle.Alloc(callback));
            BlazorWebViewNative_AddCustomScheme(this.blazorWebView, scheme, callback);
        }

        public void Invoke(Action callback)
        {
            // If we're already on the UI thread, no need to dispatch
            if (Thread.CurrentThread.ManagedThreadId == ownerThreadId)
            {
                callback();
            }
            else
            {
                Dispatcher.Invoke(callback);
            }
        }

        public void NavigateToUrl(string url)
        {
            BlazorWebViewNative_NavigateToUrl(this.blazorWebView, url);
        }

        public void SendMessage(string message)
        {
            BlazorWebViewNative_SendMessage(this.blazorWebView, message);
        }

        public void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            var onWebMessageReceivedDelegate = (WebMessageReceivedCallback)ReceiveWebMessage;
            gcHandlesToFree.Add(GCHandle.Alloc(onWebMessageReceivedDelegate));

            blazorWebView = BlazorWebViewNative_Ctor(hwndParent.Handle, onWebMessageReceivedDelegate);
            var hwnd = BlazorWebViewNative_GetHWND(blazorWebView);
            return new HandleRef(this, hwnd);
        }

        private void ReceiveWebMessage(string message)
        {
            OnWebMessageReceived?.Invoke(this, message);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            BlazorWebViewNative_Dtor(blazorWebView);
            foreach (var gcHandle in gcHandlesToFree)
            {
                gcHandle.Free();
            }
            gcHandlesToFree.Clear();
        }
    }
}
