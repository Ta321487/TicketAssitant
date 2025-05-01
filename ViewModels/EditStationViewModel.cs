using TA_WPF.ViewModels;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using System.Threading.Tasks;
using System.Windows;
using System;
using TA_WPF.Utils;
using System.Collections.Generic;
using System.Linq;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    public class EditStationViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly StationSearchService _stationSearchService;
        private readonly GeocodingService _geocodingService;
        private readonly ConfigurationService _configurationService;
        private StationInfo _stationToEdit;
        private bool _isEditing;
        private string _windowTitle;
        private Action _refreshCallback;
        private double _fontSize;  // 添加字体大小属性

        // 车站信息属性
        private string _stationName;
        private string _province;
        private string _city;
        private string _district;
        private string _longitude;
        private string _latitude;
        private string _stationPinyin;
        private string _stationCode;

        public EditStationViewModel(DatabaseService databaseService, StationSearchService stationSearchService, GeocodingService geocodingService, ConfigurationService configurationService, StationInfo stationToEdit, Action refreshCallback)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));
            _geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
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
            
            // 从应用程序资源获取当前字体大小
            if (Application.Current?.Resources != null && 
                Application.Current.Resources.Contains("MaterialDesignFontSize"))
            {
                _fontSize = (double)Application.Current.Resources["MaterialDesignFontSize"];
                OnPropertyChanged(nameof(FontSize));
            }
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

        // 添加FontSize属性
        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                }
            }
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

        private async void GetStationInfo()
        {
            // 检查车站名称是否为空
            if (string.IsNullOrWhiteSpace(StationName))
            {
                MessageBoxHelper.ShowWarning("请先输入车站名称");
                return;
            }

            IsEditing = true;

            try
            {
                // 检查API密钥是否配置
                string apiKey = _configurationService.GetSettingValue("AmapWebServiceKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBoxHelper.ShowWarning("尚未配置高德地图API密钥，请先到系统设置中添加相关信息。\n\n路径：设置 → 服务设置 → Web服务【获取车站信息功能】");
                    IsEditing = false;
                    return;
                }

                // 调用高德地图API获取车站信息
                var geocodeResults = await _geocodingService.GetGeocodingAsync(StationName);

                // 检查是否有结果
                if (geocodeResults == null || geocodeResults.Count == 0)
                {
                    MessageBoxHelper.ShowWarning($"未找到'{StationName}'的地理位置信息，请检查车站名称是否正确。");
                    IsEditing = false;
                    return;
                }

                // 处理多个结果的情况
                GeocodeResult selectedResult;
                if (geocodeResults.Count > 1)
                {
                    // 创建选项列表
                    var items = geocodeResults.Select(g => g.FormattedAddress).ToList();
                    var dialog = new SelectDialog(items, "请选择正确的地址信息");
                    
                    // 显示对话框
                    if (dialog.ShowDialog() == true && dialog.SelectedIndex >= 0)
                    {
                        selectedResult = geocodeResults[dialog.SelectedIndex];
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("必须选择一个地址信息才能继续");
                        IsEditing = false;
                        return;
                    }
                }
                else
                {
                    // 只有一个结果，直接使用
                    selectedResult = geocodeResults[0];
                }

                // 更新车站信息
                Province = selectedResult.Province;
                City = selectedResult.City;
                District = selectedResult.District;
                Longitude = selectedResult.Longitude;
                Latitude = selectedResult.Latitude;

                MessageBoxHelper.ShowInfo("已成功获取车站地理位置信息");
            }
            catch (Exception ex)
            {
                // 针对常见错误给出更明确的提示
                if (ex.Message.Contains("API密钥不正确") || ex.Message.Contains("INVALID_USER_KEY"))
                {
                    MessageBoxHelper.ShowError("高德地图API密钥无效，请在系统设置中更新正确的API密钥。\n\n路径：设置 → 服务设置 → Web服务【获取车站信息功能】");
                }
                else if (ex.Message.Contains("超出") || ex.Message.Contains("限制"))
                {
                    MessageBoxHelper.ShowError("高德地图API访问次数超出限制，请稍后再试或升级您的API密钥。");
                }
                else
                {
                    MessageBoxHelper.ShowError($"获取车站信息时发生错误：{ex.Message}");
                }
                
                LogHelper.LogError($"获取车站信息失败: {ex.Message}", ex);
            }
            finally
            {
                IsEditing = false;
            }
        }

        #endregion

        // 关闭窗口事件
        public event EventHandler CloseWindow;
    }
} 