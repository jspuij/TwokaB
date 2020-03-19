// <copyright file="PlatformSynchronizationContext.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A <see cref="System.Threading.SynchronizationContext"/> implementation that schedules operations on the UI thread.
    /// </summary>
    internal class PlatformSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// A Queue to queue work.
        /// </summary>
        private readonly WorkQueue workQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformSynchronizationContext"/> class.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel work.</param>
        public PlatformSynchronizationContext(CancellationToken cancellationToken)
        {
            this.workQueue = new WorkQueue(this, cancellationToken);
        }

        /// <summary>
        /// An event that is triggered when an unhandled exception occurs.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Checks whether the current operation is already in the right context.
        /// </summary>
        /// <returns>True when already on the right context.</returns>
        public static bool CheckAccess()
        {
            if (!(Current is PlatformSynchronizationContext synchronizationContext))
            {
                throw new InvalidOperationException("Not in the right context.");
            }

            return synchronizationContext.workQueue.CheckAccess();
        }

        /// <summary>
        /// Returns itself instead of creating a copy of this <see cref="PlatformSynchronizationContext"/>.
        /// </summary>
        /// <returns>The synchronisation context.</returns>
        public override System.Threading.SynchronizationContext CreateCopy()
        {
            return this;
        }

        /// <summary>
        /// Dispatches an asynchronous message to a synchronisation context.
        /// </summary>
        /// <param name="d">The callback to dispatch.</param>
        /// <param name="state">A state object to pass.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            this.workQueue.Queue.Add(new WorkItem() { Callback = d, Context = this, State = state, });
        }

        /// <summary>
        /// Dispatches a synchronous message to a synchronisation context.
        /// </summary>
        /// <param name="d">The callback to dispatch.</param>
        /// <param name="state">A state object to pass.</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (this.workQueue.CheckAccess())
            {
                this.workQueue.ProcessWorkitemInline(d, state);
            }
            else
            {
                var completed = new ManualResetEventSlim();
                this.workQueue.Queue.Add(new WorkItem() { Callback = d, Context = this, State = state, Completed = completed, });
                completed.Wait();
            }
        }

        /// <summary>
        /// Stops the queue that dispatches the work to ui thread.
        /// </summary>
        public void Stop()
        {
            this.workQueue.Queue.CompleteAdding();
        }

        /// <summary>
        /// A workitem to schedule.
        /// </summary>
        private struct WorkItem
        {
            /// <summary>
            /// The callback to execute.
            /// </summary>
            public SendOrPostCallback Callback;

            /// <summary>
            /// The state object.
            /// </summary>
            public object State;

            /// <summary>
            /// The context associated with the workitem.
            /// </summary>
            public System.Threading.SynchronizationContext Context;

            /// <summary>
            /// The <see cref="ManualResetEvent"/> to signal for synchronous operations.
            /// </summary>
            public ManualResetEventSlim Completed;
        }

        /// <summary>
        /// A work queue.
        /// </summary>
        private class WorkQueue
        {
            /// <summary>
            /// The thread associated with the queue.
            /// </summary>
            private readonly Thread thread;

            /// <summary>
            /// The parent Context.
            /// </summary>
            private readonly PlatformSynchronizationContext parent;

            /// <summary>
            /// The cancellation token to use.
            /// </summary>
            private readonly CancellationToken cancellationToken;

            /// <summary>
            /// Initializes a new instance of the <see cref="WorkQueue"/> class.
            /// </summary>
            /// <param name="parent">The parent SynchronizationContext.</param>
            /// <param name="cancellationToken">A cancellation token.</param>
            public WorkQueue(PlatformSynchronizationContext parent, CancellationToken cancellationToken)
            {
                this.parent = parent;
                this.cancellationToken = cancellationToken;
                this.thread = new Thread(this.ProcessQueue);
                this.thread.Start();
            }

            /// <summary>
            /// Gets the queue to process.
            /// </summary>
            public BlockingCollection<WorkItem> Queue { get; } = new BlockingCollection<WorkItem>();

            /// <summary>
            /// Processes a workitem inline.
            /// </summary>
            /// <param name="callback">The callback to execute.</param>
            /// <param name="state">The state object.</param>
            public void ProcessWorkitemInline(SendOrPostCallback callback, object state)
            {
                try
                {
                    callback(state);
                }
                catch (Exception e)
                {
                    this.parent.UnhandledException?.Invoke(this, e);
                }
            }

            /// <summary>
            /// Checks whether the current operation is already in the right context.
            /// </summary>
            /// <returns>True when already on the right context.</returns>
            public bool CheckAccess()
            {
                return Thread.CurrentThread == this.thread;
            }

            /// <summary>
            /// Process the queue.
            /// </summary>
            private void ProcessQueue()
            {
                while (!this.Queue.IsCompleted)
                {
                    WorkItem item;
                    try
                    {
                        item = this.Queue.Take(this.cancellationToken);
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    var previous = Current;
                    SetSynchronizationContext(item.Context);

                    try
                    {
                        this.ProcessWorkitemInline(item.Callback, item.State);
                    }
                    finally
                    {
                        if (item.Completed != null)
                        {
                            item.Completed.Set();
                        }

                        SetSynchronizationContext(previous);
                    }
                }
            }
        }
    }
}
