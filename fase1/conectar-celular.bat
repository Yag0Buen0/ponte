@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0conectar-celular.ps1"
if errorlevel 1 pause
