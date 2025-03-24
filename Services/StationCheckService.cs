using System;

namespace TA_WPF.Services
{
    /// <summary>
    /// 车站检查服务，用于管理是否忽略车站表检查
    /// </summary>
    public class StationCheckService
    {
        private static StationCheckService _instance;
        private bool _ignoreStationCheck = false;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static StationCheckService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StationCheckService();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private StationCheckService()
        {
        }

        /// <summary>
        /// 是否忽略车站表检查
        /// </summary>
        public bool IgnoreStationCheck
        {
            get { return _ignoreStationCheck; }
            set { _ignoreStationCheck = value; }
        }

        /// <summary>
        /// 设置忽略车站表检查
        /// </summary>
        public void SetIgnoreStationCheck()
        {
            _ignoreStationCheck = true;
        }

        /// <summary>
        /// 重置忽略车站表检查状态
        /// </summary>
        public void ResetIgnoreStationCheck()
        {
            _ignoreStationCheck = false;
        }
    }
} 