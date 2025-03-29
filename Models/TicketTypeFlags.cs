namespace TA_WPF.Models
{
    [Flags]
    public enum TicketTypeFlags
    {
        None = 0,
        StudentTicket = 1,    // 学生票
        DiscountTicket = 2,   // 优惠票
        OnlineTicket = 4,     // 网络售票
        ChildTicket = 8       // 儿童票
    }
} 