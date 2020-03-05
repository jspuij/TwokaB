# BlazorWebView

Blazor traditionally runs on dotnet core for Server Side Blazor and runs the Mono runtime on webassembly inside the
browser for client side blazor. For desktop and mobile applications this is cumbersome as it requires a bundled web-
server and retains the disadvantages of SSB or CSB respectively.

Steve Sanderson from Microsoft first escaped the server / clientside jail and released a
[dotnet core sample](https://github.com/SteveSandersonMS/WebWindow) that runs on dotnet core and leverages native
webviews on Windows / Linux / OsX to show a window on these respective operating systems.
Steve made a
[blogpost](https://blog.stevensanderson.com/2019/11/18/2019-11-18-webwindow-a-cross-platform-webview-for-dotnet-core/)
describing his efforts, an excellent introduction to what this is.

Building on these foundations I have created a BlazorWebView "Control" that is easily embedded in (Native) UI frameworks
on the following platforms:

* Xamarin Android (running on the Mono runtime)
* Xamarin iOS (running on the Mono runtime)
* Xamarin Mac (running on the Mono runtime)
* WPF (running on the dotnet core runtime)

I'm considering adding these platforms in the future:

* GTK Linux (running on the Mono runtime or dotnet core)
* GTK Mac (running on dotnet core)
* Winforms (running on the dotnet core runtime)
* Apple TV (running on the Mono runtime)
* Apple Watch  (running on the Mono runtime)

The advantage of using Xamarin on mobile platforms is that you can use the
[Xamarin Essentials](https://docs.microsoft.com/en-us/xamarin/essentials/) library to interact with mobile platform
API's easily from .NET.

## Get Started


