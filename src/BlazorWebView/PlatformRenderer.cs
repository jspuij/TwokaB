// <copyright file="PlatformRenderer.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
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
        /// Represents a canceled task.
        /// </summary>
        private static readonly Task CanceledTask = Task.FromCanceled(new CancellationToken(canceled: true));

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
        /// A concurrent queue of unacknowledged render batches.
        /// </summary>
        private readonly ConcurrentQueue<UnacknowledgedRenderBatch> unacknowledgedRenderBatches = new ConcurrentQueue<UnacknowledgedRenderBatch>();

        /// <summary>
        /// A value indicating wether the renderer is disposing.
        /// </summary>
        private bool disposing = false;

        /// <summary>
        /// A number indicating the next render batch.
        /// </summary>
        private long nextRenderId = 1;

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
        /// <param name="dispatcher">The dispatcher to use.</param>
        /// <param name="jsRuntime">The runtime to use.</param>
        /// <param name="ipc">The inter process communication channel.</param>
        /// <param name="loggerFactory">A logger factory.</param>
        public PlatformRenderer(IServiceProvider serviceProvider, IPC ipc, ILoggerFactory loggerFactory, Dispatcher dispatcher, IJSRuntime jsRuntime)
            : base(serviceProvider, loggerFactory)
        {
            this.ipc = ipc ?? throw new ArgumentNullException(nameof(ipc));
            this.Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Gets the Dispatcher associated with this renderer.
        /// </summary>
        public override Dispatcher Dispatcher { get; }

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
        /// Signals that a render batch has completed.
        /// </summary>
        /// <param name="incomingBatchId">The render batch id.</param>
        /// <param name="errorMessageOrNull">The error message or null.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task OnRenderCompletedAsync(long incomingBatchId, string errorMessageOrNull)
        {
            if (this.disposing)
            {
                // Disposing so don't do work.
                return Task.CompletedTask;
            }

            if (!this.unacknowledgedRenderBatches.TryPeek(out var nextUnacknowledgedBatch) || incomingBatchId < nextUnacknowledgedBatch.BatchId)
            {
                // TODO: Log duplicated batch ack.
                return Task.CompletedTask;
            }
            else
            {
                var lastBatchId = nextUnacknowledgedBatch.BatchId;

                // Order is important here so that we don't prematurely dequeue the last nextUnacknowledgedBatch
                while (this.unacknowledgedRenderBatches.TryPeek(out nextUnacknowledgedBatch) && nextUnacknowledgedBatch.BatchId <= incomingBatchId)
                {
                    lastBatchId = nextUnacknowledgedBatch.BatchId;

                    // At this point the queue is definitely not full, we have at least emptied one slot, so we allow a further
                    // full queue log entry the next time it fills up.
                    this.unacknowledgedRenderBatches.TryDequeue(out _);
                    this.ProcessPendingBatch(errorMessageOrNull, nextUnacknowledgedBatch);
                }

                if (lastBatchId < incomingBatchId)
                {
                    // This exception is due to a bad client input, so we mark it as such to prevent logging it as a warning and
                    // flooding the logs with warnings.
                    throw new InvalidOperationException($"Received an acknowledgement for batch with id '{incomingBatchId}' when the last batch produced was '{lastBatchId}'.");
                }

                // Normally we will not have pending renders, but it might happen that we reached the limit of
                // available buffered renders and new renders got queued.
                // Invoke ProcessBufferedRenderRequests so that we might produce any additional batch that is
                // missing.

                // We return the task in here, but the caller doesn't await it.
                return this.Dispatcher.InvokeAsync(() =>
                {
                    // Now we're on the sync context, check again whether we got disposed since this
                    // work item was queued. If so there's nothing to do.
                    if (!this.disposing)
                    {
                        this.ProcessPendingRender();
                    }
                });
            }
        }

        /// <summary>
        /// Updates the visible part of the UI.
        /// </summary>
        /// <param name="batch">The batch to render.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            if (this.disposing)
            {
                // We are being disposed, so do no work.
                return CanceledTask;
            }

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

            var renderId = Interlocked.Increment(ref this.nextRenderId);

            var pendingRender = new UnacknowledgedRenderBatch(
                renderId,
                new TaskCompletionSource<object>());

            // Buffer the rendered batches no matter what. We'll send it down immediately when the client
            // is connected or right after the client reconnects.
            this.unacknowledgedRenderBatches.Enqueue(pendingRender);

            this.ipc.Send("JS.RenderBatch", renderId, base64);

            return pendingRender.CompletionSource.Task;
        }

        /// <summary>
        /// Releases all resources currently used by this <see cref="PlatformRenderer"/> instance.
        /// </summary>
        /// <param name="disposing">true if this method is being invoked by System.IDisposable.Dispose, otherwise false.</param>
        protected override void Dispose(bool disposing)
        {
            this.disposing = true;
            while (this.unacknowledgedRenderBatches.TryDequeue(out var entry))
            {
                entry.CompletionSource.TrySetCanceled();
            }

            base.Dispose(true);
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
        /// Processes a pending batch.
        /// </summary>
        /// <param name="errorMessageOrNull">The error message or null.</param>
        /// <param name="entry">The entry to process.</param>
        private void ProcessPendingBatch(string errorMessageOrNull, UnacknowledgedRenderBatch entry)
        {
            this.CompleteRender(entry.CompletionSource, errorMessageOrNull);
        }

        /// <summary>
        /// Completes a render pass.
        /// </summary>
        /// <param name="pendingRenderInfo">The pending render info.</param>
        /// <param name="errorMessageOrNull">The error message.</param>
        private void CompleteRender(TaskCompletionSource<object> pendingRenderInfo, string errorMessageOrNull)
        {
            if (errorMessageOrNull == null)
            {
                pendingRenderInfo.TrySetResult(null);
            }
            else
            {
                pendingRenderInfo.TrySetException(new InvalidOperationException(errorMessageOrNull));
            }
        }

        /// <summary>
        /// A struct representing an unacknowledged render batch.
        /// </summary>
        internal readonly struct UnacknowledgedRenderBatch
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="UnacknowledgedRenderBatch"/> struct.
            /// </summary>
            /// <param name="batchId">The batch id.</param>
            /// <param name="completionSource">The completion source.</param>
            public UnacknowledgedRenderBatch(long batchId, TaskCompletionSource<object> completionSource)
            {
                this.BatchId = batchId;
                this.CompletionSource = completionSource;
            }

            /// <summary>
            /// Gets the batch id.
            /// </summary>
            public long BatchId { get; }

            /// <summary>
            /// Gets the completion source.
            /// </summary>
            public TaskCompletionSource<object> CompletionSource { get; }
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
