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

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace BlazorApp.iOS
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
    using System;
    using Foundation;
    using UIKit;
    using WebWindows.Blazor;

    /// <summary>
    /// A view controller for the main view.
    /// </summary>
    public partial class ViewController : UIViewController
    {
        /// <summary>
        /// Disposable to stop blazor.
        /// </summary>
        private IDisposable run;

        /// <summary>
        /// Called after the view is loaded into memory.
        /// </summary>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Perform any additional setup after loading the view, typically from a nib.
            this.run = ComponentsDesktop.Run<Startup>(this.BlazorWebView, "wwwroot/index.html");
        }

        /// <summary>
        /// Called when the system is load on memory.
        /// </summary>
        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            this.run.Dispose();
        }
    }
}