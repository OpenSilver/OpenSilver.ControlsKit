@echo off
dotnet restore %~dp0\..\OpenSilver.ControlsKit.FastControls.csproj
set PackageVersion=%1

if /i "%PackageVersion%" EQU "" (
  set /p PackageVersion="%ESC%Package version:%ESC% "
)

msbuild %~dp0\..\OpenSilver.ControlsKit.FastControls.csproj -t:pack -p:PackageVersion=%PackageVersion% -p:Configuration=Release -p:DebugSymbols=false -p:DebugType=None -p:Optimize=true