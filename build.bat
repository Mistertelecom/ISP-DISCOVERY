@echo off
echo Stopping running instances...
taskkill /F /IM "ISP Discovery.exe" 2>nul
timeout /t 2

echo Building application...
dotnet restore NetworkDiscovery.csproj
dotnet publish NetworkDiscovery.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

echo Build complete. Executable can be found in bin\Release\net6.0-windows\win-x64\publish\
pause
