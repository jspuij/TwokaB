// <copyright file="BlazorJavascriptInterface.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorWebView.Android
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using global::Android.App;
    using global::Android.Content;
    using global::Android.OS;
    using global::Android.Runtime;
    using global::Android.Views;
    using global::Android.Webkit;
    using global::Android.Widget;
    using Java.Interop;

    /// <summary>
    /// Defines a class that is the interface between Javascript and .Net.
    /// </summary>
    public class BlazorJavascriptInterface : Java.Lang.Object
    {
        /// <summary>
        /// A reference to the parent BlazorWebView.
        /// </summary>
        private readonly BlazorWebView blazorWebView;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorJavascriptInterface"/> class.
        /// </summary>
        /// <param name="blazorWebView">The blazor web view to use.</param>
        public BlazorJavascriptInterface(BlazorWebView blazorWebView)
        {
            this.blazorWebView = blazorWebView;
        }

        /// <summary>
        /// The function that is called from javasript to post a message to .NET.
        /// </summary>
        /// <param name="message">The message to post.</param>
        [Export]
        [JavascriptInterface]
        public void PostMessage(string message)
        {
            this.blazorWebView.OnReceiveWebMessage(message);
        }
    }
}