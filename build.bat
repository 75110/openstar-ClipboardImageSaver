@echo off
chcp 65001 >nul
echo Compiling Clipboard Image Saver...

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

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Output: ClipboardImageSaver.exe
    echo.
    echo Run the program to start.
) else (
    echo.
    echo Build failed!
    pause
)
