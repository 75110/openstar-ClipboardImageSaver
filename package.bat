@echo off
chcp 65001 >nul
echo ========================================
echo   Clipboard Image Saver - 自动打包
echo ========================================
echo.

:: 1. 清理旧文件
echo [1/5] 清理旧文件...
if exist ClipboardImageSaver.exe del /f /q ClipboardImageSaver.exe
if exist Release rmdir /s /q Release
if exist *.zip del /f /q *.zip
echo      完成！
echo.

:: 2. 编译程序
echo [2/5] 编译程序...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
    /target:winexe ^
    /out:ClipboardImageSaver.exe ^
    /win32icon:icon.ico ^
    /reference:System.dll ^
    /reference:System.Core.dll ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:Microsoft.CSharp.dll ^
    Program.cs

if %ERRORLEVEL% NEQ 0 (
    echo      编译失败！
    pause
    exit /b 1
)
echo      完成！
echo.

:: 3. 创建发布目录
echo [3/5] 创建发布目录...
mkdir Release
echo      完成！
echo.

:: 4. 复制文件
echo [4/5] 复制文件到发布目录...
copy ClipboardImageSaver.exe Release\ >nul
copy RELEASE-README.md Release\README.md >nul
echo      完成！
echo.

:: 5. 打包
echo [5/5] 打包压缩文件...
powershell -Command "Compress-Archive -Path 'Release\*' -DestinationPath 'ClipboardImageSaver-v1.0.zip' -Force"
echo      完成！
echo.

:: 显示结果
echo ========================================
echo   打包完成！
echo ========================================
echo.
echo 生成文件:
echo   - ClipboardImageSaver.exe
echo   - ClipboardImageSaver-v1.0.zip
echo.

:: 显示文件信息
for %%F in (ClipboardImageSaver.exe) do (
    echo EXE 大小: %%~zF 字节
)
for %%F in (ClipboardImageSaver-v1.0.zip) do (
    set /a size=%%~zF/1024
)
echo ZIP 大小: %size% KB
echo.

:: 询问是否打开目录
set /p open="是否打开文件夹? (Y/N): "
if /i "%open%"=="Y" explorer .

echo.
pause
