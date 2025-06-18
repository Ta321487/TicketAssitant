using System.Text;
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
        private string _seatTypeStatistics;
        private string _provincesList;
        private string _citiesList;
        private string _railwayBureauStatistics;

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

        // 席别统计信息
        public string SeatTypeStatistics
        {
            get => _seatTypeStatistics;
            set
            {
                if (_seatTypeStatistics != value)
                {
                    _seatTypeStatistics = value;
                    OnPropertyChanged(nameof(SeatTypeStatistics));
                }
            }
        }

        // 经过省份列表
        public string ProvincesList
        {
            get => _provincesList;
            set
            {
                if (_provincesList != value)
                {
                    _provincesList = value;
                    OnPropertyChanged(nameof(ProvincesList));
                }
            }
        }

        // 经过城市列表
        public string CitiesList
        {
            get => _citiesList;
            set
            {
                if (_citiesList != value)
                {
                    _citiesList = value;
                    OnPropertyChanged(nameof(CitiesList));
                }
            }
        }

        // 铁路局统计信息
        public string RailwayBureauStatistics
        {
            get => _railwayBureauStatistics;
            set
            {
                if (_railwayBureauStatistics != value)
                {
                    _railwayBureauStatistics = value;
                    OnPropertyChanged(nameof(RailwayBureauStatistics));
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
                }

                // 获取总花费
                decimal totalCost = await _databaseService.GetRouteTotalCostAsync(_route.Id);
                _statistics.TotalCost = totalCost;

                // 获取席别统计
                var seatTypeStats = await _databaseService.GetSeatTypeStatisticsAsync(_route.Id);
                StringBuilder seatTypeBuilder = new StringBuilder();
                foreach (var item in seatTypeStats)
                {
                    seatTypeBuilder.AppendLine($"- {item.Key}: {item.Value.Distance:N2} 公里 ({item.Value.Percentage}%)");
                }
                SeatTypeStatistics = seatTypeBuilder.ToString().TrimEnd();

                // 获取经过的省份
                var provinces = await _databaseService.GetRouteProvinceListAsync(_route.Id);
                ProvincesList = string.Join("、", provinces);
                _statistics.ProvincesPassed = ProvincesList;

                // 获取经过的城市
                var cities = await _databaseService.GetRouteCityListAsync(_route.Id);
                CitiesList = string.Join("、", cities);
                _statistics.CitiesPassed = CitiesList;

                // 获取铁路局统计
                var bureauStats = await _databaseService.GetRailwayBureauStatisticsAsync(_route.Id);
                StringBuilder bureauBuilder = new StringBuilder();
                foreach (var item in bureauStats)
                {
                    bureauBuilder.AppendLine($"- {item.Key}: {item.Value.Distance:N2} 公里 ({item.Value.Percentage}%)");
                }
                RailwayBureauStatistics = bureauBuilder.ToString().TrimEnd();

                // 转换为JSON字符串存储
                _statistics.SeatTypeStats = Newtonsoft.Json.JsonConvert.SerializeObject(seatTypeStats);
                _statistics.RailwayBureauStats = Newtonsoft.Json.JsonConvert.SerializeObject(bureauStats);

                // 更新统计数据到数据库
                await _databaseService.CreateOrUpdateRouteStatisticsAsync(_statistics);

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