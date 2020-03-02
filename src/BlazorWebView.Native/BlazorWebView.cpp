#include "BlazorWebView.h"

LPCWSTR CLASS_NAME = L"BlazorWebWindow";
LPCWSTR WINDOW_TITLE = L"BlazorWebWindow";
HINSTANCE BlazorWebView::hInstance;
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

void BlazorWebView::Register(HINSTANCE hInstance)
{
	BlazorWebView::hInstance = hInstance;

	// Register the window class	
	WNDCLASSW wc = { };
	wc.lpfnWndProc = WindowProc;
	wc.hInstance = hInstance;
	wc.lpszClassName = CLASS_NAME;
	RegisterClass(&wc);

	SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE);
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

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

BlazorWebView::~BlazorWebView()
{
	DestroyWindow(window);
}