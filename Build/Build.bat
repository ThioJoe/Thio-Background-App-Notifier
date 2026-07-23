@echo off
setlocal enabledelayedexpansion

:: Set base variables
set "BASE_NAME=Thio-Background-App-Notifier"
set "EXE_NAME=%BASE_NAME%.exe"
:: Update this if your WiX command outputs a different default MSI name
set "MSI_BASE=%BASE_NAME%_Installer" 
set "MSI_NAME=%MSI_BASE%.msi"

:: Set signtool command and parameters. Put it in the next line after the = and before the last quote. Do NOT include file name/path which will be added automatically
set "SIGNTOOL_CMD=signtool.exe sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /WHATEVER-OTHER-PARAMS"

:: 1. Sign the EXE
echo Signing %EXE_NAME%...
%SIGNTOOL_CMD% "%EXE_NAME%"

:: Retrieve the full assembly version
echo Retrieving version from %EXE_NAME%...
for /f "usebackq tokens=*" %%v in (`powershell -NoProfile -Command "(Get-Item '%EXE_NAME%').VersionInfo.FileVersion"`) do set "FULL_VER=%%v"

if "%FULL_VER%"=="" (
    echo Error: Could not retrieve version from %EXE_NAME%.
    pause
    exit /b 1
)

echo Detected Full Version: %FULL_VER%

:: Get only the first 3 parts of the version (e.g., 1.1.1.0 -> 1.1.1)
for /f "tokens=1,2,3 delims=." %%a in ("%FULL_VER%") do set "SHORT_VER=%%a.%%b.%%c"

echo Short Version for renaming: %SHORT_VER%
echo.

:: 2. Build the MSI with WiX
echo Building MSI...

if not exist "Out" mkdir "Out"

:: ====================================================================================
:: UPDATE THE VARIABLES BELOW FOR THE WIX COMMAND.
:: You'll need to replace the paths with the actual locations on your system. See build notes file for more details on wix.
:: You can get the wix toolset from this repo's releases: https://github.com/wixtoolset/wix
:: You can install it, or just download the "artifacts.zip" and extract the nupkg files and it will run portably
:: For example the main toolset is "wix.6.0.2.nupkg" (extract with 7-zip or something) and then the files are at "tools\net6.0\any"
:: For the extensions, look for the ".wixext." nupkg files and extract those and find the dll for them.
::     For the "-ext" parameters in the build command, you need to use the full path to the extension dlls even if they are right next to wix.exe
:: ====================================================================================
set "WIX_EXE=C:\WHATEVER-PATH-TO-TOOLSET-V6\wix.exe"
set "WIX_UI_EXT=C:\WHATEVER-PATH-TO-EXTENSIONS\WixToolset.UI.wixext.dll"
set "WIX_UTIL_EXT=C:\WHATEVER-PATH-TO-EXTENSIONS\WixToolset.Util.wixext.dll"

"%WIX_EXE%" build ThioBackgroundAppNotifier_Installer.wxs -arch x64 -loc InstallerText.wxl -ext "%WIX_UI_EXT%" -ext "%WIX_UTIL_EXT%" -o "Out\%MSI_NAME%" -d ProductVersion="%FULL_VER%" -pdbtype none

:: Verify the MSI was actually created before continuing
if not exist "Out\%MSI_NAME%" (
    echo.
    echo Error: Out\%MSI_NAME% was not found. The WiX build may have failed.
    pause
    exit /b 1
)

:: 3. Rename the MSI
set "NEW_MSI_NAME=%MSI_BASE%_%SHORT_VER%.msi"
echo Renaming %MSI_NAME% to %NEW_MSI_NAME%...
ren "Out\%MSI_NAME%" "%NEW_MSI_NAME%"

:: 4. Sign the MSI
echo Signing %NEW_MSI_NAME%...
%SIGNTOOL_CMD% "Out\%NEW_MSI_NAME%"

:: 5. Rename the signed EXE and move it
set "NEW_EXE_NAME=%BASE_NAME%_%SHORT_VER%.exe"
echo Renaming %EXE_NAME% to %NEW_EXE_NAME%...
ren "%EXE_NAME%" "%NEW_EXE_NAME%"

echo Moving %NEW_EXE_NAME% to Out folder...
move "%NEW_EXE_NAME%" "Out\"

echo.
echo Build and sign workflow complete!
pause
