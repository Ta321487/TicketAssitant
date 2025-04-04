using System.Text;
using System.Text.RegularExpressions;
using TA_WPF.Models;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 表单验证工具类，提供通用的表单验证功能
    /// </summary>
    public static class FormValidationHelper
    {
        /// <summary>
        /// 验证车票表单数据
        /// </summary>
        /// <param name="ticket">车票数据</param>
        /// <param name="errors">错误消息列表（出参）</param>
        /// <returns>是否验证通过</returns>
        public static bool ValidateTicketForm(TrainRideInfo ticket, List<string> errors)
        {
            if (errors == null)
            {
                errors = new List<string>();
            }
            else
            {
                errors.Clear();
            }

            bool isValid = true;

            // 验证车票号
            if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                errors.Add("未填写取票号");
                isValid = false;
            }

            // 验证出发站和到达站
            if (string.IsNullOrWhiteSpace(ticket.DepartStation))
            {
                errors.Add("未填写出发站");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ticket.ArriveStation))
            {
                errors.Add("未填写到达站");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ticket.DepartStationPinyin))
            {
                errors.Add("未填写出发站拼音");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ticket.ArriveStationPinyin))
            {
                errors.Add("未填写到达站拼音");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ticket.DepartStationCode))
            {
                errors.Add("未填写出发站代码");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ticket.ArriveStationCode))
            {
                errors.Add("未填写到达站代码");
                isValid = false;
            }

            // 验证日期和时间
            if (!ticket.DepartDate.HasValue)
            {
                errors.Add("未选择出发日期");
                isValid = false;
            }

            if (!ticket.DepartTime.HasValue)
            {
                errors.Add("未选择出发时间");
                isValid = false;
            }

            // 验证车次
            if (string.IsNullOrWhiteSpace(ticket.TrainNo))
            {
                errors.Add("未填写车次");
                isValid = false;
            }
            else
            {
                // 解析车次号
                var (trainType, trainNumber) = ParseTrainNo(ticket.TrainNo);

                // 验证车次号格式
                if (!string.IsNullOrWhiteSpace(trainNumber))
                {
                    if (!Regex.IsMatch(trainNumber, @"^\d{1,4}$"))
                    {
                        errors.Add("车次号必须为1-4位数字");
                        isValid = false;
                    }

                    // 车次号不能以0开头或只有0
                    if (trainNumber.StartsWith("0") || trainNumber.All(c => c == '0'))
                    {
                        errors.Add("车次号不能以0开头或只有0");
                        isValid = false;
                    }
                }
            }

            // 验证金额
            if (!ticket.Money.HasValue)
            {
                errors.Add("未填写票价");
                isValid = false;
            }
            else if (ticket.Money.Value > 9999.99m)
            {
                errors.Add("金额不能超过9999.99");
                isValid = false;
            }

            // 验证车厢号
            string coachNoWithoutSuffix = ticket.CoachNo?.Replace("车", "").Replace("加", "");
            if (string.IsNullOrWhiteSpace(coachNoWithoutSuffix))
            {
                errors.Add("未填写车厢号");
                isValid = false;
            }
            else
            {
                if (!Regex.IsMatch(coachNoWithoutSuffix, @"^\d+$"))
                {
                    errors.Add("车厢号必须为数字");
                    isValid = false;
                }

                // 车厢号不能只有0
                if (coachNoWithoutSuffix.All(c => c == '0'))
                {
                    errors.Add("车厢号不能只有0");
                    isValid = false;
                }

                // 车厢号只能是00~99
                if (Regex.IsMatch(coachNoWithoutSuffix, @"^\d+$"))
                {
                    if (int.TryParse(coachNoWithoutSuffix, out int coachNumber))
                    {
                        if (coachNumber < 0 || coachNumber > 99)
                        {
                            errors.Add("车厢号必须在00-99之间");
                            isValid = false;
                        }
                    }
                }
            }

            // 验证座位号（如果不是无座）
            if (ticket.SeatNo != "无座")
            {
                var (seatNumber, position, _) = ParseSeatNo(ticket.SeatNo);

                if (string.IsNullOrWhiteSpace(seatNumber))
                {
                    errors.Add("未填写座位号");
                    isValid = false;
                }
                else
                {
                    if (!Regex.IsMatch(seatNumber, @"^\d{1,3}$"))
                    {
                        errors.Add("座位号必须为1-3位数字");
                        isValid = false;
                    }

                    // 座位号可以以0开头，但不能只有0
                    if (seatNumber.All(c => c == '0'))
                    {
                        errors.Add("座位号不能只有0");
                        isValid = false;
                    }
                }
            }

            // 验证座位类型
            if (string.IsNullOrWhiteSpace(ticket.SeatType))
            {
                errors.Add("未选择座位类型");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// 获取格式化的错误消息
        /// </summary>
        /// <param name="errors">错误消息列表</param>
        /// <returns>格式化后的错误消息</returns>
        public static string GetFormattedValidationErrors(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("请修正以下错误:");

            foreach (var error in errors)
            {
                sb.AppendLine($"- {error}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 格式化车次号
        /// </summary>
        /// <param name="trainType">车次类型（G/C/D等）</param>
        /// <param name="trainNumber">车次编号</param>
        /// <returns>格式化后的车次号</returns>
        public static string FormatTrainNo(string trainType, string trainNumber)
        {
            if (string.IsNullOrWhiteSpace(trainNumber))
            {
                return string.Empty;
            }

            // 如果选择了"纯数字"类型，则不加前缀
            if (trainType == "纯数字")
            {
                return trainNumber;
            }

            // 否则添加车次类型前缀
            return $"{trainType}{trainNumber}";
        }

        /// <summary>
        /// 格式化座位号
        /// </summary>
        /// <param name="isNoSeat">是否无座</param>
        /// <param name="seatNo">座位号</param>
        /// <param name="seatPosition">座位位置（A/B/C等）</param>
        /// <returns>格式化后的座位号</returns>
        public static string FormatSeatNo(bool isNoSeat, string seatNo, string seatPosition)
        {
            if (isNoSeat)
            {
                return "无座";
            }

            if (string.IsNullOrWhiteSpace(seatNo))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(seatPosition))
            {
                return seatNo;
            }

            return $"{seatNo}{seatPosition}";
        }

        /// <summary>
        /// 解析车次号
        /// </summary>
        /// <param name="trainNo">完整车次号</param>
        /// <returns>车次类型和编号元组</returns>
        public static (string trainType, string trainNumber) ParseTrainNo(string trainNo)
        {
            if (string.IsNullOrWhiteSpace(trainNo))
            {
                return (string.Empty, string.Empty);
            }

            // 分离车次类型和编号
            var match = Regex.Match(trainNo, @"^([GCDZTKLSY])?(\d+)$");
            if (match.Success)
            {
                string trainType = !string.IsNullOrEmpty(match.Groups[1].Value)
                    ? match.Groups[1].Value
                    : "纯数字";
                string trainNumber = match.Groups[2].Value;
                return (trainType, trainNumber);
            }

            // 如果无法解析，返回原始值作为编号，类型为空
            return (string.Empty, trainNo);
        }

        /// <summary>
        /// 解析座位号
        /// </summary>
        /// <param name="seatNo">完整座位号</param>
        /// <returns>座位号和位置元组</returns>
        public static (string number, string position, bool isNoSeat) ParseSeatNo(string seatNo)
        {
            if (string.IsNullOrWhiteSpace(seatNo))
            {
                return (string.Empty, string.Empty, false);
            }

            if (seatNo == "无座")
            {
                return (string.Empty, string.Empty, true);
            }

            // 分离座位号和位置
            var match = Regex.Match(seatNo, @"^(\d+)([A-F上中下])?$");
            if (match.Success)
            {
                string number = match.Groups[1].Value;
                string position = match.Groups[2].Value;
                return (number, position, false);
            }

            // 如果无法解析，返回原始值作为号码，位置为空
            return (seatNo, string.Empty, false);
        }

        /// <summary>
        /// 确保车票号首字母大写
        /// </summary>
        /// <param name="ticketNumber">车票号</param>
        /// <returns>首字母大写的车票号</returns>
        public static string EnsureFirstLetterUpperCase(string ticketNumber)
        {
            if (string.IsNullOrEmpty(ticketNumber))
            {
                return string.Empty;
            }

            // 将首字母转换为大写
            return char.ToUpper(ticketNumber[0]) + ticketNumber.Substring(1);
        }
    }
}