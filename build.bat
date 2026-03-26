@echo off
setlocal EnableDelayedExpansion

echo =========================================
echo  ShotCutter - Build Script
echo =========================================
echo.

set SOLUTION=ShotCutter.sln
set SMART_PROJECT=src\ShotCutter.SmartAnalysis\ShotCutter.SmartAnalysis.csproj
set APP_PROJECT=src\ShotCutter.App\ShotCutter.App.csproj
set TEST_PROJECT=tests\ShotCutter.Core.Tests\ShotCutter.Core.Tests.csproj
set OUTPUT_DIR=publish
set CONFIG=Release
set RUNTIME=win-x64

:: Parse arguments
if /I "%1"=="debug"   set CONFIG=Debug
if /I "%1"=="publish" goto :publish_only
if /I "%1"=="clean"   goto :clean_only
if /I "%1"=="help"    goto :show_help

:: ---- 1. Build dependencies ----
echo [1/3] Building core libraries ^(%CONFIG%^)...
dotnet build %SMART_PROJECT% -c %CONFIG% -m:1 -nodeReuse:false -p:UseSharedCompilation=false
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] Core build failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
)
echo.

echo [2/3] Building app ^(%CONFIG%^)...
dotnet build %APP_PROJECT% -c %CONFIG% --no-restore -m:1 -nodeReuse:false -p:BuildProjectReferences=false -p:UseSharedCompilation=false
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] App build failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
)
echo.

echo [3/3] Running tests...
dotnet test %TEST_PROJECT% -c %CONFIG% --no-build
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] Tests failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
)
echo.

:: ---- Publish (only in Release mode) ----
if /I "%CONFIG%"=="Release" (
    goto :publish_only
)

echo [INFO] Debug build complete. Skipping publish.
echo.
goto :done

:publish_only
echo [publish] Publishing app ^(%RUNTIME%, self-contained^)...
dotnet publish %APP_PROJECT% ^
    -c Release ^
    -r %RUNTIME% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -m:1 ^
    -nodeReuse:false ^
    -p:BuildProjectReferences=false ^
    -p:UseSharedCompilation=false ^
    -o %OUTPUT_DIR%
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] Publish failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
)
echo.
echo [publish] Copying ffmpeg / ffprobe if available...
powershell -ExecutionPolicy Bypass -File bundle_ffmpeg_tools.ps1 -TargetDir %OUTPUT_DIR%
if !ERRORLEVEL! neq 0 (
    echo.
    echo [WARN] Failed to copy ffmpeg tools. App will rely on PATH.
)
echo.
goto :done

:clean_only
echo Cleaning solution...
dotnet clean %SOLUTION%
if exist %OUTPUT_DIR% (
    echo Removing %OUTPUT_DIR%\...
    rmdir /s /q %OUTPUT_DIR%
)
echo Clean complete.
goto :eof

:show_help
echo Usage:  build.bat [option]
echo.
echo Options:
echo   (none)   Restore + Build Release + Publish  ^(default^)
echo   debug    Restore + Build Debug only
echo   publish  Publish Release without rebuild
echo   clean    Clean all build artefacts
echo   help     Show this help
goto :eof

:done
echo =========================================
echo  Build finished successfully!
if /I "%CONFIG%"=="Release" (
    echo  Output: %OUTPUT_DIR%\ShotCutter.App.exe
)
echo =========================================
endlocal
