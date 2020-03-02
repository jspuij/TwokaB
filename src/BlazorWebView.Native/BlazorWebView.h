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
#else
#ifdef OS_LINUX
#include <gtk/gtk.h>
#endif
typedef char* AutoString;
#endif


class BlazorWebView
{
private:
    HWND window;
public:
    BlazorWebView(HWND parent);
    HWND GetHWND();
};
