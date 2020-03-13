git config --global user.email "%GH_EMAIL%"
git config --global user.name "%GH_USER%"
cd docs
dir
C:\ProgramData\chocolatey\lib\docfx\tools\docfx.exe docfx.json
cd ..\..\BlazorWebView.Docs\
git checkout master
cd ..\BlazorWebView\docs\
xcopy .\_site\*.* ..\..\BlazorWebView.Docs\ /Y /E
cd ..\..\BlazorWebView.Docs\
git add .
git commit -m "Automated Documentation."
