@echo off
taskkill /F /IM IMoRS.exe >nul 2>&1
taskkill /F /IM dotnet.exe >nul 2>&1
rd /s /q bin 2>nul
rd /s /q obj 2>nul
dotnet clean
dotnet build
echo Готово!
pause