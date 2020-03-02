using System;
using System.Windows;
using System.Windows.Controls;

namespace BlazorWebView.Wpf
{
    public sealed class BlazorWebView : FrameworkElement,  IBlazorWebView, IDisposable
    {
        BlazorWebViewNative innerView = new BlazorWebViewNative();

        public event EventHandler<string> OnWebMessageReceived;

        public void Dispose()
        {
        }

        public void Initialize(Action<WebViewOptions> configure)
        {
            int i = innerView.Test();
        }

        public void Invoke(Action callback)
        {
        }

        public void NavigateToUrl(string url)
        {
        }

        public void SendMessage(string message)
        {
        }

        public void ShowMessage(string title, string message)
        {
        }

    }
}
