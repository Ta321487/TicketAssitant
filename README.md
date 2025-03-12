# 售票辅助系统（售票处）

一个基于 WPF 开发的现代化售票辅助系统，使用 Material Design 风格的 UI 设计。

## 功能特点

- 现代化的 Material Design 界面
- 响应式布局设计
- 深色/浅色主题切换
- 数据实时同步
- 高效的票务管理
- 完善的数据统计

## 环境要求

- Windows 操作系统
- .NET 8.0 SDK 或更高版本
- MySQL 8.0 或更高版本

## 开发工具

- Visual Studio 2022 或更高版本
- 或 Visual Studio Code 配合 .NET SDK

## 安装步骤

1. 克隆仓库
```bash
git clone https://github.com/你的用户名/TA_WPF.git
cd TA_WPF
```

2. 使用 Visual Studio 打开解决方案
- 双击 `TA_WPF.sln` 文件
- 或通过 Visual Studio 的"打开项目/解决方案"菜单

3. 还原 NuGet 包
```bash
dotnet restore
```

4. 编译并运行
- 在 Visual Studio 中按 F5 运行
- 或使用命令行：
```bash
dotnet build
dotnet run
```

## 配置说明

1. 数据库配置
- 在 `App.config` 文件中配置数据库连接字符串
- 确保 MySQL 服务已启动且可访问

2. 应用程序设置
- 主题设置会自动保存
- 字体大小可在设置中调整
- 其他个性化设置可在系统设置中修改

## 技术栈

- WPF (.NET 8.0)
- Material Design in XAML
- MySQL (数据存储)
- MVVM 架构模式

## 贡献指南

欢迎提交 Issue 和 Pull Request 来帮助改进项目。

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE.txt](LICENSE.txt) 文件

## 联系方式

如有问题或建议，请通过以下方式联系：
- 提交 Issue
- 发送邮件至：[你的邮箱地址]

## 致谢

感谢所有为这个项目做出贡献的开发者。