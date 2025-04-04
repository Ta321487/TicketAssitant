using System.ComponentModel;

namespace TA_WPF.Models
{
    public class StationInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _stationName;
        private string _province;
        private string _city;
        private string _district;
        private string _longitude;
        private string _latitude;
        private string _stationCode;
        private string _stationPinyin;
        private string _stationAddress;
        private string _stationTelephone;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public string StationName
        {
            get => _stationName;
            set
            {
                if (_stationName != value)
                {
                    _stationName = value;
                    OnPropertyChanged(nameof(StationName));
                }
            }
        }

        public string Province
        {
            get => _province;
            set
            {
                if (_province != value)
                {
                    _province = value;
                    OnPropertyChanged(nameof(Province));
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged(nameof(City));
                }
            }
        }

        public string District
        {
            get => _district;
            set
            {
                if (_district != value)
                {
                    _district = value;
                    OnPropertyChanged(nameof(District));
                }
            }
        }

        public string Longitude
        {
            get => _longitude;
            set
            {
                if (_longitude != value)
                {
                    _longitude = value;
                    OnPropertyChanged(nameof(Longitude));
                }
            }
        }

        public string Latitude
        {
            get => _latitude;
            set
            {
                if (_latitude != value)
                {
                    _latitude = value;
                    OnPropertyChanged(nameof(Latitude));
                }
            }
        }

        public string StationCode
        {
            get => _stationCode;
            set
            {
                if (_stationCode != value)
                {
                    _stationCode = value;
                    OnPropertyChanged(nameof(StationCode));
                }
            }
        }

        public string StationPinyin
        {
            get => _stationPinyin;
            set
            {
                if (_stationPinyin != value)
                {
                    _stationPinyin = value;
                    OnPropertyChanged(nameof(StationPinyin));
                }
            }
        }

        public string StationAddress
        {
            get => _stationAddress;
            set
            {
                if (_stationAddress != value)
                {
                    _stationAddress = value;
                    OnPropertyChanged(nameof(StationAddress));
                }
            }
        }

        public string StationTelephone
        {
            get => _stationTelephone;
            set
            {
                if (_stationTelephone != value)
                {
                    _stationTelephone = value;
                    OnPropertyChanged(nameof(StationTelephone));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return StationName;
        }
    }
}