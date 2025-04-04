namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 年份选项类，用于表示年份筛选下拉列表中的选项
    /// </summary>
    public class YearOption
    {
        /// <summary>
        /// 获取或设置年份值，null 表示不筛选年份
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// 获取或设置显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 获取或设置是否为自定义年份选项
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="year">年份值</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="isCustom">是否为自定义年份选项</param>
        public YearOption(int? year, string displayName, bool isCustom = false)
        {
            Year = year;
            DisplayName = displayName;
            IsCustom = isCustom;
        }
    }

    /// <summary>
    /// 出发站选项类，用于表示出发站筛选下拉列表中的选项
    /// </summary>
    public class DepartStationItem
    {
        /// <summary>
        /// 获取或设置出发站名称
        /// </summary>
        public string DepartStation { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="departStation">出发站名称</param>
        public DepartStationItem(string departStation)
        {
            DepartStation = departStation;
        }
    }
}