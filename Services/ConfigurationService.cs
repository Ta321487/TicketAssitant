using System.Configuration;
using System.Globalization;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 配置服务，负责管理应用程序的配置设置
    /// </summary>
    public class ConfigurationService
    {
        /// <summary>
        /// 保存字体大小到配置文件
        /// </summary>
        /// <param name="fontSize">字体大小</param>
        public void SaveFontSizeToConfig(double fontSize)
        {
            try
            {
                // 保存字体大小到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // 确保字体大小不小于最小可读值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }

                if (config.AppSettings.Settings["FontSize"] == null)
                {
                    config.AppSettings.Settings.Add("FontSize", fontSize.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    config.AppSettings.Settings["FontSize"].Value = fontSize.ToString(CultureInfo.InvariantCulture);
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                // 确保配置文件被正确写入
                try
                {
                    // 验证配置文件是否已更新
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["FontSize"] != null)
                    {
                        double savedFontSize = double.Parse(config.AppSettings.Settings["FontSize"].Value, CultureInfo.InvariantCulture);
                        if (Math.Abs(savedFontSize - fontSize) > 0.01)
                        {
                            // 如果保存的值与预期不符，再次尝试保存
                            config.AppSettings.Settings["FontSize"].Value = fontSize.ToString(CultureInfo.InvariantCulture);
                            config.Save(ConfigurationSaveMode.Modified);
                            ConfigurationManager.RefreshSection("appSettings");
                            LogHelper.LogInfo($"字体大小设置已重新保存: {fontSize}pt");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"验证字体大小设置时出错: {ex.Message}");
                    LogHelper.LogError($"验证字体大小设置时出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存字体大小设置时出错: {ex.Message}");
                LogHelper.LogError($"保存字体大小设置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从配置文件加载字体大小
        /// </summary>
        /// <returns>字体大小，如果加载失败则返回默认值13</returns>
        public double LoadFontSizeFromConfig()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["FontSize"] != null)
                {
                    if (double.TryParse(config.AppSettings.Settings["FontSize"].Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out double fontSize))
                    {
                        // 确保字体大小不小于最小可读值
                        if (fontSize < 12)
                        {
                            fontSize = 12;
                        }
                        return fontSize;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载字体大小设置时出错: {ex.Message}");
                LogHelper.LogError($"加载字体大小设置时出错: {ex.Message}");
            }

            // 如果加载失败，返回默认值
            return 13;
        }

        /// <summary>
        /// 保存数据库名称到历史记录
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        public void SaveDatabaseNameToHistory(string databaseName)
        {
            try
            {
                // 从配置文件中读取历史数据库名称
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                List<string> historyList = new List<string>();

                // 读取现有历史记录
                if (config.AppSettings.Settings["DatabaseHistory"] != null)
                {
                    string history = config.AppSettings.Settings["DatabaseHistory"].Value;
                    if (!string.IsNullOrEmpty(history))
                    {
                        historyList = history.Split(',').ToList();
                    }
                }

                // 如果历史记录中已存在该数据库名称，则移除它
                historyList.Remove(databaseName);

                // 将新的数据库名称添加到列表开头
                historyList.Insert(0, databaseName);

                // 只保留最近的10个记录
                if (historyList.Count > 10)
                {
                    historyList = historyList.Take(10).ToList();
                }

                // 保存回配置文件
                string newHistory = string.Join(",", historyList);

                if (config.AppSettings.Settings["DatabaseHistory"] == null)
                {
                    config.AppSettings.Settings.Add("DatabaseHistory", newHistory);
                }
                else
                {
                    config.AppSettings.Settings["DatabaseHistory"].Value = newHistory;
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                // 记录日志
                LogHelper.LogSystem("配置", $"已将数据库名称 {databaseName} 添加到历史记录");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存数据库历史记录时出错: {ex.Message}");
                LogHelper.LogSystemError("配置", $"保存数据库历史记录时出错", ex);
            }
        }

        /// <summary>
        /// 保存最后使用的数据库名称
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        public void SaveLastDatabaseName(string databaseName)
        {
            try
            {
                // 保存数据库名称到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config.AppSettings.Settings["LastDatabaseName"] == null)
                {
                    config.AppSettings.Settings.Add("LastDatabaseName", databaseName);
                }
                else
                {
                    config.AppSettings.Settings["LastDatabaseName"].Value = databaseName;
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存数据库名称时出错: {ex.Message}");
                LogHelper.LogSystemError("配置", $"保存数据库名称时出错", ex);
            }
        }

        /// <summary>
        /// 从配置文件加载最后使用的数据库名称
        /// </summary>
        /// <returns>数据库名称，如果加载失败则返回空字符串</returns>
        public string LoadLastDatabaseName()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["LastDatabaseName"] != null)
                {
                    return config.AppSettings.Settings["LastDatabaseName"].Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载数据库名称时出错: {ex.Message}");
                LogHelper.LogSystemError("配置", $"加载数据库名称时出错", ex);
            }

            return string.Empty;
        }

        /// <summary>
        /// 从配置文件加载数据库历史记录
        /// </summary>
        /// <returns>数据库历史记录列表</returns>
        public List<string> LoadDatabaseHistory()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["DatabaseHistory"] != null)
                {
                    string history = config.AppSettings.Settings["DatabaseHistory"].Value;
                    if (!string.IsNullOrEmpty(history))
                    {
                        return history.Split(',').ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载数据库历史记录时出错: {ex.Message}");
                LogHelper.LogSystemError("配置", $"加载数据库历史记录时出错", ex);
            }

            return new List<string>();
        }

        /// <summary>
        /// 从连接字符串中提取数据库名称
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>数据库名称</returns>
        public string ExtractDatabaseName(string connectionString)
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
                LogHelper.LogSystemError("配置", $"提取数据库名称时出错", ex);
            }

            return string.Empty;
        }

        /// <summary>
        /// 解析连接字符串，提取服务器地址、用户名和密码
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>包含服务器地址、用户名和密码的元组</returns>
        public (string ServerAddress, string Username, string Password) ParseConnectionString(string connectionString)
        {
            string serverAddress = "";
            string username = "";
            string password = "";

            try
            {
                // 解析MySQL连接字符串
                // 格式: server=localhost;user=root;password=password;database=mydb
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].ToLower().Trim();
                        var value = keyValue[1].Trim();

                        switch (key)
                        {
                            case "server":
                            case "host":
                                serverAddress = value;
                                break;
                            case "user":
                            case "uid":
                            case "username":
                            case "user id":
                                username = value;
                                break;
                            case "password":
                            case "pwd":
                                password = value;
                                break;
                        }
                    }
                }

                // 记录日志
                LogHelper.LogSystem("配置", "数据库连接信息已解析");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析连接字符串时出错: {ex.Message}");
                LogHelper.LogSystemError("配置", $"解析连接字符串时出错", ex);
            }

            return (serverAddress, username, password);
        }

        /// <summary>
        /// 保存预算金额到配置文件
        /// </summary>
        /// <param name="budgetAmount">预算金额</param>
        public void SaveBudgetAmountToConfig(double budgetAmount)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config.AppSettings.Settings["BudgetAmount"] == null)
                {
                    config.AppSettings.Settings.Add("BudgetAmount", budgetAmount.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    config.AppSettings.Settings["BudgetAmount"].Value = budgetAmount.ToString(CultureInfo.InvariantCulture);
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存预算金额时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从配置文件加载预算金额
        /// </summary>
        /// <returns>预算金额，如果未配置则返回默认值2000</returns>
        public double LoadBudgetAmountFromConfig()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var budgetSetting = config.AppSettings.Settings["BudgetAmount"];

                if (budgetSetting != null &&
                    double.TryParse(budgetSetting.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double budget))
                {
                    return budget;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载预算金额时出错: {ex.Message}");
            }

            return 2000; // 默认预算金额
        }
    }
}