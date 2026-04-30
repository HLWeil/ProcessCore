@echo off
pushd "%~dp0.."
dotnet run --project build/build.fsproj -- %*
set EXITCODE=%ERRORLEVEL%
popd
exit /b %EXITCODE%
