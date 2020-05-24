#pragma once

#ifdef _WIN32
#include <Windows.h>
#include <wrl/event.h>
#include <map>
#include <list>
#include <string>
typedef const wchar_t* AutoString;  
#include <wrl.h>
#include <wil/com.h>
// include WebView2 header
#include <WebView2.h>
#endif

typedef void (*WebMessageReceivedCallback)(AutoString message);
typedef void* (*WebResourceRequestedCallback)(AutoString url, int* outNumBytes, AutoString* outContentType);
typedef void (*ErrorOccuredCallback)(int32_t errorCode, AutoString message);

class BlazorWebView
{
private:
    WebMessageReceivedCallback webMessageReceivedCallback;
    ErrorOccuredCallback errorOccuredCallback;
#ifdef _WIN32
    static HINSTANCE hInstance;
    std::wstring userDataFolder;
    HWND window = 0;
    wil::com_ptr<ICoreWebView2Environment> webviewEnvironment;
    wil::com_ptr<ICoreWebView2Controller> webviewController;
    wil::com_ptr<ICoreWebView2> webviewWindow;
    std::map<std::wstring, WebResourceRequestedCallback> schemeToRequestHandler;
    std::list<EventRegistrationToken> webResourceRequestedTokens;
    EventRegistrationToken webMessageReceivedToken;
#endif

public:
#ifdef _WIN32
    BlazorWebView(HWND parent, AutoString userDataFolder, WebMessageReceivedCallback webMessageReceivedCallback, ErrorOccuredCallback errorOccuredCallback);
    HWND GetHWND(); 
    static void Register(HINSTANCE hInstance);
    void RefitContent();
#endif

    bool Initialize();
    ~BlazorWebView();
    void AddCustomScheme(AutoString scheme, WebResourceRequestedCallback requestHandler);
    void NavigateToUrl(AutoString url);
    void SendWebMessage(AutoString message);
};
