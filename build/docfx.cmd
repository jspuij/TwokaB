git config --global user.email "%GH_EMAIL%"
git config --global user.name "%GH_USER%"
cd docs
dir
C:\ProgramData\chocolatey\lib\docfx\tools\docfx.exe docfx.json
cd ..\..\Cortex.Net.Docs\
git checkout master
cd ..\Cortex.Net\docs\
xcopy .\_site\*.* ..\..\Cortex.Net.Docs\ /Y /E
cd ..\..\Cortex.Net.Docs\
git add .
git commit -m "Automated Documentation."
