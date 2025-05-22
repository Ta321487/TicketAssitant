using System;
using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为PackIconKind枚举值的转换器
    /// </summary>
    public class BooleanToPackIconKindConverter : IValueConverter
    {
        /// <summary>
        /// 当布尔值为true时返回的PackIconKind
        /// </summary>
        public PackIconKind TrueValue { get; set; } = PackIconKind.FullscreenExit;
        
        /// <summary>
        /// 当布尔值为false时返回的PackIconKind
        /// </summary>
        public PackIconKind FalseValue { get; set; } = PackIconKind.Fullscreen;
        
        /// <summary>
        /// 将布尔值转换为PackIconKind
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，格式为"TrueIconKind|FalseIconKind"</param>
        /// <param name="culture">区域信息</param>
        /// <returns>转换后的PackIconKind枚举值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果指定了参数，尝试解析参数中的PackIconKind值
            if (parameter is string paramStr && !string.IsNullOrEmpty(paramStr))
            {
                var parts = paramStr.Split('|');
                if (parts.Length == 2)
                {
                    if (Enum.TryParse(parts[0], out PackIconKind trueKind))
                        TrueValue = trueKind;
                    
                    if (Enum.TryParse(parts[1], out PackIconKind falseKind))
                        FalseValue = falseKind;
                }
            }
            
            // 根据布尔值返回对应的PackIconKind
            return value is bool bValue && bValue ? TrueValue : FalseValue;
        }
        
        /// <summary>
        /// 将PackIconKind转换回布尔值（不实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 