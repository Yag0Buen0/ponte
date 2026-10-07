@echo off
if not exist "%~dp0vendor\ws-scrcpy\dist\index.js" (
    echo ws-scrcpy nao instalado. Rode: powershell -ExecutionPolicy Bypass -File "%~dp0instalar-web.ps1"
    pause
    exit /b 1
)
echo Abrindo http://localhost:8000 ...
start "" http://localhost:8000
cd /d "%~dp0vendor\ws-scrcpy\dist"
node index.js
