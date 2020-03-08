// <copyright file="NullDispatcher.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// A dispatcher that does not dispatch but invokes directly.
    /// </summary>
    internal class NullDispatcher : Dispatcher
    {
        /// <summary>
        /// the single instance.
        /// </summary>
        public static readonly Dispatcher Instance = new NullDispatcher();

        /// <summary>
        /// Initializes a new instance of the <see cref="NullDispatcher"/> class.
        /// </summary>
        private NullDispatcher()
        {
        }

        /// <summary>
        /// Returns a value that determines whether using the dispatcher to invoke a work
        /// item is required from the current context.
        /// </summary>
        /// <returns> true if invoking is required, otherwise false.</returns>
        public override bool CheckAccess() => true;

        /// <summary>
        /// Invokes the given System.Action in the context of the associated
        /// Microsoft.AspNetCore.Components.RenderTree.Renderer.
        /// </summary>
        /// <param name="workItem">The action to execute.</param>
        /// <returns>
        /// A System.Threading.Tasks.Task that will be completed when the action has finished
        /// executing.
        /// </returns>
        public override Task InvokeAsync(Action workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            workItem();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes the given System.Func'1 in the context of the associated
        /// Microsoft.AspNetCore.Components.RenderTree.Renderer.
        /// </summary>
        /// <param name="workItem">The action to execute.</param>
        /// <returns>
        /// A System.Threading.Tasks.Task that will be completed when the action has finished
        /// executing.
        /// </returns>
        public override Task InvokeAsync(Func<Task> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return workItem();
        }

        /// <summary>
        /// Invokes the given System.Func'1 in the context of the associated
        /// Microsoft.AspNetCore.Components.RenderTree.Renderer.
        /// </summary>
        /// <param name="workItem">The action to execute.</param>
        /// <returns>
        /// A System.Threading.Tasks.Task that will be completed when the action has finished
        /// executing.
        /// </returns>
        /// <typeparam name="TResult">The return type.</typeparam>
        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return Task.FromResult(workItem());
        }

        /// <summary>
        /// Invokes the given System.Func'1 in the context of the associated
        /// Microsoft.AspNetCore.Components.RenderTree.Renderer.
        /// </summary>
        /// <param name="workItem">The action to execute.</param>
        /// <returns>
        /// A System.Threading.Tasks.Task that will be completed when the action has finished
        /// executing.
        /// </returns>
        /// <typeparam name="TResult">The return type.</typeparam>
        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return workItem();
        }
    }
}