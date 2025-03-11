using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// UI服务，负责管理应用程序的UI相关操作
    /// </summary>
    public class UIService
    {
        /// <summary>
        /// 应用字体大小到应用程序
        /// </summary>
        /// <param name="fontSize">字体大小</param>
        public void ApplyFontSize(double fontSize)
        {
            try
            {
                // 确保字体大小不小于最小值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }

                // 获取应用程序资源
                var resources = Application.Current.Resources;

                // 更新MaterialDesign主题的字体大小
                resources["MaterialDesignFontSize"] = fontSize;

                // 更新其他相关字体大小
                resources["MaterialDesignSubtitle1FontSize"] = fontSize + 2;
                resources["MaterialDesignSubtitle2FontSize"] = fontSize + 1;
                resources["MaterialDesignHeadline6FontSize"] = fontSize + 4;
                resources["MaterialDesignHeadline5FontSize"] = fontSize + 6;

                // 更新主窗口字体大小
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    window.FontSize = fontSize;

                    // 递归更新所有元素的字体大小
                    UpdateFontSizeForAllElements(window, fontSize);
                }

                LogHelper.LogInfo($"字体大小已调整为 {fontSize}pt");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"应用字体大小时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归更新所有元素的字体大小
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="fontSize">字体大小</param>
        private void UpdateFontSizeForAllElements(DependencyObject parent, double fontSize)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                
                // 更新控件的字体大小
                if (child is Control control)
                {
                    control.FontSize = fontSize;
                }
                else if (child is TextBlock textBlock)
                {
                    textBlock.FontSize = fontSize;
                }
                else if (child is DataGrid dataGrid)
                {
                    dataGrid.FontSize = fontSize;
                }
                
                // 递归处理子元素
                UpdateFontSizeForAllElements(child, fontSize);
            }
        }

        /// <summary>
        /// 计算DataGrid的行高
        /// </summary>
        /// <param name="fontSize">字体大小</param>
        /// <returns>行高</returns>
        public double CalculateDataGridRowHeight(double fontSize)
        {
            // 增加行高与字体大小的比例，从2.5倍增加到3.0倍
            return fontSize * 3.0;
        }

        /// <summary>
        /// 计算DataGrid的表头字体大小
        /// </summary>
        /// <param name="fontSize">基础字体大小</param>
        /// <returns>表头字体大小</returns>
        public double CalculateDataGridHeaderFontSize(double fontSize)
        {
            // 表头字体比正常字体大2，增加可读性
            return fontSize + 2;
        }

        /// <summary>
        /// 计算DataGrid的单元格字体大小
        /// </summary>
        /// <param name="fontSize">基础字体大小</param>
        /// <returns>单元格字体大小</returns>
        public double CalculateDataGridCellFontSize(double fontSize)
        {
            // 单元格字体比正常字体大1，增加可读性
            return fontSize + 1;
        }
    }
} 