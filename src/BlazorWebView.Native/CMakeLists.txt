cmake_minimum_required(VERSION 3.10)

project(BlazorWebViewNative VERSION 1.0)

set (SOURCES BlazorWebView.cpp BlazorWebView.h Exports.cpp)

if (WIN32)
   set (SOURCES ${SOURCES} ${PROJECT_BINARY_DIR}/packages.config)
   add_definitions(-DUNICODE -D_UNICODE)
endif (WIN32)

SET (INCLUDES "${PROJECT_BINARY_DIR}")

# add the library
add_library(BlazorWebViewNative SHARED ${SOURCES})

## add config file
configure_file(BlazorWebViewConfig.h.in BlazorWebViewConfig.h)

if (WIN32)
    ## add nuget packages
    configure_file(${PROJECT_SOURCE_DIR}/packages.config.in ${PROJECT_BINARY_DIR}/packages.config COPYONLY)
    set_property(TARGET BlazorWebViewNative PROPERTY
        VS_PROJECT_IMPORT
          ../../packages/Microsoft.Windows.ImplementationLibrary.1.0.191107.2/build/native/Microsoft.Windows.ImplementationLibrary.targets
          ../../packages/Microsoft.Web.WebView2.0.9.430/build/native/Microsoft.Web.WebView2.targets
      )

endif (WIN32)

target_include_directories(BlazorWebViewNative PUBLIC ${INCLUDES})

# specify the C++ standard
set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED True)
