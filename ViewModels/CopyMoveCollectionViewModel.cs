using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;
using System.Collections.Generic;
using System.Diagnostics;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 复制/移动收藏夹视图模型
    /// </summary>
    public class CopyMoveCollectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private readonly TicketCollectionInfo _sourceCollection;
        private readonly bool _isMove;
        
        private ObservableCollection<TicketCollectionInfo> _targetCollections;
        private TicketCollectionInfo _selectedTargetCollection;
        private bool _isLoading;
        
        /// <summary>
        /// 操作结果
        /// </summary>
        public CopyMoveCollectionWindow.CopyMoveCollectionResult Result { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sourceCollection">源收藏夹</param>
        /// <param name="isMove">是否为移动操作</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public CopyMoveCollectionViewModel(TicketCollectionInfo sourceCollection, bool isMove, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            _sourceCollection = sourceCollection ?? throw new ArgumentNullException(nameof(sourceCollection));
            _isMove = isMove;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            
            // 初始化集合
            _targetCollections = new ObservableCollection<TicketCollectionInfo>();
            
            // 初始化命令
            ConfirmCommand = new RelayCommand(ConfirmAction, () => CanConfirm);
            CancelCommand = new RelayCommand(CancelAction);
            
            // 初始化结果
            Result = new CopyMoveCollectionWindow.CopyMoveCollectionResult
            {
                Success = false
            };
        }
        
        #region 属性

        /// <summary>
        /// 主视图模型
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;
        
        /// <summary>
        /// 源收藏夹
        /// </summary>
        public TicketCollectionInfo SourceCollection => _sourceCollection;
        
        /// <summary>
        /// 是否为移动操作
        /// </summary>
        public bool IsMove => _isMove;
        
        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle => _isMove ? "移动收藏夹" : "复制收藏夹";
        
        /// <summary>
        /// 目标收藏夹列表
        /// </summary>
        public ObservableCollection<TicketCollectionInfo> TargetCollections
        {
            get => _targetCollections;
            set
            {
                if (_targetCollections != value)
                {
                    _targetCollections = value;
                    OnPropertyChanged(nameof(TargetCollections));
                    OnPropertyChanged(nameof(HasTargetCollections));
                    OnPropertyChanged(nameof(HasNoTargetCollections));
                }
            }
        }
        
        /// <summary>
        /// 选中的目标收藏夹
        /// </summary>
        public TicketCollectionInfo SelectedTargetCollection
        {
            get => _selectedTargetCollection;
            set
            {
                if (_selectedTargetCollection != value)
                {
                    _selectedTargetCollection = value;
                    OnPropertyChanged(nameof(SelectedTargetCollection));
                    OnPropertyChanged(nameof(CanConfirm));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                    OnPropertyChanged(nameof(CanConfirm));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        
        /// <summary>
        /// 是否有目标收藏夹
        /// </summary>
        public bool HasTargetCollections => TargetCollections != null && TargetCollections.Count > 0;
        
        /// <summary>
        /// 是否没有目标收藏夹
        /// </summary>
        public bool HasNoTargetCollections => !HasTargetCollections;
        
        /// <summary>
        /// 是否可以确认操作
        /// </summary>
        public bool CanConfirm => !IsLoading && SelectedTargetCollection != null;
        
        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _mainViewModel.DataGridRowHeight;
        
        #endregion
        
        #region 命令
        
        /// <summary>
        /// 确认命令
        /// </summary>
        public ICommand ConfirmCommand { get; }
        
        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }
        
        #endregion
        
        #region 方法
        
        /// <summary>
        /// 加载目标收藏夹列表
        /// </summary>
        public async void LoadTargetCollections()
        {
            IsLoading = true;
            
            try
            {
                // 从数据库加载所有收藏夹
                var allCollections = await _databaseService.GetAllCollectionsAsync();
                
                // 过滤掉源收藏夹
                var filtered = allCollections.Where(c => c.Id != _sourceCollection.Id).ToList();
                
                // 更新目标收藏夹列表
                TargetCollections = new ObservableCollection<TicketCollectionInfo>(filtered);
                
                // 如果有数据，默认选中第一个
                if (HasTargetCollections)
                {
                    SelectedTargetCollection = TargetCollections[0];
                }
                else
                {
                    // 如果没有其它收藏夹，显示没有收藏夹的提示
                    MessageBoxHelper.ShowInfo("没有其他收藏夹可供选择");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载收藏夹列表失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"加载收藏夹列表失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 确认操作
        /// </summary>
        private async void ConfirmAction()
        {
            if (!CanConfirm)
                return;
                
            IsLoading = true;
            
            try
            {
                TicketCollectionInfo targetCollection = SelectedTargetCollection;
                
                // 获取源收藏夹所有车票ID
                var ticketIds = await _databaseService.GetCollectionTicketIdsAsync(_sourceCollection.Id);
                
                if (ticketIds.Count == 0)
                {
                    MessageBoxHelper.ShowInfo("源收藏夹中没有车票");
                    CloseDialog(true);
                    return;
                }
                
                // 执行复制或移动操作
                int affectedCount;
                if (_isMove)
                {
                    affectedCount = await MoveTicketsAsync(_sourceCollection.Id, targetCollection.Id, ticketIds);
                }
                else
                {
                    affectedCount = await CopyTicketsAsync(targetCollection.Id, ticketIds);
                }
                
                // 设置结果
                Result = new CopyMoveCollectionWindow.CopyMoveCollectionResult
                {
                    Success = true,
                    TargetCollection = targetCollection,
                    IsNewCollection = false,
                    AffectedTicketsCount = affectedCount
                };
                
                // 显示成功消息
                string message = _isMove
                    ? $"已将 {affectedCount} 张车票移动到收藏夹 \"{targetCollection.CollectionName}\""
                    : $"已将 {affectedCount} 张车票复制到收藏夹 \"{targetCollection.CollectionName}\"";
                
                MessageBoxHelper.ShowInformation(message);
                
                // 关闭对话框
                CloseDialog(true);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"{(_isMove ? "移动" : "复制")}车票失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"{(_isMove ? "移动" : "复制")}车票失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 复制车票到目标收藏夹
        /// </summary>
        private async Task<int> CopyTicketsAsync(int targetCollectionId, List<int> ticketIds)
        {
            int successCount = 0;
            
            try
            {
                // 获取目标收藏夹中已有的车票ID
                var existingTicketIds = await _databaseService.GetCollectionTicketIdsAsync(targetCollectionId);
                
                // 过滤掉已存在的车票
                var newTicketIds = ticketIds.Except(existingTicketIds).ToList();
                
                if (newTicketIds.Count == 0)
                {
                    MessageBoxHelper.ShowInfo("所选车票已全部存在于目标收藏夹中");
                    return 0;
                }
                
                // 创建映射关系列表
                var mappings = new List<CollectionMappedTicketInfo>();
                foreach (var ticketId in newTicketIds)
                {
                    mappings.Add(new CollectionMappedTicketInfo
                    {
                        CollectionId = targetCollectionId,
                        TicketId = ticketId,
                        AddTime = DateTime.Now
                    });
                }
                
                // 批量添加映射
                successCount = await _databaseService.AddTicketsToCollectionAsync(mappings);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"复制收藏夹失败: {ex.Message}", ex);
                throw; // 重新抛出异常由上层处理
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 移动车票到目标收藏夹
        /// </summary>
        private async Task<int> MoveTicketsAsync(int sourceCollectionId, int targetCollectionId, List<int> ticketIds)
        {
            int successCount = 0;
            
            try
            {
                // 首先复制车票到目标收藏夹
                successCount = await CopyTicketsAsync(targetCollectionId, ticketIds);
                
                if (successCount > 0)
                {
                    // 然后从源收藏夹中移除车票
                    bool removed = await _databaseService.RemoveTicketsFromCollectionAsync(sourceCollectionId, ticketIds);
                    if (!removed)
                    {
                        LogHelper.LogError("从源收藏夹移除车票失败");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"移动收藏夹失败: {ex.Message}", ex);
                throw; // 重新抛出异常由上层处理
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 取消操作
        /// </summary>
        private void CancelAction()
        {
            CloseDialog(false);
        }
        
        /// <summary>
        /// 关闭对话框
        /// </summary>
        private void CloseDialog(bool result)
        {
            if (Application.Current?.Windows != null)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.DialogResult = result;
                        window.Close();
                        break;
                    }
                }
            }
        }
        
        #endregion
    }
}