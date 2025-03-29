using System.Text.RegularExpressions;
using System.Windows;
using MySql.Data.MySqlClient;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Input;

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
        private bool _isResetting = false;  // 添加重置标志
        private CancellationTokenSource _validationCancellationTokenSource = new CancellationTokenSource();  // 添加验证取消令牌源

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
                
                // 设置出发站，直接赋值不触发校验
                _isUpdatingDepartStation = true;
                DepartStation = _originalTicket.DepartStation?.Replace("站", "");
                DepartStationPinyin = _originalTicket.DepartStationPinyin;
                DepartStationCode = _originalTicket.DepartStationCode;
                _isUpdatingDepartStation = false;
                
                // 设置到达站，直接赋值不触发校验
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
                    // 使用工具类解析车次号
                    var (trainType, trainNumber) = FormValidationHelper.ParseTrainNo(trainNo);
                    SelectedTrainType = trainType;
                    TrainNumber = trainNumber;
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
                    // 使用工具类解析座位号
                    var (number, position, isNoSeat) = FormValidationHelper.ParseSeatNo(seatNo);
                    IsNoSeat = isNoSeat;
                    SeatNo = number;
                    
                    // 设置座位类型和位置
                    SelectedSeatType = _originalTicket.SeatType;
                    UpdateSeatPositions();
                    
                    if (!string.IsNullOrEmpty(position))
                    {
                        SelectedSeatPosition = position;
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
                SetTicketTypeFlags(ticketTypeFlags);
                
                // 设置支付渠道
                SetPaymentChannelFlags(_originalTicket.PaymentChannelFlags);
                
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
                // 验证表单
                if (!ValidateForm())
                {
                    MessageBoxHelper.ShowWarning(FormValidationHelper.GetFormattedValidationErrors(_validationErrors));
                    return;
                }

                // 创建TrainRideInfo对象
                var ticket = new TrainRideInfo
                {
                    Id = _ticketId,
                    TicketNumber = TicketNumber,
                    CheckInLocation = CheckInLocation,
                    DepartStation = DepartStation + "站",
                    ArriveStation = ArriveStation + "站",
                    DepartStationPinyin = DepartStationPinyin,
                    ArriveStationPinyin = ArriveStationPinyin,
                    DepartStationCode = DepartStationCode,
                    ArriveStationCode = ArriveStationCode,
                    DepartDate = DepartDate,
                    DepartTime = new TimeSpan(DepartHour, DepartMinute, 0),
                    TrainNo = FormValidationHelper.FormatTrainNo(SelectedTrainType, TrainNumber),
                    CoachNo = IsExtraCoach ? $"加{CoachNo}车" : $"{CoachNo}车",
                    SeatNo = FormValidationHelper.FormatSeatNo(IsNoSeat, SeatNo, SelectedSeatPosition),
                    Money = Money,
                    SeatType = SelectedSeatType,
                    AdditionalInfo = SelectedAdditionalInfo,
                    TicketPurpose = SelectedTicketPurpose,
                    Hint = SelectedHint == "自定义" ? CustomHint : SelectedHint,
                    TicketModificationType = SelectedTicketModificationType,
                    TicketTypeFlags = GetTicketTypeFlags(),
                    PaymentChannelFlags = GetPaymentChannelFlags()
                };

                // 更新车票
                bool result = await _databaseService.UpdateTicketAsync(ticket);
                
                if (result)
                {
                    MessageBoxHelper.ShowInformation("车票修改成功！", "成功");
                    
                    // 触发窗口关闭事件
                    OnCloseWindow();
                }
                else
                {
                    MessageBoxHelper.ShowError("更新车票失败，可能是网络问题或车票已被删除");
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"保存车票时出错: {ex.Message}");
                LogHelper.LogTicketError("修改", $"修改车票ID:{_ticketId}时出错", ex);
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

        /// <summary>
        /// 重写处理站点输入框失去焦点事件的方法，确保初始加载时不触发验证
        /// </summary>
        /// <param name="isDepartStation">是否为出发站</param>
        public override void OnStationLostFocus(bool isDepartStation)
        {
            if (_isInitialLoad) return;  // 初始加载时不执行校验

            // 调用基类的方法
            base.OnStationLostFocus(isDepartStation);
        }

        /// <summary>
        /// 准备重置表单，在执行实际重置前设置标志并取消所有校验任务
        /// </summary>
        private new void PrepareReset()
        {
            try
            {
                // 先设置重置标志为true
                _isResetting = true;
                
                // 取消所有正在进行的校验任务
                if (_validationCancellationTokenSource != null)
                {
                    _validationCancellationTokenSource.Cancel();
                    _validationCancellationTokenSource.Dispose();
                    _validationCancellationTokenSource = new CancellationTokenSource();
                }
                
                // 直接同步执行重置操作，不再使用异步方式
                ResetForm();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"准备重置表单时出错: {ex.Message}", ex);
                // 确保标志被重置
                _isResetting = false;
            }
        }

        /// <summary>
        /// 重置表单
        /// </summary>
        public override void ResetForm()
        {
            try
            {
                Debug.WriteLine("开始重置编辑车票表单...");
                
                // 重新加载原始车票数据
                LoadTicketData();
                
                // 重置表单修改状态
                IsFormModified = false;
                
                // 重置验证错误列表
                _validationErrors.Clear();
                
                // 将焦点设置到取票号输入框
                OnFocusTextBox("TicketNumber");
                
                // 使用Dispatcher确保焦点设置在UI更新后执行
                Application.Current.Dispatcher.InvokeAsync(() => {
                    OnFocusTextBox("TicketNumber");
                }, DispatcherPriority.Render);
                
                Debug.WriteLine("编辑车票表单重置完成，焦点已设置到取票号输入框");
                
                // 重置完成，设置重置标志为false
                _isResetting = false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("重置表单时出错", ex);
                MessageBoxHelper.ShowError("重置表单时出错: " + ex.Message);
                _isResetting = false;
            }
        }

        private void ResetFormModifiedState()
        {
            base.ResetFormModifiedState();
        }
    }
} 