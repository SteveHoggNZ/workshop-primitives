@echo off
rem Runs the lab helper (tools\Lab). Builds it into .lab-bin\ the first time.
rem Delete .lab-bin\ to force a rebuild after changing tools\Lab.
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%.lab-bin\Lab.dll" (
  dotnet build "%ROOT%tools\Lab\Lab.csproj" -c Release -o "%ROOT%.lab-bin" -nologo -tl:off -v:q || exit /b 1
)
dotnet "%ROOT%.lab-bin\Lab.dll" %*
