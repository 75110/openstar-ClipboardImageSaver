# Clipboard Image Saver - 剪贴板图片保存工具

> 轻量级 Windows 图片粘贴工具，体积仅 9KB，开源免费

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

## 简介

一个轻量级的 Windows 工具，可以通过自定义快捷键快速将剪贴板中的图片保存到当前文件夹。

**特点：**
- 🎯 自动识别当前打开的文件夹
- ⌨️ 自定义快捷键
- 🌐 中英文界面切换
- 🎨 自定义托盘图标
- 💾 超小体积 (~9KB)
- 🚀 无需安装，即开即用

## 下载

[📥 下载最新版本](../../releases)

## 使用方法

1. **启动程序**：双击 `ClipboardImageSaver.exe`
2. **截图或复制图片**
3. **打开目标文件夹**
4. **按快捷键**（默认 Ctrl+S）保存图片

图片会自动保存到当前文件夹，文件名格式：`image_yyyyMMdd_HHmmss.png`

## 功能

### 自定义快捷键

右键托盘图标 → "更改快捷键" → 按下想要的组合键

### 语言切换

右键托盘图标 → "切换到英文/中文"

### 自定义图标

1. 将图标图片保存为 `source-icon.png`
2. 运行 `convert-to-icon.ps1` 转换为 ico
3. 运行 `build.bat` 重新编译

## 编译

### 要求

- Windows 7 及以上
- .NET Framework 4.0+（系统自带）

### 编译步骤

```bash
# 直接运行编译脚本
build.bat
```

或使用命令行：

```bash
csc.exe /target:winexe /out:ClipboardImageSaver.exe /win32icon:icon.ico ^
    /reference:System.dll /reference:System.Core.dll ^
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ^
    /reference:Microsoft.CSharp.dll Program.cs
```

## 文件说明

```
├── Program.cs                   # 主程序源代码
├── build.bat                    # 编译脚本
├── icon.ico                     # 程序图标
├── source-icon.png              # 图标源文件
├── convert-to-icon.ps1          # 图标转换脚本
├── README.md                    # 项目说明
└── LICENSE                      # MIT 许可证
```

## 技术栈

- **语言**: C# 5.0
- **框架**: .NET Framework 4.0
- **UI**: Windows Forms
- **体积**: ~9 KB

## 系统要求

- Windows 7 / 8 / 10 / 11
- .NET Framework 4.0+（系统自带）

## 开源协议

[MIT License](LICENSE)

## 作者

网站: [sevencn.com](https://sevencn.com)

## 贡献

欢迎提交 Issue 和 Pull Request！

## 更新日志

### v1.3 (2025-11-08)

- ✅ 首次发布
- ✅ 自定义快捷键
- ✅ 中英文界面
- ✅ 自动识别文件夹
- ✅ 系统托盘运行

## 截图

![托盘图标](screenshots/tray.png)
![快捷键设置](screenshots/hotkey.png)

---

**⭐ 如果觉得有用，请给个 Star！**
