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

namespace BlazorWebView
{
    using System;
    using System.Collections.Generic;
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
    internal class PlatformRenderer : Renderer
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
        /// Whether the incoming event is a dispatching event.
        /// </summary>
        private bool isDispatchingEvent;

        /// <summary>
        /// A queue of deferred incoming events.
        /// </summary>
        private Queue<IncomingEventInfo> deferredIncomingEvents = new Queue<IncomingEventInfo>();

        /// <summary>
        /// Initializes static members of the <see cref="PlatformRenderer"/> class.
        /// </summary>
        static PlatformRenderer()
        {
            Writer = typeof(RenderBatchWriter);
            WriteMethod = Writer.GetMethod("Write", new[] { typeof(RenderBatch).MakeByRefType() });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformRenderer"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve services from.</param>
        /// <param name="ipc">The inter process communication channel.</param>
        /// <param name="loggerFactory">A logger factory.</param>
        public PlatformRenderer(IServiceProvider serviceProvider, IPC ipc, ILoggerFactory loggerFactory)
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
        /// Associates the <see cref="IComponent"/> with the <see cref="PlatformRenderer"/>,
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
        /// Notifies the renderer that an event has occured.
        /// </summary>
        /// <param name="eventHandlerId">The event handler Id.</param>
        /// <param name="eventFieldInfo">The evet field info.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>A <see cref="Task"/>A task representing the asynchronous operation.</returns>
        public override Task DispatchEventAsync(ulong eventHandlerId, EventFieldInfo eventFieldInfo, EventArgs eventArgs)
        {
            // Be sure we only run one event handler at once. Although they couldn't run
            // simultaneously anyway (there's only one thread), they could run nested on
            // the stack if somehow one event handler triggers another event synchronously.
            // We need event handlers not to overlap because (a) that's consistent with
            // server-side Blazor which uses a sync context, and (b) the rendering logic
            // relies completely on the idea that within a given scope it's only building
            // or processing one batch at a time.
            //
            // The only currently known case where this makes a difference is in the E2E
            // tests in ReorderingFocusComponent, where we hit what seems like a Chrome bug
            // where mutating the DOM cause an element's "change" to fire while its "input"
            // handler is still running (i.e., nested on the stack) -- this doesn't happen
            // in Firefox. Possibly a future version of Chrome may fix this, but even then,
            // it's conceivable that DOM mutation events could trigger this too.
            if (this.isDispatchingEvent)
            {
                var info = new IncomingEventInfo(eventHandlerId, eventFieldInfo, eventArgs);
                this.deferredIncomingEvents.Enqueue(info);
                return info.TaskCompletionSource.Task;
            }
            else
            {
                try
                {
                    this.isDispatchingEvent = true;
                    return base.DispatchEventAsync(eventHandlerId, eventFieldInfo, eventArgs);
                }
                finally
                {
                    this.isDispatchingEvent = false;

                    if (this.deferredIncomingEvents.Count > 0)
                    {
                        // Fire-and-forget because the task we return from this method should only reflect the
                        // completion of its own event dispatch, not that of any others that happen to be queued.
                        // Also, ProcessNextDeferredEventAsync deals with its own async errors.
                        _ = this.ProcessNextDeferredEventAsync();
                    }
                }
            }
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

        /// <summary>
        /// Processes the next deferred event asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ProcessNextDeferredEventAsync()
        {
            var info = this.deferredIncomingEvents.Dequeue();
            var taskCompletionSource = info.TaskCompletionSource;

            try
            {
                await this.DispatchEventAsync(info.EventHandlerId, info.EventFieldInfo, info.EventArgs);
                taskCompletionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// A struct for the incoming event info.
        /// </summary>
        private readonly struct IncomingEventInfo
        {
            public readonly ulong EventHandlerId;
            public readonly EventFieldInfo EventFieldInfo;
            public readonly EventArgs EventArgs;
            public readonly TaskCompletionSource<object> TaskCompletionSource;

            public IncomingEventInfo(ulong eventHandlerId, EventFieldInfo eventFieldInfo, EventArgs eventArgs)
            {
                this.EventHandlerId = eventHandlerId;
                this.EventFieldInfo = eventFieldInfo;
                this.EventArgs = eventArgs;
                this.TaskCompletionSource = new TaskCompletionSource<object>();
            }
        }
    }
}
