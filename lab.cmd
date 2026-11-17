@echo off
rem Runs the lab helper (tools\Lab), building it into .lab-bin\ first (quick when nothing changed).
setlocal
set "ROOT=%~dp0"
dotnet build "%ROOT%tools\Lab\Lab.csproj" -c Release -o "%ROOT%.lab-bin" -nologo -tl:off -v:q >nul || (
  dotnet build "%ROOT%tools\Lab\Lab.csproj" -c Release -o "%ROOT%.lab-bin" -nologo -tl:off -v:q
  exit /b 1
)
dotnet "%ROOT%.lab-bin\Lab.dll" %*
