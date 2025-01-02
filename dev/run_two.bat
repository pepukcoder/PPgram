@echo off

REM Get the current directory
set "CURRENT_DIR=%cd%"

REM Change to ../PPgram.Desktop
for %%i in ("%CURRENT_DIR%") do set "PPGRAM_DIR=%%~dpiPPgram.Desktop"
cd /d "%PPGRAM_DIR%" || (echo Failed to change directory to %PPGRAM_DIR% & exit /b 1)

REM Run dotnet in parallel
start "" dotnet run

REM Wait for 5 seconds
timeout /t 5 /nobreak > nul

REM Delete files under %LocalAppData%\PPgram\*.sesf
set "SESF_FILES=%LocalAppData%\PPgram\session.sesf"
if exist "%SESF_FILES%" (
    del "%SESF_FILES%"
) else (
    echo No .sesf files to delete.
)

REM Launch another instance of dotnet run
start "" dotnet run
