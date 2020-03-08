// <copyright file="JSInteropMethods.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components.RenderTree;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    /// <summary>
    /// Methods that can be called from javascript.
    /// </summary>
    public static class JSInteropMethods
    {
        /// <summary>
        /// Dispatches an event with the specified event descriptor
        /// and event arguments to the renderer.
        /// </summary>
        /// <param name="eventDescriptor">The event descriptor.</param>
        /// <param name="eventArgsJson">The event arguments.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [JSInvokable(nameof(DispatchEvent))]
        public static async Task DispatchEvent(WebEventDescriptor eventDescriptor, string eventArgsJson)
        {
            var webEvent = WebEventData.Parse(eventDescriptor, eventArgsJson);
            var renderer = ComponentsDesktop.DesktopRenderer;
            await renderer.DispatchEventAsync(
                webEvent.EventHandlerId,
                webEvent.EventFieldInfo,
                webEvent.EventArgs);
        }

        /// <summary>
        /// Notify the navigation manager that the location has changed.
        /// </summary>
        /// <param name="uri">The new uri.</param>
        /// <param name="isInterceptedLink">Whether it is an intercepted link.</param>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uri, bool isInterceptedLink)
        {
            DesktopNavigationManager.Instance.SetLocation(uri, isInterceptedLink);
        }
    }
}
