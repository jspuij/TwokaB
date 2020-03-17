// <copyright file="ApplicationBuilder.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Application builder class for blazor applications.
    /// </summary>
    public class ApplicationBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection to use.</param>
        public ApplicationBuilder(IServiceProvider services)
        {
            this.Services = services;
            this.Entries = new List<(Type componentType, string domElementSelector)>();
        }

        /// <summary>
        /// Gets a list of component entries with associated dom selectors.
        /// </summary>
        public List<(Type componentType, string domElementSelector)> Entries { get; }

        /// <summary>
        /// Gets the list of services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Adds a component to the list of Entries.
        /// </summary>
        /// <param name="componentType">The type of the component to replace the content inside
        /// the html with.</param>
        /// <param name="domElementSelector">The selector to select the element in the DOM.</param>
        public void AddComponent(Type componentType, string domElementSelector)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (domElementSelector == null)
            {
                throw new ArgumentNullException(nameof(domElementSelector));
            }

            this.Entries.Add((componentType, domElementSelector));
        }

        /// <summary>
        /// Adds the specified component to the list of component entries
        /// to replace in html.
        /// </summary>
        /// <typeparam name="T">The type of the component to replace the content inside
        /// the html with.</typeparam>
        /// <param name="domElementSelector">The selector to select the element in the DOM.</param>
        public void AddComponent<T>(string domElementSelector)
            where T : IComponent
            => this.AddComponent(typeof(T), domElementSelector);
    }
}
