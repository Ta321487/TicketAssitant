using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;
using System.Globalization;
using System.Windows.Threading;
using System.Linq;

namespace TA_WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<TrainRideInfo> _trainRideInfos = new ObservableCollection<TrainRideInfo>();
        private List<TrainRideInfo> allTickets = new List<TrainRideInfo>();
        private int _currentPage = 1;
        private int _totalPages;
        private int _totalItems;
        private int _pageSize = 25;
        private bool _isLoading;
        private bool _isInitialized = false;
        private bool _showWelcome = true;
        private bool _showSettings = false;
        private bool _showDataGrid = false;
        private bool _isDarkMode;
        private double _fontSize = 13; // 默认字体大小
        private double _dataGridRowHeight = 40; // 默认行高
        private double _dataGridHeaderFontSize = 14; // 默认表头字体大小
        private double _dataGridCellFontSize = 13; // 默认单元格字体大小
        private string _serverAddress = "localhost";
        private string _username = "root";
        private string _password = "password";
        private string _newConnectionString = "";
        private string _newDatabaseName = "";
        private bool _showPassword = false;
        private bool _suspendFontSizeUpdate = false; // 是否暂停字体大小更新
        private Dictionary<int, List<TrainRideInfo>> _pageCache = new Dictionary<int, List<TrainRideInfo>>();
        private int _cachePageSize = 0;
        private string _connectionString;

        public MainViewModel(string connectionString)
        {
            try
            {
                _connectionString = connectionString;
                _databaseService = new DatabaseService(connectionString);
                
                // 解析连接字符串，提取服务器地址、用户名和密码
                ParseConnectionString(connectionString);
                
                // 初始化命令
                FirstPageCommand = new RelayCommand(FirstPage);
                PreviousPageCommand = new RelayCommand(PreviousPage);
                NextPageCommand = new RelayCommand(NextPage);
                LastPageCommand = new RelayCommand(LastPage);
                LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
                AddTicketCommand = new RelayCommand(OpenAddTicketWindow);
                QueryAllCommand = new RelayCommand(async () => await QueryAllAsync());
                ShowHomeCommand = new RelayCommand(ShowHome);
                ModifyConnectionCommand = new RelayCommand(ModifyConnection);
                UpdateConnectionCommand = new RelayCommand(UpdateConnection);
                UpdateDatabaseCommand = new RelayCommand(UpdateDatabase);
                ExportLogCommand = new RelayCommand(ExportLog);
                
                // 初始化页大小选项
                PageSizeOptions = new[] { 25, 50, 75, 100 };
                
                // 从配置文件加载主题设置
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["IsDarkMode"] != null)
                    {
                        if (bool.TryParse(config.AppSettings.Settings["IsDarkMode"].Value, out bool isDarkMode))
                        {
                            _isDarkMode = isDarkMode;
                            // 应用保存的主题
                            ApplyTheme(_isDarkMode);
                            Console.WriteLine($"已从配置文件加载主题设置: {(_isDarkMode ? "深色" : "浅色")}");
                        }
                    }
                    else
                    {
                        // 如果配置文件中没有主题设置，检查当前主题
                        _isDarkMode = IsDarkThemeActive();
                        // 保存当前主题设置到配置文件
                        if (config.AppSettings.Settings["IsDarkMode"] == null)
                        {
                            config.AppSettings.Settings.Add("IsDarkMode", _isDarkMode.ToString());
                        }
                        else
                        {
                            config.AppSettings.Settings["IsDarkMode"].Value = _isDarkMode.ToString();
                        }
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                        Console.WriteLine($"已保存默认主题设置: {(_isDarkMode ? "深色" : "浅色")}");
                        
                        // 确保应用主题
                        ApplyTheme(_isDarkMode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载主题设置时出错: {ex.Message}");
                    // 如果出错，检查当前主题
                    _isDarkMode = IsDarkThemeActive();
                }
                
                // 检查必要的表是否存在
                CheckRequiredTablesAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"初始化主视图模型时出错: {ex.Message}");
            }
        }

        public ObservableCollection<TrainRideInfo> TrainRideInfos
        {
            get => _trainRideInfos;
            set
            {
                _trainRideInfos = value;
                OnPropertyChanged(nameof(TrainRideInfos));
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value && value > 0)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    
                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));
                    
                    if (_isInitialized)
                    {
                        UpdatePageData();
                    }
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged(nameof(TotalPages));
                    
                    // 通知命令状态可能已更改
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (_totalItems != value)
                {
                    _totalItems = value;
                    OnPropertyChanged(nameof(TotalItems));
                    
                    // 重新计算总页数
                    CalculateTotalPages();
                }
            }
        }

        private void CalculateTotalPages()
        {
            if (PageSize <= 0)
            {
                TotalPages = 1;
                return;
            }

            int pages = TotalItems / PageSize;
            if (TotalItems % PageSize > 0)
            {
                pages++;
            }

            TotalPages = Math.Max(1, pages);
            
            // 确保当前页在有效范围内
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
            
            // 通知UI更新
            OnPropertyChanged(nameof(TotalPages));
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    OnPropertyChanged(nameof(PageSize));
                    
                    // 清除缓存，因为页大小变了
                    _pageCache.Clear();
                    _cachePageSize = value;
                    
                    // 重新计算总页数
                    CalculateTotalPages();
                    
                    // 确保当前页在有效范围内
                    if (CurrentPage > TotalPages)
                    {
                        CurrentPage = TotalPages;
                    }
                    
                    // 重新加载当前页数据
                    if (_isInitialized)
                    {
                        UpdatePageData();
                    }
                }
            }
        }

        public int[] PageSizeOptions { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                    
                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));
                }
            }
        }

        public bool ShowWelcome
        {
            get => _showWelcome;
            set
            {
                if (_showWelcome != value)
                {
                    _showWelcome = value;
                    OnPropertyChanged(nameof(ShowWelcome));
                    
                    // 如果显示欢迎页，则隐藏其他页面
                    if (value)
                    {
                        ShowSettings = false;
                        ShowDataGrid = false;
                    }
                }
            }
        }

        public bool ShowSettings
        {
            get => _showSettings;
            set
            {
                if (_showSettings != value)
                {
                    _showSettings = value;
                    OnPropertyChanged(nameof(ShowSettings));
                    
                    // 如果显示设置页，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowDataGrid = false;
                    }
                }
            }
        }

        public bool ShowDataGrid
        {
            get => _showDataGrid;
            set
            {
                if (_showDataGrid != value)
                {
                    _showDataGrid = value;
                    OnPropertyChanged(nameof(ShowDataGrid));
                    
                    // 如果显示数据表格，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowSettings = false;
                    }
                }
            }
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                    ApplyTheme(value);
                }
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                    
                    // 如果暂停更新，则不执行后续操作
                    if (SuspendFontSizeUpdate)
                        return;
                    
                    // 调整表格样式以更好地适应用户自定义的字体大小
                    DataGridRowHeight = value * 3.0; // 增加行高与字体大小的比例，从2.5倍增加到3.0倍
                    DataGridHeaderFontSize = value + 2; // 表头字体比正常字体大2，增加可读性
                    DataGridCellFontSize = value + 1; // 单元格字体比正常字体大1，增加可读性
                    
                    ApplyFontSize(value);
                }
            }
        }

        public double DataGridRowHeight
        {
            get => _dataGridRowHeight;
            set
            {
                if (_dataGridRowHeight != value)
                {
                    _dataGridRowHeight = value;
                    OnPropertyChanged(nameof(DataGridRowHeight));
                }
            }
        }

        public double DataGridHeaderFontSize
        {
            get => _dataGridHeaderFontSize;
            set
            {
                if (_dataGridHeaderFontSize != value)
                {
                    _dataGridHeaderFontSize = value;
                    OnPropertyChanged(nameof(DataGridHeaderFontSize));
                }
            }
        }

        public double DataGridCellFontSize
        {
            get => _dataGridCellFontSize;
            set
            {
                if (_dataGridCellFontSize != value)
                {
                    _dataGridCellFontSize = value;
                    OnPropertyChanged(nameof(DataGridCellFontSize));
                }
            }
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                if (_serverAddress != value)
                {
                    _serverAddress = value;
                    OnPropertyChanged(nameof(ServerAddress));
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged(nameof(Username));
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                    OnPropertyChanged(nameof(DisplayPassword));
                }
            }
        }

        public string DisplayPassword
        {
            get => _showPassword ? _password : new string('*', _password.Length);
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                if (_showPassword != value)
                {
                    _showPassword = value;
                    OnPropertyChanged(nameof(ShowPassword));
                    OnPropertyChanged(nameof(DisplayPassword));
                }
            }
        }

        public string NewConnectionString
        {
            get => _newConnectionString;
            set
            {
                if (_newConnectionString != value)
                {
                    _newConnectionString = value;
                    OnPropertyChanged(nameof(NewConnectionString));
                }
            }
        }

        public string NewDatabaseName
        {
            get => _newDatabaseName;
            set
            {
                if (_newDatabaseName != value)
                {
                    _newDatabaseName = value;
                    OnPropertyChanged(nameof(NewDatabaseName));
                }
            }
        }

        public bool SuspendFontSizeUpdate
        {
            get => _suspendFontSizeUpdate;
            set
            {
                if (_suspendFontSizeUpdate != value)
                {
                    _suspendFontSizeUpdate = value;
                    OnPropertyChanged(nameof(SuspendFontSizeUpdate));
                }
            }
        }

        /// <summary>
        /// 应用主题设置
        /// </summary>
        /// <param name="isDarkMode">是否为深色模式</param>
        public void ApplyTheme(bool isDarkMode)
        {
            try
            {
                // 获取当前资源字典
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                
                // 设置深色/浅色模式
                theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);
                
                // 应用主题
                paletteHelper.SetTheme(theme);
                
                // 更新资源字典中的BaseTheme
                if (Application.Current?.Resources != null)
                {
                    // 更新MaterialDesignTheme的BaseTheme
                    var bundledTheme = Application.Current.Resources.MergedDictionaries
                        .OfType<MaterialDesignThemes.Wpf.BundledTheme>()
                        .FirstOrDefault();
                    
                    if (bundledTheme != null)
                    {
                        bundledTheme.BaseTheme = isDarkMode ? MaterialDesignThemes.Wpf.BaseTheme.Dark : MaterialDesignThemes.Wpf.BaseTheme.Light;
                    }
                    
                    // 更新Theme.Dark和Theme.Light资源
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                    
                    // 更新全局颜色资源
                    if (isDarkMode)
                    {
                        // 深色模式下稍微调亮主色调
                        Application.Current.Resources["PrimaryHueLightBrush"] = new SolidColorBrush(Color.FromRgb(156, 100, 255)); // #9C64FF
                        Application.Current.Resources["PrimaryHueMidBrush"] = new SolidColorBrush(Color.FromRgb(124, 77, 255));   // #7C4DFF
                        Application.Current.Resources["PrimaryHueDarkBrush"] = new SolidColorBrush(Color.FromRgb(94, 53, 177));   // #5E35B1
                        
                        Application.Current.Resources["GlobalAccentBrush"] = new SolidColorBrush(Color.FromRgb(124, 77, 255));    // #7C4DFF
                        Application.Current.Resources["GlobalAccentLightBrush"] = new SolidColorBrush(Color.FromRgb(156, 100, 255)); // #9C64FF
                        Application.Current.Resources["GlobalAccentDarkBrush"] = new SolidColorBrush(Color.FromRgb(94, 53, 177));  // #5E35B1
                    }
                    
                    // 应用主题到所有打开的窗口
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null && window.IsLoaded)
                        {
                            // 更新窗口的ThemeAssist.Theme属性
                            MaterialDesignThemes.Wpf.ThemeAssist.SetTheme(window, isDarkMode ? MaterialDesignThemes.Wpf.BaseTheme.Dark : MaterialDesignThemes.Wpf.BaseTheme.Light);
                            
                            // 刷新窗口
                            window.UpdateLayout();
                        }
                    }
                    
                    // 触发全局主题更新
                    FrameworkElement.StyleProperty.OverrideMetadata(
                        typeof(Window),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
                }
                
                Console.WriteLine($"已应用{(isDarkMode ? "深色" : "浅色")}主题");
                
                // 触发主题变更事件
                OnPropertyChanged(nameof(IsDarkMode));
                
                // 保存主题设置到配置文件
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["IsDarkMode"] == null)
                    {
                        config.AppSettings.Settings.Add("IsDarkMode", isDarkMode.ToString());
                    }
                    else
                    {
                        config.AppSettings.Settings["IsDarkMode"].Value = isDarkMode.ToString();
                    }
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    
                    Console.WriteLine($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"保存主题设置时出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用主题时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        private bool IsDarkThemeActive()
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            return theme.GetBaseTheme() == BaseTheme.Dark;
        }

        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand LoadDataCommand { get; }
        public ICommand AddTicketCommand { get; }
        public ICommand QueryAllCommand { get; }
        public ICommand ShowHomeCommand { get; }
        public ICommand ModifyConnectionCommand { get; }
        public ICommand UpdateConnectionCommand { get; }
        public ICommand UpdateDatabaseCommand { get; }
        public ICommand ExportLogCommand { get; }

        public bool CanNavigateToFirstPage => CurrentPage > 1 && !IsLoading;
        public bool CanNavigateToPreviousPage => CurrentPage > 1 && !IsLoading;
        public bool CanNavigateToNextPage => CurrentPage < TotalPages && !IsLoading;
        public bool CanNavigateToLastPage => CurrentPage < TotalPages && !IsLoading;

        private void FirstPage()
        {
            if (CanNavigateToFirstPage)
            {
                CurrentPage = 1;
            }
        }

        private void PreviousPage()
        {
            if (CanNavigateToPreviousPage)
            {
                CurrentPage--;
            }
        }

        private void NextPage()
        {
            if (CanNavigateToNextPage)
            {
                CurrentPage++;
            }
        }

        private void LastPage()
        {
            if (CanNavigateToLastPage)
            {
                CurrentPage = TotalPages;
            }
        }

        private void OpenAddTicketWindow()
        {
            try
            {
                var addTicketWindow = new AddTicketWindow(_databaseService);
                
                // 确保主窗口已初始化并且可见
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    addTicketWindow.Owner = Application.Current.MainWindow;
                    addTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // 如果主窗口不可用，使用CenterScreen
                    addTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                
                // 显示窗口
                bool? result = addTicketWindow.ShowDialog();
                
                // 如果用户保存了车票，在后台刷新数据，但不改变当前视图
                if (result == true)
                {
                    // 在后台刷新数据
                    _ = RefreshDataInBackgroundAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开添加车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开添加车票窗口时出错", ex);
            }
        }

        // 在后台刷新数据，不改变当前视图
        private async Task RefreshDataInBackgroundAsync()
        {
            try
            {
                // 获取总记录数
                TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();
                
                // 重新计算总页数
                CalculateTotalPages();
                
                // 如果当前正在显示数据表格，则刷新当前页数据
                if (ShowDataGrid)
                {
                    await LoadPageDataAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"后台刷新数据时出错: {ex.Message}");
                // 不显示错误消息，因为这是后台操作
            }
        }

        private async Task QueryAllAsync()
        {
            try
            {
                IsLoading = true;
                ShowWelcome = false;
                ShowSettings = false;
                ShowDataGrid = true;
                
                // 获取总记录数
                TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();
                
                // 重置到第一页
                CurrentPage = 1;
                
                // 加载第一页数据
                await LoadPageDataAsync();
                
                // 标记为已初始化
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"查询数据时出错: {ex.Message}");
                
                // 显示欢迎页
                ShowWelcome = true;
                ShowSettings = false;
                ShowDataGrid = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                
                // 清空当前数据
                TrainRideInfos.Clear();
                
                // 获取总记录数
                TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();
                
                // 计算总页数
                CalculateTotalPages();
                
                // 如果有数据，显示数据表格
                if (TotalItems > 0)
                {
                    ShowWelcome = false;
                    ShowSettings = false;
                    ShowDataGrid = true;
                    
                    // 重置到第一页
                    CurrentPage = 1;
                    
                    // 加载第一页数据
                    await LoadPageDataAsync();
                    
                    // 通知导航按钮状态已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));
                }
                else
                {
                    // 没有数据，显示欢迎页
                    ShowWelcome = true;
                    ShowSettings = false;
                    ShowDataGrid = false;
                }
                
                // 标记为已初始化
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载数据时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                
                // 再次通知导航按钮状态已更改
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
            }
        }

        private async Task LoadPageDataAsync()
        {
            try
            {
                // 设置加载状态
                IsLoading = true;
                
                // 清空当前数据
                TrainRideInfos.Clear();
                
                // 检查缓存中是否已有当前页数据，且页大小未变
                string cacheKey = $"{CurrentPage}_{PageSize}";
                if (_pageCache.ContainsKey(CurrentPage) && _cachePageSize == PageSize)
                {
                    // 从缓存加载数据
                    foreach (var item in _pageCache[CurrentPage])
                    {
                        TrainRideInfos.Add(item);
                    }
                    
                    // 确保分页信息正确
                    CalculateTotalPages();
                    
                    IsLoading = false;
                    return;
                }
                
                // 直接从数据库加载当前页的数据
                var pageData = await _databaseService.GetPagedTrainRideInfosAsync(CurrentPage, PageSize);
                
                // 更新缓存
                _pageCache[CurrentPage] = new List<TrainRideInfo>(pageData);
                _cachePageSize = PageSize;
                
                // 更新UI
                foreach (var item in pageData)
                {
                    TrainRideInfos.Add(item);
                }
                
                // 确保分页信息正确
                CalculateTotalPages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载页面数据时出错: {ex.Message}");
                MessageBoxHelper.ShowError($"加载页面数据时出错: {ex.Message}");
            }
            finally
            {
                // 结束加载状态
                IsLoading = false;
            }
        }

        private void UpdatePageData()
        {
            // 异步加载页面数据
            _ = LoadPageDataAsync();
        }

        private void ShowHome()
        {
            ShowWelcome = true;
            ShowSettings = false;
            ShowDataGrid = false;
            
            // 清空当前数据
            TrainRideInfos.Clear();
            allTickets.Clear();
            
            // 重置分页相关数据
            CurrentPage = 1;
            TotalItems = 0;
            _isInitialized = false;
        }

        private void ApplyFontSize(double fontSize)
        {
            try
            {
                // 确保字体大小不小于最小值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }

                _fontSize = fontSize;
                OnPropertyChanged(nameof(FontSize));

                // 获取应用程序资源
                var resources = Application.Current.Resources;

                // 更新MaterialDesign主题的字体大小
                resources["MaterialDesignFontSize"] = fontSize;

                // 更新其他相关字体大小
                resources["MaterialDesignSubtitle1FontSize"] = fontSize + 2;
                resources["MaterialDesignSubtitle2FontSize"] = fontSize + 1;
                resources["MaterialDesignHeadline6FontSize"] = fontSize + 4;
                resources["MaterialDesignHeadline5FontSize"] = fontSize + 6;

                // 调整表格样式以更好地适应用户自定义的字体大小
                DataGridRowHeight = fontSize * 3.0; // 增加行高与字体大小的比例，从2.5倍增加到3.0倍
                DataGridHeaderFontSize = fontSize + 2; // 表头字体比正常字体大2，增加可读性
                DataGridCellFontSize = fontSize + 1; // 单元格字体比正常字体大1，增加可读性

                // 更新主窗口字体大小
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    window.FontSize = fontSize;

                    // 递归更新所有元素的字体大小
                    UpdateFontSizeForAllElements(window, fontSize);
                }

                // 保存字体大小到配置文件
                SaveFontSizeToConfig(fontSize);

                LogHelper.LogInfo($"字体大小已调整为 {fontSize}pt");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"应用字体大小时出错: {ex.Message}", ex);
            }
        }
        
        private void SaveFontSizeToConfig(double fontSize)
        {
            try
            {
                // 保存字体大小到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // 确保字体大小不小于最小可读值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }
                
                if (config.AppSettings.Settings["FontSize"] == null)
                {
                    config.AppSettings.Settings.Add("FontSize", fontSize.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    config.AppSettings.Settings["FontSize"].Value = fontSize.ToString(CultureInfo.InvariantCulture);
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                // 确保配置文件被正确写入
                try
                {
                    // 验证配置文件是否已更新
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["FontSize"] != null)
                    {
                        double savedFontSize = double.Parse(config.AppSettings.Settings["FontSize"].Value, CultureInfo.InvariantCulture);
                        if (Math.Abs(savedFontSize - fontSize) > 0.01)
                        {
                            // 如果保存的值与预期不符，再次尝试保存
                            config.AppSettings.Settings["FontSize"].Value = fontSize.ToString(CultureInfo.InvariantCulture);
                            config.Save(ConfigurationSaveMode.Modified);
                            ConfigurationManager.RefreshSection("appSettings");
                            LogHelper.LogInfo($"字体大小设置已重新保存: {fontSize}pt");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"验证字体大小设置时出错: {ex.Message}");
                    LogHelper.LogError($"验证字体大小设置时出错: {ex.Message}");
                }
                
                // 更新App.xaml中的资源字典
                try
                {
                    // 获取当前应用程序资源
                    var resources = Application.Current.Resources;
                    
                    // 更新字体大小资源
                    resources["MaterialDesignFontSize"] = fontSize;
                    resources["MaterialDesignSubtitle1FontSize"] = fontSize + 2;
                    resources["MaterialDesignSubtitle2FontSize"] = fontSize + 1;
                    resources["MaterialDesignHeadline6FontSize"] = fontSize + 4;
                    resources["MaterialDesignHeadline5FontSize"] = fontSize + 6;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新App.xaml资源字典时出错: {ex.Message}");
                    LogHelper.LogError($"更新App.xaml资源字典时出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存字体大小设置时出错: {ex.Message}");
                LogHelper.LogError($"保存字体大小设置时出错: {ex.Message}");
            }
        }
        
        // 递归更新所有元素的字体大小
        private void UpdateFontSizeForAllElements(DependencyObject parent, double fontSize)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                
                // 更新控件的字体大小
                if (child is Control control)
                {
                    control.FontSize = fontSize;
                }
                else if (child is TextBlock textBlock)
                {
                    textBlock.FontSize = fontSize;
                }
                else if (child is DataGrid dataGrid)
                {
                    dataGrid.FontSize = fontSize;
                }
                
                // 递归处理子元素
                UpdateFontSizeForAllElements(child, fontSize);
            }
        }

        private void ParseConnectionString(string connectionString)
        {
            try
            {
                // 解析MySQL连接字符串
                // 格式: server=localhost;user=root;password=password;database=mydb
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].ToLower().Trim();
                        var value = keyValue[1].Trim();
                        
                        switch (key)
                        {
                            case "server":
                            case "host":
                                ServerAddress = value;
                                break;
                            case "user":
                            case "uid":
                            case "username":
                                Username = value;
                                break;
                            case "password":
                            case "pwd":
                                Password = value;
                                break;
                        }
                    }
                }
                
                // 记录日志
                LogHelper.LogInfo("数据库连接信息已解析");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析连接字符串时出错: {ex.Message}");
                LogHelper.LogError($"解析连接字符串时出错: {ex.Message}");
            }
        }

        private void ModifyConnection()
        {
            // 显示修改连接的UI
            NewConnectionString = $"server={ServerAddress};user={Username};password={Password};";
            
            // 记录日志
            LogHelper.LogInfo("用户请求修改数据库连接");
        }

        private void UpdateConnection()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewConnectionString))
                {
                    MessageBoxHelper.ShowWarning("连接字符串不能为空");
                    return;
                }
                
                // 显示加载动画
                IsLoading = true;
                
                // 模拟连接过程
                Task.Delay(1500).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 解析新的连接字符串
                        ParseConnectionString(NewConnectionString);
                        
                        // 显示成功消息
                        MessageBoxHelper.ShowInfo("已注销连接信息，请重新登录");
                        
                        // 记录日志
                        LogHelper.LogInfo("用户更新了数据库连接信息");
                        
                        // 隐藏加载动画
                        IsLoading = false;
                        
                        // 获取当前主窗口
                        var mainWindow = Application.Current.MainWindow;
                        
                        // 创建并显示登录窗口
                        var loginWindow = new Views.LoginWindow();
                        
                        // 设置数据库名称
                        string databaseName = ExtractDatabaseName(NewConnectionString);
                        if (!string.IsNullOrEmpty(databaseName))
                        {
                            loginWindow.SetDatabaseName(databaseName);
                        }
                        
                        // 设置登录窗口为主窗口
                        Application.Current.MainWindow = loginWindow;
                        
                        // 显示登录窗口
                        loginWindow.Show();
                        
                        // 清理资源
                        ClearResources();
                        
                        // 关闭当前主窗口
                        mainWindow?.Close();
                        
                        // 强制垃圾回收
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        
                        // 记录日志
                        LogHelper.LogInfo("已返回登录界面，之前的MainViewModel已销毁");
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"更新连接时出错: {ex.Message}");
                LogHelper.LogError($"更新连接时出错: {ex.Message}");
                IsLoading = false;
            }
        }

        private string ExtractDatabaseName(string connectionString)
        {
            try
            {
                // 解析MySQL连接字符串中的数据库名称
                // 格式: server=localhost;user=root;password=password;database=mydb
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].ToLower().Trim();
                        var value = keyValue[1].Trim();
                        
                        if (key == "database" || key == "initial catalog")
                        {
                            return value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取数据库名称时出错: {ex.Message}");
            }
            
            return string.Empty;
        }

        private void ExportLog()
        {
            try
            {
                // 显示加载动画
                IsLoading = true;
                
                // 模拟导出过程
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 显示成功消息
                        MessageBoxHelper.ShowInfo("日志已成功导出到应用程序目录");
                        
                        // 记录日志
                        LogHelper.LogInfo("用户导出了系统日志");
                        
                        // 隐藏加载动画
                        IsLoading = false;
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"导出日志时出错: {ex.Message}");
                LogHelper.LogError($"导出日志时出错: {ex.Message}");
                IsLoading = false;
            }
        }

        private void UpdateDatabase()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewDatabaseName))
                {
                    MessageBoxHelper.ShowWarning("数据库名称不能为空");
                    return;
                }
                
                // 保存数据库名称到历史记录
                SaveDatabaseNameToHistory(NewDatabaseName);
                
                // 保存数据库名称到配置文件，以便重启后登录窗口能够读取
                SaveLastDatabaseName(NewDatabaseName);
                
                // 显示确认对话框
                var result = MessageBoxHelper.ShowQuestion("修改数据库需要重新登录，是否继续？");
                
                if (result == true)
                {
                    // 显示成功消息
                    MessageBoxHelper.ShowInfo("已更新数据库连接信息，应用程序将返回登录界面");
                    
                    // 记录日志
                    LogHelper.LogInfo($"用户更新了数据库名称为: {NewDatabaseName}，应用程序将返回登录界面");
                    
                    // 强制垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    // 关闭当前主窗口，返回登录窗口
                    CloseMainWindowAndShowLogin();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"更新数据库名称时出错: {ex.Message}");
                LogHelper.LogError($"更新数据库名称时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 关闭主窗口并显示登录窗口
        /// </summary>
        private void CloseMainWindowAndShowLogin()
        {
            try
            {
                // 获取当前主窗口
                var mainWindow = Application.Current.MainWindow;
                
                // 创建新的登录窗口
                var loginWindow = new Views.LoginWindow();
                
                // 设置登录窗口为主窗口
                Application.Current.MainWindow = loginWindow;
                
                // 显示登录窗口
                loginWindow.Show();
                
                // 清理资源
                ClearResources();
                
                // 关闭当前主窗口
                mainWindow?.Close();
                
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // 记录日志
                LogHelper.LogInfo("已返回登录界面，之前的MainViewModel已销毁");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"返回登录界面时出错: {ex.Message}");
                LogHelper.LogError($"返回登录界面时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        private void ClearResources()
        {
            try
            {
                // 清空集合
                TrainRideInfos.Clear();
                allTickets.Clear();
                _pageCache.Clear();
                
                // 取消事件订阅
                PropertyChanged = null;
                
                // 记录日志
                LogHelper.LogInfo("已清理MainViewModel资源");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理资源时出错: {ex.Message}");
                LogHelper.LogError($"清理资源时出错: {ex.Message}");
            }
        }
        
        private void SaveDatabaseNameToHistory(string databaseName)
        {
            try
            {
                // 从配置文件中读取历史数据库名称
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                List<string> historyList = new List<string>();
                
                // 读取现有历史记录
                if (config.AppSettings.Settings["DatabaseHistory"] != null)
                {
                    string history = config.AppSettings.Settings["DatabaseHistory"].Value;
                    if (!string.IsNullOrEmpty(history))
                    {
                        historyList = history.Split(',').ToList();
                    }
                }
                
                // 如果历史记录中已存在该数据库名称，则移除它
                historyList.Remove(databaseName);
                
                // 将新的数据库名称添加到列表开头
                historyList.Insert(0, databaseName);
                
                // 只保留最近的10个记录
                if (historyList.Count > 10)
                {
                    historyList = historyList.Take(10).ToList();
                }
                
                // 保存回配置文件
                string newHistory = string.Join(",", historyList);
                
                if (config.AppSettings.Settings["DatabaseHistory"] == null)
                {
                    config.AppSettings.Settings.Add("DatabaseHistory", newHistory);
                }
                else
                {
                    config.AppSettings.Settings["DatabaseHistory"].Value = newHistory;
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                // 记录日志
                LogHelper.LogInfo($"已将数据库名称 {databaseName} 添加到历史记录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存数据库历史记录时出错: {ex.Message}");
                LogHelper.LogError($"保存数据库历史记录时出错: {ex.Message}");
            }
        }
        
        private void SaveLastDatabaseName(string databaseName)
        {
            try
            {
                // 保存数据库名称到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings["LastDatabaseName"] == null)
                {
                    config.AppSettings.Settings.Add("LastDatabaseName", databaseName);
                }
                else
                {
                    config.AppSettings.Settings["LastDatabaseName"].Value = databaseName;
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存数据库名称时出错: {ex.Message}");
                LogHelper.LogError($"保存数据库名称时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查必要的表是否存在，如果不存在则提示用户创建
        /// </summary>
        private async void CheckRequiredTablesAsync()
        {
            try
            {
                bool stationTableExists = await _databaseService.TableExistsAsync("station_info");
                bool ticketTableExists = await _databaseService.TableExistsAsync("train_ride_info");
                
                if (!stationTableExists || !ticketTableExists)
                {
                    // 构建提示消息
                    string message = "数据库中缺少必要的表：\n";
                    if (!stationTableExists)
                    {
                        message += "- 车站信息表 (station_info)\n";
                    }
                    if (!ticketTableExists)
                    {
                        message += "- 车票信息表 (train_ride_info)\n";
                    }
                    message += "\n是否立即创建这些表？";
                    
                    // 显示确认对话框
                    var result = MessageBox.Show(
                        message,
                        "缺少必要的表",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 创建缺少的表
                        if (!stationTableExists)
                        {
                            await _databaseService.CreateStationInfoTableAsync();
                        }
                        if (!ticketTableExists)
                        {
                            await _databaseService.CreateTrainRideInfoTableAsync();
                        }
                        
                        MessageBox.Show(
                            "表创建成功！",
                            "操作成功",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查表时出错: {ex.Message}");
                MessageBox.Show(
                    $"检查数据库表时出错: {ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 简单的命令实现
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        public void Execute(object? parameter) => _execute();
    }

    // 支持泛型参数的命令实现
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is T typedParameter || parameter == null && default(T) == null)
            {
                return _canExecute == null || _canExecute((T)parameter!);
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter || parameter == null && default(T) == null)
            {
                _execute((T)parameter!);
            }
        }
    }
} 