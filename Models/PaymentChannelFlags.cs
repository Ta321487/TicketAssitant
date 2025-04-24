namespace TA_WPF.Models
{
    [Flags]
    public enum PaymentChannelFlags
    {
        None = 0,
        Alipay = 1,       // 支付宝售票
        WeChat = 2,       // 微信售票
        ABC = 4,          // 农业银行
        CCB = 8,          // 建设银行
        ICBC = 16,        // 工商银行
        CMB = 32,         // 招商银行
        PSBC = 64,        //中国邮政存储银行
        BOC = 128,       //中国银行
        COMM = 256,     //交通银行

    }
}