// <copyright file="BlazorWebView.cs" company="Steve Sanderson and Jan-Willem Spuij">
// Copyright 2020 Steve Sanderson and Jan-Willem Spuij
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace BlazorWebView.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;

    /// <summary>
    /// An <see cref="IBlazorWebView"/> implementation for Wpf.
    /// </summary>
    public sealed class BlazorWebView : HwndHost, IBlazorWebView
    {
        /// <summary>
        /// The name of the native DLL that is used.
        /// </summary>
        private const string DllName = "BlazorWebViewNative";

        /// <summary>
        /// A list of GC handles to free afterwards.
        /// </summary>
        private readonly List<GCHandle> gcHandlesToFree = new List<GCHandle>();

        /// <summary>
        /// The thread id of the owner thread.
        /// </summary>
        private int ownerThreadId;

        /// <summary>
        /// A reference to the native webview.
        /// </summary>
        private IntPtr blazorWebView;

        /// <summary>
        /// Initializes static members of the <see cref="BlazorWebView"/> class.
        /// </summary>
        static BlazorWebView()
        {
            var hInstance = Marshal.GetHINSTANCE(typeof(BlazorWebView).Module);
            BlazorWebViewNative_Register(hInstance);
        }

        /// <summary>
        /// A callback delegate for when a web message is received from javascript.
        /// </summary>
        /// <param name="message">The received message.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private delegate void WebMessageReceivedCallback(string message);

        /// <summary>
        /// A callback delegate to handle a Resource request.
        /// </summary>
        /// <param name="url">The url to request a resource for.</param>
        /// <param name="numBytes">The number of bytes of the resource.</param>
        /// <param name="contentType">The content type of the resource.</param>
        /// <returns>A pointer to a stream.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private delegate IntPtr WebResourceRequestedCallback(string url, out int numBytes, out string contentType);

        /// <summary>
        /// Event that is fired when a web message is received from javascript.
        /// </summary>
        public event EventHandler<string> OnWebMessageReceived;

        /// <summary>
        /// Initialize the BlazorWebView.
        /// </summary>
        /// <param name="configure">A delegate that is executed to configure the webview.</param>
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

        /// <summary>
        /// Invoke a callback on the UI thread.
        /// </summary>
        /// <param name="callback">The callback to execute.</param>
        public void Invoke(Action callback)
        {
            // If we're already on the UI thread, no need to dispatch
            if (Thread.CurrentThread.ManagedThreadId == this.ownerThreadId)
            {
                callback();
            }
            else
            {
                this.Dispatcher.Invoke(callback);
            }
        }

        /// <summary>
        /// Navigate to the specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        public void NavigateToUrl(string url)
        {
            BlazorWebViewNative_NavigateToUrl(this.blazorWebView, url);
        }

        /// <summary>
        /// Send a message to javascript.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(string message)
        {
            BlazorWebViewNative_SendMessage(this.blazorWebView, message);
        }

        /// <summary>
        /// Show a native dialog for the platform with the specified message.
        /// </summary>
        /// <param name="title">The title to show.</param>
        /// <param name="message">The message to show.</param>
        public void ShowMessage(string title, string message)
        {
            this.Invoke(() =>
            {
                MessageBox.Show(message, title);
            });
        }

        /// <summary>
        /// Creates the window to be hosted.
        /// </summary>
        /// <param name="hwndParent">The window handle of the parent window.</param>
        /// <returns> The handle to the child Win32 window to create.</returns>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this.ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            var onWebMessageReceivedDelegate = (WebMessageReceivedCallback)this.ReceiveWebMessage;
            this.gcHandlesToFree.Add(GCHandle.Alloc(onWebMessageReceivedDelegate));

            this.blazorWebView = BlazorWebViewNative_Ctor(hwndParent.Handle, onWebMessageReceivedDelegate);
            var hwnd = BlazorWebViewNative_GetHWND(this.blazorWebView);
            return new HandleRef(this, hwnd);
        }

        /// <summary>
        /// Destroys the hosted window.
        /// </summary>
        /// <param name="hwnd">A structure that contains the window handle.</param>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            BlazorWebViewNative_Dtor(this.blazorWebView);
            foreach (var gcHandle in this.gcHandlesToFree)
            {
                gcHandle.Free();
            }

            this.gcHandlesToFree.Clear();
        }

        /// <summary>
        /// Registers a window class for the window that is hosted.
        /// </summary>
        /// <param name="hInstance">The instance handle for the module.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BlazorWebViewNative_Register(IntPtr hInstance);

        /// <summary>
        /// Calls the constructor of the native webview opbject.
        /// </summary>
        /// <param name="parent">A handle to the parent window.</param>
        /// <param name="webMessageReceivedCallback">The callback to use when a message is received from javascript.</param>
        /// <returns>A pointer to the native webview object.</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr BlazorWebViewNative_Ctor(IntPtr parent, WebMessageReceivedCallback webMessageReceivedCallback);

        /// <summary>
        /// Gets the window handle of the native webview object.
        /// </summary>
        /// <param name="blazorWebView">A pointer to the native webview object.</param>
        /// <returns>A window handle.</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr BlazorWebViewNative_GetHWND(IntPtr blazorWebView);

        /// <summary>
        /// Calls the destructor of the native webview object.
        /// </summary>
        /// <param name="blazorWebView">A pointer to the native webview object.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BlazorWebViewNative_Dtor(IntPtr blazorWebView);

        /// <summary>
        /// Initializes the native webview object.
        /// </summary>
        /// <param name="blazorWebView">A pointer to the native webview object.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BlazorWebViewNative_Initialize(IntPtr blazorWebView);

        /// <summary>
        /// Adds a custom scheme to the native webview.
        /// </summary>
        /// <param name="blazorWebView">A pointer to the native webview object.</param>
        /// <param name="scheme">The schemde to register the custom scheme for.</param>
        /// <param name="requestHandler">The request handler to use.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void BlazorWebViewNative_AddCustomScheme(IntPtr blazorWebView, string scheme, WebResourceRequestedCallback requestHandler);

        /// <summary>
        /// Navigates the native webview to an Url.
        /// </summary>
        /// <param name="blazorWebView">A pointer to the native webview object.</param>
        /// <param name="url">The url to navigate to.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void BlazorWebViewNative_NavigateToUrl(IntPtr blazorWebView, string url);

        /// <summary>
        /// Sends a message to javascript.
        /// </summary>
        /// <param name="blazorWebView">A pointer to the native webview object.</param>
        /// <param name="message">The message to send.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void BlazorWebViewNative_SendMessage(IntPtr blazorWebView, string message);

        private void AddCustomScheme(string scheme, ResolveWebResourceDelegate requestHandler)
        {
            // Because of WKWebView limitations, this can only be called during the constructor
            // before the first call to Show. To enforce this, it's private and is only called
            // in response to the constructor options.
            WebResourceRequestedCallback callback = (string url, out int numBytes, out string contentType) =>
            {
                var responseStream = requestHandler(url, out contentType, out Encoding encoding);
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
                    var buffer = Marshal.AllocCoTaskMem(numBytes);
                    Marshal.Copy(ms.GetBuffer(), 0, buffer, numBytes);
                    return buffer;
                }
            };

            this.gcHandlesToFree.Add(GCHandle.Alloc(callback));
            BlazorWebViewNative_AddCustomScheme(this.blazorWebView, scheme, callback);
        }

        /// <summary>
        /// Receives a message from javascript.
        /// </summary>
        /// <param name="message">The message to receive.</param>
        private void ReceiveWebMessage(string message)
        {
            this.OnWebMessageReceived?.Invoke(this, message);
        }
    }
}
