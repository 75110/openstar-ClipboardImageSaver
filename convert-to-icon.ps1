# 图片转 ICO 图标脚本
# 使用方法：将图片保存为 source-icon.png，然后运行此脚本

Add-Type -AssemblyName System.Drawing

$sourcePath = "source-icon.png"  # 源图片文件名
$outputPath = "icon.ico"

if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: source-icon.png not found" -ForegroundColor Red
    exit
}

Write-Host "Converting icon..." -ForegroundColor Cyan

try {
    # 加载源图片
    $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $sourcePath))
    
    # 创建内存流
    $memoryStream = New-Object System.IO.MemoryStream
    
    # ICO 文件头
    $iconDir = @{
        Reserved = [uint16]0
        Type = [uint16]1  # 1 = ICO
        Count = [uint16]6  # 6 个尺寸
    }
    
    # 写入头部
    $writer = New-Object System.IO.BinaryWriter($memoryStream)
    $writer.Write($iconDir.Reserved)
    $writer.Write($iconDir.Type)
    $writer.Write($iconDir.Count)
    
    # 定义尺寸
    $sizes = @(256, 128, 64, 48, 32, 16)
    $imageDataList = @()
    $offset = 6 + (16 * $sizes.Count)  # 头部 + 目录项
    
    foreach ($size in $sizes) {
        # 调整图片大小
        $resized = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($sourceImage, 0, 0, $size, $size)
        $graphics.Dispose()
        
        # 保存为 PNG
        $pngStream = New-Object System.IO.MemoryStream
        $resized.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngData = $pngStream.ToArray()
        $pngStream.Dispose()
        $resized.Dispose()
        
        # 写入目录项
        $sizeValue = if ($size -eq 256) { 0 } else { $size }
        $writer.Write([byte]$sizeValue)
        $writer.Write([byte]$sizeValue)
        $writer.Write([byte]0)  # 颜色数
        $writer.Write([byte]0)  # 保留
        $writer.Write([uint16]1)  # 颜色平面
        $writer.Write([uint16]32)  # 位深度
        $writer.Write([uint32]$pngData.Length)  # 数据大小
        $writer.Write([uint32]$offset)  # 数据偏移
        
        $imageDataList += $pngData
        $offset += $pngData.Length
    }
    
    # 写入所有图片数据
    foreach ($data in $imageDataList) {
        $writer.Write($data)
    }
    
    # 保存到文件
    [System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot $outputPath), $memoryStream.ToArray())
    
    $writer.Dispose()
    $memoryStream.Dispose()
    $sourceImage.Dispose()
    
    Write-Host "Success! Icon created: $outputPath" -ForegroundColor Green
    Write-Host "Next: Run build.bat to recompile" -ForegroundColor Yellow
    
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}
