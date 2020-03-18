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
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// UserControl that wraps either an NewEdgeWebView or the normal EdgeWebView, depending on what is installed.
    /// </summary>
    public partial class BlazorWebView : UserControl, IBlazorWebView
    {
        /// <summary>
        /// The readonly grid.
        /// </summary>
        private readonly Grid grid;

        private IBlazorWebView innerBlazorWebView;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorWebView"/> class.
        /// </summary>
        public BlazorWebView()
        {
            this.innerBlazorWebView = new BlazorNewEdgeWebView();
            this.grid = new Grid();
            this.grid.Children.Add((BlazorNewEdgeWebView)this.innerBlazorWebView);
            this.Content = this.grid;
        }

        /// <summary>
        /// Event that is fired when a web message is received from javascript.
        /// </summary>
        public event EventHandler<string> OnWebMessageReceived
        {
            add
            {
                this.innerBlazorWebView.OnWebMessageReceived += value;
            }

            remove
            {
                this.innerBlazorWebView.OnWebMessageReceived -= value;
            }
        }

        /// <summary>
        /// Initialize the BlazorWebView.
        /// </summary>
        /// <param name="configure">A delegate that is executed to configure the webview.</param>
        public void Initialize(Action<WebViewOptions> configure)
        {
            try
            {
                this.innerBlazorWebView.Initialize(configure);
            }
            catch (InvalidOperationException)
            {
                this.grid.Children.Remove((BlazorNewEdgeWebView)this.innerBlazorWebView);
                ((BlazorNewEdgeWebView)this.innerBlazorWebView).Dispose();

                // init old edge control.
                this.innerBlazorWebView = new BlazorOldEdgeWebView();
                this.grid.Children.Add((BlazorOldEdgeWebView)this.innerBlazorWebView);
                this.innerBlazorWebView.Initialize(configure);
            }
        }

        /// <summary>
        /// Invoke a callback on the UI thread.
        /// </summary>
        /// <param name="callback">The callback to execute.</param>
        public void Invoke(Action callback)
        {
            this.innerBlazorWebView.Invoke(callback);
        }

        /// <summary>
        /// Navigate to the specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        public void NavigateToUrl(string url)
        {
            this.innerBlazorWebView.NavigateToUrl(url);
        }

        /// <summary>
        /// Send a message to javascript.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(string message)
        {
            this.innerBlazorWebView.SendMessage(message);
        }

        /// <summary>
        /// Show a native dialog for the platform with the specified message.
        /// </summary>
        /// <param name="title">The title to show.</param>
        /// <param name="message">The message to show.</param>
        public void ShowMessage(string title, string message)
        {
            this.innerBlazorWebView.ShowMessage(title, message);
        }
    }
}
