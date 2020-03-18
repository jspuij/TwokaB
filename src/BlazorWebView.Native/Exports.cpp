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

    EXPORTED BlazorWebView* BlazorWebViewNative_Ctor(HWND parent, WebMessageReceivedCallback webMessageReceivedCallback, ErrorOccuredCallback errorOccuredCallback)
    {
        return new BlazorWebView(parent, webMessageReceivedCallback, errorOccuredCallback);
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

    EXPORTED bool BlazorWebViewNative_Initialize(BlazorWebView* blazorWebView)
    {
        return blazorWebView->Initialize();
    }

    EXPORTED void BlazorWebViewNative_AddCustomScheme(BlazorWebView* instance, AutoString scheme, WebResourceRequestedCallback requestHandler)
    {
        instance->AddCustomScheme(scheme, requestHandler);
    }

    EXPORTED void BlazorWebViewNative_NavigateToUrl(BlazorWebView* blazorWebView, AutoString url)
    {
        blazorWebView->NavigateToUrl(url);
    }

    EXPORTED void BlazorWebViewNative_SendMessage(BlazorWebView* blazorWebView, AutoString message)
    {
        blazorWebView->SendWebMessage(message);
    }
}