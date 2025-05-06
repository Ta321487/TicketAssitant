using System;
using System.Collections.Generic;
using System.Linq;
using TA_WPF.Models;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 车站等级帮助类
    /// </summary>
    public static class StationLevelHelper
    {
        /// <summary>
        /// 获取所有车站等级列表（用于下拉框显示）
        /// </summary>
        /// <returns>车站等级文本和值的键值对列表</returns>
        public static List<KeyValuePair<int, string>> GetStationLevels()
        {
            return new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>((int)StationLevelEnum.Special, "特等站"),
                new KeyValuePair<int, string>((int)StationLevelEnum.FirstClass, "一等站"),
                new KeyValuePair<int, string>((int)StationLevelEnum.SecondClass, "二等站"),
                new KeyValuePair<int, string>((int)StationLevelEnum.ThirdClass, "三等站"),
                new KeyValuePair<int, string>((int)StationLevelEnum.FourthClass, "四等站"),
                new KeyValuePair<int, string>((int)StationLevelEnum.FifthClass, "五等站")
            };
        }

        /// <summary>
        /// 根据车站等级枚举值获取对应的显示文本
        /// </summary>
        /// <param name="level">车站等级枚举值</param>
        /// <returns>车站等级的显示文本</returns>
        public static string GetStationLevelText(StationLevelEnum level)
        {
            switch (level)
            {
                case StationLevelEnum.Special:
                    return "特等站";
                case StationLevelEnum.FirstClass:
                    return "一等站";
                case StationLevelEnum.SecondClass:
                    return "二等站";
                case StationLevelEnum.ThirdClass:
                    return "三等站";
                case StationLevelEnum.FourthClass:
                    return "四等站";
                case StationLevelEnum.FifthClass:
                    return "五等站";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 根据车站等级整数值获取对应的显示文本
        /// </summary>
        /// <param name="level">车站等级整数值</param>
        /// <returns>车站等级的显示文本</returns>
        public static string GetStationLevelText(int level)
        {
            return GetStationLevelText((StationLevelEnum)level);
        }

        /// <summary>
        /// 根据车站等级的显示文本获取对应的枚举值
        /// </summary>
        /// <param name="levelText">车站等级的显示文本</param>
        /// <returns>车站等级枚举值</returns>
        public static StationLevelEnum GetStationLevel(string levelText)
        {
            switch (levelText)
            {
                case "特等站":
                    return StationLevelEnum.Special;
                case "一等站":
                    return StationLevelEnum.FirstClass;
                case "二等站":
                    return StationLevelEnum.SecondClass;
                case "三等站":
                    return StationLevelEnum.ThirdClass;
                case "四等站":
                    return StationLevelEnum.FourthClass;
                case "五等站":
                    return StationLevelEnum.FifthClass;
                default:
                    return StationLevelEnum.Unknown;
            }
        }
    }
} 