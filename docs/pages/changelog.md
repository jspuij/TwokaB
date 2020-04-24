# Changelog

The following versions have been released:

## 0.2.0-preview6

* Upgraded to the latest Blazor 3.2.0-preview5
* Updated to the latest Microsoft.Web.WebView2.0.9.488
* Upgraded to the latest npms

## 0.2.0-preview5

* Fixed a bug where DPI awareness was incorrectly forced on windows 7.

## 0.2.0-preview4

* Fixed a bug where elemenentreferences were not working.

## 0.2.0-preview3

* Updated build properties for the iOS and Android packages to set some project defaults
  on these platforms to prevent compilation or runtime errors.

## 0.2.0-preview1

* Added support for Static Web Assets on all platforms.
* Updated samples to use the nuget packages.

## 0.1.0-preview10

* Fixed a JSInterop bug where the assembly argument for a dotnet invoke can be null.

## 0.1.0-preview9

* Update to dotnet 3.1.3 and Blazor 3.2.0-preview3

## 0.1.0-preview8

* Added the Dispatcher and SynchronizationContext to the DI container for service use.

## 0.1.0-preview7

* Proper implementation of a Dispatcher for the Renderer and Javascript callbacks. Fixes several multithreading bugs.

## 0.1.0-preview6

* Implemented fallback to the old Edge in case Edgium is not installed.

## 0.1.0-preview5

* Small fix to the render queue.

## 0.1.0-preview4

* Fix for async render bug. This waits for the render to complete inside the webview.

## 0.1.0-preview3

* Fix for nested events in chrome.

## 0.1.0-preview2

* First release from github actions (no code changes).

## 0.1.0-preview1

* First release.
