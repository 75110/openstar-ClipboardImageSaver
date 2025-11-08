# Clipboard Image Saver (C# Version)

专业的剪贴板图片保存工具，使用 C# 开发，体积仅 **7.5 KB**！

## 功能特点

✅ **系统托盘运行** - 后台静默运行，不占用任务栏  
✅ **全局快捷键** - `Ctrl+Shift+V` 快速保存  
✅ **气泡通知** - 保存成功后显示提示  
✅ **右键菜单** - 托盘图标右键可配置  
✅ **双击保存** - 双击托盘图标快速保存  
✅ **超小体积** - 仅 7.5 KB，无需额外依赖  

## 使用方法

### 启动程序

双击 `ClipboardImageSaver.exe`，程序会在系统托盘显示图标

### 保存图片

**方法 1：** 复制图片后按 `Ctrl+Shift+V`  
**方法 2：** 双击托盘图标  
**方法 3：** 右键托盘图标 → "Save Clipboard Image"

### 打开保存文件夹

右键托盘图标 → 点击 "Save to: ..." 路径

### 退出程序

右键托盘图标 → "Exit"

## 保存位置

图片保存在程序目录下的 `saved-images` 文件夹中

文件名格式：`image_yyyyMMdd_HHmmss.png`

## 设置开机自启动

1. 按 `Win+R`，输入 `shell:startup` 打开启动文件夹
2. 创建 `ClipboardImageSaver.exe` 的快捷方式到启动文件夹

## 重新编译

如需修改源码并重新编译，运行：

```
build.bat
```

要求：Windows 7 及以上（自带 .NET Framework 4.0+）

## 技术细节

- **语言**: C# 5.0
- **框架**: .NET Framework 4.0 (Windows 自带)
- **体积**: 7.5 KB
- **依赖**: 无（使用系统自带组件）

## 文件说明

- `ClipboardImageSaver.exe` - 主程序 (7.5 KB)
- `Program.cs` - 源代码
- `build.bat` - 编译脚本
- `README-CSharp.md` - 本说明文档

---

与 PowerShell 版本相比，C# 版本更专业：
- ✅ 系统托盘图标
- ✅ 气泡通知
- ✅ 无窗口后台运行
- ✅ 体积更小 (7.5 KB vs 4+ KB PowerShell script)
