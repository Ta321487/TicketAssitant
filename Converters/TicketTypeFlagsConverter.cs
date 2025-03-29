using System.Globalization;
using System.Windows.Data;
using TA_WPF.Models;

namespace TA_WPF.Converters
{
    public class TicketTypeFlagsConverter : IValueConverter
    {
        // 定义票种类型的中文显示名称
        private static readonly Dictionary<TicketTypeFlags, string> _ticketTypeNames = new Dictionary<TicketTypeFlags, string>
        {
            { TicketTypeFlags.StudentTicket, "学生票" },
            { TicketTypeFlags.DiscountTicket, "优惠票" },
            { TicketTypeFlags.OnlineTicket, "网络售票" },
            { TicketTypeFlags.ChildTicket, "儿童票" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int flags)
            {
                if (flags == 0)
                {
                    return "无";
                }

                var names = new List<string>();
                foreach (TicketTypeFlags flag in Enum.GetValues(typeof(TicketTypeFlags)))
                {
                    if (flag == TicketTypeFlags.None) continue;
                    
                    if ((flags & (int)flag) == (int)flag)
                    {
                        if (_ticketTypeNames.TryGetValue(flag, out string? name))
                        {
                            names.Add(name);
                        }
                        else
                        {
                            names.Add(flag.ToString());
                        }
                    }
                }
                
                return string.Join("、", names);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 