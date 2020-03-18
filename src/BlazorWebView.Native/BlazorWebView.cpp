#include "BlazorWebView.h"
#include <Shlwapi.h>
#include <atomic>
#include <comdef.h>

LPCWSTR CLASS_NAME = L"BlazorWebWindow";
LPCWSTR WINDOW_TITLE = L"BlazorWebWindow";
HINSTANCE BlazorWebView::hInstance;
HWND rootWindow;
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
std::map<HWND, BlazorWebView*> hwndToBlazorWebView;

using namespace Microsoft::WRL;

void BlazorWebView::Register(HINSTANCE hInstance)
{
	BlazorWebView::hInstance = hInstance;

	// Register the window class	
	WNDCLASSW wc = { };
	wc.lpfnWndProc = WindowProc;
	wc.hInstance = hInstance;
	wc.lpszClassName = CLASS_NAME;
	RegisterClass(&wc);

    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
}

void BlazorWebView::RefitContent()
{
    if (webviewHost)
    {
        RECT bounds;
        GetClientRect(window, &bounds);
        webviewHost->put_Bounds(bounds);
    }
}

BlazorWebView::BlazorWebView(HWND parent, WebMessageReceivedCallback webMessageReceivedCallback, ErrorOccuredCallback errorOccuredCallback)
{
    rootWindow = parent;
    this->webMessageReceivedCallback = webMessageReceivedCallback;
    this->errorOccuredCallback = errorOccuredCallback;
	this->window = CreateWindowEx(
		0,                              // Optional window styles.
		CLASS_NAME,                     // Window class
		WINDOW_TITLE,					// Window text
		WS_VISIBLE | WS_CHILD, // Window style

		// Size and position
		CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,

		parent,
		NULL,
		hInstance,
		this);
}

HWND BlazorWebView::GetHWND()
{
	return window;
}

bool BlazorWebView::Initialize()
{
    std::atomic_flag flag = ATOMIC_FLAG_INIT;
    flag.test_and_set();

    // Step 3 - Create a single WebView within the parent window
// Locate the browser and set up the environment for WebView
    HRESULT envResult = CreateCoreWebView2EnvironmentWithDetails(nullptr, nullptr, nullptr,
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [&, this](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
                if (result != S_OK) { return result; }
                this->webviewEnvironment = env;

                // Create a CoreWebView2Host and get the associated CoreWebView2 whose parent is the main window hWnd
                env->CreateCoreWebView2Host(window, Callback<ICoreWebView2CreateCoreWebView2HostCompletedHandler>(
                    [&, this](HRESULT result, ICoreWebView2Host* host) -> HRESULT {
                        if (host != nullptr) {
                            this->webviewHost = host;
                            webviewHost->get_CoreWebView2(&webviewWindow);
                        }

                        // Add a few settings for the webview
                        // this is a redundant demo step as they are the default settings values
                        ICoreWebView2Settings* Settings;
                        webviewWindow->get_Settings(&Settings);
                        Settings->put_IsScriptEnabled(TRUE);
                        Settings->put_AreDefaultScriptDialogsEnabled(TRUE);
                        Settings->put_IsWebMessageEnabled(TRUE);
                        
                        // Register interop APIs
                        webviewWindow->AddScriptToExecuteOnDocumentCreated(L"window.external = { sendMessage: function(message) { window.chrome.webview.postMessage(message); }, receiveMessage: function(callback) { window.chrome.webview.addEventListener(\'message\', function(e) { callback(e.data); }); } };", nullptr);
                        webviewWindow->add_WebMessageReceived(Callback<ICoreWebView2WebMessageReceivedEventHandler>(
                            [this](ICoreWebView2* webview, ICoreWebView2WebMessageReceivedEventArgs* args) -> HRESULT {
                                wil::unique_cotaskmem_string message;
                                HRESULT result = args->TryGetWebMessageAsString(&message);
                                if (result != S_OK) { return result; }
                                webMessageReceivedCallback(message.get());
                                return S_OK;
                            }).Get(), &this->webMessageReceivedToken);


                        // Register request handlers.
                        EventRegistrationToken webResourceRequestedToken;
                        webviewWindow->AddWebResourceRequestedFilter(L"*", CORE_WEBVIEW2_WEB_RESOURCE_CONTEXT_ALL);
                        webviewWindow->add_WebResourceRequested(Callback<ICoreWebView2WebResourceRequestedEventHandler>(
                            [this](ICoreWebView2* sender, ICoreWebView2WebResourceRequestedEventArgs* args)
                            {
                                ICoreWebView2WebResourceRequest* req;
                                args->get_Request(&req);

                                wil::unique_cotaskmem_string uri;
                                req->get_Uri(&uri);
                                std::wstring uriString = uri.get();
                                size_t colonPos = uriString.find(L':', 0);
                                if (colonPos > 0)
                                {
                                    std::wstring scheme = uriString.substr(0, colonPos);
                                    WebResourceRequestedCallback handler = schemeToRequestHandler[scheme];
                                    if (handler != NULL)
                                    {
                                        int numBytes;
                                        AutoString contentType;
                                        wil::unique_cotaskmem dotNetResponse(handler(uriString.c_str(), &numBytes, &contentType));

                                        if (dotNetResponse != nullptr && contentType != nullptr)
                                        {
                                            std::wstring contentTypeWS = contentType;

                                            IStream* dataStream = SHCreateMemStream((BYTE*)dotNetResponse.get(), numBytes);
                                            wil::com_ptr<ICoreWebView2WebResourceResponse> response;
                                            this->webviewEnvironment->CreateWebResourceResponse(
                                                dataStream, 200, L"OK", (L"Content-Type: " + contentTypeWS).c_str(),
                                                &response);
                                            args->put_Response(response.get());
                                        }
                                    }
                                }

                                return S_OK;
                            }
                        ).Get(), &webResourceRequestedToken);

                        webResourceRequestedTokens.push_back(webResourceRequestedToken);
                        hwndToBlazorWebView[window] = this;

                        RefitContent();

                        flag.clear();

                        return S_OK;
                    }).Get());
                return S_OK;
            }).Get());

    if (envResult != S_OK)
    {
        _com_error err(envResult);
        LPCTSTR errMsg = err.ErrorMessage();
        
        this->errorOccuredCallback(envResult, errMsg);

        return false;
    }
    else
    {
        // Block until it's ready. This simplifies things for the caller, so they
        // don't need to regard this process as async.
        MSG msg = { };
        while (flag.test_and_set() && GetMessage(&msg, NULL, 0, 0))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }
    return true;
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
        case WM_DESTROY:
        {
            hwndToBlazorWebView.erase(hwnd);
            return 0;
        }
        case WM_SIZE:
        {
            BlazorWebView* blazorWebView = hwndToBlazorWebView[hwnd];
            if (blazorWebView)
            {
                blazorWebView->RefitContent();
            }
            return 0;
        }
        break;
    }
	return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

BlazorWebView::~BlazorWebView()
{
    if (this->webviewWindow != nullptr)
    {
        for (EventRegistrationToken token : this->webResourceRequestedTokens)
        {
            this->webviewWindow->remove_WebResourceRequested(token);
        }
        this->webviewWindow->remove_WebMessageReceived(this->webMessageReceivedToken);
        this->webviewWindow = nullptr;
    }
    if (this->window != nullptr)
    {
        DestroyWindow(this->window);
    }
}

void BlazorWebView::AddCustomScheme(AutoString scheme, WebResourceRequestedCallback requestHandler)
{
    schemeToRequestHandler[scheme] = requestHandler;
}

void BlazorWebView::NavigateToUrl(AutoString url)
{
    webviewWindow->Navigate(url);
}

void BlazorWebView::SendWebMessage(AutoString message)
{
    webviewWindow->PostWebMessageAsString(message);
}
