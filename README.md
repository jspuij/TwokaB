<a href="https://www.buymeacoffee.com/jspuij" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" style="height: 17px !important;width: 72px !important;" ></a>

# BlazorWebView.

Blazor traditionally runs on .NET Core for Server-side Blazor and runs the Mono runtime on WebAssembly inside the
browser for Client-side Blazor. For desktop and mobile applications, this is cumbersome, as it requires a bundled web 
server and retains the disadvantages of SSB or CSB respectively.

Steve Sanderson from Microsoft first escaped the server- / client-side jail and released a
[.NET Core sample](https://github.com/SteveSandersonMS/WebWindow) that leverages native
WebViews on Windows / Linux / macOS to show a window on these respective operating systems.
Steve made a
[blog post](https://blog.stevensanderson.com/2019/11/18/2019-11-18-webwindow-a-cross-platform-webview-for-dotnet-core/)
describing his efforts; an excellent introduction to what this is.

Building on these foundations, I have created a BlazorWebView "Control" that is easily embedded in (Native) UI frameworks
on the following platforms:

* Xamarin Android (running on the Mono runtime)
* Xamarin iOS (running on the Mono runtime)
* Xamarin Mac (running on the Mono runtime)
* WPF (running on the .NET Core runtime)

I'm considering adding these platforms in the future:

* GTK Linux (running on the Mono runtime or .NET Core)
* GTK Mac (running on .NET Core)
* Windows Forms (running on the .NET Core runtime)
* Apple TV (running on the Mono runtime)
* Apple Watch  (running on the Mono runtime)

The advantage of using Xamarin on mobile platforms is that you can use the
[Xamarin Essentials](https://docs.microsoft.com/en-us/xamarin/essentials/) library to interact with mobile platform
APIs easily from .NET.

## Edgium Status

Because of the Coronavirus, the latest Edge SDK does not work with the Stable or Beta (81) builds of edge, as the code necessary
to support the SDK never made it into 81. 82 is cancelled. The first version that supports the SDK is Edge 83, but unfortunately
Microsoft created a bug that will hang the initialization of the browser and also makes fallback fail.
Bug is tracked here (https://github.com/MicrosoftEdge/WebViewFeedback/issues/151). So the only Edgium (new edge) build
that works, is the Canary channel version, which can be downloaded at:

https://www.microsoftedgeinsider.com/nl-nl/download/

## Get Started.

The instructions to get started vary depending on which platform you want to create the application for. It's best
to follow the tutorials for every platform. They are available below:

* [Xamarin Android](https://jspuij.github.io/BlazorWebView.Docs/pages/androidtutorial.html)
* [Xamarin iOS](https://jspuij.github.io/BlazorWebView.Docs/pages/iostutorial.html)
* [Xamarin Mac](https://jspuij.github.io/BlazorWebView.Docs/pages/mactutorial.html)
* [WPF](https://jspuij.github.io/BlazorWebView.Docs/pages/wpftutorial.html)

[Some guidance](prepare.md) on how to set up a Blazor project to best accommodate targeting multiple platforms is available as well.

### Install the NuGet Packages.

In general, you need to add one of the NuGet packages specific to your platform to your project:

```
PM> Install-Package BlazorWebView.Android

# OR
PM> Install-Package BlazorWebView.iOS

# OR
PM> Install-Package BlazorWebView.Mac

# OR
PM> Install-Package BlazorWebView.Wpf
```

### Add BlazorWebView to Your Activity/View, ViewController/View, or Window.

Next add the BlazorWebView (it's named like this in every package) to your Activity / View (for Android),
ViewController / View (for iOS and Mac), or your window XAML. Make sure that the BlazorWebView gets an
identifier so we can reference it in a code-behind file.

### Wire up Your Blazor Project to the BlazorWebView.

First, we need to adapt the URL to the Blazor JavaScript. It will be loaded from the NuGet assembly by
referencing a dedicated scheme. The URL to the Blazor JavaScript is:

```
framework://blazor.desktop.js
```

The rest of the URLs are relative URLs. A complete example index.html file provided below:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>MyDesktopApp</title>
    <base href="/" />
    <link href="css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="css/site.css" rel="stylesheet" />
</head>
<body>
    <app>Loading...</app>

    <script src="framework://blazor.desktop.js"></script>
</body>
</html>
```

Finally, we initialize the BlazorWebView from code-behind using the `BlazorWebViewHost` static class like this:

```csharp
BlazorWebViewHost.Run<Startup>(this.blazorWebView, "wwwroot/index.html");
```

That's it! That wasn't too difficult, was it?
