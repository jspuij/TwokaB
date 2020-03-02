#include "BlazorWebView.h"

#include <atomic>

LPCWSTR CLASS_NAME = L"BlazorWebWindow";
LPCWSTR WINDOW_TITLE = L"BlazorWebWindow";
HINSTANCE BlazorWebView::hInstance;
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

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

BlazorWebView::BlazorWebView(HWND parent)
{
	window = CreateWindowEx(
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

void BlazorWebView::Initialize()
{
    std::atomic_flag flag = ATOMIC_FLAG_INIT;
    flag.test_and_set();

    ShowWindow(window, SW_SHOW);

    // Step 3 - Create a single WebView within the parent window
// Locate the browser and set up the environment for WebView
    HRESULT result = CreateCoreWebView2EnvironmentWithDetails(nullptr, nullptr, nullptr,
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [&, this](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {

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

                        // Resize WebView to fit the bounds of the parent window
                        RECT bounds;
                        GetClientRect(window, &bounds);
                        webviewHost->put_Bounds(bounds);

                        // Schedule an async task to navigate to Bing
                        webviewWindow->Navigate(L"https://www.nu.nl/");

                        // Step 4 - Navigation events


                        // Step 5 - Scripting


                        // Step 6 - Communication between host and web content

                        flag.clear();

                        return S_OK;
                    }).Get());
                return S_OK;
            }).Get());
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

BlazorWebView::~BlazorWebView()
{
	DestroyWindow(window);
}