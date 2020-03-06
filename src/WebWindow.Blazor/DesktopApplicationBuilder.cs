// <copyright file="DesktopApplicationBuilder.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace WebWindows.Blazor
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Application builder class for blazor applications.
    /// </summary>
    public class DesktopApplicationBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopApplicationBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection to use.</param>
        public DesktopApplicationBuilder(IServiceProvider services)
        {
            this.Services = services;
            this.Entries = new List<(Type componentType, string domElementSelector)>();
        }

        public List<(Type componentType, string domElementSelector)> Entries { get; }

        public IServiceProvider Services { get; }

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

            Entries.Add((componentType, domElementSelector));
        }

        public void AddComponent<T>(string domElementSelector) where T : IComponent
            => AddComponent(typeof(T), domElementSelector);
    }
}
