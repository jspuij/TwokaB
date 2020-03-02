#pragma once

#include "BlazorWebView.h"

#ifdef _WIN32
# define EXPORTED __declspec(dllexport)
#else
# define EXPORTED
#endif

extern "C"
{
#ifdef _WIN32
    EXPORTED void BlazorWebViewNative_Register(HINSTANCE hInstance)
    {
        BlazorWebView::Register(hInstance);
    }

    EXPORTED BlazorWebView* BlazorWebViewNative_Ctor(HWND parent)
    {
        return new BlazorWebView(parent);
    }

    EXPORTED HWND BlazorWebViewNative_GetHWND(BlazorWebView* blazorWebView)
    {
        return blazorWebView->GetHWND();
    }
#endif

    EXPORTED void BlazorWebViewNative_Dtor(BlazorWebView* blazorWebView)
    {
        blazorWebView->~BlazorWebView();
    }

    EXPORTED void BlazorWebViewNative_Initialize(BlazorWebView* blazorWebView)
    {
        blazorWebView->Initialize();
    }
}