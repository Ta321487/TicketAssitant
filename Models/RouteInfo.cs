using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TA_WPF.Models
{
    public class RouteInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _routeName;
        private string _description;
        private byte[] _coverImage;
        private DateTime _createTime;
        private DateTime _updateTime;
        private decimal _totalDistance;
        private bool _isFavorite;
        private int _sortOrder;
        private bool _isSelected;

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

        public string RouteName
        {
            get => _routeName;
            set
            {
                if (_routeName != value)
                {
                    _routeName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public byte[] CoverImage
        {
            get => _coverImage;
            set
            {
                if (_coverImage != value)
                {
                    _coverImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreateTime
        {
            get => _createTime;
            set
            {
                if (_createTime != value)
                {
                    _createTime = value;
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

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
                    OnPropertyChanged();
                }
            }
        }

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