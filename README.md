# Chronoticket: Journey Archives（车票标记时光：旅程归档）
<font size = 36><i>本项目95%由AI生成！</i></font>

## 名称含义

**Chronoticket**：合成词，"Chrono-"（时间）+ "Ticket"，暗含「以车票标记时光」的科技感。  
**Journey Archives**：强调「旅程归档」，突出应用程序的整理与收藏功能。  


## 项目简介




## 开发环境
- .NET 8.0 Windows
- 平台: x64

## 依赖包
项目使用以下NuGet包：
- LiveCharts.Wpf (0.9.7)
- LiveChartsCore.SkiaSharpView.WPF (2.0.0-rc5.4)
- MaterialDesignColors (2.1.4)
- MaterialDesignThemes (4.9.0)
- MySql.Data (8.0.33)
- QRCoder (1.6.0)
- System.Configuration.ConfigurationManager (9.0.2)
- System.Drawing.Common (9.0.3)
- System.IO.Compression (4.3.0)

## 安装方法

### 开发者安装
1. 克隆仓库：
   ```
   git clone https://github.com/Ta321487/TicketAssitant.git
   ```

2. 使用Visual Studio 2022或更高版本打开解决方案文件 `TA_WPF.sln`

3. 恢复NuGet包：
   - 右键点击解决方案，选择"恢复NuGet包"
   - 或者使用Package Manager Console: `Update-Package -reinstall`

4. 编译并运行项目


### 用户安装
1. 下载最新的发布版本
2. 解压缩下载的文件到任意位置
3. 运行 TrainAssistant.exe 启动应用程序

### 数据库配置
1. 确保已安装MySQL 8.0或更高版本
2. 在App.config文件中配置数据库连接字符串