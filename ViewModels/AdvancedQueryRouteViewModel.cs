using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 距离范围枚举
    /// </summary>
    public enum DistanceRangeType
    {
        None = 0,
        Range1 = 1, // 0-100公里
        Range2 = 2, // 100-500公里
        Range3 = 3, // 500-1000公里
        Range4 = 4, // 1000-2000公里
        Range5 = 5  // 2000公里以上
    }

    /// <summary>
    /// 路线高级查询面板的ViewModel
    /// </summary>
    public class AdvancedQueryRouteViewModel : BaseViewModel
    {
        #region 字段

        private bool _isQueryPanelVisible;
        private string _routeNameFilter = string.Empty;
        private DistanceRangeType _selectedDistanceRange = DistanceRangeType.None;
        private bool _isAndCondition = true;
        private bool _isOrCondition;
        private bool _hasActiveFilters;
        private bool _isFavoriteChecked;
        private bool _isRouteNameDropdownOpen;
        private ObservableCollection<RouteInfo> _routeNameSuggestions = new ObservableCollection<RouteInfo>();
        private bool _isUpdatingRouteName = false;

        private readonly DatabaseService _databaseService;

        #endregion

        #region 事件

        /// <summary>
        /// 当应用查询条件时触发
        /// </summary>
        public event EventHandler<RouteQueryFilterEventArgs> FilterApplied;

        #endregion

        #region 构造函数

        /// <summary>
        /// 设计时构造函数
        /// </summary>
        public AdvancedQueryRouteViewModel()
        {
            // 初始化命令
            ToggleQueryPanelCommand = new RelayCommand(ToggleQueryPanel);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ResetFilterCommand = new RelayCommand(ResetFilter);
            ClearRouteNameCommand = new RelayCommand(ClearRouteName);
            ClearDistanceRangeCommand = new RelayCommand(ClearDistanceRange);
            SelectRouteNameCommand = new RelayCommand<RouteInfo>(SelectRouteName);
        }

        /// <summary>
        /// 运行时构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public AdvancedQueryRouteViewModel(DatabaseService databaseService)
            : this()
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // 设置默认值
            IsQueryPanelVisible = false;

            // 明确设置AND条件为选中状态
            _isAndCondition = true;
            _isOrCondition = false;

            // 确保通知UI更新这些属性
            OnPropertyChanged(nameof(IsAndCondition));
            OnPropertyChanged(nameof(IsOrCondition));

            // 更新按钮文本
            UpdateQueryButtonText();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 查询面板是否可见
        /// </summary>
        public bool IsQueryPanelVisible
        {
            get => _isQueryPanelVisible;
            set
            {
                if (_isQueryPanelVisible != value)
                {
                    _isQueryPanelVisible = value;
                    OnPropertyChanged(nameof(IsQueryPanelVisible));
                }
            }
        }

        /// <summary>
        /// 路线名称筛选器
        /// </summary>
        public string RouteNameFilter
        {
            get => _routeNameFilter;
            set
            {
                if (_routeNameFilter != value)
                {
                    _routeNameFilter = value;
                    OnPropertyChanged(nameof(RouteNameFilter));
                    UpdateQueryButtonText();

                    // 输入变化时搜索匹配的路线名称
                    if (!_isUpdatingRouteName)
                    {
                        SearchRouteNames(value);
                    }
                }
            }
        }

        /// <summary>
        /// 选中的距离范围
        /// </summary>
        public DistanceRangeType SelectedDistanceRange
        {
            get => _selectedDistanceRange;
            set
            {
                if (_selectedDistanceRange != value)
                {
                    _selectedDistanceRange = value;
                    OnPropertyChanged(nameof(SelectedDistanceRange));
                    OnPropertyChanged(nameof(IsDistance1Selected));
                    OnPropertyChanged(nameof(IsDistance2Selected));
                    OnPropertyChanged(nameof(IsDistance3Selected));
                    OnPropertyChanged(nameof(IsDistance4Selected));
                    OnPropertyChanged(nameof(IsDistance5Selected));
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否选中距离范围1 (0-100公里)
        /// </summary>
        public bool IsDistance1Selected
        {
            get => _selectedDistanceRange == DistanceRangeType.Range1;
            set
            {
                if (value && _selectedDistanceRange != DistanceRangeType.Range1)
                {
                    SelectedDistanceRange = DistanceRangeType.Range1;
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否选中距离范围2 (100-500公里)
        /// </summary>
        public bool IsDistance2Selected
        {
            get => _selectedDistanceRange == DistanceRangeType.Range2;
            set
            {
                if (value && _selectedDistanceRange != DistanceRangeType.Range2)
                {
                    SelectedDistanceRange = DistanceRangeType.Range2;
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否选中距离范围3 (500-1000公里)
        /// </summary>
        public bool IsDistance3Selected
        {
            get => _selectedDistanceRange == DistanceRangeType.Range3;
            set
            {
                if (value && _selectedDistanceRange != DistanceRangeType.Range3)
                {
                    SelectedDistanceRange = DistanceRangeType.Range3;
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否选中距离范围4 (1000-2000公里)
        /// </summary>
        public bool IsDistance4Selected
        {
            get => _selectedDistanceRange == DistanceRangeType.Range4;
            set
            {
                if (value && _selectedDistanceRange != DistanceRangeType.Range4)
                {
                    SelectedDistanceRange = DistanceRangeType.Range4;
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否选中距离范围5 (2000公里以上)
        /// </summary>
        public bool IsDistance5Selected
        {
            get => _selectedDistanceRange == DistanceRangeType.Range5;
            set
            {
                if (value && _selectedDistanceRange != DistanceRangeType.Range5)
                {
                    SelectedDistanceRange = DistanceRangeType.Range5;
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否勾选"我收藏的路线"
        /// </summary>
        public bool IsFavoriteChecked
        {
            get => _isFavoriteChecked;
            set
            {
                if (_isFavoriteChecked != value)
                {
                    _isFavoriteChecked = value;
                    OnPropertyChanged(nameof(IsFavoriteChecked));
                    UpdateQueryButtonText();
                }
            }
        }

        /// <summary>
        /// 是否使用AND条件
        /// </summary>
        public bool IsAndCondition
        {
            get => _isAndCondition;
            set
            {
                if (_isAndCondition != value)
                {
                    _isAndCondition = value;
                    OnPropertyChanged(nameof(IsAndCondition));

                    // 当选择AND条件时，设置OR条件为相反值
                    if (value && _isOrCondition)
                    {
                        _isOrCondition = false;
                        OnPropertyChanged(nameof(IsOrCondition));
                    }
                }
            }
        }

        /// <summary>
        /// 是否使用OR条件
        /// </summary>
        public bool IsOrCondition
        {
            get => _isOrCondition;
            set
            {
                if (_isOrCondition != value)
                {
                    _isOrCondition = value;
                    OnPropertyChanged(nameof(IsOrCondition));

                    // 当选择OR条件时，设置AND条件为相反值
                    if (value && _isAndCondition)
                    {
                        _isAndCondition = false;
                        OnPropertyChanged(nameof(IsAndCondition));
                    }
                }
            }
        }

        /// <summary>
        /// 是否有激活的筛选条件
        /// </summary>
        public bool HasActiveFilters
        {
            get => _hasActiveFilters;
            private set
            {
                if (_hasActiveFilters != value)
                {
                    _hasActiveFilters = value;
                    OnPropertyChanged(nameof(HasActiveFilters));
                }
            }
        }

        /// <summary>
        /// 查询按钮的文本
        /// </summary>
        public string QueryButtonText
        {
            get => HasAnyActiveFilter() ? "查询" : "查询全部";
        }

        /// <summary>
        /// 路线名称下拉框是否打开
        /// </summary>
        public bool IsRouteNameDropdownOpen
        {
            get => _isRouteNameDropdownOpen;
            set
            {
                if (_isRouteNameDropdownOpen != value)
                {
                    _isRouteNameDropdownOpen = value;
                    OnPropertyChanged(nameof(IsRouteNameDropdownOpen));
                }
            }
        }

        /// <summary>
        /// 路线名称建议列表
        /// </summary>
        public ObservableCollection<RouteInfo> RouteNameSuggestions
        {
            get => _routeNameSuggestions;
            set
            {
                if (_routeNameSuggestions != value)
                {
                    _routeNameSuggestions = value;
                    OnPropertyChanged(nameof(RouteNameSuggestions));
                }
            }
        }

        /// <summary>
        /// 选择路线名称命令
        /// </summary>
        public ICommand SelectRouteNameCommand { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 切换查询面板可见性命令
        /// </summary>
        public ICommand ToggleQueryPanelCommand { get; }

        /// <summary>
        /// 应用筛选条件命令
        /// </summary>
        public ICommand ApplyFilterCommand { get; }

        /// <summary>
        /// 重置筛选条件命令
        /// </summary>
        public ICommand ResetFilterCommand { get; }

        /// <summary>
        /// 清空路线名称命令
        /// </summary>
        public ICommand ClearRouteNameCommand { get; }

        /// <summary>
        /// 清空距离范围命令
        /// </summary>
        public ICommand ClearDistanceRangeCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 切换查询面板的可见性
        /// </summary>
        private void ToggleQueryPanel()
        {
            IsQueryPanelVisible = !IsQueryPanelVisible;
        }

        /// <summary>
        /// 检查是否有任何激活的筛选条件
        /// </summary>
        /// <returns>是否有激活的筛选条件</returns>
        private bool HasAnyActiveFilter()
        {
            return !string.IsNullOrWhiteSpace(RouteNameFilter) ||
                   SelectedDistanceRange != DistanceRangeType.None ||
                   IsFavoriteChecked;
        }

        /// <summary>
        /// 更新查询按钮文本
        /// </summary>
        private void UpdateQueryButtonText()
        {
            HasActiveFilters = HasAnyActiveFilter();
            OnPropertyChanged(nameof(QueryButtonText));
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilter()
        {
            Debug.WriteLine("应用路线高级查询筛选条件");

            // 构建筛选条件参数
            var args = new RouteQueryFilterEventArgs
            {
                RouteName = string.IsNullOrWhiteSpace(RouteNameFilter) ? null : RouteNameFilter,
                DistanceRange = SelectedDistanceRange,
                IsFavorite = IsFavoriteChecked,
                IsAndCondition = IsAndCondition
            };

            // 触发筛选应用事件，让父级ViewModel处理具体的数据查询
            FilterApplied?.Invoke(this, args);
        }

        /// <summary>
        /// 重置筛选条件
        /// </summary>
        public void ResetFilter()
        {
            // 重置所有筛选条件
            RouteNameFilter = string.Empty;
            SelectedDistanceRange = DistanceRangeType.None;
            IsFavoriteChecked = false;
            IsAndCondition = true;
            IsOrCondition = false;

            // 更新按钮文本
            UpdateQueryButtonText();

            // 触发查询所有路线的事件
            ApplyFilter();

            Debug.WriteLine("重置所有筛选条件");
        }

        /// <summary>
        /// 清空路线名称
        /// </summary>
        private void ClearRouteName()
        {
            RouteNameFilter = string.Empty;
            IsRouteNameDropdownOpen = false;
            UpdateQueryButtonText();
        }

        /// <summary>
        /// 清空距离范围选择
        /// </summary>
        private void ClearDistanceRange()
        {
            SelectedDistanceRange = DistanceRangeType.None;
            UpdateQueryButtonText();
        }

        /// <summary>
        /// 搜索匹配的路线名称
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private async void SearchRouteNames(string searchText)
        {
            // 如果搜索文本为空，关闭下拉框
            if (string.IsNullOrWhiteSpace(searchText))
            {
                IsRouteNameDropdownOpen = false;
                RouteNameSuggestions.Clear();
                return;
            }

            try
            {
                // 调用数据库服务搜索路线
                var routes = await _databaseService.SearchRoutesByNameAsync(searchText);

                // 更新建议列表
                RouteNameSuggestions.Clear();
                foreach (var route in routes)
                {
                    RouteNameSuggestions.Add(route);
                }

                // 显示或隐藏下拉框
                IsRouteNameDropdownOpen = RouteNameSuggestions.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"搜索路线失败: {ex.Message}");
                // 出错时隐藏下拉框
                IsRouteNameDropdownOpen = false;
            }
        }

        /// <summary>
        /// 选择路线名称
        /// </summary>
        private void SelectRouteName(RouteInfo route)
        {
            if (route != null)
            {
                try
                {
                    // 设置标记，防止更新文本时触发搜索
                    _isUpdatingRouteName = true;

                    // 更新文本框
                    RouteNameFilter = route.RouteName;

                    // 关闭下拉框
                    IsRouteNameDropdownOpen = false;

                    // 清空建议列表
                    RouteNameSuggestions.Clear();
                }
                finally
                {
                    // 恢复标记
                    _isUpdatingRouteName = false;
                }
            }
        }

        /// <summary>
        /// 通知所有重要属性已更改，确保UI更新
        /// </summary>
        public void NotifyPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsAndCondition));
            OnPropertyChanged(nameof(IsOrCondition));
            OnPropertyChanged(nameof(RouteNameFilter));
            OnPropertyChanged(nameof(SelectedDistanceRange));
            OnPropertyChanged(nameof(IsDistance1Selected));
            OnPropertyChanged(nameof(IsDistance2Selected));
            OnPropertyChanged(nameof(IsDistance3Selected));
            OnPropertyChanged(nameof(IsDistance4Selected));
            OnPropertyChanged(nameof(IsDistance5Selected));
            OnPropertyChanged(nameof(IsFavoriteChecked));
            OnPropertyChanged(nameof(QueryButtonText));
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(IsRouteNameDropdownOpen));
            OnPropertyChanged(nameof(RouteNameSuggestions));
        }

        #endregion
    }

    /// <summary>
    /// 路线筛选条件参数
    /// </summary>
    public class RouteQueryFilterEventArgs : EventArgs
    {
        public string RouteName { get; set; }
        public DistanceRangeType DistanceRange { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsAndCondition { get; set; }
    }

    /// <summary>
    /// 设计时使用的ViewModel
    /// </summary>
    public class DesignTimeAdvancedQueryRouteViewModel : AdvancedQueryRouteViewModel
    {
        public DesignTimeAdvancedQueryRouteViewModel()
        {
            IsQueryPanelVisible = true;
        }
    }
}