using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BlazorWebView.Wpf
{
    internal class BlazorWebViewNative
    {
        const string DllName = "BlazorWebViewNative";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern int BlazorTest();

        public int Test()
        {
            return BlazorTest();
        }
    }
}
