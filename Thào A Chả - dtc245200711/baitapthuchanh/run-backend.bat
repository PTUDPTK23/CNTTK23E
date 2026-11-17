@echo off
cd /d "%~dp0backend\IotMonitoring.Api"
dotnet run --urls http://localhost:5000
