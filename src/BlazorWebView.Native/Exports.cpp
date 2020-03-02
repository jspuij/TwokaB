#pragma once

#include "BlazorWebView.h"

#ifdef _WIN32
# define EXPORTED __declspec(dllexport)
#else
# define EXPORTED
#endif

extern "C"
{
    EXPORTED BlazorWebView* BlazorWebViewNative_Ctor(HWND parent)
    {
        return new BlazorWebView(parent);
    }

    EXPORTED HWND BlazorWebViewNative_GetHWND(BlazorWebView* blazorWebView)
    {
        return blazorWebView->GetHWND();
    }
}