using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace TA_WPF.Utils
{
    public class ChineseDatePicker : DatePicker
    {
        static ChineseDatePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChineseDatePicker), new FrameworkPropertyMetadata(typeof(ChineseDatePicker)));
        }

        public ChineseDatePicker()
        {
            // 设置中文文化
            Language = System.Windows.Markup.XmlLanguage.GetLanguage("zh-CN");
            
            // 自定义日历样式
            CalendarStyle = CreateChineseCalendarStyle();
            
            // 设置日期格式
            SelectedDateFormat = DatePickerFormat.Long;
        }

        private Style CreateChineseCalendarStyle()
        {
            var style = new Style(typeof(System.Windows.Controls.Calendar));
            
            // 设置日历的基本属性
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.DisplayModeProperty, CalendarMode.Month));
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.SelectionModeProperty, CalendarSelectionMode.SingleDate));
            
            // 设置日历的视觉样式
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.BackgroundProperty, Brushes.White));
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(221, 221, 221))));
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.BorderThicknessProperty, new Thickness(1)));
            
            // 设置日历的字体
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.FontFamilyProperty, new FontFamily("Microsoft YaHei")));
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.FontSizeProperty, 12.0));
            
            // 设置日历的语言和文化
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.LanguageProperty, System.Windows.Markup.XmlLanguage.GetLanguage("zh-CN")));
            
            // 自定义日历单元格模板
            var calendarDayButtonStyle = new Style(typeof(CalendarDayButton));
            calendarDayButtonStyle.Setters.Add(new Setter(CalendarDayButton.TemplateProperty, CreateCalendarDayButtonTemplate()));
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.CalendarDayButtonStyleProperty, calendarDayButtonStyle));
            
            // 自定义日历头部模板
            var calendarButtonStyle = new Style(typeof(CalendarButton));
            calendarButtonStyle.Setters.Add(new Setter(CalendarButton.FontWeightProperty, FontWeights.Bold));
            style.Setters.Add(new Setter(System.Windows.Controls.Calendar.CalendarButtonStyleProperty, calendarButtonStyle));
            
            return style;
        }

        private ControlTemplate CreateCalendarDayButtonTemplate()
        {
            var template = new ControlTemplate(typeof(CalendarDayButton));
            
            // 创建根Grid
            var rootGrid = new FrameworkElementFactory(typeof(Grid));
            rootGrid.Name = "RootGrid";
            
            // 创建背景矩形
            var background = new FrameworkElementFactory(typeof(System.Windows.Shapes.Rectangle));
            background.Name = "Background";
            background.SetValue(System.Windows.Shapes.Rectangle.FillProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            rootGrid.AppendChild(background);
            
            // 创建内容呈现器
            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            rootGrid.AppendChild(contentPresenter);
            
            // 设置模板的可视树
            template.VisualTree = rootGrid;
            
            return template;
        }

        protected override void OnSelectedDateChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectedDateChanged(e);
            
            // 当日期改变时，更新显示格式
            if (SelectedDate.HasValue)
            {
                var date = SelectedDate.Value;
                var chineseCulture = new CultureInfo("zh-CN");
                
                // 使用中文格式显示日期
                var formattedDate = date.ToString("yyyy年MM月dd日", chineseCulture);
                
                // 可以在这里添加农历日期的显示，如果需要
            }
        }
    }
} 