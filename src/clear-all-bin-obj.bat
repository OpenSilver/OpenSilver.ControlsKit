@echo off
setlocal enabledelayedexpansion

set BIN_FOUND=0
set OBJ_FOUND=0

echo Searching and deleting bin directories...
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S bin 2^>nul') DO (
    set BIN_FOUND=1
    echo Deleting: %%G
    RMDIR /S /Q "%%G"
)

echo Searching and deleting obj directories...
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj 2^>nul') DO (
    set OBJ_FOUND=1
    echo Deleting: %%G
    RMDIR /S /Q "%%G"
)

if !BIN_FOUND! EQU 0 (
    echo No bin directories found.
)

if !OBJ_FOUND! EQU 0 (
    echo No obj directories found.
)

endlocal

pause
