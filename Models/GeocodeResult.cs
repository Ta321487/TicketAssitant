using System;

namespace TA_WPF.Models
{
    /// <summary>
    /// 高德地图地理编码结果模型
    /// </summary>
    public class GeocodeResult
    {
        /// <summary>
        /// 格式化后的地址
        /// </summary>
        public string FormattedAddress { get; set; }

        /// <summary>
        /// 省份
        /// </summary>
        public string Province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// 区县
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public string Longitude { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        public string Latitude { get; set; }

        /// <summary>
        /// 重写ToString方法，返回格式化的地址
        /// </summary>
        /// <returns>格式化的地址</returns>
        public override string ToString()
        {
            return FormattedAddress;
        }
    }
} 