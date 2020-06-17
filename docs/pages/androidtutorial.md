# BlazorWebView Android Tutorial.

We will base our Android App on the [preparations](prepare.md) we have done before.
The preparations involved bringing over our Blazor App into a Razor Class Library (RCL).
This way we can share all of the application code between all the native apps we are
creating in these tutorials. The code for the start can be found in
[this branch](https://github.com/jspuij/BlazorWebViewTutorial/tree/1_prepare).

## Add a Xamarin Android Project.

Let's start by adding a Xamarin Android Project to the solution. Perform the following
steps to add a new Android Project to the solution. Right click on the solution node in
the Solution Explorer and select `Add` -> `New project...`. This will open the following
dialogs:

# [Add Android Project 1](#tab/addandroid-1)

![Add Android Project, Page 1](../images/addandroidproject1.png)

# [Add Android Project 2](#tab/addandroid-2)

![Add Android Project, Page 2](../images/addandroidproject2.png)

# [Add Android Project 3](#tab/addandroid-3)

![Add Android Project, Page 3](../images/addandroidproject3.png)

***

> However tempting, don't have your project name contain ".Android" somewhere. It will
conflict with the global `Android` namespace from Xamarin itself. I used "AndroidApp" as
project suffix, which is safe.

Make sure you choose the (Native) Android App Template and not the Xamarin Forms Android
Template for this tutorial. Finally, select the "Blank App" Template in the final dialog.
A new project will be added to the solution. Set the Android Project as the startup project
and Press F5 to start the Android Emulator and make sure everything works before we start
adding the Blazor bits to the Android Project. You can keep the emulator running, it
is a lot faster to debug this way if neccessary.

> It is possible to use native view in a Xamarin Forms app. It is however outside of
the scope of this tutorial. More information can be found in the 
[xamarin documentation](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/connect/mobile-embedding-native-views-in-your-xamarin-forms-apps)

## Add References.

We start by adding a reference to the Shared project from the Android Project. Click
right on the references node of the Android Project and select `Add reference...`.
Select the shared RCL Project from the projects list and click "OK".

Now that we have references the Shared RCL Project, it's time to install the NuGet
package for the BlazorWebView for Android. Enter the following lines into the Package
Console:

```
PM> Install-Package BlazorWebView.Android
```

This should install the package. We need an `HttpClient` for this platform, 
so we install `System.Net.Http` from NuGet:

```
PM> Install-Package System.Net.Http
```

Optionally you can update the Xamarin.Essentials NuGet package in the project, because
the default template comes with a very old version. Rebuild the project, 
there should be no build errors.

> There is a build warning however, warning MSB3277: Found conflicts between different
versions of "System.Numerics.Vectors" that could not be resolved. This is because of
the incompatibility of `System.Text.Json` on Xamarin platforms. Two bugs are tracked
[here](https://github.com/xamarin/Essentials/issues/904) and
[here](https://github.com/dotnet/runtime/issues/31326). The use of the Serializer luckily
is limited enough that Blazor works on iOS. You are encouraged to use Newtonsoft JSON for
now if you want to do serialization for HTTP calls and you experience issues.

Lets continue to the next step.

## Copy `wwwroot` Files.

We need a `wwwroot` folder and an `index.html` for this project as well. Let's copy the wwwroot
folder from the WebAssembly client project to the Android project. Now that we have added
the wwwroot folder with the index.html file (and favicon to prevent a 404), we have to change
the properties of these files:

* Change <strong>BuildAction</strong> from `Content` to `None`
* Change <strong>Copy To Output directory</strong> from `Do not Copy` to `Copy if newer`

The project should look like this:

![Project and Build Properties](../images/androidbuildproperties.png)

> The wwwroot folder from the Android Project will be combined with the Static Assets of
the Razor Class Libraries into a zipfile called wwwroot.zip. This zipfile is added to the
Android Assets during the build process. It has to be zipped because Android Assets are
very limited in their filename and folder structure. At first start of the application,
the zipfile is extracted to the personal data folder of the app.

## Change `index.html`.

We need to change the name and location where the framework script is loaded from.
BlazorWebView will intercept URLs loaded from the `framework://` scheme and present the
content directly to the native operating webview. We will load the Blazor JavaScript
file from the following location:

`framework://blazor.desktop.js`

Change the index.html file inside the wwwroot folder of the Android Project to read:

```html
<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>BlazorWebViewTutorial.Client</title>
    <base href="/" />
    <!-- Add _content/BlazorWebViewTutorial.Shared below -->
    <link href="_content/BlazorWebViewTutorial.Shared/css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="_content/BlazorWebViewTutorial.Shared/css/site.css" rel="stylesheet" />
</head>

<body>
    <app>Loading...</app>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>
    <!-- change the script location -->
    <script src="framework://blazor.desktop.js"></script>
</body>

</html>
```

## Prepare the Main Activity.

The Android App has a single activity that is called MainActivity. It consists of two
parts:

* A Resource XML file called "activity_main.xml" inside the `Resources/layout` subfolder
  of the project.
* The "MainActivity.cs" file in the root of the Android App project.

We need to update the first to add the "BlazorWebView" fragment to the layout, then we
update the second one to wire up the BlazorWebView to show our Blazor App.

### Add the BlazorWebView Fragment.

Open the "activity_main.xml" file by double clicking it. We need to add a Fragment to
it, where the Blazor content will be rendered. The file should read like this when done:

```xml
<RelativeLayout xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:app="http://schemas.android.com/apk/res-auto"
    xmlns:tools="http://schemas.android.com/tools"
    android:layout_width="match_parent"
    android:layout_height="match_parent">
<!-- The fragment begins here. -->
 <fragment
        android:name="BlazorWebView.Android.BlazorWebView"
        android:layout_width="match_parent"
        android:layout_height="match_parent"
        android:minWidth="25px"
        android:minHeight="25px"
        android:id="@+id/blazorWebView" />
<!-- The fragment ends here. -->
</RelativeLayout>
```

We have added a Fragment with the type `BlazorWebView.Android.BlazorWebView`, that matches
its parent in height and width, and we have given it the `blazorWebView` ID. We will need
that ID later to reference the Fragment in the code-behind file.
Close the Designer and build the project, it should still build.

### Wire up the BlazorWebView Fragment.

Open the MainActivity.cs file in the Text Editor. First we need to add two namespaces
<em>inside</em> the namespace of the file (to avoid naming conflicts) like so:

```csharp
namespace BlazorWebViewTutorial.AndroidApp
{
    // add usings here
    using BlazorWebView.Android;
    using BlazorWebView;
```

To be able to reference the BlazorWebView inside the `MainActivity` class and to be able
to dispose of the BlazorWebViewHost we add two private fields to the class:

```csharp
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private BlazorWebView blazorWebView;

        private IDisposable disposable;
```

Now, we are ready to assign the BlazorWebView Fragment to the field and initialize Blazor.
We do this by adding the following two statements to the `OnCreate` method of the class:

```csharp
            this.blazorWebView = (BlazorWebView)this.SupportFragmentManager.FindFragmentById(Resource.Id.blazorWebView);

            // run blazor.
            this.disposable = BlazorWebViewHost.Run<Startup>(this.blazorWebView, "wwwroot/index.html", new AndroidAssetResolver(this.Assets, "wwwroot/index.html").Resolve);
```

The first line will assign the `blazorWebView` field by looking up its instance through the SupportFragmentManager of
the android activity. Fragment instances that were created by inflating the XML Layout have to be requested from the
SupportFragmentManager by ID.
The second line will start Blazor. We'll take it apart step by step, to see what is going on:

* The result of the assignment is a Disposable instance that can be used to tear down and cleanup blazor. We should save it
  and call dispose when the activity is destroyed. 
* We start Blazor by calling the Run method on the `BlazorWebViewHost` static class. The run method takes a Generic type that
  specifies the Startup class that will initialize Blazor. We still use a Startup class, although Blazor WebAssembly has moved
  away from it. This might change in the future, but for now we keep the Startup class. We will define a Startup class in the
  next chapter.
* The first argument to the run method is the `IBlazorWebView` instance for the platform that we will use. In this case, it's
  the BlazorWebView instance that we got from the SupportFragmentManager. 
* The second argument is the relative path to the `index.html` resource inside the project. It usually is index.html and it has
  to be the `wwwroot` folder.
* The third argument is the resolve method of a specific asset resolver for the platform. For a lot of platforms, the `wwwroot`
  folder can be copied to a location somewhere inside the published output or inside the APK. For Android Assets however
  a special resolver is neccessary that extracts a zipped asset from the APK and inflates it into the personal folder of the
  device on first startup.

The Blazor App has its own Action Bar to navigate, so the default Android Action Bar should be hidden. It is useless as we have
a single Android Activity anyway. Add the following statement to the `OnCreate` method as well to hide the Action Bar:

```csharp
            this.SupportActionBar.Hide();
```

We have to make sure that we clean up nicely when the activity is destroyed, so we add the following method to the `MainActivity`
class:

```csharp
        protected override void OnDestroy()
        {
            if (this.disposable != null)
            {
                this.disposable.Dispose();
                this.disposable = null;
            }
            base.OnDestroy();
        }
```

Now we need to resolve some usings, but after we have done this the final version of the `MainActivity.cs` should look like this:

```csharp
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System;

namespace BlazorWebViewTutorial.AndroidApp
{
    // add usings here
    using BlazorWebView.Android;
    using BlazorWebView;

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private BlazorWebView blazorWebView;

        private IDisposable disposable;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            this.SupportActionBar.Hide();
            this.blazorWebView = (BlazorWebView)this.SupportFragmentManager.FindFragmentById(Resource.Id.blazorWebView);

            // run blazor.
            this.disposable = BlazorWebViewHost.Run<Startup>(this.blazorWebView, "wwwroot/index.html", new AndroidAssetResolver(this.Assets, "wwwroot/index.html").Resolve);
        }

        protected override void OnDestroy()
        {
            if (this.disposable != null)
            {
                this.disposable.Dispose();
                this.disposable = null;
            }
            base.OnDestroy();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
```

Well done, you've implemented the MainActivity, so we can move on to the final bit of this tutorial, which
is implementing the `Startup` class.

## Implement the Startup Class.

We have to wire up the Blazor Dependency Injection and define the root App class for Blazor to be able to run.
This Startup class closely resembles the AspnetCore default startup class for a web application.
We could define it in the Shared RCL Project, but as it most likely will contain DI registrations specific to
the platform, a better place is the Android Project. Let's add the following class to the Android Project:

```csharp
using System.Net.Http;

using BlazorWebView;
using BlazorWebViewTutorial.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWebViewTutorial.AndroidApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<HttpClient>();
        }

        /// <summary>
        /// Configure the app.
        /// </summary>
        /// <param name="app">The application builder for apps.</param>
        public void Configure(ApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
```

The startup class has two methods. The first method configures the services
for the DI container. We add an `HttpClient` from System.Net.Http as the
Android Platform does not come with a built-in one.

The second method is the configuration for the platform. The method accepts
an `ApplicationBuilder` that can be used to add the root component for the
app.

Press F5 to build and run the project. You should be greeted by a familiar
Blazor application:

![Android emulator](../images/androidemulator.png)

## Fix the Last Runtime Issue.

When you navigate to the `Fetch-Data` Page, you'll notice that the data is no
longer shown. The data is included inside the Android APK, but the `HttpClient`
that we have added to the DI container is outside of the Browser and won't be
intercepted by the BlazorWebView. Let's get the data from Github directly
to solve this issue. Change the `Oninitialized` method inside `FetchData.Razor`
component in the shared RCL project to read:

```csharp
    protected override async Task OnInitializedAsync()
    {
        forecasts = await Http.GetJsonAsync<WeatherForecast[]>("https://raw.githubusercontent.com/jspuij/BlazorWebViewTutorial/master/BlazorWebViewTutorial.Shared/wwwroot/sample-data/weather.json");
    }
```

The Android App should now be fully functioning. The source for the Android App
is in this branch:

https://github.com/jspuij/BlazorWebViewTutorial/tree/2_android
