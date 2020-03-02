#pragma once

#ifdef _WIN32
#include <Windows.h>
#include <wrl/event.h>
#include <map>
#include <string>
typedef const wchar_t* AutoString;
#else
#ifdef OS_LINUX
#include <gtk/gtk.h>
#endif
typedef char* AutoString;
#endif