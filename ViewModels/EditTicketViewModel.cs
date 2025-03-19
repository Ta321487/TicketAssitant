using System.Text.RegularExpressions;
using System.Windows;
using MySql.Data.MySqlClient;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class EditTicketViewModel : AddTicketViewModel
    {
        private readonly TrainRideInfo _originalTicket;
        private int _ticketId;
        private bool _isUpdatingDepartStation = false;
        private bool _isUpdatingArriveStation = false;
        private bool _isInitialLoad = true;  // 添加初始加载标志
        private bool _isInitializing = true;  // 添加初始化标志

        public EditTicketViewModel(DatabaseService databaseService, MainViewModel mainViewModel, TrainRideInfo ticket) 
            : base(databaseService, mainViewModel)
        {
            _originalTicket = ticket;
            _ticketId = ticket.Id;

            // 加载原始车票数据
            LoadTicketData();
            
            // 初始加载完成
            _isInitialLoad = false;
            
            // 重置表单修改状态，因为这是初始加载，不应该被视为用户修改
            ResetFormModifiedState();
        }

        /// <summary>
        /// 重写基类的搜索站点方法，在初始加载时不显示下拉列表
        /// </summary>
        protected override async void SearchStations(string searchText, bool isDepartStation)
        {
            if (_isInitialLoad) return;  // 初始加载时不执行搜索
            await Task.Yield(); // 确保异步方法签名匹配
            base.SearchStations(searchText, isDepartStation);
        }

        private void LoadTicketData()
        {
            try
            {
                // 设置基本信息
                TicketNumber = _originalTicket.TicketNumber;
                CheckInLocation = _originalTicket.CheckInLocation;
                
                // 设置车站信息
                DepartStation = _originalTicket.DepartStation?.Replace("站", "");
                ArriveStation = _originalTicket.ArriveStation?.Replace("站", "");
                
                // 更新搜索文本框的显示
                _isUpdatingDepartStation = true;
                _isUpdatingArriveStation = true;
                DepartStationSearchText = _originalTicket.DepartStation?.Replace("站", "");
                ArriveStationSearchText = _originalTicket.ArriveStation?.Replace("站", "");
                _isUpdatingDepartStation = false;
                _isUpdatingArriveStation = false;
                
                DepartStationPinyin = _originalTicket.DepartStationPinyin;
                ArriveStationPinyin = _originalTicket.ArriveStationPinyin;
                Money = _originalTicket.Money ?? 0m;
                DepartStationCode = _originalTicket.DepartStationCode;
                ArriveStationCode = _originalTicket.ArriveStationCode;

                // 设置日期和时间
                DepartDate = _originalTicket.DepartDate ?? DateTime.Today;
                DepartHour = _originalTicket.DepartTime?.Hours ?? 0;
                DepartMinute = _originalTicket.DepartTime?.Minutes ?? 0;

                // 设置车次信息
                string trainNo = _originalTicket.TrainNo;
                if (trainNo != null)
                {
                    // 分离车次类型和编号
                    var match = Regex.Match(trainNo, @"^([GCDZTKLSY])?(\d+)$");
                    if (match.Success)
                    {
                        SelectedTrainType = !string.IsNullOrEmpty(match.Groups[1].Value) 
                            ? match.Groups[1].Value 
                            : "纯数字";
                        TrainNumber = match.Groups[2].Value;
                    }
                }

                // 设置车厢号
                string coachNo = _originalTicket.CoachNo;
                if (coachNo != null)
                {
                    if (coachNo.StartsWith("加"))
                    {
                        IsExtraCoach = true;
                        CoachNo = coachNo.Replace("加", "").Replace("车", "");
                    }
                    else
                    {
                        IsExtraCoach = false;
                        CoachNo = coachNo.Replace("车", "");
                    }
                }

                // 设置座位信息
                string seatNo = _originalTicket.SeatNo;
                if (seatNo != null)
                {
                    if (seatNo == "无座")
                    {
                        IsNoSeat = true;
                        SeatNo = "";
                    }
                    else
                    {
                        IsNoSeat = false;
                        // 分离座位号和位置
                        var seatMatch = Regex.Match(seatNo, @"^(\d+)([A-F上中下])?$");
                        if (seatMatch.Success)
                        {
                            SeatNo = seatMatch.Groups[1].Value;
                            string position = seatMatch.Groups[2].Value;
                            if (!string.IsNullOrEmpty(position))
                            {
                                SelectedSeatType = _originalTicket.SeatType;
                                UpdateSeatPositions();
                                SelectedSeatPosition = position;
                            }
                        }
                    }
                }

                // 设置座位类型
                SelectedSeatType = _originalTicket.SeatType;

                // 设置附加信息
                SelectedAdditionalInfo = _originalTicket.AdditionalInfo;
                SelectedTicketPurpose = _originalTicket.TicketPurpose;
                
                // 设置提示信息
                string hint = _originalTicket.Hint;
                if (!string.IsNullOrEmpty(hint) && !HintOptions.Contains(hint))
                {
                    CustomHint = hint;
                    HintOptions.Insert(HintOptions.Count - 1, hint);
                    SelectedHint = hint;
                }
                else
                {
                    SelectedHint = hint;
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载车票数据时出错: {ex.Message}");
                LogHelper.LogError($"加载车票数据时出错", ex);
            }
        }

        /// <summary>
        /// 保存修改后的车票
        /// </summary>
        protected override async void SaveTicket()
        {
            if (!ValidateForm())
            {
                // 显示验证错误
                string errorMessage = string.Join("\n", _validationErrors);
                MessageBoxHelper.ShowWarning(errorMessage, "表单验证失败");
                return;
            }

            try
            {
                // 创建车票对象
                var ticket = new TrainRideInfo
                {
                    Id = _ticketId,
                    TicketNumber = TicketNumber,
                    CheckInLocation = CheckInLocation,
                    DepartStation = DepartStation + "站",
                    ArriveStation = ArriveStation + "站",
                    DepartStationPinyin = DepartStationPinyin,
                    ArriveStationPinyin = ArriveStationPinyin,
                    Money = Money,
                    DepartStationCode = DepartStationCode,
                    ArriveStationCode = ArriveStationCode,
                    DepartDate = DepartDate,
                    DepartTime = new TimeSpan(DepartHour, DepartMinute, 0),
                    TrainNo = SelectedTrainType == "纯数字" ? TrainNumber : $"{SelectedTrainType}{TrainNumber}",
                    SeatType = SelectedSeatType
                };

                // 处理车厢号
                if (IsExtraCoach)
                {
                    // 去掉前导零并添加"加"和"车"
                    int coachNum = int.Parse(CoachNo);
                    ticket.CoachNo = $"加{coachNum}车";
                }
                else
                    ticket.CoachNo = $"{CoachNo}车";

                // 处理座位号
                if (IsNoSeat)
                    ticket.SeatNo = "无座";
                else if (SelectedSeatType == "新空调硬座")
                    ticket.SeatNo = SeatNo;
                else
                    ticket.SeatNo = $"{SeatNo}{SelectedSeatPosition}";

                // 处理附加信息
                ticket.AdditionalInfo = SelectedAdditionalInfo;
                
                // 处理车票用途
                ticket.TicketPurpose = SelectedTicketPurpose;
                
                // 处理提示信息
                if (SelectedHint == "自定义")
                    ticket.Hint = CustomHint;
                else
                    ticket.Hint = SelectedHint;

                // 设置超时任务
                var saveTask = _databaseService.UpdateTicketAsync(ticket);
                
                // 添加5秒超时
                var timeoutTask = Task.Delay(5000);
                
                // 等待任务完成或超时
                if (await Task.WhenAny(saveTask, timeoutTask) == timeoutTask)
                {
                    // 操作超时
                    MessageBoxHelper.ShowError("保存车票操作超时，请检查数据库连接");
                    return;
                }
                
                // 确保任务完成且没有异常
                await saveTask;
                
                MessageBoxHelper.ShowInformation("车票修改成功！", "成功");
                
                // 重置表单修改状态
                ResetFormModifiedState();
                
                // 安全地关闭窗口
                try
                {
                    // 关闭窗口，并设置DialogResult为true，表示保存成功
                    var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                    if (window != null)
                    {
                        window.DialogResult = true;
                    }
                    else
                    {
                        // 如果找不到窗口，使用事件关闭
                        OnCloseWindow();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("关闭修改车票窗口时出错", ex);
                    // 尝试使用事件关闭
                    OnCloseWindow();
                }
                
                // 在窗口关闭后再刷新仪表盘和查询全部数据
                if (_mainViewModel != null)
                {
                    // 刷新仪表盘数据
                    if (_mainViewModel.DashboardViewModel != null)
                    {
                        await _mainViewModel.DashboardViewModel.RefreshDataAsync();
                    }
                    
                    // 刷新查询全部数据
                    if (_mainViewModel.QueryAllTicketsViewModel != null)
                    {
                        await _mainViewModel.QueryAllTicketsViewModel.QueryAllAsync();
                    }
                }
            }
            catch (MySqlException sqlEx)
            {
                LogHelper.LogError($"数据库错误: {sqlEx.Message}", sqlEx);
                MessageBoxHelper.ShowError($"数据库错误: {sqlEx.Message}\n错误代码: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"保存车票失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"保存车票失败: {ex.Message}");
            }
        }
    }
} 