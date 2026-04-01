@echo off
setlocal enabledelayedexpansion

echo.
echo Welcome to DocuBot Agent Setup!
echo.

:: 1. Locating the EXE
set "EXE_NAME=docubot.exe"
if not exist "%~dp0%EXE_NAME%" (
    :: Fallback for initial build if named DocuBot.Agent.exe
    if exist "%~dp0DocuBot.Agent.exe" (
        set "EXE_NAME=DocuBot.Agent.exe"
    ) else (
        echo Error: docubot.exe not found in this folder.
        pause
        exit /b 1
    )
)

:: 2. Installation Path
set "INSTALL_DIR=%LOCALAPPDATA%\DocuBot"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo Installing to %INSTALL_DIR%...
copy /y "%~dp0%EXE_NAME%" "%INSTALL_DIR%\docubot.exe" >nul

:: 3. Updating PATH (User Level)
echo Configuring System PATH...
powershell -Command "[Environment]::GetEnvironmentVariable('Path', 'User') -split ';' | Where-Object { $_ -ne '' -and $_ -ne '%INSTALL_DIR%' } | Set-Variable CurrentPath; [Environment]::SetEnvironmentVariable('Path', [string]::Join(';', $CurrentPath + '%INSTALL_DIR%'), 'User')"

:: 4. Fixing API Key
powershell -Command "$key = [Environment]::GetEnvironmentVariable('GROQAI_API_KEY', 'User'); if (-not $key) { $newKey = Read-Host '🔑 Please enter your Groq API Key'; if ($newKey) { [Environment]::SetEnvironmentVariable('GROQAI_API_KEY', $newKey, 'User'); Write-Host '✅ API Key saved locally.' } } else { Write-Host '✅ API Key already configured.' }"

:: 5. Git Hook Configuration
if exist ".git\" (
    echo.
    echo Git repository detected. Setting up hooks...
    if not exist ".git\hooks" mkdir ".git\hooks"
    echo #!/bin/sh > ".git\hooks\prepare-commit-msg"
    echo docubot "$1" >> ".git\hooks\prepare-commit-msg"
    echo Git hook installed successfully.
) else (
    echo No Git repository found in this folder. Skipping hook setup.
)

echo.
echo DocuBot Agent setup is complete!
echo.
echo NOTE: You may need to RESTART your terminal for 'docubot' command to be recognized.
echo.
pause
