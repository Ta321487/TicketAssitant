using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TA_WPF.Models
{
    /// <summary>
    /// 路线与车票的映射关系模型
    /// </summary>
    public class RouteTicketMapping : INotifyPropertyChanged
    {
        private int _id;
        private int _routeId;
        private int _ticketId;
        private DateTime _addTime;
        private TrainRideInfo _ticket;
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
        /// 车票ID
        /// </summary>
        public int TicketId
        {
            get => _ticketId;
            set
            {
                if (_ticketId != value)
                {
                    _ticketId = value;
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
        /// 关联的车票信息
        /// </summary>
        public TrainRideInfo Ticket
        {
            get => _ticket;
            set
            {
                if (_ticket != value)
                {
                    _ticket = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}