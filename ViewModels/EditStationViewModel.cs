using TA_WPF.ViewModels;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using System.Threading.Tasks;
using System.Windows;
using System;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class EditStationViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly StationSearchService _stationSearchService;
        private StationInfo _stationToEdit;
        private bool _isEditing;
        private string _windowTitle;
        private Action _refreshCallback;

        // 车站信息属性
        private string _stationName;
        private string _province;
        private string _city;
        private string _district;
        private string _longitude;
        private string _latitude;
        private string _stationPinyin;
        private string _stationCode;

        public EditStationViewModel(DatabaseService databaseService, StationSearchService stationSearchService, StationInfo stationToEdit, Action refreshCallback)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));
            _stationToEdit = stationToEdit ?? throw new ArgumentNullException(nameof(stationToEdit));
            _refreshCallback = refreshCallback;

            // 加载待编辑的车站信息
            LoadStationInfo();

            // 初始化命令
            SaveCommand = new RelayCommand(SaveStation, () => CanSaveStation);
            CancelCommand = new RelayCommand(Cancel);
            GetStationInfoCommand = new RelayCommand(GetStationInfo);

            // 设置窗口标题
            WindowTitle = $"编辑车站 - {_stationName}";
        }

        #region 属性

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged(nameof(WindowTitle));
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

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }

        // 添加CanSaveStation属性
        public bool CanSaveStation
        {
            get => !string.IsNullOrWhiteSpace(StationName) && 
                   !string.IsNullOrWhiteSpace(StationCode) && 
                   !string.IsNullOrWhiteSpace(StationPinyin);
        }

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand GetStationInfoCommand { get; }

        #endregion

        #region 命令方法

        private void LoadStationInfo()
        {
            if (_stationToEdit != null)
            {
                StationName = _stationToEdit.StationName;
                Province = _stationToEdit.Province;
                City = _stationToEdit.City;
                District = _stationToEdit.District;
                Longitude = _stationToEdit.Longitude;
                Latitude = _stationToEdit.Latitude;
                StationPinyin = _stationToEdit.StationPinyin;
                StationCode = _stationToEdit.StationCode;
            }
        }

        private async void SaveStation()
        {
            IsEditing = true;

            try
            {
                // 更新StationInfo对象
                _stationToEdit.StationName = StationName;
                _stationToEdit.Province = Province;
                _stationToEdit.City = City;
                _stationToEdit.District = District;
                _stationToEdit.Longitude = Longitude;
                _stationToEdit.Latitude = Latitude;
                _stationToEdit.StationPinyin = StationPinyin;
                _stationToEdit.StationCode = StationCode;

                // 使用数据库服务更新车站信息
                bool success = await _databaseService.UpdateStationAsync(_stationToEdit);

                if (success)
                {
                    // 更新成功，关闭窗口
                    MessageBoxHelper.ShowInfo("车站信息更新成功！");
                    
                    // 调用回调函数刷新车站列表
                    _refreshCallback?.Invoke();
                    
                    // 关闭窗口
                    CloseWindow?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBoxHelper.ShowError("更新车站信息失败，请重试。");
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"更新车站信息时发生错误：{ex.Message}");
            }
            finally
            {
                IsEditing = false;
            }
        }

        private void Cancel()
        {
            // 关闭窗口
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        private void GetStationInfo()
        {
            // 这里可以添加获取车站信息的逻辑，如通过API获取地理位置信息等
            MessageBoxHelper.ShowInfo("获取车站信息功能待实现");
        }

        #endregion

        // 关闭窗口事件
        public event EventHandler CloseWindow;
    }
} 