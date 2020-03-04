using System;
using System.Text;
using Foundation;
using WebKit;

namespace BlazorWebView.iOS
{
    public class UrlSchemeHandler : NSObject, IWKUrlSchemeHandler
    {
        private ResolveWebResourceDelegate requestHandler;

        public UrlSchemeHandler(ResolveWebResourceDelegate requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
            var url = urlSchemeTask.Request.Url;

            var stream = this.requestHandler(url.AbsoluteString, out string contentType, out Encoding encoding);

            NSDictionary headers = new NSMutableDictionary();
            headers.SetValueForKey((NSString)contentType, (NSString)"Content-Type");
            headers.SetValueForKey((NSString)"no-cache", (NSString)"Cache-Control");
            var response = new NSHttpUrlResponse(url, stream != null ? 200 : 404, "HTTP/1.1", headers);
            urlSchemeTask.DidReceiveResponse(response);
            if (stream != null)
            {
                urlSchemeTask.DidReceiveData(NSData.FromStream(stream));
            }
            urlSchemeTask.DidFinish();
        }

        public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
        }
    }
}
