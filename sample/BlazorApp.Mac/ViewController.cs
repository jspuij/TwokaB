// <copyright file="ViewController.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorApp.Mac
{
    using System;
    using AppKit;
    using BlazorWebView;
    using Foundation;

    /// <summary>
    /// A view controller for the main view.
    /// </summary>
    public partial class ViewController : NSViewController
    {
        /// <summary>
        /// Disposable to stop blazor.
        /// </summary>
        private IDisposable run;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewController"/> class.
        /// </summary>
        /// <param name="handle">The handle to the native object.</param>
        public ViewController(IntPtr handle)
            : base(handle)
        {
        }

        /// <summary>
        /// Gets or sets the represented object.
        /// </summary>
        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }

            set
            {
                base.RepresentedObject = value;
            }
        }

        /// <summary>
        /// Called after the view is loaded into memory.
        /// </summary>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.run = BlazorWebViewHost.Run<Startup>(this.BlazorWebView, "wwwroot/index.html");
        }
    }
}
