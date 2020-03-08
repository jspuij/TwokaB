// <copyright file="DesktopNavigationManager.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    /// <summary>
    /// A <see cref="NavigationManager"/> implementation for the platform.
    /// </summary>
    internal class DesktopNavigationManager : NavigationManager
    {
        /// <summary>
        /// The single instance of the Desktop navigation manager.
        /// </summary>
        public static readonly DesktopNavigationManager Instance = new DesktopNavigationManager();

        /// <summary>
        /// The prefix for the interop to the javascript part of the navigation manager.
        /// </summary>
        private static readonly string InteropPrefix = "Blazor._internal.navigationManager.";

        /// <summary>
        /// The method name for the javascript function to navigate to an url.
        /// </summary>
        private static readonly string InteropNavigateTo = InteropPrefix + "navigateTo";

        /// <summary>
        /// Sets the specified location.
        /// </summary>
        /// <param name="uri">The uri to set as location.</param>
        /// <param name="isInterceptedLink">Indicates whether this uri is an intercepted link.</param>
        public void SetLocation(string uri, bool isInterceptedLink)
        {
            this.Uri = uri;
            this.NotifyLocationChanged(isInterceptedLink);
        }

        /// <summary>
        /// Ensures that the navigation manager is intialized with
        /// the right initial and base Uri.
        /// </summary>
        protected override void EnsureInitialized()
        {
            this.Initialize(ComponentsDesktop.BaseUriAbsolute, ComponentsDesktop.InitialUriAbsolute);
        }

        /// <summary>
        /// Navigates to the specified Uri.
        /// </summary>
        /// <param name="uri">The uri to load.</param>
        /// <param name="forceLoad">Force a reload by the browser.</param>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            ComponentsDesktop.DesktopJSRuntime.InvokeAsync<object>(InteropNavigateTo, uri, forceLoad);
        }
    }
}
