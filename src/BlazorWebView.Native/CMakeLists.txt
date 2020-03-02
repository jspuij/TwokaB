cmake_minimum_required(VERSION 3.10)

project(BlazorWebViewNative VERSION 1.0)

set (SOURCES BlazorWebView.cpp BlazorWebView.h Exports.cpp)
# add the library
add_library(BlazorWebViewNative SHARED ${SOURCES})

## add config file
configure_file(BlazorWebViewConfig.h.in BlazorWebViewConfig.h)
target_include_directories(BlazorWebViewNative PUBLIC "${PROJECT_BINARY_DIR}")

# specify the C++ standard
set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED True)
