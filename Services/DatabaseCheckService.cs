using TA_WPF.Utils;
using TA_WPF.Views;
using System.Diagnostics;

namespace TA_WPF.Services
{
    /// <summary>
    /// 数据库检测服务，负责检测数据库表是否存在
    /// </summary>
    public class DatabaseCheckService
    {
        private readonly DatabaseService _databaseService;
        private List<string> _requiredTables = new List<string> { "station_info", "train_ride_info", "ticket_collections_info", "collection_mapped_tickets_info" }; // 必要的表

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public DatabaseCheckService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// 检测必要的表是否存在，如果不存在则提示用户创建
        /// </summary>
        public async Task CheckRequiredTablesAsync()
        {
            try
            {
                // 检查表是否存在
                bool stationTableExists = await _databaseService.TableExistsAsync("station_info");
                bool ticketTableExists = await _databaseService.TableExistsAsync("train_ride_info");
                bool ticketCollectionsTableExists = await _databaseService.TableExistsAsync("ticket_collections_info");
                bool collectionMappedTicketsTableExists = await _databaseService.TableExistsAsync("collection_mapped_tickets_info");

                if (!stationTableExists || !ticketTableExists || !ticketCollectionsTableExists || !collectionMappedTicketsTableExists)
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
                    if (!ticketCollectionsTableExists)
                    {
                        message += "- 车票收藏夹信息表 (ticket_collections_info)\n";
                    }
                    if (!collectionMappedTicketsTableExists)
                    {
                        message += "- 收藏夹与车票关联表 (collection_mapped_tickets_info)\n";
                    }
                    message += "\n是否立即创建这些表？";

                    // 显示确认对话框
                    var result = MessageDialog.Show(
                        message,
                        "缺少必要的表",
                        MessageType.Question,
                        MessageButtons.YesNo);

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
                        if (!ticketCollectionsTableExists)
                        {
                            await _databaseService.CreateTicketCollectionsInfoTableAsync();
                        }
                        if (!collectionMappedTicketsTableExists)
                        {
                            await _databaseService.CreateCollectionMappedTicketsInfoTableAsync();
                        }

                        MessageDialog.Show(
                            "表创建成功！",
                            "操作成功",
                            MessageType.Information,
                            MessageButtons.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测表时出错: {ex.Message}");
                LogHelper.LogError($"检测表时出错: {ex.Message}");
                MessageDialog.Show(
                    $"检测数据库表时出错: {ex.Message}",
                    "错误",
                    MessageType.Error,
                    MessageButtons.Ok);
            }
        }
    }
}