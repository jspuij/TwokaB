using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BlazorWebView.Android
{
    public class AndroidAssetResolver
    {
        private readonly AssetManager assetManager;
        private readonly string hostHtmlPath;

        public AndroidAssetResolver(AssetManager assetManager, string hostHtmlPath)
        {
            this.assetManager = assetManager;
            this.hostHtmlPath = hostHtmlPath;
        }

        public Stream Resolve(string url, out string contentType, out Encoding encoding)
         {
            var contentRootAbsolute = Path.GetDirectoryName(Path.GetFullPath(hostHtmlPath));

            // TODO: Only intercept for the hostname 'app' and passthrough for others
            // TODO: Prevent directory traversal?
            var appFile = Path.Combine(contentRootAbsolute, new Uri(url).AbsolutePath.Substring(1));
            if (appFile == contentRootAbsolute)
            {
                appFile = hostHtmlPath;
            }

            contentType = GetContentType(appFile);

            try
            {
                if (appFile.StartsWith("/"))
                {
                    appFile = appFile.Substring(1);
                }
                var stream = assetManager.Open(appFile);

                // Read the BOM
                var bom = new byte[4];

                stream.Read(bom, 0, 4);
                // cannot seek.
                stream.Close();
                stream = assetManager.Open(appFile);

                // lets assume UTF8 is reasonably safe for web.
                encoding = Encoding.UTF8;

                // Analyze the BOM
                if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) encoding = Encoding.UTF7;
                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) encoding = Encoding.UTF8;
                if (bom[0] == 0xff && bom[1] == 0xfe) encoding = Encoding.Unicode; //UTF-16LE
                if (bom[0] == 0xfe && bom[1] == 0xff) encoding = Encoding.BigEndianUnicode; //UTF-16BE
                if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) encoding = Encoding.UTF32;

                return stream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot resolve {appFile}");
                encoding = Encoding.Default;
                return null;
            }
        }

        private static string GetContentType(string url)
        {
            var ext = Path.GetExtension(url);
            switch (ext)
            {
                case ".html": return "text/html";
                case ".css": return "text/css";
                case ".js": return "text/javascript";
                case ".wasm": return "application/wasm";
            }
            return "application/octet-stream";
        }

    }
}