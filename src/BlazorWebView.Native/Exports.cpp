#pragma once

#include "BlazorWebView.h"

#ifdef _WIN32
# define EXPORTED __declspec(dllexport)
#else
# define EXPORTED
#endif

extern "C"
{
    EXPORTED int BlazorTest()
    {
        return 42;
    }
}