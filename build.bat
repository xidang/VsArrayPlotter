@echo off
chcp 65001 >nul
setlocal

echo ==========================================
echo  VsArrayPlotter 构建脚本
echo ==========================================

set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

if exist "%MSBUILD%" (
    echo [使用 MSBuild] %MSBUILD%
    "%MSBUILD%" src\VsArrayPlotter\VsArrayPlotter.csproj -t:Restore,Build -p:Configuration=Release -p:DeployExtension=false
) else (
    where dotnet >nul 2>nul
    if %errorlevel%==0 (
        echo [使用 dotnet build]
        dotnet build src\VsArrayPlotter\VsArrayPlotter.csproj -c Release
    ) else (
        echo [错误] 未找到 MSBuild 或 dotnet。
        echo 请先安装 VS 的 "使用 C# 的桌面开发" 工作负载，或安装 .NET SDK。
        exit /b 1
    )
)

echo.
echo 输出目录：src\VsArrayPlotter\bin\Release\ （VsArrayPlotter.vsix）
pause
