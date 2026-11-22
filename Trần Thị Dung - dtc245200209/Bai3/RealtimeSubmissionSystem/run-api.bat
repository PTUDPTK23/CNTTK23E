@echo off
title EduSubmit - Realtime Api (Port 5000)
echo Dang publish va khoi chay Submission.RealtimeApi...
dotnet publish src/Submission.RealtimeApi -o "%TEMP%\RealtimeApi"
dotnet "%TEMP%\RealtimeApi\Submission.RealtimeApi.dll" --urls http://localhost:5000
pause
