using System;

namespace BlazorWebView
{
    public interface IBlazorWebView
    {
        event EventHandler<string> OnWebMessageReceived;

        void Initialize(Action<WebViewOptions> configure);
        void Invoke(Action callback);
        void SendMessage(string message);
        void NavigateToUrl(string url);
        void ShowMessage(string title, string message);
    }
}
