using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 网络工具类，提供获取IP地址等网络相关功能
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// 获取本地IP地址
        /// </summary>
        /// <returns>本地IP地址，如果获取失败则返回"127.0.0.1"</returns>
        public static string GetLocalIPAddress()
        {
            try
            {
                // 获取所有网络接口
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                // 遍历所有网络接口
                foreach (var network in networkInterfaces)
                {
                    // 获取IPv4地址
                    var properties = network.GetIPProperties();
                    var address = properties.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(a => a.Address)
                        .FirstOrDefault();

                    if (address != null)
                    {
                        return address.ToString();
                    }
                }

                // 如果上面的方法失败，尝试另一种方法
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取本地IP地址时出错: {ex.Message}");
                LogHelper.LogSystemError("网络", $"获取本地IP地址时出错", ex);
            }

            // 如果所有方法都失败，返回本地回环地址
            return "127.0.0.1";
        }

        /// <summary>
        /// 获取数据库服务器IP地址
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns>数据库服务器IP地址</returns>
        public static string GetDatabaseServerIP(string connectionString)
        {
            try
            {
                // 解析连接字符串中的服务器地址
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].ToLower().Trim();
                        var value = keyValue[1].Trim();

                        if (key == "server" || key == "host")
                        {
                            // 如果是localhost或127.0.0.1，返回实际IP地址
                            if (value.ToLower() == "localhost" || value == "127.0.0.1" || value == "::1")
                            {
                                return GetLocalIPAddress();
                            }
                            return value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取数据库服务器IP地址时出错: {ex.Message}");
                LogHelper.LogSystemError("网络", $"获取数据库服务器IP地址时出错", ex);
            }

            return "未知";
        }
    }
}