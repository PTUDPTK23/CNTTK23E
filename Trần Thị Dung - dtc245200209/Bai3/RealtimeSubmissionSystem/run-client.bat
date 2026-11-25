@echo off
title EduSubmit - Realtime Client
echo Dang publish va khoi chay Submission.RealtimeClient...
dotnet publish src/Submission.RealtimeClient -o "%TEMP%\RealtimeClient"
dotnet "%TEMP%\RealtimeClient\Submission.RealtimeClient.dll" http://localhost:5000 SV001
pause
