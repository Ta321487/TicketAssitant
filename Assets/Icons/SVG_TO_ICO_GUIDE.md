# SVG到ICO转换指南

## 步骤1：准备SVG文件
1. 将您的SVG图标文件放置在此文件夹中，并命名为`app_icon.svg`
2. 确保SVG文件是正方形的，以便转换后的图标不会变形

## 步骤2：转换SVG到ICO
有多种方法可以将SVG转换为ICO文件：

### 方法1：使用在线转换工具
1. 访问以下任一在线转换工具：
   - [Convertio](https://convertio.co/svg-ico/)
   - [SVG2ICO](https://www.aconvert.com/icon/svg-to-ico/)
   - [CloudConvert](https://cloudconvert.com/svg-to-ico)
2. 上传您的SVG文件
3. 设置输出选项，确保包含多种尺寸（16x16, 32x32, 48x48, 64x64, 128x128, 256x256）
4. 下载转换后的ICO文件
5. 将文件重命名为`app_icon.ico`并放置在此文件夹中

### 方法2：使用专业软件
1. 使用Adobe Illustrator、Inkscape或其他矢量图形编辑软件打开SVG文件
2. 导出为多种尺寸的PNG文件
3. 使用ICO转换工具（如IcoFX、IconWorkshop等）将PNG文件合并为一个ICO文件
4. 将文件命名为`app_icon.ico`并放置在此文件夹中

### 方法3：使用命令行工具
如果您熟悉命令行，可以使用以下工具：
1. 安装ImageMagick：[https://imagemagick.org/](https://imagemagick.org/)
2. 使用以下命令转换SVG到ICO：
   ```
   magick convert -background transparent app_icon.svg -define icon:auto-resize=16,32,48,64,128,256 app_icon.ico
   ```

## 步骤3：替换临时图标
1. 将生成的`app_icon.ico`文件复制到此文件夹，替换现有的空文件
2. 重新编译应用程序以应用新图标

## 注意事项
- ICO文件应包含多种尺寸，以确保在不同场景下显示清晰
- 图标设计应简洁明了，在小尺寸下仍能辨认
- 建议使用透明背景，以适应不同的显示环境 