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
                // 设置正在初始化标志
                _isInitializing = true;
                
                // 设置车票号
                TicketNumber = _originalTicket.TicketNumber;
                
                // 设置检票口
                CheckInLocation = _originalTicket.CheckInLocation;
                
                // 设置出发站
                _isUpdatingDepartStation = true;
                DepartStation = _originalTicket.DepartStation?.Replace("站", "");
                DepartStationPinyin = _originalTicket.DepartStationPinyin;
                DepartStationCode = _originalTicket.DepartStationCode;
                _isUpdatingDepartStation = false;
                
                // 设置到达站
                _isUpdatingArriveStation = true;
                ArriveStation = _originalTicket.ArriveStation?.Replace("站", "");
                ArriveStationPinyin = _originalTicket.ArriveStationPinyin;
                ArriveStationCode = _originalTicket.ArriveStationCode;
                _isUpdatingArriveStation = false;
                
                // 设置车票改签类型
                SelectedTicketModificationType = _originalTicket.TicketModificationType;
                
                // 设置日期和时间
                if (_originalTicket.DepartDate.HasValue)
                    DepartDate = _originalTicket.DepartDate.Value;
                
                if (_originalTicket.DepartTime.HasValue)
                {
                    DepartHour = _originalTicket.DepartTime.Value.Hours;
                    DepartMinute = _originalTicket.DepartTime.Value.Minutes;
                }
                
                // 更新搜索文本框的显示
                _isUpdatingDepartStation = true;
                _isUpdatingArriveStation = true;
                DepartStationSearchText = _originalTicket.DepartStation?.Replace("站", "");
                ArriveStationSearchText = _originalTicket.ArriveStation?.Replace("站", "");
                _isUpdatingDepartStation = false;
                _isUpdatingArriveStation = false;
                
                Money = _originalTicket.Money ?? 0m;

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
                
                // 设置票种类型
                int ticketTypeFlags = _originalTicket.TicketTypeFlags;
                IsStudentTicket = (ticketTypeFlags & (int)TicketTypeFlags.StudentTicket) != 0;
                IsDiscountTicket = (ticketTypeFlags & (int)TicketTypeFlags.DiscountTicket) != 0;
                IsOnlineTicket = (ticketTypeFlags & (int)TicketTypeFlags.OnlineTicket) != 0;
                IsChildTicket = (ticketTypeFlags & (int)TicketTypeFlags.ChildTicket) != 0;
                
                // 设置支付渠道
                int paymentChannelFlags = _originalTicket.PaymentChannelFlags;
                IsAlipayPayment = (paymentChannelFlags & (int)PaymentChannelFlags.Alipay) != 0;
                IsWeChatPayment = (paymentChannelFlags & (int)PaymentChannelFlags.WeChat) != 0;
                IsABCPayment = (paymentChannelFlags & (int)PaymentChannelFlags.ABC) != 0;
                IsCCBPayment = (paymentChannelFlags & (int)PaymentChannelFlags.CCB) != 0;
                IsICBCPayment = (paymentChannelFlags & (int)PaymentChannelFlags.ICBC) != 0;
                
                // 初始化完成
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载车票数据时出错: {ex.Message}");
                LogHelper.LogTicketError("加载", $"加载车票数据ID:{_ticketId}时出错", ex);
            }
        }

        /// <summary>
        /// 保存修改后的车票
        /// </summary>
        protected override async void SaveTicket()
        {
            try
            {
                // 验证数据有效性
                if (!ValidateForm())
                {
                    return;
                }

                // 更新车票对象
                _originalTicket.TicketNumber = TicketNumber;
                _originalTicket.CheckInLocation = CheckInLocation;
                _originalTicket.DepartStation = DepartStation + "站";
                _originalTicket.TrainNo = SelectedTrainType == "纯数字" ? TrainNumber : $"{SelectedTrainType}{TrainNumber}";
                _originalTicket.ArriveStation = ArriveStation + "站";
                _originalTicket.DepartStationPinyin = DepartStationPinyin;
                _originalTicket.ArriveStationPinyin = ArriveStationPinyin;
                _originalTicket.DepartDate = DepartDate;
                _originalTicket.DepartTime = new TimeSpan(DepartHour, DepartMinute, 0);
                _originalTicket.Money = Money;
                _originalTicket.DepartStationCode = DepartStationCode;
                _originalTicket.ArriveStationCode = ArriveStationCode;
                _originalTicket.SeatType = SelectedSeatType;
                _originalTicket.TicketModificationType = SelectedTicketModificationType;
                _originalTicket.TicketTypeFlags = GetTicketTypeFlags();
                _originalTicket.PaymentChannelFlags = GetPaymentChannelFlags();

                // 处理车厢号
                _originalTicket.CoachNo = IsExtraCoach ? CoachNo + "加车" : CoachNo + "车";

                // 处理座位号
                if (IsNoSeat)
                    _originalTicket.SeatNo = "无座";
                else if (SelectedSeatType == "新空调硬座")
                    _originalTicket.SeatNo = SeatNo;
                else
                    _originalTicket.SeatNo = $"{SeatNo}{SelectedSeatPosition}";

                // 处理附加信息
                _originalTicket.AdditionalInfo = SelectedAdditionalInfo;
                
                // 处理车票用途
                _originalTicket.TicketPurpose = SelectedTicketPurpose;
                
                // 处理提示信息
                if (SelectedHint == "自定义")
                    _originalTicket.Hint = CustomHint;
                else
                    _originalTicket.Hint = SelectedHint;

                // 设置超时任务
                var saveTask = _databaseService.UpdateTicketAsync(_originalTicket);
                
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
                    LogHelper.LogTicketError("关闭", "关闭修改车票窗口时失败", ex);
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
                LogHelper.LogTicketError("保存", $"更新车票时数据库错误: {sqlEx.Message}, 错误代码: {sqlEx.Number}", sqlEx);
                MessageBoxHelper.ShowError($"数据库错误: {sqlEx.Message}\n错误代码: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                LogHelper.LogTicketError("保存", $"更新车票失败(ID:{_ticketId}): {ex.Message}", ex);
                MessageBoxHelper.ShowError($"保存车票失败: {ex.Message}");
            }
        }

        // 获取票种类型标志位
        private int GetTicketTypeFlags()
        {
            int flags = 0;
            if (IsStudentTicket) flags |= (int)TicketTypeFlags.StudentTicket;
            if (IsDiscountTicket) flags |= (int)TicketTypeFlags.DiscountTicket;
            if (IsOnlineTicket) flags |= (int)TicketTypeFlags.OnlineTicket;
            if (IsChildTicket) flags |= (int)TicketTypeFlags.ChildTicket;
            return flags;
        }
        
        // 获取支付渠道标志位
        private int GetPaymentChannelFlags()
        {
            int flags = 0;
            if (IsAlipayPayment) flags |= (int)PaymentChannelFlags.Alipay;
            if (IsWeChatPayment) flags |= (int)PaymentChannelFlags.WeChat;
            if (IsABCPayment) flags |= (int)PaymentChannelFlags.ABC;
            if (IsCCBPayment) flags |= (int)PaymentChannelFlags.CCB;
            if (IsICBCPayment) flags |= (int)PaymentChannelFlags.ICBC;
            return flags;
        }
    }
} 