@ECHO off

SETLOCAL

IF "%~1" == "--help" (
	GOTO :help
)

IF "%~1" == "-h" (
	GOTO :help
)

SET CFG=Release
SET BUILD_DIR=%~dp0

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

REM Define the PackageVersion and OpenSilverPkgVersion variables
IF "%~1" == "" (
	SET /P PackageVersion="%ESC%[92mOpenSilver.ControlsKit version:%ESC%[0m "
	SET /P OpenSilverPkgVersion="%ESC%[92mOpenSilver version:%ESC%[0m "
) ELSE (
	SET PackageVersion=%1
	IF "%~2" == "" (
		SET OpenSilverPkgVersion=%1
	) ELSE (
		SET OpenSilverPkgVersion=%2
	)
)

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0mFastControl %CFG% %ESC%[95mconfiguration%ESC%[0m
ECHO. 
msbuild "%BUILD_DIR%..\OpenSilver.ControlsKit.FastControls\OpenSilver.ControlsKit.FastControls.csproj" -p:Configuration=%CFG%;OpenSilverVersion=%OpenSilverPkgVersion% -verbosity:minimal -restore

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0mControlsKit %CFG% %ESC%[95mconfiguration%ESC%[0m
ECHO. 
msbuild "%BUILD_DIR%..\OpenSilver.ControlsKit.Controls\OpenSilver.ControlsKit.Controls.csproj" -p:Configuration=%CFG%;OpenSilverVersion=%OpenSilverPkgVersion% -verbosity:minimal -restore

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0mOpenSilver.ControlsKit %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget.exe pack %BUILD_DIR%\nuspec\ControlsKit.nuspec -OutputDirectory "%BUILD_DIR%\output\ControlsKit" -Properties "PackageVersion=%PackageVersion%;Configuration=%CFG%;OpenSilverVersion=%OpenSilverPkgVersion%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver.ControlsKit"

EXIT /b

:help
ECHO [1] OpenSilver.ControlsKit NuGet package Version
ECHO [2] OpenSilver Version

ENDLOCAL