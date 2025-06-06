using System.Diagnostics;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.Services
{
    /// <summary>
    /// 数据库检测服务，负责检测数据库表是否存在
    /// </summary>
    public class DatabaseCheckService
    {
        private readonly DatabaseService _databaseService;
        private List<string> _requiredTables = new List<string> { "station_info", "train_ride_info", "ticket_collections_info", "collection_mapped_tickets_info", "route_info", "route_ticket_mapping", "route_station_mapping", "route_statistics" }; // 必要的表

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

                // 检查路线相关表是否存在
                bool routeInfoTableExists = await _databaseService.TableExistsAsync("route_info");
                bool routeTicketMappingTableExists = await _databaseService.TableExistsAsync("route_ticket_mapping");
                bool routeStationMappingTableExists = await _databaseService.TableExistsAsync("route_station_mapping");
                bool routeStatisticsTableExists = await _databaseService.TableExistsAsync("route_statistics");

                // 检查是否所有必要的表都存在
                bool allTablesExist = stationTableExists && ticketTableExists && ticketCollectionsTableExists &&
                                    collectionMappedTicketsTableExists && routeInfoTableExists &&
                                    routeTicketMappingTableExists && routeStationMappingTableExists &&
                                    routeStatisticsTableExists;

                // 如果有表不存在，提示用户创建
                if (!allTablesExist)
                {
                    string missingTables = string.Join(", ", new[]
                    {
                        !stationTableExists ? "车站信息表" : null,
                        !ticketTableExists ? "车票信息表" : null,
                        !ticketCollectionsTableExists ? "收藏夹信息表" : null,
                        !collectionMappedTicketsTableExists ? "收藏夹-车票映射表" : null,
                        !routeInfoTableExists ? "路线信息表" : null,
                        !routeTicketMappingTableExists ? "路线-车票映射表" : null,
                        !routeStationMappingTableExists ? "路线-车站映射表" : null,
                        !routeStatisticsTableExists ? "路线统计信息表" : null
                    }.Where(t => t != null));

                    // 弹出对话框询问是否创建
                    bool? result = MessageDialog.Show(
                        $"数据库中缺少以下表：{missingTables}，是否创建？",
                        "表不存在",
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
                        if (!routeInfoTableExists)
                        {
                            await _databaseService.CreateRouteInfoTableAsync();
                        }
                        if (!routeTicketMappingTableExists)
                        {
                            await _databaseService.CreateRouteTicketMappingTableAsync();
                        }
                        if (!routeStationMappingTableExists)
                        {
                            await _databaseService.CreateRouteStationMappingTableAsync();
                        }
                        if (!routeStatisticsTableExists)
                        {
                            await _databaseService.CreateRouteStatisticsTableAsync();
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