using System;
using System.Windows;
using System.Windows.Controls;

namespace BlazorWebView.Wpf
{
    public class BlazorWebView : FrameworkElement,  IBlazorWebView
    {
        public event EventHandler<string> OnWebMessageReceived;

        public void Initialize(Action<WebViewOptions> configure)
        {
            throw new NotImplementedException();
        }

        public void Invoke(Action callback)
        {
            throw new NotImplementedException();
        }

        public void NavigateToUrl(string url)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(string message)
        {
            throw new NotImplementedException();
        }

        public void ShowMessage(string title, string message)
        {
            throw new NotImplementedException();
        }
    }
}
