// <copyright file="IPC.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using BlazorWebView;
    using WebWindows;

    /// <summary>
    /// Inter process communication channel.
    /// </summary>
    internal class IPC
    {
        /// <summary>
        /// Dictionary of registrations by key.
        /// </summary>
        private readonly Dictionary<string, List<Action<object>>> registrations = new Dictionary<string, List<Action<object>>>();

        /// <summary>
        /// The <see cref="IBlazorWebView"/> to communicate with.
        /// </summary>
        private readonly IBlazorWebView blazorWebView;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPC"/> class.
        /// </summary>
        /// <param name="blazorWebView">The <see cref="IBlazorWebView"/> to communicate with.</param>
        public IPC(IBlazorWebView blazorWebView)
        {
            this.blazorWebView = blazorWebView ?? throw new ArgumentNullException(nameof(blazorWebView));
            this.blazorWebView.OnWebMessageReceived += this.HandleScriptNotify;
        }

        /// <summary>
        /// Sends a message, triggering a javascript event with the specified arguments.
        /// </summary>
        /// <param name="eventName">The eventname.</param>
        /// <param name="args">The arguments.</param>
        public void Send(string eventName, params object[] args)
        {
            try
            {
                // invoke on the controls UI thread.
                this.blazorWebView.Invoke(() =>
                {
                    this.blazorWebView.SendMessage($"{eventName}:{JsonSerializer.Serialize(args)}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Registers a callback for an event name.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="callback">The callback to execute.</param>
        public void On(string eventName, Action<object> callback)
        {
            lock (this.registrations)
            {
                if (!this.registrations.TryGetValue(eventName, out var group))
                {
                    group = new List<Action<object>>();
                    this.registrations.Add(eventName, group);
                }

                group.Add(callback);
            }
        }

        /// <summary>
        /// Registers a callback to receive an event only once.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="callback">The callback to execute.</param>
        public void Once(string eventName, Action<object> callback)
        {
            Action<object> callbackOnce = null;
            callbackOnce = arg =>
            {
                this.Off(eventName, callbackOnce);
                callback(arg);
            };

            this.On(eventName, callbackOnce);
        }

        /// <summary>
        /// Removes the callback function for the event name.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="callback">The callback to execute.</param>
        public void Off(string eventName, Action<object> callback)
        {
            lock (this.registrations)
            {
                if (this.registrations.TryGetValue(eventName, out var group))
                {
                    group.Remove(callback);
                }
            }
        }

        /// <summary>
        /// Handles a notification of a javascript call.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The message.</param>
        private void HandleScriptNotify(object sender, string message)
        {
            var value = message;

            // Move off the browser UI thread
            Task.Factory.StartNew(() =>
            {
                if (value.StartsWith("ipc:"))
                {
                    var spacePos = value.IndexOf(' ');
                    var eventName = value.Substring(4, spacePos - 4);
                    var argsJson = value.Substring(spacePos + 1);
                    var args = JsonSerializer.Deserialize<object[]>(argsJson);

                    Action<object>[] callbacksCopy;
                    lock (this.registrations)
                    {
                        if (!this.registrations.TryGetValue(eventName, out var callbacks))
                        {
                            return;
                        }

                        callbacksCopy = callbacks.ToArray();
                    }

                    foreach (var callback in callbacksCopy)
                    {
                        callback(args);
                    }
                }
            });
        }
    }
}
