# 主题颜色更换分析报告

## 📊 难度评估

**总体难度：⭐⭐⭐ (中等)**

### 难度分解
- **配置修改难度**：⭐ (简单) - 只需修改几个配置文件
- **代码修改难度**：⭐⭐ (中等) - 需要修改 ThemeService.cs 中的硬编码颜色
- **测试验证难度**：⭐⭐⭐⭐ (较难) - 需要测试 33+ 个窗口和页面
- **兼容性风险**：⭐⭐ (中等) - 深色/浅色模式都需要适配

---

## 🎨 当前主题颜色配置

### 主色调（Primary Color）
- **当前颜色**：DeepPurple（深紫色）
- **颜色值**：
  - Light: `#9C64FF`
  - Mid: `#7C4DFF`
  - Dark: `#5E35B1`

### 辅助色调（Secondary/Accent Color）
- **当前颜色**：Lime（青柠色）
- **颜色值**：`#CDDC39`

---

## 📍 需要修改的文件位置

### 1. 核心配置文件（必须修改）

#### App.xaml
**位置**：第 16-20 行
```xml
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="DeepPurple" SecondaryColor="Lime" />
<ResourceDictionary Source=".../MaterialDesignColor.DeepPurple.xaml" />
<ResourceDictionary Source=".../MaterialDesignColor.Lime.xaml" />
```

**需要修改**：
- 第 16 行：`PrimaryColor` 和 `SecondaryColor` 属性
- 第 19 行：Primary 颜色资源文件路径
- 第 20 行：Accent 颜色资源文件路径
- 第 36-38 行：硬编码的 PrimaryHue 颜色值
- 第 39 行：硬编码的 SecondaryHue 颜色值
- 第 53-56 行：硬编码的 GlobalAccent 颜色值

#### DesignTimeResources.xaml
**位置**：第 8 行
```xml
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="DeepPurple" SecondaryColor="Lime" />
```

**需要修改**：
- 第 8 行：`PrimaryColor` 和 `SecondaryColor` 属性

### 2. 服务层代码（必须修改）

#### Services/ThemeService.cs
**位置**：第 161-187 行（深色模式颜色设置）

**需要修改的硬编码颜色值**：
```csharp
// 第 161-163 行：深色模式选择颜色
Color.FromArgb(100, 124, 77, 255)  // #7C4DFF with opacity

// 第 181-183 行：深色模式主色调
Color.FromRgb(156, 100, 255)  // #9C64FF
Color.FromRgb(124, 77, 255)   // #7C4DFF
Color.FromRgb(94, 53, 177)    // #5E35B1

// 第 185-187 行：深色模式全局强调色
Color.FromRgb(124, 77, 255)   // #7C4DFF
Color.FromRgb(156, 100, 255)  // #9C64FF
Color.FromRgb(94, 53, 177)    // #5E35B1
```

**位置**：第 172-173 行（浅色模式颜色设置）

**需要修改的硬编码颜色值**：
```csharp
// 第 172 行：浅色模式选择颜色
Color.FromArgb(77, 124, 77, 255)  // #7C4DFF with opacity
```

### 3. 视图文件（检查使用情况）

**影响范围**：33 个视图文件，385 处颜色引用

**主要使用的颜色资源**：
- `PrimaryHueMidBrush` - 最常用
- `GlobalAccentBrush` - 常用
- `PrimaryHueLightBrush` / `PrimaryHueDarkBrush` - 较少使用
- `SecondaryHueMidBrush` / `GlobalSecondaryBrush` - 较少使用

**关键文件**：
- Views/LoginWindow.xaml
- Views/EditTicketWindow.xaml
- Views/AddTicketWindow.xaml
- Views/DashboardView.xaml
- Views/QueryAllRoutesPage.xaml
- Views/QueryAllCollectionsPage.xaml
- ... 等 27 个其他视图文件

---

## ✅ 准备工作清单

### 阶段一：准备工作

- [ ] **1. 选择新主题颜色**
  - 确定新的 Primary Color（主色调）
  - 确定新的 Secondary Color（辅助色调）
  - 确保颜色符合 Material Design 规范
  - 验证颜色在深色/浅色模式下的对比度

- [ ] **2. 获取颜色值**
  - 查找 MaterialDesignColors 库中对应的颜色资源文件
  - 获取 Light/Mid/Dark 三个色阶的 RGB 值
  - 计算深色模式下的适配颜色值

- [ ] **3. 创建备份**
  - 备份 App.xaml
  - 备份 ThemeService.cs
  - 备份 DesignTimeResources.xaml
  - 创建 Git 分支用于主题更换

### 阶段二：核心修改

- [ ] **4. 修改 App.xaml**
  - 更新 BundledTheme 的 PrimaryColor 和 SecondaryColor
  - 更新颜色资源文件路径
  - 更新硬编码的 SolidColorBrush 颜色值（9 处）

- [ ] **5. 修改 DesignTimeResources.xaml**
  - 更新 BundledTheme 的 PrimaryColor 和 SecondaryColor

- [ ] **6. 修改 ThemeService.cs**
  - 更新深色模式下的颜色值（7 处）
  - 更新浅色模式下的颜色值（1 处）
  - 确保颜色值与 App.xaml 中的定义一致

### 阶段三：验证测试

- [ ] **7. 编译检查**
  - 确保项目可以正常编译
  - 检查是否有编译错误或警告

- [ ] **8. 功能测试**
  - 测试浅色模式下的所有窗口
  - 测试深色模式下的所有窗口
  - 验证按钮、链接、强调元素的颜色
  - 验证文本可读性（对比度）

- [ ] **9. 重点窗口测试**
  - LoginWindow（登录窗口）
  - MainWindow（主窗口）
  - DashboardView（仪表板）
  - EditTicketWindow（编辑车票）
  - AddTicketWindow（添加车票）
  - QueryAllRoutesPage（查询路线）
  - QueryAllCollectionsPage（查询收藏）

- [ ] **10. 边界情况测试**
  - 切换深色/浅色模式
  - 检查选择状态的颜色
  - 检查悬停状态的颜色
  - 检查禁用状态的颜色

---

## 🎯 Material Design 推荐颜色

### 可用的 Primary Colors
- Amber（琥珀色）
- Blue（蓝色）
- BlueGrey（蓝灰色）
- Brown（棕色）
- Cyan（青色）
- DeepOrange（深橙色）
- DeepPurple（深紫色）**← 当前使用**
- Green（绿色）
- Grey（灰色）
- Indigo（靛蓝色）
- LightBlue（浅蓝色）
- LightGreen（浅绿色）
- Lime（青柠色）
- Orange（橙色）
- Pink（粉色）
- Purple（紫色）
- Red（红色）
- Teal（青绿色）
- Yellow（黄色）

### 可用的 Accent Colors
- Amber（琥珀色）
- Blue（蓝色）
- Cyan（青色）
- DeepOrange（深橙色）
- DeepPurple（深紫色）
- Green（绿色）
- Indigo（靛蓝色）
- LightBlue（浅蓝色）
- LightGreen（浅绿色）
- Lime（青柠色）**← 当前使用**
- Orange（橙色）
- Pink（粉色）
- Purple（紫色）
- Red（红色）
- Teal（青绿色）
- Yellow（黄色）

---

## ⚠️ 注意事项

### 1. 颜色对比度
- 确保新颜色在深色/浅色模式下都有足够的对比度
- 文本颜色必须符合 WCAG 2.1 AA 标准（至少 4.5:1）

### 2. 品牌一致性
- 如果项目有品牌色要求，优先使用品牌色
- 确保新颜色与项目整体风格一致

### 3. 用户体验
- 避免使用过于鲜艳或刺眼的颜色
- 考虑色盲用户的体验（避免红绿搭配）

### 4. 深色模式适配
- 深色模式下可能需要调整颜色亮度
- 确保选择状态、悬停状态的颜色清晰可见

### 5. 测试覆盖
- 必须测试所有 33+ 个窗口
- 特别关注数据表格、按钮、输入框的颜色显示

---

## 📝 修改示例

### 示例：将主题改为 Blue + Orange

#### App.xaml 修改
```xml
<!-- 修改前 -->
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="DeepPurple" SecondaryColor="Lime" />
<ResourceDictionary Source=".../MaterialDesignColor.DeepPurple.xaml" />
<ResourceDictionary Source=".../MaterialDesignColor.Lime.xaml" />

<!-- 修改后 -->
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Orange" />
<ResourceDictionary Source=".../MaterialDesignColor.Blue.xaml" />
<ResourceDictionary Source=".../MaterialDesignColor.Orange.xaml" />
```

#### ThemeService.cs 修改
```csharp
// 修改前
Color.FromRgb(124, 77, 255)   // #7C4DFF (DeepPurple Mid)

// 修改后
Color.FromRgb(33, 150, 243)   // #2196F3 (Blue Mid)
```

---

## 🔧 推荐工具

1. **Material Design Color Tool**
   - https://material.io/resources/color/
   - 用于测试颜色组合和对比度

2. **WebAIM Contrast Checker**
   - https://webaim.org/resources/contrastchecker/
   - 用于验证颜色对比度是否符合标准

3. **Color Picker**
   - 用于提取 MaterialDesignColors 库中的颜色值

---

## 📈 预计工作量

- **准备阶段**：1-2 小时
- **核心修改**：1-2 小时
- **测试验证**：4-6 小时
- **总计**：6-10 小时

---

## 🎉 完成标准

- [ ] 所有文件修改完成
- [ ] 项目编译无错误
- [ ] 浅色模式测试通过
- [ ] 深色模式测试通过
- [ ] 所有关键窗口颜色显示正常
- [ ] 颜色对比度符合标准
- [ ] 用户体验良好

---

---

## 🎨 自定义颜色配置实现方案

### 目标
实现用户可自定义主题颜色功能，支持：
1. 从预设颜色中选择（Material Design 标准颜色）
2. 自定义颜色搭配（用户自己选择颜色）
3. 实时预览效果
4. 保存用户配置

### 实现难度评估
**总体难度：⭐⭐⭐⭐ (较难)**

- **UI 开发难度**：⭐⭐⭐ (中等) - 需要创建颜色选择器界面
- **后端逻辑难度**：⭐⭐⭐⭐ (较难) - 需要动态切换主题颜色
- **配置管理难度**：⭐⭐ (中等) - 需要保存/加载颜色配置
- **兼容性风险**：⭐⭐⭐ (中等) - 需要确保深色/浅色模式都正常工作

---

## 📋 实现步骤

### 阶段一：扩展 ThemeService（核心功能）

#### 1.1 添加颜色配置管理方法

**文件**：`Services/ThemeService.cs`

**新增方法**：
```csharp
/// <summary>
/// 应用自定义主题颜色
/// </summary>
/// <param name="primaryColor">主色调（Color 对象或颜色名称）</param>
/// <param name="secondaryColor">辅助色调（Color 对象或颜色名称）</param>
/// <param name="isDarkMode">是否为深色模式</param>
public void ApplyCustomColors(object primaryColor, object secondaryColor, bool isDarkMode)
{
    try
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        
        // 设置基础主题
        theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);
        
        // 处理主色调
        if (primaryColor is Color primaryColorValue)
        {
            // 自定义颜色
            theme.SetPrimaryColor(primaryColorValue);
        }
        else if (primaryColor is string primaryColorName)
        {
            // 预设颜色名称
            var swatch = SwatchHelper.Lookup[primaryColorName];
            theme.SetPrimaryColor(swatch);
        }
        
        // 处理辅助色调
        if (secondaryColor is Color secondaryColorValue)
        {
            // 自定义颜色
            theme.SetSecondaryColor(secondaryColorValue);
        }
        else if (secondaryColor is string secondaryColorName)
        {
            // 预设颜色名称
            var swatch = SwatchHelper.Lookup[secondaryColorName];
            theme.SetSecondaryColor(swatch);
        }
        
        // 应用主题
        paletteHelper.SetTheme(theme);
        
        // 更新全局资源
        UpdateGlobalColorResources(primaryColor, secondaryColor, isDarkMode);
        
        // 保存配置
        SaveColorConfig(primaryColor, secondaryColor, isDarkMode);
        
        // 触发事件
        ThemeChanged?.Invoke(this, isDarkMode);
    }
    catch (Exception ex)
    {
        LogHelper.LogError($"应用自定义颜色失败: {ex.Message}", ex);
        throw;
    }
}

/// <summary>
/// 更新全局颜色资源
/// </summary>
private void UpdateGlobalColorResources(object primaryColor, object secondaryColor, bool isDarkMode)
{
    if (Application.Current?.Resources == null) return;
    
    // 获取颜色值
    Color primaryMid = GetColorMidValue(primaryColor);
    Color secondaryMid = GetColorMidValue(secondaryColor);
    
    // 更新全局资源
    Application.Current.Resources["PrimaryHueMidBrush"] = new SolidColorBrush(primaryMid);
    Application.Current.Resources["GlobalAccentBrush"] = new SolidColorBrush(primaryMid);
    Application.Current.Resources["SecondaryHueMidBrush"] = new SolidColorBrush(secondaryMid);
    Application.Current.Resources["GlobalSecondaryBrush"] = new SolidColorBrush(secondaryMid);
    
    // 计算 Light 和 Dark 色阶
    var primaryLight = LightenColor(primaryMid, 0.2);
    var primaryDark = DarkenColor(primaryMid, 0.2);
    
    Application.Current.Resources["PrimaryHueLightBrush"] = new SolidColorBrush(primaryLight);
    Application.Current.Resources["PrimaryHueDarkBrush"] = new SolidColorBrush(primaryDark);
    Application.Current.Resources["GlobalAccentLightBrush"] = new SolidColorBrush(primaryLight);
    Application.Current.Resources["GlobalAccentDarkBrush"] = new SolidColorBrush(primaryDark);
}

/// <summary>
/// 获取颜色的中间值
/// </summary>
private Color GetColorMidValue(object color)
{
    if (color is Color c) return c;
    if (color is string name && SwatchHelper.Lookup.ContainsKey(name))
    {
        var swatch = SwatchHelper.Lookup[name];
        return swatch.ExemplarHue.Color;
    }
    return Colors.DeepPurple; // 默认颜色
}

/// <summary>
/// 保存颜色配置
/// </summary>
private void SaveColorConfig(object primaryColor, object secondaryColor, bool isDarkMode)
{
    try
    {
        string primaryStr = primaryColor is Color pc ? pc.ToString() : primaryColor.ToString();
        string secondaryStr = secondaryColor is Color sc ? sc.ToString() : secondaryColor.ToString();
        
        ConfigUtils.SaveStringValue("ThemePrimaryColor", primaryStr);
        ConfigUtils.SaveStringValue("ThemeSecondaryColor", secondaryStr);
        ConfigUtils.SaveBoolValue("IsDarkMode", isDarkMode);
        ConfigUtils.SaveBoolValue("IsCustomColor", primaryColor is Color);
        
        LogHelper.LogSystem("主题", $"已保存颜色配置: Primary={primaryStr}, Secondary={secondaryStr}");
    }
    catch (Exception ex)
    {
        LogHelper.LogError($"保存颜色配置失败: {ex.Message}", ex);
    }
}

/// <summary>
/// 加载颜色配置
/// </summary>
public void LoadColorConfig()
{
    try
    {
        bool isDarkMode = ConfigUtils.GetBoolValue("IsDarkMode", false);
        bool isCustomColor = ConfigUtils.GetBoolValue("IsCustomColor", false);
        string primaryStr = ConfigUtils.GetStringValue("ThemePrimaryColor", "DeepPurple");
        string secondaryStr = ConfigUtils.GetStringValue("ThemeSecondaryColor", "Lime");
        
        object primaryColor, secondaryColor;
        
        if (isCustomColor)
        {
            // 自定义颜色
            primaryColor = (Color)ColorConverter.ConvertFromString(primaryStr);
            secondaryColor = (Color)ColorConverter.ConvertFromString(secondaryStr);
        }
        else
        {
            // 预设颜色
            primaryColor = primaryStr;
            secondaryColor = secondaryStr;
        }
        
        ApplyCustomColors(primaryColor, secondaryColor, isDarkMode);
    }
    catch (Exception ex)
    {
        LogHelper.LogError($"加载颜色配置失败: {ex.Message}", ex);
        // 使用默认颜色
        ApplyCustomColors("DeepPurple", "Lime", false);
    }
}

/// <summary>
/// 颜色变亮
/// </summary>
private Color LightenColor(Color color, double factor)
{
    return Color.FromRgb(
        (byte)Math.Min(255, color.R + (255 - color.R) * factor),
        (byte)Math.Min(255, color.G + (255 - color.G) * factor),
        (byte)Math.Min(255, color.B + (255 - color.B) * factor)
    );
}

/// <summary>
/// 颜色变暗
/// </summary>
private Color DarkenColor(Color color, double factor)
{
    return Color.FromRgb(
        (byte)(color.R * (1 - factor)),
        (byte)(color.G * (1 - factor)),
        (byte)(color.B * (1 - factor))
    );
}
```

#### 1.2 添加 SwatchHelper（颜色预设管理）

**新建文件**：`Utils/SwatchHelper.cs`

```csharp
using MaterialDesignColors;
using System.Collections.Generic;
using System.Linq;

namespace TA_WPF.Utils
{
    /// <summary>
    /// Material Design 颜色预设辅助类
    /// </summary>
    public static class SwatchHelper
    {
        /// <summary>
        /// 所有可用的颜色预设
        /// </summary>
        public static Dictionary<string, Swatch> Lookup { get; }
        
        static SwatchHelper()
        {
            var swatchesProvider = new SwatchesProvider();
            Lookup = swatchesProvider.Swatches.ToDictionary(s => s.Name);
        }
        
        /// <summary>
        /// 获取所有主色调名称
        /// </summary>
        public static IEnumerable<string> GetPrimaryColorNames()
        {
            // Material Design 主色调列表（排除 Accent 颜色）
            return new[]
            {
                "Amber", "Blue", "BlueGrey", "Brown", "Cyan",
                "DeepOrange", "DeepPurple", "Green", "Grey", "Indigo",
                "LightBlue", "LightGreen", "Lime", "Orange", "Pink",
                "Purple", "Red", "Teal", "Yellow"
            };
        }
        
        /// <summary>
        /// 获取所有辅助色调名称
        /// </summary>
        public static IEnumerable<string> GetAccentColorNames()
        {
            // Material Design 辅助色调列表
            return new[]
            {
                "Amber", "Blue", "Cyan", "DeepOrange", "DeepPurple",
                "Green", "Indigo", "LightBlue", "LightGreen", "Lime",
                "Orange", "Pink", "Purple", "Red", "Teal", "Yellow"
            };
        }
        
        /// <summary>
        /// 根据名称获取 Swatch
        /// </summary>
        public static Swatch GetSwatch(string name)
        {
            return Lookup.TryGetValue(name, out var swatch) ? swatch : null;
        }
    }
}
```

**注意**：MaterialDesignColors 2.1.4 的 API 可能与最新版本不同，如果 `SwatchesProvider` 不可用，可以使用硬编码的颜色名称列表。

---

### 阶段二：创建颜色选择器界面

#### 2.1 创建 ViewModel

**新建文件**：`ViewModels/ThemeColorPickerViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class ThemeColorPickerViewModel : BaseViewModel
    {
        private readonly ThemeService _themeService;
        private bool _isDarkMode;
        private bool _usePresetColors = true;
        private string _selectedPrimaryPreset;
        private string _selectedSecondaryPreset;
        private Color _customPrimaryColor = Colors.DeepPurple;
        private Color _customSecondaryColor = Colors.Lime;
        
        public ThemeColorPickerViewModel()
        {
            _themeService = ThemeService.Instance;
            _isDarkMode = _themeService.IsDarkThemeActive();
            
            // 初始化预设颜色列表
            PrimaryPresetColors = new ObservableCollection<string>(SwatchHelper.GetPrimaryColorNames());
            SecondaryPresetColors = new ObservableCollection<string>(SwatchHelper.GetAccentColorNames());
            
            SelectedPrimaryPreset = "DeepPurple";
            SelectedSecondaryPreset = "Lime";
            
            // 初始化命令
            ApplyPresetColorsCommand = new RelayCommand(ApplyPresetColors, CanApplyPresetColors);
            ApplyCustomColorsCommand = new RelayCommand(ApplyCustomColors, CanApplyCustomColors);
            ResetToDefaultCommand = new RelayCommand(ResetToDefault);
            PreviewPresetColorsCommand = new RelayCommand<string>(PreviewPresetColors);
            PreviewCustomColorsCommand = new RelayCommand(PreviewCustomColors);
        }
        
        public ObservableCollection<string> PrimaryPresetColors { get; }
        public ObservableCollection<string> SecondaryPresetColors { get; }
        
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public bool UsePresetColors
        {
            get => _usePresetColors;
            set
            {
                if (_usePresetColors != value)
                {
                    _usePresetColors = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string SelectedPrimaryPreset
        {
            get => _selectedPrimaryPreset;
            set
            {
                if (_selectedPrimaryPreset != value)
                {
                    _selectedPrimaryPreset = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string SelectedSecondaryPreset
        {
            get => _selectedSecondaryPreset;
            set
            {
                if (_selectedSecondaryPreset != value)
                {
                    _selectedSecondaryPreset = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public Color CustomPrimaryColor
        {
            get => _customPrimaryColor;
            set
            {
                if (_customPrimaryColor != value)
                {
                    _customPrimaryColor = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public Color CustomSecondaryColor
        {
            get => _customSecondaryColor;
            set
            {
                if (_customSecondaryColor != value)
                {
                    _customSecondaryColor = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public ICommand ApplyPresetColorsCommand { get; }
        public ICommand ApplyCustomColorsCommand { get; }
        public ICommand ResetToDefaultCommand { get; }
        public ICommand PreviewPresetColorsCommand { get; }
        public ICommand PreviewCustomColorsCommand { get; }
        
        private bool CanApplyPresetColors() => !string.IsNullOrEmpty(SelectedPrimaryPreset) && !string.IsNullOrEmpty(SelectedSecondaryPreset);
        private bool CanApplyCustomColors() => true;
        
        private void ApplyPresetColors()
        {
            _themeService.ApplyCustomColors(SelectedPrimaryPreset, SelectedSecondaryPreset, IsDarkMode);
        }
        
        private void ApplyCustomColors()
        {
            _themeService.ApplyCustomColors(CustomPrimaryColor, CustomSecondaryColor, IsDarkMode);
        }
        
        private void ResetToDefault()
        {
            SelectedPrimaryPreset = "DeepPurple";
            SelectedSecondaryPreset = "Lime";
            ApplyPresetColors();
        }
        
        private void PreviewPresetColors(string parameter)
        {
            // 实时预览预设颜色
            if (parameter == "Primary" && !string.IsNullOrEmpty(SelectedPrimaryPreset))
            {
                _themeService.ApplyCustomColors(SelectedPrimaryPreset, SelectedSecondaryPreset, IsDarkMode);
            }
            else if (parameter == "Secondary" && !string.IsNullOrEmpty(SelectedSecondaryPreset))
            {
                _themeService.ApplyCustomColors(SelectedPrimaryPreset, SelectedSecondaryPreset, IsDarkMode);
            }
        }
        
        private void PreviewCustomColors()
        {
            // 实时预览自定义颜色
            _themeService.ApplyCustomColors(CustomPrimaryColor, CustomSecondaryColor, IsDarkMode);
        }
    }
}
```

#### 2.2 创建颜色选择器窗口

**新建文件**：`Views/ThemeColorPickerWindow.xaml`

```xml
<Window x:Class="TA_WPF.Views.ThemeColorPickerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="主题颜色设置" Width="800" Height="600"
        WindowStartupLocation="CenterOwner"
        Style="{StaticResource MaterialDesignWindow}">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- 标题 -->
        <TextBlock Grid.Row="0" Text="自定义主题颜色" 
                   Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                   Margin="0,0,0,20"/>
        
        <!-- 主要内容 -->
        <TabControl Grid.Row="1">
            <!-- 预设颜色选项卡 -->
            <TabItem Header="预设颜色">
                <Grid Margin="10">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <!-- 主色调选择 -->
                    <StackPanel Grid.Row="0" Margin="0,0,0,20">
                        <TextBlock Text="主色调 (Primary Color)" 
                                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                                   Margin="0,0,0,10"/>
                        <ComboBox ItemsSource="{Binding PrimaryPresetColors}"
                                  SelectedItem="{Binding SelectedPrimaryPreset}"
                                  Style="{StaticResource MaterialDesignComboBox}"
                                  materialDesign:HintAssist.Hint="选择主色调"/>
                    </StackPanel>
                    
                    <!-- 辅助色调选择 -->
                    <StackPanel Grid.Row="1" Margin="0,0,0,20">
                        <TextBlock Text="辅助色调 (Secondary Color)" 
                                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                                   Margin="0,0,0,10"/>
                        <ComboBox ItemsSource="{Binding SecondaryPresetColors}"
                                  SelectedItem="{Binding SelectedSecondaryPreset}"
                                  Style="{StaticResource MaterialDesignComboBox}"
                                  materialDesign:HintAssist.Hint="选择辅助色调"/>
                    </StackPanel>
                    
                    <!-- 颜色预览 -->
                    <GroupBox Grid.Row="2" Header="颜色预览">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            
                            <Border Grid.Column="0" Background="{DynamicResource PrimaryHueMidBrush}"
                                    Height="100" Margin="10">
                                <TextBlock Text="主色调" HorizontalAlignment="Center" 
                                           VerticalAlignment="Center"
                                           Foreground="{DynamicResource MaterialDesignBody}"/>
                            </Border>
                            
                            <Border Grid.Column="1" Background="{DynamicResource SecondaryHueMidBrush}"
                                    Height="100" Margin="10">
                                <TextBlock Text="辅助色调" HorizontalAlignment="Center" 
                                           VerticalAlignment="Center"
                                           Foreground="{DynamicResource MaterialDesignBody}"/>
                            </Border>
                        </Grid>
                    </GroupBox>
                </Grid>
            </TabItem>
            
            <!-- 自定义颜色选项卡 -->
            <TabItem Header="自定义颜色">
                <Grid Margin="10">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <!-- 主色调颜色选择器 -->
                    <StackPanel Grid.Row="0" Margin="0,0,0,20">
                        <TextBlock Text="主色调 (Primary Color)" 
                                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                                   Margin="0,0,0,10"/>
                        <!-- 方案1: 使用 MaterialDesign ColorPicker (如果可用) -->
                        <!-- <materialDesign:ColorPicker SelectedColor="{Binding CustomPrimaryColor}"
                                                    ShowAdvancedButton="True"
                                                    Margin="0,0,0,10"/> -->
                        <!-- 方案2: 使用 WPF 标准 ColorPicker (Windows 10+) -->
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <Border Grid.Column="0" 
                                    Background="{Binding CustomPrimaryColor, Converter={StaticResource ColorToBrushConverter}}"
                                    Height="40" Margin="0,0,10,0"
                                    BorderBrush="{DynamicResource MaterialDesignDivider}"
                                    BorderThickness="1"
                                    Cursor="Hand"
                                    MouseLeftButtonDown="PrimaryColorBorder_MouseLeftButtonDown"/>
                            <Button Grid.Column="1" 
                                    Content="选择颜色"
                                    Command="{Binding PickPrimaryColorCommand}"
                                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
                        </Grid>
                    </StackPanel>
                    
                    <!-- 辅助色调颜色选择器 -->
                    <StackPanel Grid.Row="1" Margin="0,0,0,20">
                        <TextBlock Text="辅助色调 (Secondary Color)" 
                                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                                   Margin="0,0,0,10"/>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <Border Grid.Column="0" 
                                    Background="{Binding CustomSecondaryColor, Converter={StaticResource ColorToBrushConverter}}"
                                    Height="40" Margin="0,0,10,0"
                                    BorderBrush="{DynamicResource MaterialDesignDivider}"
                                    BorderThickness="1"
                                    Cursor="Hand"
                                    MouseLeftButtonDown="SecondaryColorBorder_MouseLeftButtonDown"/>
                            <Button Grid.Column="1" 
                                    Content="选择颜色"
                                    Command="{Binding PickSecondaryColorCommand}"
                                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
                        </Grid>
                    </StackPanel>
                    
                    <!-- 颜色预览 -->
                    <GroupBox Grid.Row="2" Header="颜色预览">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            
                            <Border Grid.Column="0" 
                                    Background="{Binding CustomPrimaryColor, Converter={StaticResource ColorToBrushConverter}}"
                                    Height="100" Margin="10">
                                <TextBlock Text="主色调" HorizontalAlignment="Center" 
                                           VerticalAlignment="Center"
                                           Foreground="{DynamicResource MaterialDesignBody}"/>
                            </Border>
                            
                            <Border Grid.Column="1" 
                                    Background="{Binding CustomSecondaryColor, Converter={StaticResource ColorToBrushConverter}}"
                                    Height="100" Margin="10">
                                <TextBlock Text="辅助色调" HorizontalAlignment="Center" 
                                           VerticalAlignment="Center"
                                           Foreground="{DynamicResource MaterialDesignBody}"/>
                            </Border>
                        </Grid>
                    </GroupBox>
                </Grid>
            </TabItem>
        </TabControl>
        
        <!-- 底部按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" 
                    HorizontalAlignment="Right" Margin="0,20,0,0">
            <CheckBox Content="深色模式" IsChecked="{Binding IsDarkMode}"
                      VerticalAlignment="Center" Margin="0,0,20,0"/>
            
            <Button Content="重置为默认" 
                    Command="{Binding ResetToDefaultCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}"
                    Margin="0,0,10,0"/>
            
            <Button Content="应用预设颜色" 
                    Command="{Binding ApplyPresetColorsCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Margin="0,0,10,0"
                    Visibility="{Binding UsePresetColors, Converter={StaticResource BooleanToVisibilityConverter}}"/>
            
            <Button Content="应用自定义颜色" 
                    Command="{Binding ApplyCustomColorsCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Visibility="{Binding UsePresetColors, Converter={StaticResource InverseBooleanConverter}}"/>
        </StackPanel>
    </Grid>
</Window>
```

---

### 阶段三：集成到设置页面

#### 3.1 在设置页面添加颜色设置入口

**修改文件**：`Views/SettingsPage.xaml`

在适当位置添加：
```xml
<Button Content="主题颜色设置"
        Command="{Binding OpenThemeColorPickerCommand}"
        Style="{StaticResource MaterialDesignRaisedButton}"
        Margin="0,10"/>
```

#### 3.2 在 SettingsViewModel 中添加命令

**修改文件**：`ViewModels/SettingsViewModel.cs`

```csharp
public ICommand OpenThemeColorPickerCommand { get; }

// 在构造函数中初始化
OpenThemeColorPickerCommand = new RelayCommand(OpenThemeColorPicker);

private void OpenThemeColorPicker()
{
    var colorPickerWindow = new ThemeColorPickerWindow();
    colorPickerWindow.Owner = Application.Current.MainWindow;
    colorPickerWindow.ShowDialog();
}
```

---

### 阶段四：应用启动时加载配置

#### 4.1 修改 App.xaml.cs

**修改文件**：`App.xaml.cs`

在 `OnStartup` 方法中添加：
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // 加载主题颜色配置
    ThemeService.Instance.LoadColorConfig();
    
    // 其他初始化代码...
}
```

---

## 📦 需要的 NuGet 包

✅ **已安装的包**：
- `MaterialDesignThemes` (4.9.0) - 已安装
- `MaterialDesignColors` (2.1.4) - 已安装 ✅

⚠️ **注意事项**：
- MaterialDesignThemes 4.9.0 版本可能不包含 `ColorPicker` 控件
- 如果 `ColorPicker` 不可用，可以使用标准的 WPF `ColorPicker` 或第三方颜色选择器
- 或者使用 MaterialDesign 的 `ColorZone` 配合自定义颜色选择逻辑

---

## 🎯 实现要点

### 1. 使用 PaletteHelper 动态设置颜色
参考 [MaterialDesignInXAML Toolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) 的实现方式：
```csharp
var paletteHelper = new PaletteHelper();
var theme = paletteHelper.GetTheme();
theme.SetPrimaryColor(swatch);
theme.SetSecondaryColor(swatch);
paletteHelper.SetTheme(theme);
```

### 2. 颜色预设管理
使用 `SwatchesProvider` 获取所有可用的 Material Design 颜色预设。

### 3. 自定义颜色支持
支持用户通过 `ColorPicker` 控件选择任意颜色。

### 4. 配置持久化
使用 `ConfigUtils` 保存用户选择的颜色配置。

---

## ⚠️ 注意事项

1. **颜色对比度**：自定义颜色时，需要验证文本可读性
2. **深色模式适配**：确保自定义颜色在深色模式下也能正常显示
3. **性能考虑**：实时预览可能会频繁更新主题，注意性能优化
4. **错误处理**：颜色值无效时的错误处理
5. **ColorPicker 控件**：
   - MaterialDesignThemes 4.9.0 可能不包含 `ColorPicker` 控件
   - 可以使用 WPF 标准 `ColorPicker`（Windows 10+）或第三方颜色选择器
   - 或者使用 `Microsoft.Windows.Shell.ColorPickerDialog`（需要额外引用）

## 🔧 颜色选择器替代方案

### 方案1：使用 WPF 标准 ColorPicker（推荐）

**优点**：原生支持，无需额外依赖  
**缺点**：仅 Windows 10+ 支持

```csharp
// 在 ViewModel 中添加
private void PickPrimaryColor()
{
    var colorDialog = new System.Windows.Forms.ColorDialog();
    if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
        CustomPrimaryColor = Color.FromRgb(
            colorDialog.Color.R,
            colorDialog.Color.G,
            colorDialog.Color.B
        );
    }
}
```

### 方案2：使用 MaterialDesign ColorZone + 自定义选择器

创建一个自定义的颜色选择器，使用 MaterialDesign 的 `ColorZone` 和 `Card` 控件。

### 方案3：使用第三方颜色选择器

- Extended.Wpf.Toolkit（Xceed）
- ModernWpf.Controls（如果升级到 .NET 6+）

## 📝 颜色转换器

**新建文件**：`Converters/ColorToBrushConverter.cs`

```csharp
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TA_WPF.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Transparent;
        }
    }
}
```

在 `App.xaml` 中注册：
```xml
<converters:ColorToBrushConverter x:Key="ColorToBrushConverter" />
```

---

## 📈 预计工作量

- **ThemeService 扩展**：3-4 小时
- **颜色选择器 UI**：4-6 小时
- **集成和测试**：3-4 小时
- **总计**：10-14 小时

---

**最后更新**：2024年
