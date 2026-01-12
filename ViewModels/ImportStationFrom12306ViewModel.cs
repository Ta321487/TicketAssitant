using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class ImportStationFrom12306ViewModel : BaseViewModel
    {
        private readonly StationImportService _stationImportService;
        private readonly MainViewModel _mainViewModel;
        private bool _isImporting;
        private int _importProgress;
        private int _totalStations;
        private string _statusMessage;
        private string _importSummary;
        private bool _hasImportResult;
        private bool _dataChanged = false; // 标记数据是否被修改
        private List<StationInfo> _importedStations; // 保存导入的车站信息
        private bool _shouldGuideToClose = false; // 引导用户点击关闭按钮
        private CancellationTokenSource _cancellationTokenSource; // 添加取消令牌源

        // 添加导入完成后的刷新回调
        public Action DataRefreshCallback { get; set; }

        // 记录已导入的车站ID，用于回滚
        private List<int> _importedStationIds = new List<int>();

        public ImportStationFrom12306ViewModel(StationImportService stationImportService, MainViewModel mainViewModel)
        {
            _stationImportService = stationImportService ?? throw new ArgumentNullException(nameof(stationImportService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // 初始化命令
            ImportCommand = new RelayCommand(ImportStations, CanImportStations);
            CloseCommand = new RelayCommand<Window>(CloseWindow);
        }

        #region 属性

        /// <summary>
        /// 是否正在导入
        /// </summary>
        public bool IsImporting
        {
            get => _isImporting;
            set
            {
                if (_isImporting != value)
                {
                    _isImporting = value;
                    OnPropertyChanged(nameof(IsImporting));
                    OnPropertyChanged(nameof(IsNotImporting));
                }
            }
        }

        /// <summary>
        /// 是否不在导入状态（用于UI绑定）
        /// </summary>
        public bool IsNotImporting => !_isImporting;

        /// <summary>
        /// 是否应该引导用户点击关闭按钮
        /// </summary>
        public bool ShouldGuideToClose
        {
            get => _shouldGuideToClose;
            set
            {
                if (_shouldGuideToClose != value)
                {
                    _shouldGuideToClose = value;
                    OnPropertyChanged(nameof(ShouldGuideToClose));
                }
            }
        }

        /// <summary>
        /// 导入进度（百分比）
        /// </summary>
        public int ImportProgress
        {
            get => _importProgress;
            set
            {
                if (_importProgress != value)
                {
                    _importProgress = value;
                    OnPropertyChanged(nameof(ImportProgress));
                }
            }
        }

        /// <summary>
        /// 总车站数量
        /// </summary>
        public int TotalStations
        {
            get => _totalStations;
            set
            {
                if (_totalStations != value)
                {
                    _totalStations = value;
                    OnPropertyChanged(nameof(TotalStations));
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        /// <summary>
        /// 导入摘要
        /// </summary>
        public string ImportSummary
        {
            get => _importSummary;
            set
            {
                if (_importSummary != value)
                {
                    _importSummary = value;
                    OnPropertyChanged(nameof(ImportSummary));
                }
            }
        }

        /// <summary>
        /// 是否有导入结果
        /// </summary>
        public bool HasImportResult
        {
            get => _hasImportResult;
            set
            {
                if (_hasImportResult != value)
                {
                    _hasImportResult = value;
                    OnPropertyChanged(nameof(HasImportResult));
                }
            }
        }

        #endregion

        #region 命令

        public ICommand ImportCommand { get; }
        public ICommand CloseCommand { get; }

        #endregion

        #region 命令方法

        /// <summary>
        /// 是否可以导入车站
        /// </summary>
        /// <returns>是否可导入</returns>
        private bool CanImportStations() => !IsImporting;

        /// <summary>
        /// 导入车站
        /// </summary>
        private async void ImportStations()
        {
            try
            {
                // 创建新的取消令牌源
                _cancellationTokenSource = new CancellationTokenSource();

                // 重置状态
                IsImporting = true;
                ImportProgress = 0;
                TotalStations = 0;
                StatusMessage = "正在从12306获取车站数据...";
                HasImportResult = false;
                ImportSummary = string.Empty;
                _importedStations = new List<StationInfo>(); // 重置导入车站列表

                // 获取12306车站数据
                string stationData = await _stationImportService.FetchStationDataAsync();

                // 如果在获取数据后已经请求取消，则直接返回
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    StatusMessage = "导入已取消";
                    IsImporting = false;
                    return;
                }

                // 解析车站数据
                StatusMessage = "正在解析车站数据...";
                List<StationInfo> stations = _stationImportService.ParseStationData(stationData);
                TotalStations = stations.Count;

                // 如果在解析数据后已经请求取消，则直接返回
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    StatusMessage = "导入已取消";
                    IsImporting = false;
                    return;
                }

                // 检查数据一致性
                StatusMessage = "正在检查数据一致性...";
                var (officialCount, databaseCount, missingInDatabase, extraInDatabase, extraStationCodes, missingStationCodes) = 
                    await _stationImportService.CheckDataConsistencyAsync();

                // 构建确认消息
                string confirmMessage = $"从12306获取到{TotalStations}个车站信息。\n\n";
                confirmMessage += $"数据统计：\n";
                confirmMessage += $"  官方车站数：{officialCount}\n";
                confirmMessage += $"  数据库当前数：{databaseCount}\n";
                
                if (extraInDatabase > 0)
                {
                    confirmMessage += $"  数据库中有{extraInDatabase}个车站不在官方数据中\n";
                }
                
                if (missingInDatabase > 0)
                {
                    confirmMessage += $"  官方数据中有{missingInDatabase}个车站不在数据库中\n";
                }
                
                confirmMessage += $"\n是否导入并同步数据？\n";
                if (extraInDatabase > 0)
                {
                    confirmMessage += $"（将删除{extraInDatabase}个不在官方数据中的车站）";
                }

                // 确认导入
                if (TotalStations > 0)
                {
                    var confirmResult = MessageBoxHelper.ShowConfirmation(
                        confirmMessage,
                        "确认导入并同步");

                    if (confirmResult != MessageBoxResult.Yes)
                    {
                        StatusMessage = "导入已取消";
                        IsImporting = false;
                        return;
                    }

                    // 如果在确认导入后已经请求取消，则直接返回
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        StatusMessage = "导入已取消";
                        IsImporting = false;
                        return;
                    }

                    // 开始导入
                    StatusMessage = "正在导入车站数据...";
                    var (total, imported, skipped, newStations, importedIds) = await _stationImportService.ImportStationsAsync(
                        stations,
                        UpdateProgress,
                        _cancellationTokenSource.Token);

                    // 更新已导入的站点ID
                    _importedStationIds.Clear();
                    _importedStationIds.AddRange(importedIds);

                    // 如果数据库中有不在官方数据中的车站，进行同步删除
                    int deletedCount = 0;
                    if (extraInDatabase > 0)
                    {
                        StatusMessage = $"正在同步数据，删除{extraInDatabase}个不在官方数据中的车站...";
                        var (deleted, deletedStationNames) = await _stationImportService.SyncWithOfficialDataAsync(
                            UpdateProgress,
                            _cancellationTokenSource.Token);
                        
                        deletedCount = deleted;
                        if (deletedCount > 0)
                        {
                            LogHelper.LogInfo($"已删除{deletedCount}个不在12306官方数据中的车站");
                        }
                    }

                    // 标记数据已变更（只要有导入成功的车站或删除的车站）
                    _dataChanged = imported > 0 || deletedCount > 0;
                    
                    // 如果数据已变更，立即触发刷新回调，更新总记录数
                    if (_dataChanged && DataRefreshCallback != null)
                    {
                        try
                        {
                            DataRefreshCallback.Invoke();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogWarning($"触发数据刷新回调时出错: {ex.Message}");
                        }
                    }

                    // 显示导入结果
                    StatusMessage = "导入完成！请点击【关闭】按钮返回。";
                    HasImportResult = true;
                    ShouldGuideToClose = true;

                    // 保存导入的车站列表，便于后续操作
                    foreach (var stationName in newStations)
                    {
                        var station = stations.FirstOrDefault(s => s.StationName == stationName);
                        if (station != null)
                        {
                            _importedStations.Add(station);
                        }
                    }

                    // 再次检查数据一致性，获取最终统计
                    var (finalOfficialCount, finalDatabaseCount, finalMissing, finalExtra, _, _) = 
                        await _stationImportService.CheckDataConsistencyAsync();

                    string newStationsText = string.Empty;
                    if (imported > 0 && newStations.Count > 0)
                    {
                        // 限制显示的新增车站数量，避免界面过长
                        int showLimit = Math.Min(newStations.Count, 20);
                        newStationsText = $"\n\n新增车站（显示前{showLimit}个）：\n";
                        newStationsText += string.Join("、", newStations.GetRange(0, showLimit));

                        if (newStations.Count > showLimit)
                        {
                            newStationsText += $"...等{newStations.Count}个";
                        }
                    }

                    string syncText = "";
                    if (deletedCount > 0)
                    {
                        syncText = $"\n已删除：{deletedCount}个不在官方数据中的车站";
                    }

                    ImportSummary = $"总共：{total}个车站\n" +
                                    $"新增：{imported}个车站\n" +
                                    $"已存在：{skipped}个车站" +
                                    syncText +
                                    $"\n\n同步后统计：\n" +
                                    $"  官方车站数：{finalOfficialCount}\n" +
                                    $"  数据库车站数：{finalDatabaseCount}" +
                                    newStationsText;
                }
                else
                {
                    StatusMessage = "未能获取到有效的车站数据";
                    MessageBoxHelper.ShowWarning("未能从12306获取到有效的车站数据，请稍后再试。", "获取失败");
                    IsImporting = false;
                }
            }
            catch (OperationCanceledException)
            {
                // 操作被取消
                StatusMessage = "导入已取消";
                IsImporting = false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"导入车站时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"导入车站失败: {ex.Message}", "导入错误");
                StatusMessage = "导入过程中发生错误";
                IsImporting = false;
            }
            finally
            {
                // 释放取消令牌源
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="window">要关闭的窗口</param>
        private void CloseWindow(Window window)
        {
            if (window == null) return;

            // 检查是否正在导入（仅在实际导入过程中才提示，已完成但未重置状态的不提示）
            if (IsImporting && !HasImportResult)
            {
                // 弹出确认对话框
                var result = MessageBoxHelper.ShowConfirmation(
                    "正在导入车站，关闭会导致导入中断，是否确认关闭？",
                    "确认关闭");

                if (result != MessageBoxResult.Yes)
                {
                    // 用户取消关闭，继续导入
                    return;
                }

                // 用户确认关闭，取消并回滚已导入的数据
                CancelAndRollbackImport();

                // 等待一小段时间确保取消操作被处理
                System.Threading.Thread.Sleep(100);
            }

            // 重置导入状态，确保下次打开窗口时按钮可用
            IsImporting = false;

            // 如果数据已经发生变化，触发回调进行刷新
            if (_dataChanged && DataRefreshCallback != null)
            {
                DataRefreshCallback.Invoke();
            }

            window.Close();
        }

        /// <summary>
        /// 取消导入并回滚已导入的数据
        /// </summary>
        public async void CancelAndRollbackImport()
        {
            try
            {
                // 请求取消导入
                _cancellationTokenSource?.Cancel();

                // 标记为不再继续导入
                IsImporting = false;

                // 显示取消导入消息
                StatusMessage = "正在取消导入...";

                // 检查是否有需要回滚的数据
                if (_importedStationIds.Count > 0)
                {
                    StatusMessage = "正在回滚导入的数据...";

                    // 调用服务回滚数据
                    await _stationImportService.RollbackImportedStationsAsync(_importedStationIds);

                    // 重置数据变更标志
                    _dataChanged = false;

                    // 清空已导入ID列表
                    _importedStationIds.Clear();

                    LogHelper.LogInfo("已回滚导入的车站数据");
                }

                StatusMessage = "导入已取消";
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"回滚导入的车站数据时出错: {ex.Message}", ex);
            }
        }

        #endregion

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="current">当前进度</param>
        /// <param name="total">总进度</param>
        private void UpdateProgress(int current, int total)
        {
            // 计算百分比进度
            ImportProgress = (int)Math.Round((double)current / total * 100);
            StatusMessage = $"正在导入车站数据...{current}/{total}";
        }
    }
}