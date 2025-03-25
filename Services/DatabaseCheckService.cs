using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 数据库检查服务，负责检查数据库表是否存在
    /// </summary>
    public class DatabaseCheckService
    {
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public DatabaseCheckService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// 检查必要的表是否存在，如果不存在则提示用户创建
        /// </summary>
        public async Task CheckRequiredTablesAsync()
        {
            try
            {
                bool stationTableExists = await _databaseService.TableExistsAsync("station_info");
                bool ticketTableExists = await _databaseService.TableExistsAsync("train_ride_info");
                
                if (!stationTableExists || !ticketTableExists)
                {
                    // 构建提示消息
                    string message = "数据库中缺少必要的表：\n";
                    if (!stationTableExists)
                    {
                        message += "- 车站信息表 (station_info)\n";
                    }
                    if (!ticketTableExists)
                    {
                        message += "- 车票信息表 (train_ride_info)\n";
                    }
                    message += "\n是否立即创建这些表？";
                    
                    // 显示确认对话框
                    var result = TA_WPF.Views.MessageDialog.Show(
                        message,
                        "缺少必要的表",
                        TA_WPF.Views.MessageType.Question,
                        TA_WPF.Views.MessageButtons.YesNo);
                    
                    if (result == true)
                    {
                        // 创建缺少的表
                        if (!stationTableExists)
                        {
                            await _databaseService.CreateStationInfoTableAsync();
                        }
                        if (!ticketTableExists)
                        {
                            await _databaseService.CreateTrainRideInfoTableAsync();
                        }
                        
                        TA_WPF.Views.MessageDialog.Show(
                            "表创建成功！",
                            "操作成功",
                            TA_WPF.Views.MessageType.Information,
                            TA_WPF.Views.MessageButtons.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查表时出错: {ex.Message}");
                LogHelper.LogError($"检查表时出错: {ex.Message}");
                TA_WPF.Views.MessageDialog.Show(
                    $"检查数据库表时出错: {ex.Message}",
                    "错误",
                    TA_WPF.Views.MessageType.Error,
                    TA_WPF.Views.MessageButtons.Ok);
            }
        }
    }
} 