# 图标转换指南

## 方法 1：在线转换（推荐）

1. 打开 https://convertio.co/zh/svg-ico/ 或 https://www.aconvert.com/cn/icon/svg-to-ico/
2. 上传 `icon.svg` 文件
3. 选择输出尺寸：256x256, 128x128, 64x64, 48x48, 32x32, 16x16（多选）
4. 转换并下载为 `icon.ico`
5. 将 `icon.ico` 放到项目根目录

## 方法 2：使用 ImageMagick（需要安装）

```powershell
magick convert icon.svg -define icon:auto-resize=256,128,64,48,32,16 icon.ico
```

## 方法 3：使用 Python PIL（需要安装）

```python
from PIL import Image
import cairosvg

# SVG 转 PNG
cairosvg.svg2png(url='icon.svg', write_to='icon.png', output_width=256, output_height=256)

# PNG 转 ICO
img = Image.open('icon.png')
img.save('icon.ico', format='ICO', sizes=[(256,256), (128,128), (64,64), (48,48), (32,32), (16,16)])
```

## 转换完成后

1. 确保 `icon.ico` 在项目根目录
2. 运行 `build.bat` 重新编译
3. 新的 exe 文件将包含图标

## 临时方案

如果暂时无法转换，程序会使用系统默认图标。
