// <copyright file="IBlazorWebView.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorWebView
{
    using System;

    /// <summary>
    /// Interface to be implemented by all platform specific BlazorWebView versions.
    /// </summary>
    public interface IBlazorWebView
    {
        /// <summary>
        /// Event that is fired when a web message is received from javascript.
        /// </summary>
        event EventHandler<string> OnWebMessageReceived;

        /// <summary>
        /// Initialize the BlazorWebView.
        /// </summary>
        /// <param name="configure">A delegate that is executed to configure the webview.</param>
        void Initialize(Action<WebViewOptions> configure);

        /// <summary>
        /// Invoke a callback on the UI thread.
        /// </summary>
        /// <param name="callback">The callback to execute.</param>
        void Invoke(Action callback);

        /// <summary>
        /// Send a message to javascript.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessage(string message);

        /// <summary>
        /// Navigate to the specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        void NavigateToUrl(string url);

        /// <summary>
        /// Show a native dialog for the platform with the specified message.
        /// </summary>
        /// <param name="title">The title to show.</param>
        /// <param name="message">The message to show.</param>
        void ShowMessage(string title, string message);
    }
}
