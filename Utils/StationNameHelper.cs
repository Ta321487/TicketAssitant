using System;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 车站名称处理工具类
    /// </summary>
    public static class StationNameHelper
    {
        /// <summary>
        /// 移除车站名称中的"站"后缀
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>标准化后的车站名称</returns>
        public static string RemoveStationSuffix(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return stationName;
            }
            
            return stationName.Replace("站", "");
        }
        
        /// <summary>
        /// 确保车站名称以"站"结尾
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>标准化后的车站名称</returns>
        public static string EnsureStationSuffix(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return stationName;
            }
            
            if (!stationName.EndsWith("站"))
            {
                return stationName + "站";
            }
            
            return stationName;
        }
        
        /// <summary>
        /// 标准化车站名称（去除空格和其他无效字符）
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>标准化后的车站名称</returns>
        public static string NormalizeStationName(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return stationName;
            }
            
            // 移除首尾空格
            string normalized = stationName.Trim();
            
            return normalized;
        }
    }
} 