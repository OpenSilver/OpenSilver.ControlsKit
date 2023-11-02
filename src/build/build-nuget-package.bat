@echo off

IF NOT EXIST "nuspec/ControlsKit.nuspec" (
echo Wrong working directory. Please navigate to the folder that contains the BAT file before executing it.
PAUSE
EXIT
)

rem Define the escape character for colored text
for /F %%a in ('"prompt $E$S & echo on & for %%b in (1) do rem"') do set "ESC=%%a"

echo. 
echo %ESC%[95mRestoring NuGet packages%ESC%[0m
echo. 
dotnet restore "%~dp0..\FastControls\OpenSilver.ControlsKit.FastControls.csproj"
dotnet restore "%~dp0..\OpenSilver.ControlsKit.Controls\OpenSilver.ControlsKit.Controls.csproj"

rem If argument 1 is not given, ask for PackageVersion:
set PackageVersion=%1
if /i "%PackageVersion%" EQU "" (
  set /p PackageVersion="%ESC% Package version:%ESC% "
)


rem If argument 2 is not given, use default value for OpenSilverVersion:
set "OpenSilverVersion=%~2"
if not defined OpenSilverVersion set "OpenSilverVersion=2.0.1"

rem Get the current date and time:
for /F "tokens=2" %%i in ('date /t') do set currentdate=%%i
set currenttime=%time%

rem Create a Version.txt file with the date:
md temp
@echo OpenSilver.ControlsKit %PackageVersion% (%currentdate% %currenttime%)> temp/Version.txt

echo. 
echo %ESC%[95mBuilding %ESC%[0m FastControl Release %ESC%[95mconfiguration%ESC%[0m
echo. 
msbuild "%~dp0..\FastControls\OpenSilver.ControlsKit.FastControls.csproj" -p:Configuration=Release -p:DebugSymbols=true -p:Optimize=true -p:GenerateDocumentation=true -clp:ErrorsOnly -restore

echo. 
echo %ESC%[95mBuilding %ESC%[0m Controls Release %ESC%[95mconfiguration%ESC%[0m
echo. 
msbuild "%~dp0..\OpenSilver.ControlsKit.Controls\OpenSilver.ControlsKit.Controls.csproj" -p:Configuration=Release -p:DebugSymbols=true -p:Optimize=true -p:GenerateDocumentation=true -clp:ErrorsOnly -restore

echo. 
echo %ESC%[95mPacking %ESC%[0mOpenSilver.ControlsKit.Controls %ESC%[95mNuGet package%ESC%[0m
echo. 
nuget.exe pack nuspec\ControlsKit.nuspec -OutputDirectory "output/ControlsKit" -Properties "PackageVersion=%PackageVersion%;Configuration=Release;OpenSilverVersion=%OpenSilverVersion%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver.ControlsKit"

explorer "output\ControlsKit"

pause
