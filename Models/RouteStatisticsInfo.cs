using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace TA_WPF.Models
{
    public class RouteStatisticsInfo : INotifyPropertyChanged
    {
        private int _id;
        private int _routeId;
        private decimal _totalCost;
        private decimal _totalDistance;
        private string _provincesPassed;
        private string _citiesPassed;
        private string _seatTypeStats;
        private string _railwayBureauStats;
        private DateTime _updateTime;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RouteId
        {
            get => _routeId;
            set
            {
                if (_routeId != value)
                {
                    _routeId = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set
            {
                if (_totalCost != value)
                {
                    _totalCost = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalDistance
        {
            get => _totalDistance;
            set
            {
                if (_totalDistance != value)
                {
                    _totalDistance = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProvincesPassed
        {
            get => _provincesPassed;
            set
            {
                if (_provincesPassed != value)
                {
                    _provincesPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CitiesPassed
        {
            get => _citiesPassed;
            set
            {
                if (_citiesPassed != value)
                {
                    _citiesPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SeatTypeStats
        {
            get => _seatTypeStats;
            set
            {
                if (_seatTypeStats != value)
                {
                    _seatTypeStats = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RailwayBureauStats
        {
            get => _railwayBureauStats;
            set
            {
                if (_railwayBureauStats != value)
                {
                    _railwayBureauStats = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime UpdateTime
        {
            get => _updateTime;
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        // 获取席别统计的字典对象
        public Dictionary<string, decimal> GetSeatTypeStatsDict()
        {
            if (string.IsNullOrEmpty(SeatTypeStats))
                return new Dictionary<string, decimal>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, decimal>>(SeatTypeStats);
            }
            catch
            {
                return new Dictionary<string, decimal>();
            }
        }

        // 获取铁路局统计的字典对象
        public Dictionary<string, decimal> GetRailwayBureauStatsDict()
        {
            if (string.IsNullOrEmpty(RailwayBureauStats))
                return new Dictionary<string, decimal>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, decimal>>(RailwayBureauStats);
            }
            catch
            {
                return new Dictionary<string, decimal>();
            }
        }

        // 获取经过省份的列表
        public List<string> GetProvincesList()
        {
            if (string.IsNullOrEmpty(ProvincesPassed))
                return new List<string>();

            return ProvincesPassed.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // 获取经过城市的列表
        public List<string> GetCitiesList()
        {
            if (string.IsNullOrEmpty(CitiesPassed))
                return new List<string>();

            return CitiesPassed.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}