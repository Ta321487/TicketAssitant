using System;

namespace TA_WPF.Models
{
    /// <summary>
    /// 车站等级枚举
    /// </summary>
    [Flags]
    public enum StationLevelEnum
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// 特等站
        /// </summary>
        Special = 1,
        
        /// <summary>
        /// 一等站
        /// </summary>
        FirstClass = 2,
        
        /// <summary>
        /// 二等站
        /// </summary>
        SecondClass = 4,
        
        /// <summary>
        /// 三等站
        /// </summary>
        ThirdClass = 8,
        
        /// <summary>
        /// 四等站
        /// </summary>
        FourthClass = 16,
        
        /// <summary>
        /// 五等站
        /// </summary>
        FifthClass = 32
    }
} 