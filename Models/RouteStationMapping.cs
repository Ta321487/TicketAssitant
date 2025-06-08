using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TA_WPF.Models
{
    /// <summary>
    /// 路线与车站的映射关系模型
    /// </summary>
    public class RouteStationMapping : INotifyPropertyChanged
    {
        private int _id;
        private int _routeId;
        private int _stationId;
        private int _orderIndex;
        private byte _stationRole;
        private int _stayTime;
        private string _notes;
        private DateTime _addTime;
        private decimal _distanceFromPrev;
        private decimal _distanceFromStart;
        private StationInfo _station;
        private bool _isSelected;

        /// <summary>
        /// 映射ID
        /// </summary>
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

        /// <summary>
        /// 路线ID
        /// </summary>
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

        /// <summary>
        /// 车站ID
        /// </summary>
        public int StationId
        {
            get => _stationId;
            set
            {
                if (_stationId != value)
                {
                    _stationId = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 在路线中的顺序
        /// </summary>
        public int OrderIndex
        {
            get => _orderIndex;
            set
            {
                if (_orderIndex != value)
                {
                    _orderIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 车站角色：1=起点,2=终点,4=经停,8=换乘
        /// </summary>
        public byte StationRole
        {
            get => _stationRole;
            set
            {
                if (_stationRole != value)
                {
                    _stationRole = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StationRoleText));
                }
            }
        }

        /// <summary>
        /// 车站角色文本描述
        /// </summary>
        public string StationRoleText
        {
            get
            {
                if (StationRole == 1) return "起点";
                if (StationRole == 2) return "终点";
                if (StationRole == 4) return "经停";
                if (StationRole == 8) return "换乘";
                if ((StationRole & 1) != 0 && (StationRole & 8) != 0) return "起点/换乘";
                if ((StationRole & 2) != 0 && (StationRole & 8) != 0) return "终点/换乘";
                if ((StationRole & 4) != 0 && (StationRole & 8) != 0) return "经停/换乘";
                return "未知";
            }
        }

        /// <summary>
        /// 计划停留时间(分钟)
        /// </summary>
        public int StayTime
        {
            get => _stayTime;
            set
            {
                if (_stayTime != value)
                {
                    _stayTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddTime
        {
            get => _addTime;
            set
            {
                if (_addTime != value)
                {
                    _addTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 距离上一站点距离(公里)
        /// </summary>
        public decimal DistanceFromPrev
        {
            get => _distanceFromPrev;
            set
            {
                if (_distanceFromPrev != value)
                {
                    _distanceFromPrev = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 距起点累计距离(公里)
        /// </summary>
        public decimal DistanceFromStart
        {
            get => _distanceFromStart;
            set
            {
                if (_distanceFromStart != value)
                {
                    _distanceFromStart = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 关联的车站信息
        /// </summary>
        public StationInfo Station
        {
            get => _station;
            set
            {
                if (_station != value)
                {
                    _station = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否为起点站
        /// </summary>
        public bool IsStartStation
        {
            get => (StationRole & 1) != 0;
            set
            {
                if (value)
                {
                    StationRole = (byte)(StationRole | 1); // 设置起点标志位
                }
                else
                {
                    StationRole = (byte)(StationRole & ~1); // 清除起点标志位
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否为终点站
        /// </summary>
        public bool IsEndStation
        {
            get => (StationRole & 2) != 0;
            set
            {
                if (value)
                {
                    StationRole = (byte)(StationRole | 2); // 设置终点标志位
                }
                else
                {
                    StationRole = (byte)(StationRole & ~2); // 清除终点标志位
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否为经停站
        /// </summary>
        public bool IsPassingStation
        {
            get => (StationRole & 4) != 0;
            set
            {
                if (value)
                {
                    StationRole = (byte)(StationRole | 4); // 设置经停标志位
                }
                else
                {
                    StationRole = (byte)(StationRole & ~4); // 清除经停标志位
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否为换乘站
        /// </summary>
        public bool IsTransferStation
        {
            get => (StationRole & 8) != 0;
            set
            {
                if (value)
                {
                    StationRole = (byte)(StationRole | 8); // 设置换乘标志位
                }
                else
                {
                    StationRole = (byte)(StationRole & ~8); // 清除换乘标志位
                }
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}