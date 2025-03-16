using System;
using System.ComponentModel;

namespace TA_WPF.Models
{
    public class TrainRideInfo : INotifyPropertyChanged
    {
        private int _id;
        private string? _ticketNumber;
        private string? _checkInLocation;
        private string? _departStation;
        private string? _trainNo;
        private string? _arriveStation;
        private string? _departStationPinyin;
        private string? _arriveStationPinyin;
        private DateTime? _departDate;
        private TimeSpan? _departTime;
        private string? _coachNo;
        private string? _seatNo;
        private decimal? _money;
        private string? _seatType;
        private string? _additionalInfo;
        private string? _ticketPurpose;
        private string? _hint;
        private string? _departStationCode;
        private string? _arriveStationCode;
        private bool _isSelected;

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

        public string? TicketNumber 
        { 
            get => _ticketNumber; 
            set 
            { 
                if (_ticketNumber != value)
                {
                    _ticketNumber = value;
                    OnPropertyChanged(nameof(TicketNumber));
                }
            } 
        }

        public string? CheckInLocation 
        { 
            get => _checkInLocation; 
            set 
            { 
                if (_checkInLocation != value)
                {
                    _checkInLocation = value;
                    OnPropertyChanged(nameof(CheckInLocation));
                }
            } 
        }

        public string? DepartStation 
        { 
            get => _departStation; 
            set 
            { 
                if (_departStation != value)
                {
                    _departStation = value;
                    OnPropertyChanged(nameof(DepartStation));
                }
            } 
        }

        public string? TrainNo 
        { 
            get => _trainNo; 
            set 
            { 
                if (_trainNo != value)
                {
                    _trainNo = value;
                    OnPropertyChanged(nameof(TrainNo));
                }
            } 
        }

        public string? ArriveStation 
        { 
            get => _arriveStation; 
            set 
            { 
                if (_arriveStation != value)
                {
                    _arriveStation = value;
                    OnPropertyChanged(nameof(ArriveStation));
                }
            } 
        }

        public string? DepartStationPinyin 
        { 
            get => _departStationPinyin; 
            set 
            { 
                if (_departStationPinyin != value)
                {
                    _departStationPinyin = value;
                    OnPropertyChanged(nameof(DepartStationPinyin));
                }
            } 
        }

        public string? ArriveStationPinyin 
        { 
            get => _arriveStationPinyin; 
            set 
            { 
                if (_arriveStationPinyin != value)
                {
                    _arriveStationPinyin = value;
                    OnPropertyChanged(nameof(ArriveStationPinyin));
                }
            } 
        }

        public DateTime? DepartDate 
        { 
            get => _departDate; 
            set 
            { 
                if (_departDate != value)
                {
                    _departDate = value;
                    OnPropertyChanged(nameof(DepartDate));
                }
            } 
        }

        public TimeSpan? DepartTime 
        { 
            get => _departTime; 
            set 
            { 
                if (_departTime != value)
                {
                    _departTime = value;
                    OnPropertyChanged(nameof(DepartTime));
                }
            } 
        }

        public string? CoachNo 
        { 
            get => _coachNo; 
            set 
            { 
                if (_coachNo != value)
                {
                    _coachNo = value;
                    OnPropertyChanged(nameof(CoachNo));
                }
            } 
        }

        public string? SeatNo 
        { 
            get => _seatNo; 
            set 
            { 
                if (_seatNo != value)
                {
                    _seatNo = value;
                    OnPropertyChanged(nameof(SeatNo));
                }
            } 
        }

        public decimal? Money 
        { 
            get => _money; 
            set 
            { 
                if (_money != value)
                {
                    _money = value;
                    OnPropertyChanged(nameof(Money));
                }
            } 
        }

        public string? SeatType 
        { 
            get => _seatType; 
            set 
            { 
                if (_seatType != value)
                {
                    _seatType = value;
                    OnPropertyChanged(nameof(SeatType));
                }
            } 
        }

        public string? AdditionalInfo 
        { 
            get => _additionalInfo; 
            set 
            { 
                if (_additionalInfo != value)
                {
                    _additionalInfo = value;
                    OnPropertyChanged(nameof(AdditionalInfo));
                }
            } 
        }

        public string? TicketPurpose 
        { 
            get => _ticketPurpose; 
            set 
            { 
                if (_ticketPurpose != value)
                {
                    _ticketPurpose = value;
                    OnPropertyChanged(nameof(TicketPurpose));
                }
            } 
        }

        public string? Hint 
        { 
            get => _hint; 
            set 
            { 
                if (_hint != value)
                {
                    _hint = value;
                    OnPropertyChanged(nameof(Hint));
                }
            } 
        }

        public string? DepartStationCode 
        { 
            get => _departStationCode; 
            set 
            { 
                if (_departStationCode != value)
                {
                    _departStationCode = value;
                    OnPropertyChanged(nameof(DepartStationCode));
                }
            } 
        }

        public string? ArriveStationCode 
        { 
            get => _arriveStationCode; 
            set 
            { 
                if (_arriveStationCode != value)
                {
                    _arriveStationCode = value;
                    OnPropertyChanged(nameof(ArriveStationCode));
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
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 