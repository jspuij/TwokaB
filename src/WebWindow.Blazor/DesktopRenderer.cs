// <copyright file="DesktopRenderer.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.RenderTree;
    using Microsoft.AspNetCore.Components.Server.Circuits;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.JSInterop;

    /// <summary>
    /// An HTML renderer for the platform.
    /// </summary>
    /// <remarks>
    /// Many aspects of the layering here are not what we really want, but it won't affect
    /// people prototyping applications with it. We can put more work into restructuring the
    /// hosting and startup models in the future if it's justified.
    /// </remarks>
    internal class DesktopRenderer : Renderer
    {
        /// <summary>
        /// The renderer Id.
        /// </summary>
        private const int RendererId = 0; // Not relevant, since we have only one renderer in Desktop

        /// <summary>
        /// Reference to the renderbatch type.
        /// </summary>
        private static readonly Type Writer;

        /// <summary>
        /// Reference to the write method on the renderbatch type.
        /// </summary>
        private static readonly MethodInfo WriteMethod;

        /// <summary>
        /// The inter process communication channel.
        /// </summary>
        private readonly IPC ipc;

        /// <summary>
        /// The javacript runtime implementation.
        /// </summary>
        private readonly IJSRuntime jsRuntime;

        /// <summary>
        /// Initializes static members of the <see cref="DesktopRenderer"/> class.
        /// </summary>
        static DesktopRenderer()
        {
            Writer = typeof(RenderBatchWriter);
            WriteMethod = Writer.GetMethod("Write", new[] { typeof(RenderBatch).MakeByRefType() });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopRenderer"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve services from.</param>
        /// <param name="ipc">The inter process communication channel.</param>
        /// <param name="loggerFactory">A logger factory.</param>
        public DesktopRenderer(IServiceProvider serviceProvider, IPC ipc, ILoggerFactory loggerFactory)
            : base(serviceProvider, loggerFactory)
        {
            this.ipc = ipc ?? throw new ArgumentNullException(nameof(ipc));
            this.jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
        }

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Gets the Dispatcher associated with this renderer.
        /// </summary>
        public override Dispatcher Dispatcher { get; } = NullDispatcher.Instance;

        /// <summary>
        /// Attaches a new root component to the renderer,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task AddComponentAsync<TComponent>(string domElementSelector)
            where TComponent : IComponent
        {
            return this.AddComponentAsync(typeof(TComponent), domElementSelector);
        }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="DesktopRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task AddComponentAsync(Type componentType, string domElementSelector)
        {
            var component = this.InstantiateComponent(componentType);
            var componentId = this.AssignRootComponentId(component);

            var attachComponentTask = this.jsRuntime.InvokeAsync<object>(
                "Blazor._internal.attachRootComponentToElement",
                domElementSelector,
                componentId,
                RendererId);
            this.CaptureAsyncExceptions(attachComponentTask);
            return this.RenderRootComponentAsync(componentId);
        }

        /// <summary>
        /// Updates the visible part of the UI.
        /// </summary>
        /// <param name="batch">The batch to render.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            string base64;
            using (var memoryStream = new MemoryStream())
            {
                object renderBatchWriter = Activator.CreateInstance(Writer, new object[] { memoryStream, false });
                using (renderBatchWriter as IDisposable)
                {
                    // TODO: use delegate instead of reflection for more performance.
                    WriteMethod.Invoke(renderBatchWriter, new object[] { batch });
                }

                var batchBytes = memoryStream.ToArray();
                base64 = Convert.ToBase64String(batchBytes);
            }

            this.ipc.Send("JS.RenderBatch", RendererId, base64);

            // TODO: Consider finding a way to get back a completion message from the Desktop side
            // in case there was an error. We don't really need to wait for anything to happen, since
            // this is not prerendering and we don't care how quickly the UI is updated, but it would
            // be desirable to flow back errors.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles an exception.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        protected override void HandleException(Exception exception)
        {
            // TODO: Logging to the console is not very useful.
            Console.WriteLine(exception.ToString());
        }

        /// <summary>
        /// Captures an async exception for the task and invokes <see cref="UnhandledException"/>.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        private async void CaptureAsyncExceptions(ValueTask<object> task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                this.UnhandledException?.Invoke(this, ex);
            }
        }
    }
}
