using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// PDF导入视图模型
    /// </summary>
    public class PdfImportViewModel : BaseViewModel
    {
        private readonly PdfService _pdfService;
        private readonly MainViewModel _mainViewModel;
        private string _pdfContent = string.Empty;
        private string _selectedPdfPath = string.Empty;
        private bool _isLoading = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        public PdfImportViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _pdfService = new PdfService();

            // 初始化命令
            SelectPdfCommand = new RelayCommand(SelectPdfFile);
            ImportTicketCommand = new RelayCommand(ImportTicket, CanImportTicket);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>
        /// PDF内容
        /// </summary>
        public string PdfContent
        {
            get => _pdfContent;
            set
            {
                if (_pdfContent != value)
                {
                    _pdfContent = value;
                    OnPropertyChanged(nameof(PdfContent));
                }
            }
        }

        /// <summary>
        /// 选中的PDF文件路径
        /// </summary>
        public string SelectedPdfPath
        {
            get => _selectedPdfPath;
            set
            {
                if (_selectedPdfPath != value)
                {
                    _selectedPdfPath = value;
                    OnPropertyChanged(nameof(SelectedPdfPath));
                    OnPropertyChanged(nameof(HasSelectedPdf));
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
                }
            }
        }

        /// <summary>
        /// 是否已选择PDF文件
        /// </summary>
        public bool HasSelectedPdf => !string.IsNullOrEmpty(SelectedPdfPath);

        /// <summary>
        /// 选择PDF文件命令
        /// </summary>
        public ICommand SelectPdfCommand { get; }

        /// <summary>
        /// 导入车票命令
        /// </summary>
        public ICommand ImportTicketCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 选择PDF文件
        /// </summary>
        private async void SelectPdfFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "PDF文件 (*.pdf)|*.pdf",
                    Title = "选择12306车票PDF文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    SelectedPdfPath = openFileDialog.FileName;

                    // 异步读取PDF内容
                    await LoadPdfContentAsync(SelectedPdfPath);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"选择PDF文件时出错: {ex.Message}");
                LogHelper.LogError($"选择PDF文件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 异步加载PDF内容
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        private async Task LoadPdfContentAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    PdfContent = "文件不存在或路径无效";
                    return;
                }

                // 使用PdfService读取PDF内容
                PdfContent = await _pdfService.ReadPdfContentAsync(filePath);
            }
            catch (Exception ex)
            {
                PdfContent = $"读取PDF内容时出错: {ex.Message}";
                LogHelper.LogError($"读取PDF内容时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入车票
        /// </summary>
        private void ImportTicket()
        {
            // 这里只是示例，真正的导入逻辑需要根据实际情况实现
            MessageBoxHelper.ShowInfo("导入功能尚未完全实现，目前仅支持PDF内容查看");
        }

        /// <summary>
        /// 是否可以导入车票
        /// </summary>
        /// <returns>是否可以导入车票</returns>
        private bool CanImportTicket()
        {
            return HasSelectedPdf && !IsLoading;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            // 关闭窗口
            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?.Close();
        }
    }
} 