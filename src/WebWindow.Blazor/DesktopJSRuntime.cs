// <copyright file="DesktopJSRuntime.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using System.Threading.Tasks;
    using Microsoft.JSInterop;
    using Microsoft.JSInterop.Infrastructure;

    /// <summary>
    /// Javascript .Net bridge for the BlazorWebView.
    /// </summary>
    internal class DesktopJSRuntime : JSRuntime
    {
        /// <summary>
        /// The type of VoidTaskResult.
        /// </summary>
        private static Type voidTaskResultType = typeof(Task).Assembly
            .GetType("System.Threading.Tasks.VoidTaskResult", true);

        /// <summary>
        /// The inter process communication channel.
        /// </summary>
        private readonly IPC ipc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopJSRuntime"/> class.
        /// </summary>
        /// <param name="ipc">The inter process communication channel to use.</param>
        public DesktopJSRuntime(IPC ipc)
        {
            this.ipc = ipc ?? throw new ArgumentNullException(nameof(ipc));
        }

        /// <summary>
        /// Begin an asynchronous operation to invoke a javascript function.
        /// </summary>
        /// <param name="asyncHandle">A handle uniquely identifying the asynchronous operation.</param>
        /// <param name="identifier">The method identifier.</param>
        /// <param name="argsJson">The arguments as JSON string.</param>
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            this.ipc.Send("JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }

        /// <summary>
        /// Ends an asynchronous operation invoking a .NET function from javascript.
        /// </summary>
        /// <param name="invocationInfo">The invocation info.</param>
        /// <param name="invocationResult">The invocation result.</param>
        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            // The other params aren't strictly required and are only used for logging
            var resultOrError = invocationResult.Success ? HandlePossibleVoidTaskResult(invocationResult.Result) : invocationResult.Exception.ToString();
            if (resultOrError != null)
            {
                this.ipc.Send("JS.EndInvokeDotNet", invocationInfo.CallId, invocationResult.Success, resultOrError);
            }
            else
            {
                this.ipc.Send("JS.EndInvokeDotNet", invocationInfo.CallId, invocationResult.Success);
            }
        }

        /// <summary>
        /// Handle a possible void taskresult.
        /// </summary>
        /// <param name="result">The result to handle.</param>
        /// <returns>Return null on void.</returns>
        private static object HandlePossibleVoidTaskResult(object result)
        {
            // Looks like the TaskGenericsUtil logic in Microsoft.JSInterop doesn't know how to
            // understand System.Threading.Tasks.VoidTaskResult
            return result?.GetType() == voidTaskResultType ? null : result;
        }
    }
}
