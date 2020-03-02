#pragma once

#ifdef _WIN32
#include <Windows.h>
#include <wrl/event.h>
#include <map>
#include <string>
typedef const wchar_t* AutoString;
#include <wrl.h>
#include <wil/com.h>
// include WebView2 header
#include <WebView2.h>
#endif


class BlazorWebView
{
private:
#ifdef _WIN32
    static HINSTANCE hInstance;
    HWND window;
    wil::com_ptr<ICoreWebView2Host> webviewHost;
    wil::com_ptr<ICoreWebView2> webviewWindow;
#endif

public:
#ifdef _WIN32
    BlazorWebView(HWND parent);
    ~BlazorWebView();
    HWND GetHWND(); 
    void Initialize();
    static void Register(HINSTANCE hInstance);
#endif

};
