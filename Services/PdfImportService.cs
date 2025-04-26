using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TA_WPF.Models;
using TA_WPF.Utils;
using System.Diagnostics;
using System.Linq;

namespace TA_WPF.Services
{
    /// <summary>
    /// PDF导入服务，负责处理PDF文件的导入、解析和保存
    /// </summary>
    public class PdfImportService
    {
        private readonly PdfService _pdfService;
        private readonly DatabaseService _databaseService;
        private readonly StationSearchService _stationSearchService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        public PdfImportService(DatabaseService databaseService, StationSearchService stationSearchService)
        {
            _pdfService = new PdfService();
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));
        }

        /// <summary>
        /// 异步加载PDF文件内容
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>PDF文件内容</returns>
        public async Task<string> LoadPdfContentAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return "文件不存在或路径无效";
                }

                // 使用PdfService读取PDF内容
                return await _pdfService.ReadPdfContentAsync(filePath);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"读取PDF内容时出错: {ex.Message}");
                return $"读取PDF内容时出错: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析PDF内容提取车票信息
        /// </summary>
        /// <param name="content">PDF内容</param>
        /// <returns>提取的车票信息</returns>
        public TrainRideInfo ParsePdfContent(string content)
        {
            Debug.WriteLine("[PdfImportService] Starting ParsePdfContent...");
            if (string.IsNullOrEmpty(content))
            {
                Debug.WriteLine("[PdfImportService] Content is null or empty, returning null.");
                return null;
            }

            var ticket = new TrainRideInfo();
            // ticket.CheckInLocation = null; // Assume CheckInLocation property exists if needed

            try
            {
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Debug.WriteLine($"[PdfImportService] Total lines found: {lines.Length}");

                // --- 提取订单号 (填充到取票号字段) ---
                var orderNumberRegex = new Regex(@"订\s*单\s*号[：:]\s*(\S+)");
                var orderLine = lines.FirstOrDefault(line => orderNumberRegex.IsMatch(line));
                if (orderLine != null)
                {
                    var orderMatch = orderNumberRegex.Match(orderLine);
                    if (orderMatch.Success)
                    {
                        ticket.TicketNumber = orderMatch.Groups[1].Value.Trim();
                        Debug.WriteLine($"[PdfImportService] Extracted TicketNumber (from OrderNo): '{ticket.TicketNumber}'");
                    }
                    else { Debug.WriteLine("[PdfImportService] Order number regex matched line, but group extraction failed."); }
                }
                else { Debug.WriteLine("[PdfImportService] No line matched order number regex."); }

                // --- 提取车次和站点信息 (从第6行) ---
                int stationLineIndex = 5; // 第6行索引
                if (lines.Length > stationLineIndex)
                {
                    var stationLine = lines[stationLineIndex].Trim();
                    var parts = stationLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        ticket.TrainNo = parts[0].Trim();         // Example: "7516" or "G1234"
                        ticket.DepartStation = parts[1].Trim(); // Example: "白银市"
                        ticket.ArriveStation = parts[2].Trim(); // Example: "白银西"
                        Debug.WriteLine($"[PdfImportService] Extracted TrainNo: '{ticket.TrainNo}', DepartStation: '{ticket.DepartStation}', ArriveStation: '{ticket.ArriveStation}'");

                        // **移除/注释掉 Pinyin 自动填充**
                        // EnrichStationInfo(ticket); 
                    }
                    else { Debug.WriteLine($"[PdfImportService] Station line (Index {stationLineIndex}) parts count: {parts.Length}, expected >= 3."); }
                }
                else { Debug.WriteLine($"[PdfImportService] Not enough lines to process station info (Index {stationLineIndex}). Total lines: {lines.Length}."); }

                // --- 提取出发站和到达站拼音 (从第8行) ---
                int pinyinLineIndex = 7; // 第8行索引
                if (lines.Length > pinyinLineIndex)
                {
                    var pinyinLine = lines[pinyinLineIndex].Trim();
                    var pinyinParts = pinyinLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (pinyinParts.Length >= 2)
                    {
                        ticket.DepartStationPinyin = pinyinParts[0].Trim(); // Example: "Baiyinshi"
                        ticket.ArriveStationPinyin = pinyinParts[1].Trim(); // Example: "Baiyinxi"
                        Debug.WriteLine($"[PdfImportService] Extracted DepartStationPinyin: '{ticket.DepartStationPinyin}', ArriveStationPinyin: '{ticket.ArriveStationPinyin}'");
                    }
                    else { Debug.WriteLine($"[PdfImportService] Pinyin line (Index {pinyinLineIndex}) parts count: {pinyinParts.Length}, expected >= 2."); }
                }
                else { Debug.WriteLine($"[PdfImportService] Not enough lines to process pinyin info (Index {pinyinLineIndex}). Total lines: {lines.Length}."); }

                // --- 提取出发日期和时间 ---
                var dateTimeRegex = new Regex(@"开车时间\s*(\d{4})年\s*(\d{1,2})\s*月\s*(\d{1,2})\s*日\s*(\d{1,2}):(\d{1,2})");
                var dateTimeLine = lines.FirstOrDefault(line => dateTimeRegex.IsMatch(line));
                if (dateTimeLine != null)
                {
                    var dateTimeMatch = dateTimeRegex.Match(dateTimeLine);
                    if (dateTimeMatch.Success)
                    {
                        try
                        {
                            int year = int.Parse(dateTimeMatch.Groups[1].Value);
                            int month = int.Parse(dateTimeMatch.Groups[2].Value);
                            int day = int.Parse(dateTimeMatch.Groups[3].Value);
                            int hour = int.Parse(dateTimeMatch.Groups[4].Value);
                            int minute = int.Parse(dateTimeMatch.Groups[5].Value);
                            ticket.DepartDate = new DateTime(year, month, day);
                            ticket.DepartTime = new TimeSpan(hour, minute, 0);
                            Debug.WriteLine($"[PdfImportService] Extracted DepartDate: {ticket.DepartDate:yyyy-MM-dd}, DepartTime: {ticket.DepartTime:hh\\:mm}");
                        }
                        catch (FormatException ex)
                        { Debug.WriteLine($"[PdfImportService] Error parsing date/time parts: {ex.Message}"); }
                    }
                    else { Debug.WriteLine("[PdfImportService] DateTime regex matched line, but group extraction failed."); }
                }
                else { Debug.WriteLine("[PdfImportService] No line matched DateTime regex."); }

                // --- 提取金额 ---
                var moneyRegex = new Regex(@"票价[：:]\s*(\d+\.?\d*)");
                var moneyLine = lines.FirstOrDefault(line => moneyRegex.IsMatch(line));
                if (moneyLine != null)
                {
                    var moneyMatch = moneyRegex.Match(moneyLine);
                    if (moneyMatch.Success)
                    {
                        if (decimal.TryParse(moneyMatch.Groups[1].Value, out decimal money))
                        {
                            ticket.Money = money;
                            Debug.WriteLine($"[PdfImportService] 识别到金额: {ticket.Money}");
                        }
                        else { Debug.WriteLine($"[PdfImportService] 无法解析金额值。原值: '{moneyMatch.Groups[1].Value}'"); }
                    }
                    else { Debug.WriteLine("[PdfImportService] 匹配到金额正则行，但分组提取失败。"); }
                }
                else { Debug.WriteLine("[PdfImportService] 没有行匹配金额正则。"); }

                // --- 提取车厢号、座位号、座位类型 (从第9行) ---
                int seatLineIndex = 8; // 第9行索引
                if (lines.Length > seatLineIndex)
                {
                    var seatLine = lines[seatLineIndex].Trim();
                    var seatRegex = new Regex(@"(?:(加)?)(\d+)车([^号]+)号\s*([^\s]+)"); // Removed trailing \
                    var seatMatch = seatRegex.Match(seatLine);

                    // --- 初始化 Flags ---
                    ticket.TicketTypeFlags = (int)TicketTypeFlags.None;
                    ticket.PaymentChannelFlags = (int)PaymentChannelFlags.None;

                    if (seatMatch.Success)
                    {
                        bool isExtra = seatMatch.Groups[1].Success;
                        string coachNum = seatMatch.Groups[2].Value;
                        string seatCode = seatMatch.Groups[3].Value.Trim(); // e.g., "001"
                        string seatTypeText = seatMatch.Groups[4].Value.Trim(); // e.g., "硬座"

                        // Build CoachNo string as expected by ViewModel
                        ticket.CoachNo = isExtra ? $"加{coachNum}车" : $"{coachNum}车";
                        // Assign SeatCode directly (ViewModel handles splitting A-F, etc.)
                        ticket.SeatNo = seatCode; 
                        // Map extracted text to the required SeatType value
                        ticket.SeatType = MapSeatType(seatTypeText);
                        Debug.WriteLine($"[PdfImportService] 提取到车厢号: '{ticket.CoachNo}', 座位号: '{ticket.SeatNo}', 座位类型: '{ticket.SeatType}' (原始文本: '{seatTypeText}')");

                        // --- 提取票种和支付渠道标志 ---
                        string remainingSeatLine = seatLine.Substring(seatMatch.Index + seatMatch.Length).Trim();
                        Debug.WriteLine($"[PdfImportService] Remaining seat line for flags: '{remainingSeatLine}'");

                        if (remainingSeatLine.Contains("孩"))
                        {
                            ticket.TicketTypeFlags |= (int)TicketTypeFlags.ChildTicket;
                            Debug.WriteLine("[PdfImportService] 检测到 '孩' (ChildTicket)");
                        }
                        if (remainingSeatLine.Contains("惠"))
                        {
                            ticket.TicketTypeFlags |= (int)TicketTypeFlags.DiscountTicket;
                            Debug.WriteLine("[PdfImportService] 检测到 '惠' (DiscountTicket)");
                        }

                        // 支付渠道识别
                        if (remainingSeatLine.Contains("招")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.CMB; Debug.WriteLine("[PdfImportService] 检测到 '招' (CMB)"); }
                        else if (remainingSeatLine.Contains("邮")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.PSBC; Debug.WriteLine("[PdfImportService] 检测到 '邮' (PSBC)"); }
                        else if (remainingSeatLine.Contains("中")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.BOC; Debug.WriteLine("[PdfImportService] 检测到 '中' (BOC)"); }
                        else if (remainingSeatLine.Contains("交")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.COMM; Debug.WriteLine("[PdfImportService] 检测到 '交' (COMM)"); }
                        else if (remainingSeatLine.Contains("农")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.ABC; Debug.WriteLine("[PdfImportService] 检测到 '农' (ABC)"); }
                        else if (remainingSeatLine.Contains("建")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.CCB; Debug.WriteLine("[PdfImportService] 检测到 '建' (CCB)"); }
                        else if (remainingSeatLine.Contains("工")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.ICBC; Debug.WriteLine("[PdfImportService] 检测到 '工' (ICBC)"); }
                        else if (remainingSeatLine.Contains("支")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.Alipay; Debug.WriteLine("[PdfImportService] 检测到 '支' (Alipay)"); }
                        else if (remainingSeatLine.Contains("微")) { ticket.PaymentChannelFlags |= (int)PaymentChannelFlags.WeChat; Debug.WriteLine("[PdfImportService] 检测到 '微' (WeChat)"); }

                    }
                    else { Debug.WriteLine($"[PdfImportService] Seat line (Index {seatLineIndex}) '{seatLine}' did not match regex."); }
                }
                else { Debug.WriteLine($"[PdfImportService] Not enough lines to process seat info (Index {seatLineIndex}). Total lines: {lines.Length}."); }

                Debug.WriteLine("[PdfImportService] Finished parsing attempts.");
                return ticket;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfImportService] *** EXCEPTION during parsing: {ex.Message} *** StackTrace: {ex.StackTrace}");
                LogHelper.LogError($"解析PDF内容时出错: {ex.Message}");
                return null; // 返回 null 很可能导致 UI 字段变为空白
            }
        }

        /// <summary>
        /// Maps extracted seat type text to standardized form values.
        /// </summary>
        private string MapSeatType(string extractedType)
        {
            // Simple mapping, expand as needed
            switch (extractedType)
            {
                case "硬座":
                    return "新空调硬座"; // Or just "硬座" if ViewModel/UI expects that
                case "软座":
                    return "软座";
                case "硬卧":
                    return "新空调硬卧";
                case "软卧":
                    return "新空调软卧";
                case "一等座":
                    return "一等座";
                case "二等座":
                    return "二等座";
                case "商务座":
                    return "商务座";
                // Add other mappings (e.g., for 无座, 动卧, 高级软卧, etc.)
                default:
                    return extractedType; // Return original if no map found
            }
        }

        /// <summary>
        /// 保存车票信息到数据库
        /// </summary>
        /// <param name="ticket">车票信息</param>
        /// <returns>保存结果</returns>
        public async Task<bool> SaveTicketAsync(TrainRideInfo ticket)
        {
            try
            {
                if (ticket == null)
                    return false;

                // 使用数据库服务保存车票信息
                await _databaseService.AddTicketAsync(ticket);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"保存车票信息时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 丰富站点信息
        /// </summary>
        /// <param name="ticket">车票信息</param>
        private async void EnrichStationInfo(TrainRideInfo ticket)
        {
            try
            {
                // 确保车站搜索服务初始化完成
                if (_stationSearchService != null && !_stationSearchService.IsInitialized)
                {
                    await _stationSearchService.InitializeAsync();
                }

                // 查找出发站信息
                if (!string.IsNullOrEmpty(ticket.DepartStation))
                {
                    var departStation = await _stationSearchService.GetClosestStationMatchAsync(ticket.DepartStation);
                    if (departStation != null)
                    {
                        ticket.DepartStationCode = departStation.StationCode;
                        ticket.DepartStationPinyin = departStation.StationPinyin;
                    }
                }

                // 查找到达站信息
                if (!string.IsNullOrEmpty(ticket.ArriveStation))
                {
                    var arriveStation = await _stationSearchService.GetClosestStationMatchAsync(ticket.ArriveStation);
                    if (arriveStation != null)
                    {
                        ticket.ArriveStationCode = arriveStation.StationCode;
                        ticket.ArriveStationPinyin = arriveStation.StationPinyin;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"丰富站点信息时出错: {ex.Message}");
            }
        }
    }
} 