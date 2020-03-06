// <copyright file="ConventionBasedStartup.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Handles processing of a Convention based Startup class.
    /// </summary>
    internal class ConventionBasedStartup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedStartup"/> class.
        /// </summary>
        /// <param name="instance">The startup instance.</param>
        public ConventionBasedStartup(object instance)
        {
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        /// <summary>
        /// Gets the instance of the startup class.
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// Calls the ConfigureServices method on the startup class.
        /// </summary>
        /// <param name="services">The services collection to use.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                var method = this.GetConfigureServicesMethod();
                if (method != null)
                {
                    method.Invoke(this.Instance, new object[] { services });
                }
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }

                throw;
            }
        }

        /// <summary>
        /// Calls configure on the startup class.
        /// </summary>
        /// <param name="app">The application builder to use.</param>
        /// <param name="services">The services to register.</param>
        public void Configure(DesktopApplicationBuilder app, IServiceProvider services)
        {
            try
            {
                var method = this.GetConfigureMethod();
                Debug.Assert(method != null, "Did not find configure method.");

                var parameters = method.GetParameters();
                var arguments = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    arguments[i] = parameter.ParameterType == typeof(DesktopApplicationBuilder)
                        ? app
                        : services.GetRequiredService(parameter.ParameterType);
                }

                method.Invoke(this.Instance, arguments);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }

                throw;
            }
        }

        /// <summary>
        /// Tries to get the configure method from the startup instance.
        /// </summary>
        /// <returns>The MethodInfo object.</returns>
        internal MethodInfo GetConfigureMethod()
        {
            var methods = this.Instance.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => string.Equals(m.Name, "Configure", StringComparison.Ordinal))
                .ToArray();

            if (methods.Length == 1)
            {
                return methods[0];
            }
            else if (methods.Length == 0)
            {
                throw new InvalidOperationException("The startup class must define a 'Configure' method.");
            }
            else
            {
                throw new InvalidOperationException("Overloading the 'Configure' method is not supported.");
            }
        }

        /// <summary>
        /// Tries to get the configureservices method from the startup instance.
        /// </summary>
        /// <returns>The MethodInfo object.</returns>
        internal MethodInfo GetConfigureServicesMethod()
        {
            return this.Instance.GetType()
                .GetMethod(
                    "ConfigureServices",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(IServiceCollection), },
                    Array.Empty<ParameterModifier>());
        }
    }
}