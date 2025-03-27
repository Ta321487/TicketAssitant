using System;
using System.Configuration;
using System.Globalization;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 配置文件工具类，提供通用的配置文件操作方法
    /// </summary>
    public static class ConfigUtils
    {
        /// <summary>
        /// 保存字符串值到配置文件
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        /// <returns>是否保存成功</returns>
        public static bool SaveStringValue(string key, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                    
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config.AppSettings.Settings[key] == null)
                {
                    config.AppSettings.Settings.Add(key, value);
                }
                else
                {
                    config.AppSettings.Settings[key].Value = value;
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置'{key}'时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 保存数值到配置文件
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        /// <returns>是否保存成功</returns>
        public static bool SaveDoubleValue(string key, double value)
        {
            return SaveStringValue(key, value.ToString(CultureInfo.InvariantCulture));
        }
        
        /// <summary>
        /// 保存布尔值到配置文件
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        /// <returns>是否保存成功</returns>
        public static bool SaveBoolValue(string key, bool value)
        {
            return SaveStringValue(key, value.ToString().ToLower());
        }
        
        /// <summary>
        /// 从配置文件读取字符串值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>读取的字符串值，如果不存在则返回默认值</returns>
        public static string GetStringValue(string key, string defaultValue = "")
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return defaultValue;
                    
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings[key] != null)
                {
                    return config.AppSettings.Settings[key].Value;
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取配置'{key}'时出错: {ex.Message}");
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 从配置文件读取数值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>读取的数值，如果不存在或解析失败则返回默认值</returns>
        public static double GetDoubleValue(string key, double defaultValue = 0)
        {
            string value = GetStringValue(key);
            
            if (string.IsNullOrEmpty(value) || !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return defaultValue;
            }
            
            return result;
        }
        
        /// <summary>
        /// 从配置文件读取布尔值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>读取的布尔值，如果不存在或解析失败则返回默认值</returns>
        public static bool GetBoolValue(string key, bool defaultValue = false)
        {
            string value = GetStringValue(key);
            
            if (string.IsNullOrEmpty(value) || !bool.TryParse(value, out bool result))
            {
                return defaultValue;
            }
            
            return result;
        }
    }
} 