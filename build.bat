@echo off
setlocal EnableDelayedExpansion

echo =========================================
echo  ShotCutter - Build Script
echo =========================================
echo.

set SOLUTION=ShotCutter.sln
set APP_PROJECT=src\ShotCutter.App\ShotCutter.App.csproj
set OUTPUT_DIR=publish
set CONFIG=Release
set RUNTIME=win-x64

:: Parse arguments
if /I "%1"=="debug"   set CONFIG=Debug
if /I "%1"=="publish" goto :publish_only
if /I "%1"=="clean"   goto :clean_only
if /I "%1"=="help"    goto :show_help

:: ---- 1. Restore ----
echo [1/3] Restoring NuGet packages...
dotnet restore %SOLUTION%
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] Restore failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
)
echo.

:: ---- 2. Build ----
echo [2/3] Building solution ^(%CONFIG%^)...
dotnet build %SOLUTION% -c %CONFIG% --no-restore
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] Build failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
)
echo.

:: ---- 3. Publish (only in Release mode) ----
if /I "%CONFIG%"=="Release" (
    goto :publish_only
)

echo [INFO] Debug build complete. Skipping publish.
echo.
goto :done

:publish_only
echo [3/3] Publishing app ^(%RUNTIME%, self-contained^)...
dotnet publish %APP_PROJECT% ^
    -c Release ^
    -r %RUNTIME% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o %OUTPUT_DIR%
if !ERRORLEVEL! neq 0 (
    echo.
    echo [ERROR] Publish failed ^(exit code !ERRORLEVEL!^)
    exit /b !ERRORLEVEL!
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
    echo  Output: %OUTPUT_DIR%\ShotCutter.exe
)
echo =========================================
endlocal
