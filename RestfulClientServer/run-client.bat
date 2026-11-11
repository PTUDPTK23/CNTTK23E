@echo off
echo Dang chay Submission.Client...
dotnet run --project src\Submission.Client --no-build -- http://localhost:5000
pause

