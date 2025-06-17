using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class RouteStatisticalAbstractViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private RouteInfo _route;
        private RouteStatisticsInfo _statistics;
        private bool _isLoading;

        public RouteStatisticalAbstractViewModel(RouteInfo route, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _route = route ?? throw new ArgumentNullException(nameof(route));
            
            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
        }

        #region 属性

        // 主ViewModel引用，用于绑定字体大小等全局设置
        public MainViewModel MainViewModel => _mainViewModel;

        // 当前路线信息
        public RouteInfo Route
        {
            get => _route;
            set
            {
                if (_route != value)
                {
                    _route = value;
                    OnPropertyChanged(nameof(Route));
                }
            }
        }

        // 统计信息
        public RouteStatisticsInfo Statistics
        {
            get => _statistics;
            set
            {
                if (_statistics != value)
                {
                    _statistics = value;
                    OnPropertyChanged(nameof(Statistics));
                }
            }
        }

        // 加载状态
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; }

        #endregion

        #region 方法

        // 刷新数据
        public async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;

                // 尝试从数据库加载统计信息
                var existingStats = await _databaseService.GetRouteStatisticsAsync(_route.Id);
                
                if (existingStats != null)
                {
                    // 使用现有数据
                    _statistics = existingStats;
                    
                    // 设置总里程（从路线信息中获取）
                    _statistics.TotalDistance = _route.TotalDistance;
                }
                else
                {
                    // 创建新的统计记录
                    _statistics = new RouteStatisticsInfo
                    {
                        RouteId = _route.Id,
                        TotalCost = 0,
                        TotalDistance = _route.TotalDistance, // 从路线信息中获取总里程
                        ProvincesPassed = "",
                        CitiesPassed = "",
                        SeatTypeStats = "{}",
                        RailwayBureauStats = "{}",
                        UpdateTime = DateTime.Now
                    };
                    
                    // 将新记录保存到数据库（不包含TotalDistance字段）
                    await _databaseService.CreateOrUpdateRouteStatisticsAsync(_statistics);
                }

                OnPropertyChanged(nameof(Statistics));
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"刷新路线统计数据失败: {ex.Message}", ex);
                
                // 出错时创建一个本地对象，避免UI显示空白
                _statistics = new RouteStatisticsInfo
                {
                    RouteId = _route.Id,
                    TotalDistance = _route.TotalDistance, // 从路线信息中获取总里程
                    UpdateTime = DateTime.Now
                };
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
} 