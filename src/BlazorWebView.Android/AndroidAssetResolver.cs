// <copyright file="AndroidAssetResolver.cs" company="Steve Sanderson and Jan-Willem Spuij">
// Copyright 2020 Steve Sanderson and Jan-Willem Spuij
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace BlazorWebView.Android
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using global::Android.Content.Res;
    using Microsoft.AspNetCore.StaticFiles;

    /// <summary>
    /// Resolves an asset on the Android platform.
    /// </summary>
    public class AndroidAssetResolver
    {
        /// <summary>
        /// Gets a file extension content type provider.
        /// </summary>
        private static readonly FileExtensionContentTypeProvider FileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();

        /// <summary>
        /// The asset manager to use.
        /// </summary>
        private readonly AssetManager assetManager;

        /// <summary>
        /// The path to the index.html file on the "Host".
        /// </summary>
        private readonly string hostHtmlPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AndroidAssetResolver"/> class.
        /// </summary>
        /// <param name="assetManager">The asset manager to use.</param>
        /// <param name="hostHtmlPath">The path to the index.html file on the "Host".</param>
        public AndroidAssetResolver(AssetManager assetManager, string hostHtmlPath)
        {
            if (string.IsNullOrEmpty(hostHtmlPath))
            {
                throw new ArgumentException("message", nameof(hostHtmlPath));
            }

            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

            // set the location to resolve from to the user folder of the app, combined with the relative "host" html path.
            this.hostHtmlPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), hostHtmlPath);
            this.ExpandAssets(assetManager);
        }

        /// <summary>
        /// Resolve an asset specified by the given URL.
        /// </summary>
        /// <param name="url">The Url to resolve.</param>
        /// <param name="contentType">The content type of the resource.</param>
        /// <param name="encoding">The encoding of the resource.</param>
        /// <returns>A stream with the resource data.</returns>
        public Stream Resolve(string url, out string contentType, out Encoding encoding)
        {
            var contentRootAbsolute = Path.GetDirectoryName(Path.GetFullPath(this.hostHtmlPath));

            // TODO: Only intercept for the hostname 'app' and passthrough for others
            // TODO: Prevent directory traversal?
            var appFile = Path.Combine(contentRootAbsolute, new Uri(url).AbsolutePath.Substring(1));
            if (appFile == contentRootAbsolute)
            {
                appFile = this.hostHtmlPath;
            }

            contentType = GetContentType(appFile);

            try
            {
                if (appFile.StartsWith("/"))
                {
                    appFile = appFile.Substring(1);
                }

                var stream = File.OpenRead(appFile);

                // Read the BOM
                var bom = new byte[4];

                stream.Read(bom, 0, 4);

                // lets assume UTF8 is reasonably safe for web.
                encoding = Encoding.UTF8;

                int bomPos = 0;

                // Analyze the BOM
                if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
                {
                    encoding = Encoding.UTF7;
                    bomPos = 3;
                }
                else if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                {
                    encoding = Encoding.UTF8;
                    bomPos = 3;
                }
                else if (bom[0] == 0xff && bom[1] == 0xfe)
                {
                    encoding = Encoding.Unicode;
                    bomPos = 2;
                }
                else if (bom[0] == 0xfe && bom[1] == 0xff)
                {
                    encoding = Encoding.BigEndianUnicode;
                    bomPos = 2;
                }
                else if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
                {
                    encoding = Encoding.UTF32;
                    bomPos = 4;
                }

                stream.Position = bomPos;

                return stream;
            }
            catch
            {
                // TODO: Handle a resolve error.
                System.Diagnostics.Debug.WriteLine($"Cannot resolve {appFile}");
                encoding = Encoding.Default;
                return null;
            }
        }

        /// <summary>
        /// Gets the content type for the url.
        /// </summary>
        /// <param name="url">The url to use.</param>
        /// <returns>The content type.</returns>
        private static string GetContentType(string url)
        {
            if (FileExtensionContentTypeProvider.TryGetContentType(url, out string result))
            {
                return result;
            }

            return "application/octet-stream";
        }

        /// <summary>
        /// Expands the assets from the resource zip file if required.
        /// </summary>
        /// <param name="assetManager">The asset manager to use.</param>
        private void ExpandAssets(AssetManager assetManager)
        {
            var hostDirectory = Path.GetDirectoryName(this.hostHtmlPath);

            Directory.CreateDirectory(hostDirectory);
            using (var asset = assetManager.Open("wwwroot.zip"))
            {
                using (var zipFile = new ZipArchive(asset))
                {
                    foreach (var entry in zipFile.Entries)
                    {
                        var filename = Path.Combine(hostDirectory, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                        using (var outputStream = File.Create(filename))
                        using (var inputStream = entry.Open())
                        {
                            inputStream.CopyTo(outputStream);
                        }
                    }
                }
            }
        }
    }
}