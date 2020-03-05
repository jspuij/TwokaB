using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using WebWindows.Blazor;
using BlazorWebView.Android;
using System.IO;
using Java.Util.Zip;
using System.IO.Compression;

namespace BlazorApp.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SupportActionBar.Hide();
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var blazorWebView = (BlazorWebView.Android.BlazorWebView)this.SupportFragmentManager.FindFragmentById(Resource.Id.blazorWebView);

            ComponentsDesktop.Run<Startup>(blazorWebView, "wwwroot/index.html", new AndroidAssetResolver(this.Assets, "wwwroot/index.html").Resolve);

        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] global::Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}