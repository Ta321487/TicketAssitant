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
using System.Collections.ObjectModel;

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
        private bool _isFormModified = false; // 表单修改标志
        private bool _isInitializing = true; // 初始化标志

        // 车站信息属性
        private string _stationName;
        private string _province;
        private string _city;
        private string _district;
        private string _longitude;
        private string _latitude;
        private string _stationPinyin;
        private string _stationCode;
        
        // 原始车站信息（用于比较变更）
        private StationInfo _originalStationInfo;
        
        // 新增属性
        private int _stationLevel;
        private string _railwayBureau;
        private ObservableCollection<KeyValuePair<int, string>> _stationLevels;
        private ObservableCollection<string> _railwayBureauSuggestions;
        private string _railwayBureauInput;
        private bool _isRailwayBureauDropdownOpen;

        public EditStationViewModel(DatabaseService databaseService, StationSearchService stationSearchService, GeocodingService geocodingService, ConfigurationService configurationService, StationInfo stationToEdit, Action refreshCallback)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));
            _geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _stationToEdit = stationToEdit ?? throw new ArgumentNullException(nameof(stationToEdit));
            _refreshCallback = refreshCallback;
            
            // 保存原始车站信息（用于比较变更）
            _originalStationInfo = new StationInfo
            {
                StationName = stationToEdit.StationName,
                Province = stationToEdit.Province,
                City = stationToEdit.City,
                District = stationToEdit.District,
                Longitude = stationToEdit.Longitude,
                Latitude = stationToEdit.Latitude,
                StationPinyin = stationToEdit.StationPinyin,
                StationCode = stationToEdit.StationCode,
                StationLevel = stationToEdit.StationLevel,
                RailwayBureau = stationToEdit.RailwayBureau
            };

            // 初始化车站等级列表
            _stationLevels = new ObservableCollection<KeyValuePair<int, string>>(StationLevelHelper.GetStationLevels());
            
            // 初始化铁路局建议列表
            _railwayBureauSuggestions = new ObservableCollection<string>();

            // 加载待编辑的车站信息
            LoadStationInfo();

            // 初始化命令
            SaveCommand = new RelayCommand(SaveStation, () => CanSaveStation);
            CancelCommand = new RelayCommand(Cancel);
            GetStationInfoCommand = new RelayCommand(GetStationInfo);
            RailwayBureauTextChangedCommand = new RelayCommand<string>(OnRailwayBureauTextChanged);

            // 设置窗口标题
            WindowTitle = $"编辑车站 - {_stationName}";
            
            // 从应用程序资源获取当前字体大小
            if (Application.Current?.Resources != null && 
                Application.Current.Resources.Contains("MaterialDesignFontSize"))
            {
                _fontSize = (double)Application.Current.Resources["MaterialDesignFontSize"];
                OnPropertyChanged(nameof(FontSize));
            }
            
            // 初始化完成
            _isInitializing = false;
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
        
        // 新增属性
        public int StationLevel
        {
            get => _stationLevel;
            set
            {
                if (_stationLevel != value)
                {
                    _stationLevel = value;
                    OnPropertyChanged(nameof(StationLevel));
                }
            }
        }
        
        public string RailwayBureau
        {
            get => _railwayBureau;
            set
            {
                if (_railwayBureau != value)
                {
                    _railwayBureau = value;
                    _railwayBureauInput = value; // 同步更新输入框文本
                    OnPropertyChanged(nameof(RailwayBureau));
                    OnPropertyChanged(nameof(RailwayBureauInput));
                }
            }
        }
        
        public string RailwayBureauInput
        {
            get => _railwayBureauInput;
            set
            {
                if (_railwayBureauInput != value)
                {
                    _railwayBureauInput = value;
                    OnPropertyChanged(nameof(RailwayBureauInput));
                    
                    // 当用户输入时，更新建议列表
                    UpdateRailwayBureauSuggestions(value);
                }
            }
        }
        
        public ObservableCollection<KeyValuePair<int, string>> StationLevels
        {
            get => _stationLevels;
        }
        
        public ObservableCollection<string> RailwayBureauSuggestions
        {
            get => _railwayBureauSuggestions;
        }
        
        public bool IsRailwayBureauDropdownOpen
        {
            get => _isRailwayBureauDropdownOpen;
            set
            {
                if (_isRailwayBureauDropdownOpen != value)
                {
                    _isRailwayBureauDropdownOpen = value;
                    OnPropertyChanged(nameof(IsRailwayBureauDropdownOpen));
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
        
        // 添加表单修改状态属性
        public bool IsFormModified
        {
            get => _isFormModified;
            private set
            {
                if (_isFormModified != value)
                {
                    _isFormModified = value;
                    OnPropertyChanged(nameof(IsFormModified));
                }
            }
        }

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand GetStationInfoCommand { get; }
        public ICommand RailwayBureauTextChangedCommand { get; }

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
                StationLevel = _stationToEdit.StationLevel;
                RailwayBureau = _stationToEdit.RailwayBureau;
                RailwayBureauInput = _stationToEdit.RailwayBureau;
            }
        }

        private async void SaveStation()
        {
            IsEditing = true;

            try
            {
                // 1. 验证车站名称是否存在于列表中
                await _stationSearchService.EnsureInitializedAsync();
                
                // 检查是否为当前编辑的车站
                bool isCurrentStation = _stationToEdit.StationName == StationName || 
                                        _stationToEdit.StationName == StationName + "站" || 
                                        StationName == _stationToEdit.StationName + "站";
                
                // 如果不是当前编辑的车站，则检查是否已存在
                if (!isCurrentStation)
                {
                    bool existsInList = _stationSearchService.Stations
                        .Any(s => s.StationName == StationName || 
                                  s.StationName == StationName + "站" || 
                                  StationName == s.StationName + "站");
                                  
                    if (!existsInList)
                    {
                        MessageBoxHelper.ShowWarning($"车站名称 '{StationName}' 不在车站列表中，请先添加该车站或选择已有车站。");
                        IsEditing = false;
                        return;
                    }
                }
                
                // 检查是当前编辑的车站之外的其他车站是否已经使用了相同的名称
                bool isDuplicateName = _stationSearchService.Stations
                    .Any(s => s.Id != _stationToEdit.Id && 
                              (s.StationName == StationName || 
                               s.StationName == StationName + "站" || 
                               StationName == s.StationName + "站"));
                               
                if (isDuplicateName)
                {
                    MessageBoxHelper.ShowWarning($"车站名称 '{StationName}' 已存在，请使用其他名称。");
                    IsEditing = false;
                    return;
                }
                
                // 2. 验证铁路局名称
                if (!string.IsNullOrWhiteSpace(RailwayBureauInput))
                {
                    string validBureau = RailwayBureauHelper.GetClosestRailwayBureau(RailwayBureauInput);
                    if (validBureau != null)
                    {
                        RailwayBureau = validBureau;
                    }
                    else
                    {
                        // 如果没有找到匹配的铁路局，直接报错
                        MessageBoxHelper.ShowError($"输入的铁路局 '{RailwayBureauInput}' 不是标准的铁路局名称，请选择列表中的铁路局。");
                        IsEditing = false;
                        return;
                    }
                }

                // 更新StationInfo对象
                _stationToEdit.StationName = StationName;
                _stationToEdit.Province = Province;
                _stationToEdit.City = City;
                _stationToEdit.District = District;
                _stationToEdit.Longitude = Longitude;
                _stationToEdit.Latitude = Latitude;
                _stationToEdit.StationPinyin = StationPinyin;
                _stationToEdit.StationCode = StationCode;
                _stationToEdit.StationLevel = StationLevel;
                _stationToEdit.RailwayBureau = RailwayBureau;

                // 使用数据库服务更新车站信息
                bool success = await _databaseService.UpdateStationAsync(_stationToEdit);

                if (success)
                {
                    // 更新成功，关闭窗口
                    MessageBoxHelper.ShowInfo("车站信息更新成功！");
                    
                    // 调用回调函数刷新车站列表
                    _refreshCallback?.Invoke();
                    
                    // 重置表单修改状态
                    IsFormModified = false;
                    
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
            // 检查是否有未保存的更改
            if (HasUnsavedChanges())
            {
                // 显示确认对话框
                var result = MessageBoxHelper.ShowConfirmation(
                    "您有未保存的修改，是否保存？",
                    "未保存的修改");
                    
                if (result == MessageBoxResult.Yes)
                {
                    // 保存更改
                    SaveStation();
                    return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // 取消关闭
                    return;
                }
                // 否则继续关闭窗口，不保存更改
            }
            
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
        
        /// <summary>
        /// 处理铁路局输入框文本变化
        /// </summary>
        private void OnRailwayBureauTextChanged(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
                
            // 设置铁路局值
            RailwayBureauInput = text;
            RailwayBureau = text;
            
            // 关闭下拉框
            IsRailwayBureauDropdownOpen = false;
        }
        
        /// <summary>
        /// 更新铁路局建议列表
        /// </summary>
        private void UpdateRailwayBureauSuggestions(string inputText)
        {
            RailwayBureauSuggestions.Clear();
            
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                var suggestions = RailwayBureauHelper.GetMatchedRailwayBureaus(inputText);
                foreach (var suggestion in suggestions)
                {
                    RailwayBureauSuggestions.Add(suggestion);
                }
                
                IsRailwayBureauDropdownOpen = suggestions.Count > 0;
            }
            else
            {
                IsRailwayBureauDropdownOpen = false;
            }
        }
        
        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges()
        {
            // 如果表单已标记为已修改，则直接返回true
            if (IsFormModified) return true;
            
            // 比较当前值和原始值
            return 
                _originalStationInfo.StationName != StationName ||
                _originalStationInfo.Province != Province ||
                _originalStationInfo.City != City ||
                _originalStationInfo.District != District ||
                _originalStationInfo.Longitude != Longitude ||
                _originalStationInfo.Latitude != Latitude ||
                _originalStationInfo.StationPinyin != StationPinyin ||
                _originalStationInfo.StationCode != StationCode ||
                _originalStationInfo.StationLevel != StationLevel ||
                _originalStationInfo.RailwayBureau != RailwayBureau;
        }

        #endregion

        // 关闭窗口事件
        public event EventHandler CloseWindow;
        
        /// <summary>
        /// 重写OnPropertyChanged方法，以跟踪表单修改状态
        /// </summary>
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            
            // 当属性发生更改且不在初始化阶段且不是IsFormModified属性本身时，
            // 标记表单已被修改
            if (!_isInitializing && propertyName != nameof(IsFormModified) 
                                 && propertyName != nameof(IsRailwayBureauDropdownOpen))
            {
                IsFormModified = true;
            }
        }
    }
} 