// <copyright file="WebViewOptions.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Delegate that can be executed when a web resource needs to be resolved.
    /// </summary>
    /// <param name="url">The url for the web resource.</param>
    /// <param name="contentType">The content type of the web resource.</param>
    /// <param name="encoding">The encoding of the resource.</param>
    /// <returns>A stream with the content of the web resource.</returns>
    public delegate Stream ResolveWebResourceDelegate(string url, out string contentType, out Encoding encoding);

    /// <summary>
    /// The options for the webview.
    /// </summary>
    public class WebViewOptions
    {
        /// <summary>
        /// Gets a dictionary of Http Scheme handlers.
        /// </summary>
        public IDictionary<string, ResolveWebResourceDelegate> SchemeHandlers { get; }
            = new Dictionary<string, ResolveWebResourceDelegate>();
    }
}
