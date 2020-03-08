// <copyright file="MainActivity.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorApp.Android
{
    using System.IO;
    using System.IO.Compression;
    using BlazorWebView;
    using BlazorWebView.Android;
    using global::Android.App;
    using global::Android.OS;
    using global::Android.Runtime;
    using global::Android.Support.V7.App;
    using global::Android.Widget;
    using Java.Util.Zip;

    /// <summary>
    /// Main activity for the app.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        /// <summary>
        /// Executes when permissions are requested.
        /// </summary>
        /// <param name="requestCode">The request code.</param>
        /// <param name="permissions">The requested permissions.</param>
        /// <param name="grantResults">The grant results.</param>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] global::Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <summary>
        /// Executes when the activity is created.
        /// </summary>
        /// <param name="savedInstanceState">Optional saved state in case the actvity is resumed.</param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            this.SupportActionBar.Hide();

            // Set our view from the "main" layout resource
            this.SetContentView(Resource.Layout.activity_main);

            var blazorWebView = (BlazorWebView)this.SupportFragmentManager.FindFragmentById(Resource.Id.blazorWebView);

            // run blazor.
            ComponentsDesktop.Run<Startup>(blazorWebView, "wwwroot/index.html", new AndroidAssetResolver(this.Assets, "wwwroot/index.html").Resolve);
        }
    }
}