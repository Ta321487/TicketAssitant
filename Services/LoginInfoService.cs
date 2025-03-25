using System.Configuration;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 登录信息服务，负责管理登录相关信息
    /// </summary>
    public class LoginInfoService
    {
        /// <summary>
        /// 保存最后登录时间
        /// </summary>
        public void SaveLastLoginTime()
        {
            try
            {
                // 获取当前时间
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 保存到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings["LastLoginTime"] == null)
                {
                    config.AppSettings.Settings.Add("LastLoginTime", currentTime);
                }
                else
                {
                    config.AppSettings.Settings["LastLoginTime"].Value = currentTime;
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                // 记录日志
                LogHelper.LogSystem("登录", $"用户登录成功，登录时间：{currentTime}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存登录时间时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", $"保存登录时间时出错", ex);
            }
        }
        
        /// <summary>
        /// 获取上次登录时间
        /// </summary>
        /// <returns>上次登录时间，如果没有则返回空字符串</returns>
        public string GetLastLoginTime()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["LastLoginTime"] != null)
                {
                    return config.AppSettings.Settings["LastLoginTime"].Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取上次登录时间时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", $"获取上次登录时间时出错", ex);
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 从连接字符串中提取数据库名称
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>数据库名称</returns>
        public string GetDatabaseName(string connectionString)
        {
            try
            {
                // 解析MySQL连接字符串中的数据库名称
                // 格式: server=localhost;user=root;password=password;database=mydb
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].ToLower().Trim();
                        var value = keyValue[1].Trim();
                        
                        if (key == "database" || key == "initial catalog")
                        {
                            return value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"提取数据库名称时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", $"提取数据库名称时出错", ex);
            }
            
            return "未知";
        }
    }
} 