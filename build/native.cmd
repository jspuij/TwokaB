cd ..\src\BlazorWebView.Native\
rmdir build32 /q /s
mkdir build32 && cd build32 && cmake -G "Visual Studio 16 2019" -A Win32 ..
cd ..\
rmdir build64 /q /s
mkdir build64 && cd build64 && cmake -G "Visual Studio 16 2019" -A x64 ..
cd ..\..\..\
nuget restore .\src\BlazorWebView.Native\build32\BlazorWebViewNative.sln -PackagesDirectory .\packages\
cd .\src\BlazorWebView.Native\
cmake --build build32 --config Release
cmake --build build64 --config Release