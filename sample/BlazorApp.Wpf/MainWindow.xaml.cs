// <copyright file="MainWindow.xaml.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorApp.Wpf
{
    using System;
    using System.Windows;
    using BlazorWebView;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// disposable usable to stop blazor..
        /// </summary>
        private IDisposable run;

        /// <summary>
        /// Bool signaling whether the application is initialized.
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Raises the ContentRendered event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (!this.initialized)
            {
                this.initialized = true;
                this.run = ComponentsDesktop.Run<Startup>(this.BlazorWebView, "wwwroot/index.html");
            }
        }
    }
}
