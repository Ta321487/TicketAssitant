using System;
using System.Collections.Generic;
using System.Linq;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 铁路局帮助类
    /// </summary>
    public static class RailwayBureauHelper
    {
        /// <summary>
        /// 获取所有铁路局列表
        /// </summary>
        public static List<string> GetAllRailwayBureaus()
        {
            // 中国铁路下属的18个铁路局/集团公司列表
            return new List<string>()
            {
                "北京铁路局",
                "上海铁路局",
                "广州铁路集团",
                "哈尔滨铁路局",
                "沈阳铁路局",
                "太原铁路局",
                "郑州铁路局",
                "武汉铁路局",
                "西安铁路局",
                "济南铁路局",
                "南昌铁路局",
                "成都铁路局",
                "昆明铁路局",
                "南宁铁路局",
                "兰州铁路局",
                "乌鲁木齐铁路局",
                "青藏铁路公司",
                "呼和浩特铁路局"
            };
        }

        /// <summary>
        /// 根据输入的文本获取匹配的铁路局列表
        /// </summary>
        /// <param name="inputText">用户输入的文本</param>
        /// <returns>匹配的铁路局列表</returns>
        public static List<string> GetMatchedRailwayBureaus(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return new List<string>();
            }

            return GetAllRailwayBureaus()
                .Where(bureau => bureau.Contains(inputText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 检查指定的铁路局名称是否有效
        /// </summary>
        /// <param name="bureauName">铁路局名称</param>
        /// <returns>是否有效</returns>
        public static bool IsValidRailwayBureau(string bureauName)
        {
            if (string.IsNullOrWhiteSpace(bureauName))
            {
                return false;
            }

            return GetAllRailwayBureaus().Contains(bureauName);
        }

        /// <summary>
        /// 获取最接近的铁路局名称（用于自动补全）
        /// </summary>
        /// <param name="inputText">用户输入的文本</param>
        /// <returns>最接近的铁路局名称，如果没有匹配项则返回null</returns>
        public static string? GetClosestRailwayBureau(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return null;
            }

            var matchedBureaus = GetMatchedRailwayBureaus(inputText);
            
            // 如果有精确匹配项，直接返回
            var exactMatch = matchedBureaus.FirstOrDefault(b => b.Equals(inputText, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // 如果有前缀匹配项，返回第一个
            var prefixMatch = matchedBureaus.FirstOrDefault(b => b.StartsWith(inputText, StringComparison.OrdinalIgnoreCase));
            if (prefixMatch != null)
            {
                return prefixMatch;
            }

            // 否则返回第一个包含输入文本的项
            return matchedBureaus.FirstOrDefault();
        }
    }
} 