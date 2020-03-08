// <copyright file="DesktopNavigationInterception.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components.Routing;

    /// <summary>
    /// Class that setups navigation interception on this platform.
    /// </summary>
    /// <remarks>Not necessary with web views, so this class does nothing but
    /// implement <see cref="INavigationInterception" />.
    /// </remarks>
    internal class DesktopNavigationInterception : INavigationInterception
    {
        /// <summary>
        /// Contract to setup navigation interception on the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task EnableNavigationInterceptionAsync()
        {
            // We don't actually need to set anything up in this environment
            return Task.CompletedTask;
        }
    }
}