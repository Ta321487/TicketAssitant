using System.Globalization;
using System.Windows.Data;
using TA_WPF.Models;

namespace TA_WPF.Converters
{
    public class PaymentChannelFlagsConverter : IValueConverter
    {
        // 定义支付渠道的中文显示名称
        private static readonly Dictionary<PaymentChannelFlags, string> _paymentChannelNames = new Dictionary<PaymentChannelFlags, string>
        {
            { PaymentChannelFlags.Alipay, "支付宝" },
            { PaymentChannelFlags.WeChat, "微信" },
            { PaymentChannelFlags.ABC, "农业银行" },
            { PaymentChannelFlags.CCB, "建设银行" },
            { PaymentChannelFlags.ICBC, "工商银行" }
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
                foreach (PaymentChannelFlags flag in Enum.GetValues(typeof(PaymentChannelFlags)))
                {
                    if (flag == PaymentChannelFlags.None) continue;

                    if ((flags & (int)flag) == (int)flag)
                    {
                        if (_paymentChannelNames.TryGetValue(flag, out string? name))
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