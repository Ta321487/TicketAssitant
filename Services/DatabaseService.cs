using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;
using TA_WPF.Models;
using TA_WPF.Utils;
using TA_WPF.ViewModels; // 添加引用，以使用SeatPositionType枚举

namespace TA_WPF.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private const int MaxRetryCount = 1; // 最大重试次数
        private const int RetryDelayMs = 500; // 重试延迟（毫秒）

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 创建数据库连接并尝试打开，带有自动重试功能
        /// </summary>
        /// <returns>已打开的数据库连接</returns>
        private async Task<MySqlConnection> GetOpenConnectionWithRetryAsync()
        {
            MySqlConnection connection = new MySqlConnection(_connectionString);
            int retryCount = 0;

            while (true)
            {
                try
                {
                    await connection.OpenAsync();
                    return connection; // 成功打开连接
                }
                catch (MySqlException ex)
                {
                    retryCount++;

                    // 检查是否为认证问题
                    bool isAuthIssue = ex.Message.Contains("Access denied") ||
                                       ex.Message.Contains("Authentication") ||
                                       ex.Message.Contains("using method");

                    if (isAuthIssue)
                    {
                        // 记录详细的认证错误信息
                        LogHelper.LogSystemError("数据库", $"认证失败: {ex.Message}, 错误代码: {ex.Number}", ex);

                        // 如果是认证问题，直接抛出异常，因为重试可能无法解决
                        throw;
                    }

                    if (retryCount >= MaxRetryCount)
                    {
                        // 记录详细的最终错误信息
                        LogHelper.LogSystemError("数据库", $"连接失败。错误: {ex.Message}, 错误代码: {ex.Number}", ex);
                        throw; // 重新抛出异常
                    }

                    // 记录重试信息
                    LogHelper.LogSystemWarning("数据库", $"连接失败，正在重试。错误: {ex.Message}");

                    // 等待一段时间后重试
                    await Task.Delay(RetryDelayMs);
                }
            }
        }


        public async Task<List<StationInfo>> GetStationsAsync()
        {
            // 调用分页方法获取所有记录
            // 传递最大可能的页大小，确保获取所有记录
            return await GetStationsAsync(1, int.MaxValue, "station_name", true);
        }

        /// <summary>
        /// 获取分页的车站信息
        /// </summary>
        /// <param name="pageNumber">页码 (从1开始)</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序字段</param>
        /// <param name="ascending">是否升序</param>
        /// <returns>车站信息列表</returns>
        public async Task<List<StationInfo>> GetStationsAsync(int pageNumber, int pageSize, string orderBy = "id", bool ascending = true)
        {
            var items = new List<StationInfo>();
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10; // Default page size

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建排序方向
                    string direction = ascending ? "ASC" : "DESC";

                    // 显式列出所有需要的列
                    string columns = @"`id`, `station_name`, `province`, `city`, `district`, 
                                     `longitude`, `latitude`, `station_code`, `station_pinyin`, 
                                     `station_level`, `railway_bureau`";

                    // 构建查询语句
                    string query = $"SELECT {columns} FROM station_info ";

                    // 添加排序
                    query += $"ORDER BY `{orderBy}` {direction} ";

                    // 只有分页查询需要LIMIT子句
                    if (pageSize < int.MaxValue)
                    {
                        query += "LIMIT @Offset, @PageSize";
                    }

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 只有分页查询需要添加这些参数
                        if (pageSize < int.MaxValue)
                        {
                            command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                            command.Parameters.AddWithValue("@PageSize", pageSize);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(MapStationInfo(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取车站列表失败: {ex.Message}", ex);
                throw;
            }

            return items;
        }

        /// <summary>
        /// 根据条件构建车站查询
        /// </summary>
        /// <param name="command">MySqlCommand对象</param>
        /// <param name="whereClause">WHERE子句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>车站信息列表</returns>
        private async Task<List<StationInfo>> QueryStationsAsync(MySqlCommand command, string whereClause = null, Dictionary<string, object> parameters = null)
        {
            var items = new List<StationInfo>();

            try
            {
                string query = "SELECT * FROM station_info";

                // 添加WHERE子句
                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    query += $" WHERE {whereClause}";
                }

                command.CommandText = query;

                // 添加参数
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(MapStationInfo(reader));
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"查询车站失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取车站总数
        /// </summary>
        /// <returns>车站总数</returns>
        public async Task<int> GetStationCountAsync()
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand("SELECT COUNT(*) FROM station_info", connection))
                    {
                        // Set timeout for count operation as well
                        command.CommandTimeout = 15; // 15 seconds
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取车站总数时出错: {ex.Message}", ex);
                throw new Exception($"获取车站总数时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据名称获取车站信息
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>车站信息，如果不存在则返回null</returns>
        public async Task<StationInfo> GetStationByNameAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return null;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "@StationName", stationName }
                    };

                    using (var command = new MySqlCommand(null, connection))
                    {
                        var results = await QueryStationsAsync(command, "station_name = @StationName", parameters);
                        return results.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"根据名称获取车站信息失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 根据部分名称搜索车站
        /// </summary>
        /// <param name="partialName">部分名称</param>
        /// <returns>匹配的车站列表</returns>
        public async Task<List<StationInfo>> SearchStationsByNameAsync(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                return new List<StationInfo>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 修改查询：使用子查询和MIN(id)来获取每个站名的唯一记录
                    string query = @"SELECT * FROM station_info 
                                    WHERE id IN (
                                        SELECT MIN(id) 
                                        FROM station_info
                                        WHERE station_name LIKE @PartialName
                                        GROUP BY station_name
                                    )
                                    ORDER BY LENGTH(station_name), station_name
                                    LIMIT 10";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 设置命令超时为10秒
                        command.CommandTimeout = 10;
                        command.Parameters.AddWithValue("@PartialName", "%" + partialName + "%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var results = new List<StationInfo>();
                            while (await reader.ReadAsync())
                            {
                                results.Add(MapStationInfo(reader));
                            }
                            return results;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索车站失败: {ex.Message}", ex);
                return new List<StationInfo>();
            }
        }

        /// <summary>
        /// 添加车站信息
        /// </summary>
        /// <param name="station">车站信息</param>
        /// <returns>添加是否成功</returns>
        public async Task<bool> AddStationAsync(StationInfo station)
        {
            if (station == null) throw new ArgumentNullException(nameof(station));

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    return await InsertStationInternalAsync(station, connection, null);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"添加车站信息失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 批量添加车站信息
        /// </summary>
        /// <param name="stations">车站信息列表</param>
        /// <returns>添加成功的数量</returns>
        public async Task<int> AddStationsAsync(List<StationInfo> stations)
        {
            if (stations == null || !stations.Any()) return 0;

            int successCount = 0;
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 使用事务以提高批量插入效率
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var station in stations)
                        {
                            try
                            {
                                bool success = await InsertStationInternalAsync(station, connection, transaction as MySqlTransaction);
                                if (success)
                                {
                                    successCount++;
                                }
                            }
                            catch (MySqlException ex)
                            {
                                // 记录具体哪个车站添加失败
                                LogHelper.LogError($"添加车站 {station.StationName} 失败: {ex.Message}", ex);
                                // 继续处理下一个，不中断事务
                            }
                        }

                        // 提交事务
                        await transaction.CommitAsync();
                    }
                }

                return successCount;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"批量添加车站信息失败: {ex.Message}", ex);
                return successCount; // 返回已经成功添加的数量
            }
        }

        /// <summary>
        /// 内部方法：插入车站数据
        /// </summary>
        /// <param name="station">车站信息</param>
        /// <param name="connection">数据库连接</param>
        /// <param name="transaction">事务</param>
        /// <returns>是否成功</returns>
        private async Task<bool> InsertStationInternalAsync(StationInfo station, MySqlConnection connection, MySqlTransaction transaction)
        {
            string query = @"INSERT INTO station_info 
                           (station_name, province, city, district, longitude, latitude, station_code, station_pinyin, station_level, railway_bureau) 
                           VALUES 
                           (@StationName, @Province, @City, @District, @Longitude, @Latitude, @StationCode, @StationPinyin, @StationLevel, @RailwayBureau)";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@StationName", station.StationName);
                command.Parameters.AddWithValue("@Province", station.Province ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@City", station.City ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@District", station.District ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Longitude", station.Longitude ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Latitude", station.Latitude ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@StationCode", station.StationCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@StationPinyin", station.StationPinyin ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@StationLevel", station.StationLevel);
                command.Parameters.AddWithValue("@RailwayBureau", station.RailwayBureau ?? (object)DBNull.Value);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// 根据ID列表删除车站
        /// </summary>
        /// <param name="stationIds">车站ID列表</param>
        /// <returns>是否成功删除</returns>
        public async Task<bool> DeleteStationsByIdsAsync(List<int> stationIds)
        {
            if (stationIds == null || stationIds.Count == 0)
            {
                return true;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建包含所有ID的IN子句
                    string idList = string.Join(",", stationIds);
                    string query = $"DELETE FROM station_info WHERE id IN ({idList})";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"批量删除车站失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 重置车站表的自增ID
        /// </summary>
        /// <returns>是否重置成功</returns>
        public async Task<bool> ResetStationsAutoIncrementAsync()
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 获取当前最大ID
                    int maxId = 0;
                    using (var countCommand = new MySqlCommand("SELECT COALESCE(MAX(id), 0) FROM station_info", connection))
                    {
                        object result = await countCommand.ExecuteScalarAsync();
                        maxId = Convert.ToInt32(result);
                    }

                    // 重置自增值为最大ID+1，确保没有空洞
                    string query = $"ALTER TABLE station_info AUTO_INCREMENT = {maxId + 1}";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"重置车站表自增ID失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 将 DataReader 映射到 StationInfo 对象
        /// </summary>
        private StationInfo MapStationInfo(DbDataReader reader)
        {
            return new StationInfo
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                StationName = reader.IsDBNull(reader.GetOrdinal("station_name")) ? null : reader.GetString(reader.GetOrdinal("station_name")),
                Province = reader.IsDBNull(reader.GetOrdinal("province")) ? null : reader.GetString(reader.GetOrdinal("province")),
                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                District = reader.IsDBNull(reader.GetOrdinal("district")) ? null : reader.GetString(reader.GetOrdinal("district")),
                Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetString(reader.GetOrdinal("longitude")),
                Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetString(reader.GetOrdinal("latitude")),
                StationCode = reader.IsDBNull(reader.GetOrdinal("station_code")) ? null : reader.GetString(reader.GetOrdinal("station_code")),
                StationPinyin = reader.IsDBNull(reader.GetOrdinal("station_pinyin")) ? null : reader.GetString(reader.GetOrdinal("station_pinyin")),
                StationLevel = reader.IsDBNull(reader.GetOrdinal("station_level")) ? 0 : reader.GetInt32(reader.GetOrdinal("station_level")),
                RailwayBureau = reader.IsDBNull(reader.GetOrdinal("railway_bureau")) ? null : reader.GetString(reader.GetOrdinal("railway_bureau"))
            };
        }

        public async Task AddTicketAsync(TrainRideInfo ticket)
        {
            // 创建带有超时设置的连接字符串
            var builder = new MySqlConnectionStringBuilder(_connectionString)
            {
                ConnectionTimeout = 10 // 设置连接超时为10秒
            };

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                try
                {
                    string query = @"INSERT INTO train_ride_info (
                                    ticket_number, check_in_location, depart_station, train_no, 
                                    arrive_station, depart_station_pinyin, arrive_station_pinyin, 
                                    depart_date, depart_time, coach_no, seat_no, money, 
                                    seat_type, additional_info, ticket_purpose, hint, 
                                    depart_station_code, arrive_station_code, ticket_modification_type,
                                    ticket_type_flags, payment_channel_flags)
                                  VALUES (
                                    @TicketNumber, @CheckInLocation, @DepartStation, @TrainNo, 
                                    @ArriveStation, @DepartStationPinyin, @ArriveStationPinyin, 
                                    @DepartDate, @DepartTime, @CoachNo, @SeatNo, @Money, 
                                    @SeatType, @AdditionalInfo, @TicketPurpose, @Hint, 
                                    @DepartStationCode, @ArriveStationCode, @TicketModificationType,
                                    @TicketTypeFlags, @PaymentChannelFlags)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 设置命令超时为5秒
                        command.CommandTimeout = 10;

                        command.Parameters.AddWithValue("@TicketNumber", ticket.TicketNumber);
                        command.Parameters.AddWithValue("@CheckInLocation", ticket.CheckInLocation);
                        command.Parameters.AddWithValue("@DepartStation", ticket.DepartStation);
                        command.Parameters.AddWithValue("@TrainNo", ticket.TrainNo);
                        command.Parameters.AddWithValue("@ArriveStation", ticket.ArriveStation);
                        command.Parameters.AddWithValue("@DepartStationPinyin", ticket.DepartStationPinyin);
                        command.Parameters.AddWithValue("@ArriveStationPinyin", ticket.ArriveStationPinyin);
                        command.Parameters.AddWithValue("@DepartDate", ticket.DepartDate);
                        command.Parameters.AddWithValue("@DepartTime", ticket.DepartTime);
                        command.Parameters.AddWithValue("@CoachNo", ticket.CoachNo);
                        command.Parameters.AddWithValue("@SeatNo", ticket.SeatNo);
                        command.Parameters.AddWithValue("@Money", ticket.Money);
                        command.Parameters.AddWithValue("@SeatType", ticket.SeatType);
                        command.Parameters.AddWithValue("@AdditionalInfo", ticket.AdditionalInfo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TicketPurpose", ticket.TicketPurpose ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Hint", ticket.Hint ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DepartStationCode", ticket.DepartStationCode);
                        command.Parameters.AddWithValue("@ArriveStationCode", ticket.ArriveStationCode);
                        command.Parameters.AddWithValue("@TicketModificationType", ticket.TicketModificationType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TicketTypeFlags", ticket.TicketTypeFlags);
                        command.Parameters.AddWithValue("@PaymentChannelFlags", ticket.PaymentChannelFlags);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    // 记录详细的数据库错误信息
                    Debug.WriteLine($"数据库错误: {ex.Message}, 错误代码: {ex.Number}");
                    throw; // 重新抛出异常以便上层处理
                }
            }
        }

        public async Task<List<TrainRideInfo>> GetAllTrainRideInfosAsync()
        {
            var items = new List<TrainRideInfo>();

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 获取所有数据
                string query = "SELECT * FROM train_ride_info ORDER BY id";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(MapTrainRideInfo(reader));
                        }
                    }
                }
            }

            return items;
        }

        // 添加新的分页方法，直接从数据库获取分页数据
        public async Task<List<TrainRideInfo>> GetPagedTrainRideInfosAsync(int pageNumber, int pageSize, string orderBy = "id", bool ascending = true)
        {
            var items = new List<TrainRideInfo>();

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 构建排序方向
                string direction = ascending ? "ASC" : "DESC";

                // 构建查询语句，使用参数化查询防止SQL注入
                // 添加USE INDEX提示以确保使用正确的索引
                string query = $@"SELECT * FROM train_ride_info 
                                USE INDEX (PRIMARY)
                                ORDER BY {orderBy} {direction}
                                LIMIT @Offset, @PageSize";

                using (var command = new MySqlCommand(query, connection))
                {
                    // 设置命令超时时间
                    command.CommandTimeout = 30; // 30秒
                    command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(MapTrainRideInfo(reader));
                        }
                    }
                }
            }

            return items;
        }

        // 获取总记录数的方法
        public async Task<int> GetTotalTrainRideInfoCountAsync()
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand("SELECT COUNT(*) FROM train_ride_info", connection))
                    {
                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取车票总数时出错: {ex.Message}", ex);
            }
        }

        /**
         * 修改 GetFilteredTrainRideInfoCountAsync 方法以支持座位类型筛选
         */
        /// <summary>
        /// 根据筛选条件获取车票总数
        /// </summary>
        /// <param name="departStation">出发车站</param>
        /// <param name="trainNo">车次号</param>
        /// <param name="year">出发年份</param>
        /// <param name="seatPosition">座位位置类型</param>
        /// <param name="isAndCondition">是否使用AND条件</param>
        /// <returns>符合条件的车票总数</returns>
        public async Task<int> GetFilteredTrainRideInfoCountAsync(
            string departStation,
            string trainNo,
            int? year,
            SeatPositionType? seatPosition,
            bool isAndCondition,
            DateTime? departDate = null)
        {
            int count = 0;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建基本查询
                    var query = new StringBuilder();
                    query.Append("SELECT COUNT(*) FROM train_ride_info");

                    // 准备参数列表和有效条件列表
                    var parameters = new Dictionary<string, object>();
                    var conditions = new List<string>();

                    // 检查是否有任何筛选条件
                    bool hasAnyCondition =
                        !string.IsNullOrWhiteSpace(departStation) ||
                        !string.IsNullOrWhiteSpace(trainNo) ||
                        year.HasValue ||
                        departDate.HasValue ||
                        (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None);

                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        conditions.Add("depart_station LIKE @departStation");
                        parameters.Add("@departStation", $"%{departStation}%");
                    }
                    else if (isAndCondition && hasAnyCondition && (!string.IsNullOrWhiteSpace(trainNo) || year.HasValue || departDate.HasValue || (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)))
                    {
                        // 只有在AND模式且至少有一个其他条件时，才添加IS NULL限制
                        conditions.Add("depart_station IS NULL");
                    }

                    // 添加车次号筛选条件
                    if (!string.IsNullOrWhiteSpace(trainNo))
                    {
                        conditions.Add("train_no LIKE @trainNo");
                        parameters.Add("@trainNo", $"%{trainNo}%");
                    }
                    else if (isAndCondition && hasAnyCondition && (!string.IsNullOrWhiteSpace(departStation) || year.HasValue || departDate.HasValue || (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)))
                    {
                        // 只有在AND模式且至少有一个其他条件时，才添加IS NULL限制
                        conditions.Add("train_no IS NULL");
                    }

                    // 添加年份筛选条件
                    if (year.HasValue)
                    {
                        conditions.Add("YEAR(depart_date) = @year");
                        parameters.Add("@year", year.Value);
                    }
                    else if (isAndCondition && hasAnyCondition && !departDate.HasValue &&
                             (!string.IsNullOrWhiteSpace(departStation) || !string.IsNullOrWhiteSpace(trainNo) ||
                              (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)))
                    {
                        // 只有在AND模式且至少有一个其他条件且没有指定具体日期时，才添加IS NULL限制
                        conditions.Add("depart_date IS NULL");
                    }

                    // 添加出发日期筛选条件
                    if (departDate.HasValue)
                    {
                        conditions.Add("depart_date = @departDate");
                        parameters.Add("@departDate", departDate.Value.Date);
                    }

                    // 添加座位位置筛选条件
                    if (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)
                    {
                        if (seatPosition.Value == SeatPositionType.Window)
                        {
                            conditions.Add("(seat_no LIKE '%A' OR seat_no LIKE '%F' OR seat_no LIKE '%1' OR seat_no LIKE '%5')");
                        }
                        else if (seatPosition.Value == SeatPositionType.Aisle)
                        {
                            conditions.Add("(seat_no LIKE '%C' OR seat_no LIKE '%D')");
                        }
                        else if (seatPosition.Value == SeatPositionType.Middle)
                        {
                            conditions.Add("(seat_no LIKE '%B' OR seat_no LIKE '%E')");
                        }
                    }
                    else if (isAndCondition && hasAnyCondition && seatPosition.HasValue && seatPosition.Value == SeatPositionType.None &&
                             (!string.IsNullOrWhiteSpace(departStation) || !string.IsNullOrWhiteSpace(trainNo) || year.HasValue || departDate.HasValue))
                    {
                        // 只有在AND模式且至少有一个其他条件且明确指定了None时，才添加IS NULL限制
                        conditions.Add("seat_no IS NULL");
                    }

                    // 构建WHERE子句（如果有条件）
                    if (conditions.Count > 0)
                    {
                        query.Append(" WHERE ");
                        query.Append(string.Join(isAndCondition ? " AND " : " OR ", conditions));
                    }

                    // 输出完整的SQL查询（包括参数值）
                    var debugSql = query.ToString();
                    foreach (var param in parameters)
                    {
                        debugSql = debugSql.Replace(param.Key, param.Value.ToString());
                    }
                    Debug.WriteLine($"SQL查询计数: {debugSql}");

                    // 创建命令
                    using (var command = new MySqlCommand(query.ToString(), connection))
                    {
                        // 添加参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        // 执行查询并获取总数
                        count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取筛选车票总数失败: {ex.Message}", ex);
                throw;
            }

            return count;
        }

        /**
         * 修改 GetFilteredTrainRideInfosAsync 方法以支持座位类型筛选
         */
        /// <summary>
        /// 根据筛选条件获取分页车票信息
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页记录数</param>
        /// <param name="departStation">出发车站</param>
        /// <param name="trainNo">车次号</param>
        /// <param name="year">出发年份</param>
        /// <param name="seatPosition">座位位置类型</param>
        /// <param name="isAndCondition">是否使用AND条件</param>
        /// <returns>符合条件的车票列表</returns>
        public async Task<List<TrainRideInfo>> GetFilteredTrainRideInfosAsync(
            int pageNumber,
            int pageSize,
            string departStation,
            string trainNo,
            int? year,
            SeatPositionType? seatPosition,
            bool isAndCondition,
            DateTime? departDate = null)
        {
            var tickets = new List<TrainRideInfo>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建基本查询
                    var query = new StringBuilder();
                    query.Append("SELECT * FROM train_ride_info");

                    // 准备参数列表和有效条件列表
                    var parameters = new Dictionary<string, object>();
                    var conditions = new List<string>();

                    // 检查是否有任何筛选条件
                    bool hasAnyCondition =
                        !string.IsNullOrWhiteSpace(departStation) ||
                        !string.IsNullOrWhiteSpace(trainNo) ||
                        year.HasValue ||
                        departDate.HasValue ||
                        (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None);

                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        conditions.Add("depart_station LIKE @departStation");
                        parameters.Add("@departStation", $"%{departStation}%");
                    }
                    else if (isAndCondition && hasAnyCondition && (!string.IsNullOrWhiteSpace(trainNo) || year.HasValue || departDate.HasValue || (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)))
                    {
                        // 只有在AND模式且至少有一个其他条件时，才添加IS NULL限制
                        conditions.Add("depart_station IS NULL");
                    }

                    // 添加车次号筛选条件
                    if (!string.IsNullOrWhiteSpace(trainNo))
                    {
                        conditions.Add("train_no LIKE @trainNo");
                        parameters.Add("@trainNo", $"%{trainNo}%");
                    }
                    else if (isAndCondition && hasAnyCondition && (!string.IsNullOrWhiteSpace(departStation) || year.HasValue || departDate.HasValue || (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)))
                    {
                        // 只有在AND模式且至少有一个其他条件时，才添加IS NULL限制
                        conditions.Add("train_no IS NULL");
                    }

                    // 添加年份筛选条件
                    if (year.HasValue)
                    {
                        conditions.Add("YEAR(depart_date) = @year");
                        parameters.Add("@year", year.Value);
                    }
                    else if (isAndCondition && hasAnyCondition && !departDate.HasValue &&
                             (!string.IsNullOrWhiteSpace(departStation) || !string.IsNullOrWhiteSpace(trainNo) ||
                              (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)))
                    {
                        // 只有在AND模式且至少有一个其他条件且没有指定具体日期时，才添加IS NULL限制
                        conditions.Add("depart_date IS NULL");
                    }

                    // 添加出发日期筛选条件
                    if (departDate.HasValue)
                    {
                        conditions.Add("depart_date = @departDate");
                        parameters.Add("@departDate", departDate.Value.Date);
                    }

                    // 添加座位位置筛选条件
                    if (seatPosition.HasValue && seatPosition.Value != SeatPositionType.None)
                    {
                        if (seatPosition.Value == SeatPositionType.Window)
                        {
                            conditions.Add("(seat_no LIKE '%A' OR seat_no LIKE '%F' OR seat_no LIKE '%1' OR seat_no LIKE '%5')");
                        }
                        else if (seatPosition.Value == SeatPositionType.Aisle)
                        {
                            conditions.Add("(seat_no LIKE '%C' OR seat_no LIKE '%D')");
                        }
                        else if (seatPosition.Value == SeatPositionType.Middle)
                        {
                            conditions.Add("(seat_no LIKE '%B' OR seat_no LIKE '%E')");
                        }
                    }
                    else if (isAndCondition && hasAnyCondition && seatPosition.HasValue && seatPosition.Value == SeatPositionType.None &&
                             (!string.IsNullOrWhiteSpace(departStation) || !string.IsNullOrWhiteSpace(trainNo) || year.HasValue || departDate.HasValue))
                    {
                        // 只有在AND模式且至少有一个其他条件且明确指定了None时，才添加IS NULL限制
                        conditions.Add("seat_no IS NULL");
                    }

                    // 构建WHERE子句（如果有条件）
                    if (conditions.Count > 0)
                    {
                        query.Append(" WHERE ");
                        query.Append(string.Join(isAndCondition ? " AND " : " OR ", conditions));
                    }

                    // 添加排序和分页
                    query.Append(" ORDER BY depart_date DESC, depart_time DESC LIMIT @offset, @limit");
                    parameters.Add("@offset", (pageNumber - 1) * pageSize);
                    parameters.Add("@limit", pageSize);

                    // 输出完整的SQL查询（包括参数值）
                    var debugSql = query.ToString();
                    foreach (var param in parameters)
                    {
                        if (param.Value is DateTime)
                        {
                            DateTime dt = (DateTime)param.Value;
                            debugSql = debugSql.Replace(param.Key, $"'{dt:yyyy-MM-dd}'");
                        }
                        else
                        {
                            debugSql = debugSql.Replace(param.Key, param.Value.ToString());
                        }
                    }
                    Debug.WriteLine($"SQL查询列表: {debugSql}");

                    // 创建命令
                    using (var command = new MySqlCommand(query.ToString(), connection))
                    {
                        // 添加参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        // 执行查询
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tickets.Add(MapTrainRideInfo(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取筛选车票列表失败: {ex.Message}", ex);
                throw;
            }

            return tickets;
        }

        private TrainRideInfo MapTrainRideInfo(DbDataReader reader)
        {
            var trainRideInfo = new TrainRideInfo
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                TicketNumber = reader.IsDBNull(reader.GetOrdinal("ticket_number")) ? null : reader.GetString(reader.GetOrdinal("ticket_number")),
                CheckInLocation = reader.IsDBNull(reader.GetOrdinal("check_in_location")) ? null : reader.GetString(reader.GetOrdinal("check_in_location")),
                DepartStation = reader.IsDBNull(reader.GetOrdinal("depart_station")) ? null : reader.GetString(reader.GetOrdinal("depart_station")),
                TrainNo = reader.IsDBNull(reader.GetOrdinal("train_no")) ? null : reader.GetString(reader.GetOrdinal("train_no")),
                ArriveStation = reader.IsDBNull(reader.GetOrdinal("arrive_station")) ? null : reader.GetString(reader.GetOrdinal("arrive_station")),
                DepartStationPinyin = reader.IsDBNull(reader.GetOrdinal("depart_station_pinyin")) ? null : reader.GetString(reader.GetOrdinal("depart_station_pinyin")),
                ArriveStationPinyin = reader.IsDBNull(reader.GetOrdinal("arrive_station_pinyin")) ? null : reader.GetString(reader.GetOrdinal("arrive_station_pinyin")),
                DepartDate = reader.IsDBNull(reader.GetOrdinal("depart_date")) ? null : reader.GetDateTime(reader.GetOrdinal("depart_date")),
                CoachNo = reader.IsDBNull(reader.GetOrdinal("coach_no")) ? null : reader.GetString(reader.GetOrdinal("coach_no")),
                SeatNo = reader.IsDBNull(reader.GetOrdinal("seat_no")) ? null : reader.GetString(reader.GetOrdinal("seat_no")),
                Money = reader.IsDBNull(reader.GetOrdinal("money")) ? null : reader.GetDecimal(reader.GetOrdinal("money")),
                SeatType = reader.IsDBNull(reader.GetOrdinal("seat_type")) ? null : reader.GetString(reader.GetOrdinal("seat_type")),
                AdditionalInfo = reader.IsDBNull(reader.GetOrdinal("additional_info")) ? null : reader.GetString(reader.GetOrdinal("additional_info")),
                TicketPurpose = reader.IsDBNull(reader.GetOrdinal("ticket_purpose")) ? null : reader.GetString(reader.GetOrdinal("ticket_purpose")),
                Hint = reader.IsDBNull(reader.GetOrdinal("hint")) ? null : reader.GetString(reader.GetOrdinal("hint")),
                DepartStationCode = reader.IsDBNull(reader.GetOrdinal("depart_station_code")) ? null : reader.GetString(reader.GetOrdinal("depart_station_code")),
                ArriveStationCode = reader.IsDBNull(reader.GetOrdinal("arrive_station_code")) ? null : reader.GetString(reader.GetOrdinal("arrive_station_code")),
                TicketModificationType = reader.IsDBNull(reader.GetOrdinal("ticket_modification_type")) ? null : reader.GetString(reader.GetOrdinal("ticket_modification_type")),
                TicketTypeFlags = reader.IsDBNull(reader.GetOrdinal("ticket_type_flags")) ? 0 : reader.GetInt32(reader.GetOrdinal("ticket_type_flags")),
                PaymentChannelFlags = reader.IsDBNull(reader.GetOrdinal("payment_channel_flags")) ? 0 : reader.GetInt32(reader.GetOrdinal("payment_channel_flags"))
            };

            // 处理时间字段
            if (!reader.IsDBNull(reader.GetOrdinal("depart_time")))
            {
                try
                {
                    // 尝试从MySQL读取时间
                    if (reader is MySqlDataReader mysqlReader)
                    {
                        trainRideInfo.DepartTime = mysqlReader.GetTimeSpan(reader.GetOrdinal("depart_time"));
                    }
                    else
                    {
                        // 尝试从字符串解析
                        string timeStr = reader.GetString(reader.GetOrdinal("depart_time"));
                        trainRideInfo.DepartTime = TimeSpan.Parse(timeStr);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"解析时间出错: {ex.Message}");
                }
            }

            return trainRideInfo;
        }

        /// <summary>
        /// 检测数据库中是否存在指定的表
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>表是否存在</returns>
        public async Task<bool> TableExistsAsync(string tableName)
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 获取数据库名称
                string databaseName = connection.Database;

                string query = @"SELECT COUNT(*) 
                               FROM information_schema.tables 
                               WHERE table_schema = @DatabaseName 
                               AND table_name = @TableName";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", databaseName);
                    command.Parameters.AddWithValue("@TableName", tableName);

                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// 创建车站信息表
        /// </summary>
        public async Task CreateStationInfoTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `station_info`;
                    CREATE TABLE `station_info`  (
                    `id` int NOT NULL AUTO_INCREMENT COMMENT 'id',
                    `station_name` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站名称',
                    `province` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站所在省',
                    `city` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站所在市',
                    `district` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站所在区',
                    `longitude` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '经度',
                    `latitude` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '纬度',
                    `station_code` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站代码',
                    `station_pinyin` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站拼音',
                    `station_level` int NULL DEFAULT 0 COMMENT '车站等级：1=特等站,2=一等站,4=二等站,8=三等站,16=四等站,32=五等站',
                    `railway_bureau` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '所属路局',
                    PRIMARY KEY (`id`) USING BTREE,
                    INDEX `station_name`(`station_name` ASC) USING BTREE,
                    UNIQUE INDEX `fk_arrive_code`(`station_code` ASC) USING BTREE,
                    INDEX `station_pinyin`(`station_pinyin` ASC) USING BTREE
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = DYNAMIC;
                    ALTER TABLE `station_info` AUTO_INCREMENT = 1;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 创建车票信息表
        /// </summary>
        public async Task CreateTrainRideInfoTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `train_ride_info`;
                    CREATE TABLE `train_ride_info`  (
                    `id` int NOT NULL AUTO_INCREMENT COMMENT 'id',
                    `ticket_number` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '取票号',
                    `check_in_location` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '检票位置',
                    `depart_station` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站',
                    `train_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车次号',
                    `arrive_station` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站',
                    `depart_station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站拼音',
                    `arrive_station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站拼音',
                    `depart_date` date NULL DEFAULT NULL COMMENT '出发日期',
                    `depart_time` time NULL DEFAULT NULL COMMENT '出发时间',
                    `coach_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车厢号',
                    `seat_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '座位号',
                    `money` decimal(6, 2) NULL DEFAULT NULL COMMENT '金额',
                    `seat_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '席别',
                    `additional_info` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '附加信息（退票费/限乘当日当次车）',
                    `ticket_purpose` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车票用途',
                    `ticket_modification_type` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车票改签类型',
                    `ticket_type_flags` int NULL DEFAULT 0 COMMENT '票种类型（枚举）',
                    `payment_channel_flags` int NULL DEFAULT 0 COMMENT '支付渠道（枚举）',
                    `hint` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '提示信息',
                    `depart_station_code` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站代码',
                    `arrive_station_code` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站代码',
                    PRIMARY KEY (`id`) USING BTREE,
                    INDEX `station_code`(`depart_station_code` ASC) USING BTREE,
                    INDEX `arrive_station_code`(`arrive_station_code` ASC) USING BTREE,
                    INDEX `fk_depart_station_pinyin`(`depart_station_pinyin` ASC) USING BTREE,
                    INDEX `fk_arrive_station_pinyin`(`arrive_station_pinyin` ASC) USING BTREE,
                    INDEX `idx_train_no`(`train_no` ASC, `depart_date` ASC) USING BTREE,
                    INDEX `idx_depart_station`(`depart_station` ASC, `depart_date` ASC) USING BTREE,
                    CONSTRAINT `fc_dc_arrive` FOREIGN KEY (`arrive_station_code`) REFERENCES `station_info` (`station_code`) ON DELETE CASCADE ON UPDATE CASCADE,
                    CONSTRAINT `fk_sc_depart` FOREIGN KEY (`depart_station_code`) REFERENCES `station_info` (`station_code`) ON DELETE CASCADE ON UPDATE CASCADE
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = DYNAMIC;
                    ALTER TABLE `train_ride_info` AUTO_INCREMENT = 1;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 创建车票收藏夹信息表
        /// </summary>
        public async Task CreateTicketCollectionsInfoTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `ticket_collections_info`;
                    CREATE TABLE `ticket_collections_info`  (
                      `id` int NOT NULL AUTO_INCREMENT,
                      `collection_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '收藏夹名称',
                      `description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '收藏夹描述',
                      `cover_image` blob NOT NULL COMMENT '封面图片base64',
                      `create_time` datetime NULL DEFAULT CURRENT_TIMESTAMP,
                      `update_time` datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                      `importance` int NULL DEFAULT 0 COMMENT '评分1-5',
                      `sort_order` int NULL DEFAULT 0 COMMENT '排序顺序',
                      PRIMARY KEY (`id`) USING BTREE
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 创建收藏夹与车票关联信息表
        /// </summary>
        public async Task CreateCollectionMappedTicketsInfoTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `collection_mapped_tickets_info`;
                    CREATE TABLE `collection_mapped_tickets_info`  (
                      `id` int NOT NULL AUTO_INCREMENT,
                      `collection_id` int NOT NULL COMMENT '收藏夹ID',
                      `ticket_count` int NULL DEFAULT NULL COMMENT '包含车票数量',
                      `ticket_id` int NOT NULL COMMENT '车票ID',
                      `add_time` datetime NULL DEFAULT CURRENT_TIMESTAMP,
                      PRIMARY KEY (`id`) USING BTREE,
                      INDEX `idx_collection`(`collection_id` ASC) USING BTREE,
                      INDEX `idx_ticket`(`ticket_id` ASC) USING BTREE,
                      CONSTRAINT `fk_ct_collection` FOREIGN KEY (`collection_id`) REFERENCES `ticket_collections_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
                      CONSTRAINT `fk_ct_ticket` FOREIGN KEY (`ticket_id`) REFERENCES `train_ride_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 更新车票信息
        /// </summary>
        /// <param name="ticket">要更新的车票信息</param>
        /// <returns>更新是否成功</returns>
        public async Task<bool> UpdateTicketAsync(TrainRideInfo ticket)
        {
            // 创建带有超时设置的连接字符串
            var builder = new MySqlConnectionStringBuilder(_connectionString)
            {
                ConnectionTimeout = 10 // 设置连接超时为10秒
            };

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                try
                {
                    string query = @"UPDATE train_ride_info 
                                   SET ticket_number = @TicketNumber,
                                       check_in_location = @CheckInLocation,
                                       depart_station = @DepartStation,
                                       train_no = @TrainNo,
                                       arrive_station = @ArriveStation,
                                       depart_station_pinyin = @DepartStationPinyin,
                                       arrive_station_pinyin = @ArriveStationPinyin,
                                       depart_date = @DepartDate,
                                       depart_time = @DepartTime,
                                       coach_no = @CoachNo,
                                       seat_no = @SeatNo,
                                       money = @Money,
                                       seat_type = @SeatType,
                                       additional_info = @AdditionalInfo,
                                       ticket_purpose = @TicketPurpose,
                                       hint = @Hint,
                                       depart_station_code = @DepartStationCode,
                                       arrive_station_code = @ArriveStationCode,
                                       ticket_modification_type = @TicketModificationType,
                                       ticket_type_flags = @TicketTypeFlags,
                                       payment_channel_flags = @PaymentChannelFlags
                                   WHERE id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 设置命令超时为5秒
                        command.CommandTimeout = 10;

                        command.Parameters.AddWithValue("@Id", ticket.Id);
                        command.Parameters.AddWithValue("@TicketNumber", ticket.TicketNumber);
                        command.Parameters.AddWithValue("@CheckInLocation", ticket.CheckInLocation);
                        command.Parameters.AddWithValue("@DepartStation", ticket.DepartStation);
                        command.Parameters.AddWithValue("@TrainNo", ticket.TrainNo);
                        command.Parameters.AddWithValue("@ArriveStation", ticket.ArriveStation);
                        command.Parameters.AddWithValue("@DepartStationPinyin", ticket.DepartStationPinyin);
                        command.Parameters.AddWithValue("@ArriveStationPinyin", ticket.ArriveStationPinyin);
                        command.Parameters.AddWithValue("@DepartDate", ticket.DepartDate);
                        command.Parameters.AddWithValue("@DepartTime", ticket.DepartTime);
                        command.Parameters.AddWithValue("@CoachNo", ticket.CoachNo);
                        command.Parameters.AddWithValue("@SeatNo", ticket.SeatNo);
                        command.Parameters.AddWithValue("@Money", ticket.Money);
                        command.Parameters.AddWithValue("@SeatType", ticket.SeatType);
                        command.Parameters.AddWithValue("@AdditionalInfo", ticket.AdditionalInfo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TicketPurpose", ticket.TicketPurpose ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Hint", ticket.Hint ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DepartStationCode", ticket.DepartStationCode);
                        command.Parameters.AddWithValue("@ArriveStationCode", ticket.ArriveStationCode);
                        command.Parameters.AddWithValue("@TicketModificationType", ticket.TicketModificationType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TicketTypeFlags", ticket.TicketTypeFlags);
                        command.Parameters.AddWithValue("@PaymentChannelFlags", ticket.PaymentChannelFlags);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (MySqlException ex)
                {
                    // 记录详细的数据库错误信息
                    Debug.WriteLine($"数据库错误: {ex.Message}, 错误代码: {ex.Number}");
                    throw; // 重新抛出异常以便上层处理
                }
            }
        }

        /// <summary>
        /// 删除车票
        /// </summary>
        /// <param name="ticketId">车票ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteTicketAsync(int ticketId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = "DELETE FROM train_ride_info WHERE id = @TicketId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TicketId", ticketId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除车票时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有不同的出发车站
        /// </summary>
        /// <returns>不同出发车站列表</returns>
        public async Task<List<string>> GetDistinctDepartStationsAsync()
        {
            try
            {
                var stations = new List<string>();

                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = "SELECT DISTINCT depart_station FROM train_ride_info WHERE depart_station IS NOT NULL ORDER BY depart_station";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string stationName = reader.GetString(0);
                                // 使用工具类确保站名以"站"结尾
                                stations.Add(StationNameHelper.EnsureStationSuffix(stationName));
                            }
                        }
                    }
                }

                return stations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取出发车站列表时出错: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有不同的到达车站
        /// </summary>
        /// <returns>不同到达车站列表</returns>
        public async Task<List<string>> GetDistinctArriveStationsAsync()
        {
            try
            {
                var stations = new List<string>();

                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = "SELECT DISTINCT arrive_station FROM train_ride_info WHERE arrive_station IS NOT NULL ORDER BY arrive_station";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string stationName = reader.GetString(0);
                                // 使用工具类确保站名以"站"结尾
                                stations.Add(StationNameHelper.EnsureStationSuffix(stationName));
                            }
                        }
                    }
                }

                return stations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取到达车站列表时出错: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 更新车站信息
        /// </summary>
        /// <param name="station">要更新的车站信息</param>
        /// <returns>更新是否成功</returns>
        public async Task<bool> UpdateStationAsync(StationInfo station)
        {
            if (station == null)
            {
                return false;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        UPDATE station_info 
                        SET 
                            station_name = @StationName,
                            province = @Province,
                            city = @City,
                            district = @District,
                            longitude = @Longitude,
                            latitude = @Latitude,
                            station_pinyin = @StationPinyin,
                            station_code = @StationCode,
                            station_level = @StationLevel,
                            railway_bureau = @RailwayBureau
                        WHERE id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", station.Id);
                        command.Parameters.AddWithValue("@StationName", station.StationName);
                        command.Parameters.AddWithValue("@Province", station.Province ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@City", station.City ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@District", station.District ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Longitude", station.Longitude ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Latitude", station.Latitude ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@StationPinyin", station.StationPinyin ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@StationCode", station.StationCode ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@StationLevel", station.StationLevel);
                        command.Parameters.AddWithValue("@RailwayBureau", station.RailwayBureau ?? (object)DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新车站信息失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 高级查询车站信息
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="stationName">车站名称</param>
        /// <param name="province">省份</param>
        /// <param name="city">城市</param>
        /// <param name="district">区/县</param>
        /// <param name="myDepartStations">我的常用车站列表（包含出发站和到达站）</param>
        /// <param name="routeStationIds">路线车站ID列表</param>
        /// <returns>符合条件的车站列表</returns>
        public async Task<List<StationInfo>> QueryStationsAdvancedAsync(
            int pageNumber,
            int pageSize,
            string stationName = null,
            string province = null,
            string city = null,
            string district = null,
            List<string> myDepartStations = null,
            List<int> routeStationIds = null) // 添加路线车站ID参数
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建查询条件
                    var conditions = new List<string>();
                    var orConditions = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    // 添加车站名称条件
                    if (!string.IsNullOrWhiteSpace(stationName))
                    {
                        conditions.Add("(station_name LIKE @StationName OR station_pinyin LIKE @StationPinyin)");
                        parameters["@StationName"] = $"%{stationName}%";
                        parameters["@StationPinyin"] = $"%{stationName}%";
                    }

                    // 添加省份条件
                    if (!string.IsNullOrWhiteSpace(province))
                    {
                        conditions.Add("province = @Province");
                        parameters["@Province"] = province;
                    }

                    // 添加城市条件
                    if (!string.IsNullOrWhiteSpace(city))
                    {
                        conditions.Add("city = @City");
                        parameters["@City"] = city;
                    }

                    // 添加区县条件
                    if (!string.IsNullOrWhiteSpace(district))
                    {
                        conditions.Add("district = @District");
                        parameters["@District"] = district;
                    }

                    // 添加常用车站条件
                    if (myDepartStations != null && myDepartStations.Count > 0)
                    {
                        // 构建IN子句
                        var stationNames = string.Join(",", myDepartStations.Select(s => $"'{s.Replace("'", "''")}'"));
                        orConditions.Add($"station_name IN ({stationNames})");
                    }

                    // 添加路线车站条件
                    if (routeStationIds != null && routeStationIds.Count > 0)
                    {
                        var stationIds = string.Join(",", routeStationIds);
                        orConditions.Add($"id IN ({stationIds})");
                    }

                    // 处理OR条件（常用车站和路线车站的并集）
                    if (orConditions.Count > 0)
                    {
                        conditions.Add($"({string.Join(" OR ", orConditions)})");
                    }

                    // 构建完整的查询条件
                    string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

                    // 构建完整查询语句
                    string query = @"
                        SELECT id, station_name, province, city, district, 
                               longitude, latitude, station_code, station_pinyin,
                               station_level, railway_bureau
                        FROM station_info
                        {0}
                        ORDER BY id ASC
                        LIMIT @Offset, @PageSize";

                    // 格式化完整SQL
                    string sql = string.Format(query, whereClause);

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        // 添加分页参数
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        // 添加其他参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        // 执行查询
                        var stations = new List<StationInfo>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stations.Add(MapStationInfo(reader));
                            }
                        }

                        return stations;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"高级查询车站数据失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取高级查询的车站总数
        /// </summary>
        public async Task<int> GetStationCountAdvancedAsync(
            string stationName = null,
            string province = null,
            string city = null,
            string district = null,
            List<string> myDepartStations = null,
            List<int> routeStationIds = null) // 添加路线车站ID参数
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建查询条件
                    var conditions = new List<string>();
                    var orConditions = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    // 添加车站名称条件
                    if (!string.IsNullOrWhiteSpace(stationName))
                    {
                        conditions.Add("(station_name LIKE @StationName OR station_pinyin LIKE @StationPinyin)");
                        parameters["@StationName"] = $"%{stationName}%";
                        parameters["@StationPinyin"] = $"%{stationName}%";
                    }

                    // 添加省份条件
                    if (!string.IsNullOrWhiteSpace(province))
                    {
                        conditions.Add("province = @Province");
                        parameters["@Province"] = province;
                    }

                    // 添加城市条件
                    if (!string.IsNullOrWhiteSpace(city))
                    {
                        conditions.Add("city = @City");
                        parameters["@City"] = city;
                    }

                    // 添加区县条件
                    if (!string.IsNullOrWhiteSpace(district))
                    {
                        conditions.Add("district = @District");
                        parameters["@District"] = district;
                    }

                    // 添加常用车站条件
                    if (myDepartStations != null && myDepartStations.Count > 0)
                    {
                        // 构建IN子句
                        var stationNames = string.Join(",", myDepartStations.Select(s => $"'{s.Replace("'", "''")}'"));
                        orConditions.Add($"station_name IN ({stationNames})");
                    }

                    // 添加路线车站条件
                    if (routeStationIds != null && routeStationIds.Count > 0)
                    {
                        var stationIds = string.Join(",", routeStationIds);
                        orConditions.Add($"id IN ({stationIds})");
                    }

                    // 处理OR条件（常用车站和路线车站的并集）
                    if (orConditions.Count > 0)
                    {
                        conditions.Add($"({string.Join(" OR ", orConditions)})");
                    }

                    // 构建完整的查询条件
                    string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

                    // 构建完整查询语句
                    string query = $"SELECT COUNT(*) FROM station_info {whereClause}";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 添加参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        // 执行查询
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取高级查询车站总数失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 添加收藏夹信息
        /// </summary>
        /// <param name="collection">收藏夹信息</param>
        /// <returns>添加是否成功</returns>
        public async Task<bool> AddCollectionAsync(TicketCollectionInfo collection)
        {
            if (collection == null)
            {
                return false;
            }

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(collection.CollectionName))
            {
                MessageBoxHelper.ShowError("收藏夹名称不能为空");
                Debug.WriteLine("添加收藏夹失败: 收藏夹名称不能为空");
                return false;
            }

            if (collection.CoverImage == null || collection.CoverImage.Length == 0)
            {
                MessageBoxHelper.ShowError("封面图片不能为空");
                Debug.WriteLine("添加收藏夹失败: 封面图片不能为空");
                return false;
            }

            // 检查图片大小是否超过数据库限制 (16MB - mediumblob限制)
            // 实际设置为1MB以便有足够余量
            if (collection.CoverImage.Length > 1024 * 1024)
            {
                MessageBoxHelper.ShowError("图片大小超过限制(1MB)，请使用较小的图片");
                Debug.WriteLine($"添加收藏夹失败: 图片大小({collection.CoverImage.Length / 1024}KB)超过限制(1MB)");
                return false;
            }

            // 记录评分值，确保保存前评分值正确
            Debug.WriteLine($"保存到数据库的评分值: {collection.Importance}");

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"INSERT INTO ticket_collections_info 
                                   (collection_name, description, cover_image, create_time, update_time, sort_order, importance) 
                                   VALUES 
                                   (@CollectionName, @Description, @CoverImage, @CreateTime, @UpdateTime, @SortOrder, @Importance)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionName", collection.CollectionName);
                        command.Parameters.AddWithValue("@Description", collection.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CoverImage", collection.CoverImage);
                        command.Parameters.AddWithValue("@CreateTime", collection.CreateTime);
                        command.Parameters.AddWithValue("@UpdateTime", collection.UpdateTime);
                        command.Parameters.AddWithValue("@SortOrder", collection.SortOrder);

                        // 确保Importance参数类型正确，将其显式转换为int
                        command.Parameters.AddWithValue("@Importance", (int)collection.Importance);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        bool success = rowsAffected > 0;

                        // 记录执行结果
                        Debug.WriteLine($"添加收藏夹结果: {success}, 影响行数: {rowsAffected}");

                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"添加收藏夹信息失败: {ex.Message}", ex);
                Debug.WriteLine($"添加收藏夹失败: {ex.Message}");

                // 如果是数据过长错误，提供更明确的错误信息
                if (ex.Message.Contains("Data too long"))
                {
                    MessageBoxHelper.ShowError("图片数据过大，无法保存到数据库。请选择较小的图片或进一步调整图片尺寸后再试。");
                }
                else
                {
                    MessageBoxHelper.ShowError($"保存收藏夹失败: {ex.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// 获取收藏夹总数
        /// </summary>
        /// <returns>收藏夹总数</returns>
        public async Task<int> GetCollectionCountAsync()
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand("SELECT COUNT(*) FROM ticket_collections_info", connection))
                    {
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取收藏夹总数失败: {ex.Message}", ex);
                Debug.WriteLine($"获取收藏夹总数失败: {ex.Message}");
                throw new Exception($"获取收藏夹总数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取收藏夹列表
        /// </summary>
        /// <param name="pageNumber">页码(从1开始)</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序字段</param>
        /// <param name="ascending">是否升序</param>
        /// <returns>收藏夹列表</returns>
        public async Task<List<TicketCollectionInfo>> GetCollectionsAsync(int pageNumber = 1, int pageSize = 10, string orderBy = "id", bool ascending = true)
        {
            var items = new List<TicketCollectionInfo>();
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建排序方向
                    string direction = ascending ? "ASC" : "DESC";

                    string query = $@"SELECT * FROM ticket_collections_info 
                                   ORDER BY {orderBy} {direction}
                                   LIMIT @Offset, @PageSize";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(MapCollectionInfo(reader));
                            }
                        }
                    }

                    // 获取每个收藏夹关联的车票数量
                    foreach (var collection in items)
                    {
                        string countQuery = "SELECT COUNT(*) FROM collection_mapped_tickets_info WHERE collection_id = @CollectionId";
                        using (var countCommand = new MySqlCommand(countQuery, connection))
                        {
                            countCommand.Parameters.AddWithValue("@CollectionId", collection.Id);
                            object result = await countCommand.ExecuteScalarAsync();
                            collection.TicketCount = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取收藏夹列表失败: {ex.Message}", ex);
                Debug.WriteLine($"获取收藏夹列表失败: {ex.Message}");
                throw;
            }

            return items;
        }

        /// <summary>
        /// 映射数据读取器到收藏夹信息对象
        /// </summary>
        /// <param name="reader">数据读取器</param>
        /// <returns>收藏夹信息对象</returns>
        private TicketCollectionInfo MapCollectionInfo(DbDataReader reader)
        {
            try
            {
                // 获取importance的索引
                int importanceIdx = reader.GetOrdinal("importance");
                int importanceValue = 0;

                // 正确读取importance值
                if (!reader.IsDBNull(importanceIdx))
                {
                    importanceValue = reader.GetInt32(importanceIdx);
                    // 记录读取到的评分值
                    Debug.WriteLine($"从数据库读取到的评分值: {importanceValue}");
                }
                else
                {
                    Debug.WriteLine("从数据库读取的评分值为NULL，使用默认值0");
                }

                // 获取ticket_count的值
                int ticketCount = 0;
                if (reader.HasRows && reader.FieldCount > 0)
                {
                    // 检查是否有ticket_count字段
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader.GetName(i).Equals("ticket_count", StringComparison.OrdinalIgnoreCase))
                        {
                            ticketCount = reader.IsDBNull(i) ? 0 : reader.GetInt32(i);
                            break;
                        }
                    }
                }

                var collection = new TicketCollectionInfo
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CollectionName = reader.IsDBNull(reader.GetOrdinal("collection_name")) ? null : reader.GetString(reader.GetOrdinal("collection_name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    CreateTime = reader.GetDateTime(reader.GetOrdinal("create_time")),
                    UpdateTime = reader.GetDateTime(reader.GetOrdinal("update_time")),
                    SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32(reader.GetOrdinal("sort_order")),
                    Importance = importanceValue, // 使用上面获取的评分值
                    TicketCount = ticketCount     // 设置车票数量
                };

                // 读取BLOB数据(封面图片)
                if (!reader.IsDBNull(reader.GetOrdinal("cover_image")))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        long bytesRead;
                        long fieldOffset = 0;
                        using (var stream = reader.GetStream(reader.GetOrdinal("cover_image")))
                        {
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, (int)bytesRead);
                                fieldOffset += bytesRead;
                            }
                        }
                        collection.CoverImage = ms.ToArray();
                    }
                }

                return collection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"映射收藏夹信息异常: {ex.Message}");
                LogHelper.LogError($"映射收藏夹信息异常: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 根据名称查询收藏夹
        /// </summary>
        /// <param name="collectionName">收藏夹名称</param>
        /// <returns>找到的收藏夹信息，如果不存在则返回null</returns>
        public async Task<TicketCollectionInfo> GetCollectionByNameAsync(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return null;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = "SELECT * FROM ticket_collections_info WHERE collection_name = @CollectionName LIMIT 1";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionName", collectionName);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapCollectionInfo(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"按名称查询收藏夹失败: {ex.Message}", ex);
                Debug.WriteLine($"按名称查询收藏夹失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 查询所有与基本名称匹配的收藏夹
        /// </summary>
        /// <param name="baseName">基本名称（不包括括号后缀）</param>
        /// <returns>匹配的收藏夹列表</returns>
        public async Task<List<string>> GetCollectionNamesByBaseNameAsync(string baseName)
        {
            var names = new List<string>();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return names;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 使用LIKE查询匹配基本名称开头的所有收藏夹
                    // 例如: baseName="11"，会匹配"11"、"11(1)"、"11(2)"等
                    string query = "SELECT collection_name FROM ticket_collections_info WHERE collection_name LIKE @BaseNamePattern";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BaseNamePattern", baseName + "%");
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                names.Add(reader.GetString(reader.GetOrdinal("collection_name")));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"查询匹配收藏夹名称失败: {ex.Message}", ex);
                Debug.WriteLine($"查询匹配收藏夹名称失败: {ex.Message}");
            }

            return names;
        }

        /// <summary>
        /// 更新收藏夹信息
        /// </summary>
        /// <param name="collection">要更新的收藏夹信息</param>
        /// <returns>是否更新成功</returns>
        public async Task<bool> UpdateCollectionAsync(TicketCollectionInfo collection)
        {
            if (collection == null || collection.Id <= 0)
            {
                return false;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"UPDATE ticket_collections_info 
                                   SET collection_name = @CollectionName, 
                                       description = @Description, 
                                       cover_image = @CoverImage, 
                                       update_time = @UpdateTime, 
                                       importance = @Importance,
                                       sort_order = @SortOrder 
                                   WHERE id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", collection.Id);
                        command.Parameters.AddWithValue("@CollectionName", collection.CollectionName);
                        command.Parameters.AddWithValue("@Description", collection.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CoverImage", collection.CoverImage);
                        command.Parameters.AddWithValue("@UpdateTime", collection.UpdateTime);
                        command.Parameters.AddWithValue("@Importance", (int)collection.Importance);
                        command.Parameters.AddWithValue("@SortOrder", collection.SortOrder);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        bool success = rowsAffected > 0;

                        // 记录执行结果
                        Debug.WriteLine($"更新收藏夹结果: {success}, 影响行数: {rowsAffected}");

                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新收藏夹信息失败: {ex.Message}", ex);
                Debug.WriteLine($"更新收藏夹失败: {ex.Message}");

                // 如果是数据过长错误，提供更明确的错误信息
                if (ex.Message.Contains("Data too long"))
                {
                    MessageBoxHelper.ShowError("图片数据过大，无法保存到数据库。请选择较小的图片或进一步调整图片尺寸后再试。");
                }
                else
                {
                    MessageBoxHelper.ShowError($"更新收藏夹失败: {ex.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// 根据ID列表删除收藏夹
        /// </summary>
        /// <param name="collectionIds">收藏夹ID列表</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteCollectionsByIdsAsync(List<int> collectionIds)
        {
            if (collectionIds == null || collectionIds.Count == 0)
            {
                return true; // 没有要删除的ID，视为成功
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建包含所有ID的IN子句
                    string idList = string.Join(",", collectionIds);
                    string query = $"DELETE FROM ticket_collections_info WHERE id IN ({idList})";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"批量删除收藏夹失败: {ex.Message}", ex);
                Debug.WriteLine($"批量删除收藏夹失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量更新收藏夹排序顺序
        /// </summary>
        /// <param name="collectionSortOrders">收藏夹ID和排序顺序的字典</param>
        /// <returns>更新是否成功</returns>
        public async Task<bool> UpdateCollectionSortOrdersAsync(Dictionary<int, int> collectionSortOrders)
        {
            if (collectionSortOrders == null || collectionSortOrders.Count == 0)
            {
                return true; // 没有要更新的项，视为成功
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 使用事务确保批量更新的原子性
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        string query = "UPDATE ticket_collections_info SET sort_order = @SortOrder WHERE id = @Id";
                        using (var command = new MySqlCommand(query, connection, transaction as MySqlTransaction))
                        {
                            // 创建可重用的参数
                            var idParam = command.Parameters.Add("@Id", MySqlDbType.Int32);
                            var sortOrderParam = command.Parameters.Add("@SortOrder", MySqlDbType.Int32);

                            // 执行每条更新
                            int successCount = 0;
                            foreach (var kvp in collectionSortOrders)
                            {
                                idParam.Value = kvp.Key;        // 收藏夹ID
                                sortOrderParam.Value = kvp.Value; // 排序顺序

                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    successCount++;
                                }
                            }

                            // 提交事务
                            await transaction.CommitAsync();

                            // 记录执行结果
                            Debug.WriteLine($"批量更新收藏夹排序顺序结果: {successCount}/{collectionSortOrders.Count} 条记录更新成功");

                            return successCount > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"批量更新收藏夹排序顺序失败: {ex.Message}", ex);
                Debug.WriteLine($"批量更新收藏夹排序顺序失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取收藏夹中已有的车票ID列表
        /// </summary>
        /// <param name="collectionId">收藏夹ID</param>
        /// <returns>车票ID列表</returns>
        public async Task<List<int>> GetCollectionTicketIdsAsync(int collectionId)
        {
            var ticketIds = new List<int>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = "SELECT ticket_id FROM collection_mapped_tickets_info WHERE collection_id = @CollectionId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionId", collectionId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ticketIds.Add(reader.GetInt32(reader.GetOrdinal("ticket_id")));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取收藏夹中车票ID列表失败: {ex.Message}", ex);
                Debug.WriteLine($"获取收藏夹中车票ID列表失败: {ex.Message}");
            }

            return ticketIds;
        }

        /// <summary>
        /// 获取收藏夹中的车票
        /// </summary>
        /// <param name="collectionId">收藏夹ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>车票列表</returns>
        public async Task<List<TrainRideInfo>> GetCollectionTicketsAsync(int collectionId, int pageNumber = 1, int pageSize = 10)
        {
            var tickets = new List<TrainRideInfo>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT t.* 
                        FROM train_ride_info t
                        INNER JOIN collection_mapped_tickets_info m ON t.id = m.ticket_id
                        WHERE m.collection_id = @CollectionId
                        ORDER BY m.add_time DESC
                        LIMIT @Offset, @PageSize";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionId", collectionId);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tickets.Add(MapTrainRideInfo(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取收藏夹中车票失败: {ex.Message}", ex);
                Debug.WriteLine($"获取收藏夹中车票失败: {ex.Message}");
            }

            return tickets;
        }

        /// <summary>
        /// 获取收藏夹中的车票总数
        /// </summary>
        /// <param name="collectionId">收藏夹ID</param>
        /// <returns>车票总数</returns>
        public async Task<int> GetCollectionTicketCountAsync(int collectionId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = "SELECT COUNT(*) FROM collection_mapped_tickets_info WHERE collection_id = @CollectionId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionId", collectionId);
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取收藏夹中车票总数失败: {ex.Message}", ex);
                Debug.WriteLine($"获取收藏夹中车票总数失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 将车票添加到收藏夹
        /// </summary>
        /// <param name="collectionId">收藏夹ID</param>
        /// <param name="ticketId">车票ID</param>
        /// <returns>是否添加成功</returns>
        public async Task<bool> AddTicketToCollectionAsync(int collectionId, int ticketId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 检查是否已存在
                    string checkQuery = "SELECT COUNT(*) FROM collection_mapped_tickets_info WHERE collection_id = @CollectionId AND ticket_id = @TicketId";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@CollectionId", collectionId);
                        checkCommand.Parameters.AddWithValue("@TicketId", ticketId);
                        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            // 已存在，视为成功
                            return true;
                        }
                    }

                    // 添加映射
                    string insertQuery = @"
                        INSERT INTO collection_mapped_tickets_info (collection_id, ticket_id, add_time)
                        VALUES (@CollectionId, @TicketId, @AddTime)";

                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionId", collectionId);
                        command.Parameters.AddWithValue("@TicketId", ticketId);
                        command.Parameters.AddWithValue("@AddTime", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"添加车票到收藏夹失败: {ex.Message}", ex);
                Debug.WriteLine($"添加车票到收藏夹失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量添加车票到收藏夹
        /// </summary>
        /// <param name="mappings">收藏夹与车票的映射列表</param>
        /// <returns>添加成功的数量</returns>
        public async Task<int> AddTicketsToCollectionAsync(List<CollectionMappedTicketInfo> mappings)
        {
            if (mappings == null || mappings.Count == 0)
            {
                return 0;
            }

            int successCount = 0;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 使用事务以提高批量插入效率
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        string insertQuery = @"
                            INSERT INTO collection_mapped_tickets_info (collection_id, ticket_id, add_time)
                            VALUES (@CollectionId, @TicketId, @AddTime)";

                        using (var command = new MySqlCommand(insertQuery, connection, transaction as MySqlTransaction))
                        {
                            // 创建可重用的参数
                            var collectionIdParam = command.Parameters.Add("@CollectionId", MySqlDbType.Int32);
                            var ticketIdParam = command.Parameters.Add("@TicketId", MySqlDbType.Int32);
                            var addTimeParam = command.Parameters.Add("@AddTime", MySqlDbType.DateTime);

                            foreach (var mapping in mappings)
                            {
                                // 检查是否已存在
                                string checkQuery = "SELECT COUNT(*) FROM collection_mapped_tickets_info WHERE collection_id = @CollectionId AND ticket_id = @TicketId";
                                using (var checkCommand = new MySqlCommand(checkQuery, connection, transaction as MySqlTransaction))
                                {
                                    checkCommand.Parameters.AddWithValue("@CollectionId", mapping.CollectionId);
                                    checkCommand.Parameters.AddWithValue("@TicketId", mapping.TicketId);
                                    int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                                    if (count > 0)
                                    {
                                        // 已存在，跳过
                                        continue;
                                    }
                                }

                                // 设置参数值
                                collectionIdParam.Value = mapping.CollectionId;
                                ticketIdParam.Value = mapping.TicketId;
                                addTimeParam.Value = mapping.AddTime;

                                // 执行插入
                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    successCount++;
                                }
                            }
                        }

                        // 提交事务
                        await transaction.CommitAsync();
                    }

                    // 更新收藏夹中的车票数量
                    if (successCount > 0 && mappings.Count > 0)
                    {
                        await UpdateCollectionTicketCountAsync(mappings[0].CollectionId);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"批量添加车票到收藏夹失败: {ex.Message}", ex);
                Debug.WriteLine($"批量添加车票到收藏夹失败: {ex.Message}");
            }

            return successCount;
        }

        /// <summary>
        /// 从收藏夹中移除车票
        /// </summary>
        /// <param name="collectionId">收藏夹ID</param>
        /// <param name="ticketIds">车票ID列表</param>
        /// <returns>是否移除成功</returns>
        public async Task<bool> RemoveTicketsFromCollectionAsync(int collectionId, List<int> ticketIds)
        {
            if (ticketIds == null || ticketIds.Count == 0)
            {
                return true;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建包含所有ID的IN子句
                    string idList = string.Join(",", ticketIds);
                    string query = $"DELETE FROM collection_mapped_tickets_info WHERE collection_id = @CollectionId AND ticket_id IN ({idList})";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionId", collectionId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        // 更新收藏夹中的车票数量
                        await UpdateCollectionTicketCountAsync(collectionId);

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"从收藏夹中移除车票失败: {ex.Message}", ex);
                Debug.WriteLine($"从收藏夹中移除车票失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新收藏夹中的车票数量
        /// </summary>
        /// <param name="collectionId">收藏夹ID</param>
        /// <returns>是否更新成功</returns>
        private async Task<bool> UpdateCollectionTicketCountAsync(int collectionId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 获取当前车票数量
                    int ticketCount = await GetCollectionTicketCountAsync(collectionId);

                    // 更新收藏夹中的车票数量字段
                    string query = "UPDATE ticket_collections_info SET update_time = @UpdateTime WHERE id = @CollectionId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CollectionId", collectionId);
                        command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新收藏夹车票数量失败: {ex.Message}", ex);
                Debug.WriteLine($"更新收藏夹车票数量失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有收藏夹
        /// </summary>
        public async Task<List<TicketCollectionInfo>> GetAllCollectionsAsync()
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"SELECT tci.*, 
                                        (SELECT COUNT(*) FROM collection_mapped_tickets_info cmti WHERE cmti.collection_id = tci.id) AS ticket_count
                                    FROM ticket_collections_info tci
                                    ORDER BY tci.sort_order ASC, tci.update_time DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var collections = new List<TicketCollectionInfo>();

                            while (await reader.ReadAsync())
                            {
                                collections.Add(MapCollectionInfo(reader));
                            }

                            return collections;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取所有收藏夹失败: {ex.Message}", ex);
                return new List<TicketCollectionInfo>();
            }
        }

        /// <summary>
        /// 根据名称模糊查询收藏夹
        /// </summary>
        /// <param name="searchText">搜索关键词</param>
        /// <returns>匹配的收藏夹列表</returns>
        public async Task<List<TicketCollectionInfo>> SearchCollectionsByNameAsync(string searchText)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"SELECT tci.*, 
                                      (SELECT COUNT(*) FROM collection_mapped_tickets_info cmti WHERE cmti.collection_id = tci.id) AS ticket_count
                                    FROM ticket_collections_info tci
                                    WHERE tci.collection_name LIKE @SearchText
                                    ORDER BY tci.sort_order ASC, tci.update_time DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SearchText", $"%{searchText}%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var collections = new List<TicketCollectionInfo>();

                            while (await reader.ReadAsync())
                            {
                                collections.Add(MapCollectionInfo(reader));
                            }

                            return collections;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索收藏夹失败: {ex.Message}", ex);
                return new List<TicketCollectionInfo>();
            }
        }

        /// <summary>
        /// 创建路线信息表
        /// </summary>
        public async Task CreateRouteInfoTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `route_info`;
                    CREATE TABLE `route_info` (
                      `id` int NOT NULL AUTO_INCREMENT COMMENT '路线ID',
                      `route_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '路线名称',
                      `description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '路线描述',
                      `cover_image` blob NULL COMMENT '封面图片',
                      `create_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
                      `update_time` datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
                      `total_distance` decimal(10, 2) NULL DEFAULT 0 COMMENT '总里程(公里)',
                      `is_favorite` tinyint(1) NULL DEFAULT 0 COMMENT '是否收藏',
                      `sort_order` int NULL DEFAULT 0 COMMENT '排序顺序',
                      PRIMARY KEY (`id`) USING BTREE
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 创建路线车票映射表
        /// </summary>
        public async Task CreateRouteTicketMappingTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `route_ticket_mapping`;
                    CREATE TABLE `route_ticket_mapping` (
                      `id` int NOT NULL AUTO_INCREMENT COMMENT 'ID',
                      `route_id` int NOT NULL COMMENT '路线ID',
                      `ticket_id` int NOT NULL COMMENT '车票ID',
                      `add_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '添加时间',
                      PRIMARY KEY (`id`) USING BTREE,
                      INDEX `idx_route`(`route_id` ASC) USING BTREE,
                      INDEX `idx_ticket`(`ticket_id` ASC) USING BTREE,
                      CONSTRAINT `fk_rt_route` FOREIGN KEY (`route_id`) REFERENCES `route_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
                      CONSTRAINT `fk_rt_ticket` FOREIGN KEY (`ticket_id`) REFERENCES `train_ride_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 创建路线与车站映射表
        /// </summary>
        public async Task CreateRouteStationMappingTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `route_station_mapping`;
                    CREATE TABLE `route_station_mapping` (
                      `id` int NOT NULL AUTO_INCREMENT COMMENT 'ID',
                      `route_id` int NOT NULL COMMENT '路线ID',
                      `station_id` int NOT NULL COMMENT '车站ID',
                      `station_role` tinyint NULL DEFAULT 0 COMMENT '车站角色：1=起点,2=终点,4=经停,8=换乘',
                      `stay_time` int NULL DEFAULT 0 COMMENT '计划停留时间(分钟)',
                      `notes` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '备注',
                      `add_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '添加时间',
                      `distance_from_prev` decimal(10, 2) NULL DEFAULT 0.00 COMMENT '距离上一站点距离(公里)',
                      `distance_from_start` decimal(10, 2) NULL DEFAULT 0.00 COMMENT '距起点累计距离(公里)',
                      PRIMARY KEY (`id`) USING BTREE,
                      INDEX `idx_route`(`route_id` ASC) USING BTREE,
                      INDEX `idx_station`(`station_id` ASC) USING BTREE,
                      CONSTRAINT `fk_rs_route` FOREIGN KEY (`route_id`) REFERENCES `route_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
                      CONSTRAINT `fk_rs_station` FOREIGN KEY (`station_id`) REFERENCES `station_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 创建路线统计信息表
        /// </summary>
        public async Task CreateRouteStatisticsTableAsync()
        {
            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    await disableChecksCmd.ExecuteNonQueryAsync();
                }

                string query = @"
                    DROP TABLE IF EXISTS `route_statistics`;
                    CREATE TABLE `route_statistics` (
                      `id` int NOT NULL AUTO_INCREMENT COMMENT '统计ID',
                      `route_id` int NOT NULL COMMENT '路线ID',
                      `total_cost` decimal(10, 2) NULL DEFAULT 0 COMMENT '总花费',
                      `provinces_passed` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '经过的省份(逗号分隔)',
                      `cities_passed` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '经过的城市(逗号分隔)',
                      `seat_type_stats` json NULL COMMENT '不同席别的里程统计',
                      `railway_bureau_stats` json NULL COMMENT '不同铁路局的统计',
                      `update_time` datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
                      PRIMARY KEY (`id`) USING BTREE,
                      UNIQUE INDEX `uk_route`(`route_id` ASC) USING BTREE,
                      CONSTRAINT `fk_stat_route` FOREIGN KEY (`route_id`) REFERENCES `route_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;";

                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    await enableChecksCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 获取所有路线信息
        /// </summary>
        /// <returns>路线信息列表</returns>
        public async Task<List<RouteInfo>> GetRoutesAsync()
        {
            // 调用分页方法获取所有记录
            // 传递最大可能的页大小，确保获取所有记录
            return await GetRoutesAsync(1, int.MaxValue, "id", true);
        }

        /// <summary>
        /// 获取分页的路线信息
        /// </summary>
        /// <param name="pageNumber">页码 (从1开始)</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序字段</param>
        /// <param name="ascending">是否升序</param>
        /// <returns>路线信息列表</returns>
        public async Task<List<RouteInfo>> GetRoutesAsync(int pageNumber, int pageSize, string orderBy = "id", bool ascending = true)
        {
            var items = new List<RouteInfo>();
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10; // Default page size

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建排序方向
                    string direction = ascending ? "ASC" : "DESC";

                    // 显式列出所有需要的列
                    string columns = @"`id`, `route_name`, `description`, `cover_image`, 
                                     `create_time`, `update_time`, `total_distance`, 
                                     `is_favorite`, `sort_order`";

                    // 构建查询语句
                    string query = $"SELECT {columns} FROM route_info ";

                    // 添加排序
                    query += $"ORDER BY `{orderBy}` {direction} ";

                    // 只有分页查询需要LIMIT子句
                    if (pageSize < int.MaxValue)
                    {
                        query += "LIMIT @Offset, @PageSize";
                    }

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 只有分页查询需要添加这些参数
                        if (pageSize < int.MaxValue)
                        {
                            command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                            command.Parameters.AddWithValue("@PageSize", pageSize);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(MapRouteInfo(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线列表失败: {ex.Message}", ex);
                throw;
            }

            return items;
        }

        /// <summary>
        /// 获取路线总数
        /// </summary>
        /// <returns>路线总数</returns>
        public async Task<int> GetRouteCountAsync()
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand("SELECT COUNT(*) FROM route_info", connection))
                    {
                        // Set timeout for count operation as well
                        command.CommandTimeout = 15; // 15 seconds
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线总数时出错: {ex.Message}", ex);
                throw new Exception($"获取路线总数时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 将 DataReader 映射到 RouteInfo 对象
        /// </summary>
        private RouteInfo MapRouteInfo(DbDataReader reader)
        {
            try
            {
                var route = new RouteInfo
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    RouteName = reader.IsDBNull(reader.GetOrdinal("route_name")) ? null : reader.GetString(reader.GetOrdinal("route_name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    TotalDistance = reader.IsDBNull(reader.GetOrdinal("total_distance")) ? 0 : reader.GetDecimal(reader.GetOrdinal("total_distance")),
                    CreateTime = reader.IsDBNull(reader.GetOrdinal("create_time")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("create_time")),
                    UpdateTime = reader.IsDBNull(reader.GetOrdinal("update_time")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("update_time")),
                    IsFavorite = reader.IsDBNull(reader.GetOrdinal("is_favorite")) ? false : reader.GetBoolean(reader.GetOrdinal("is_favorite")),
                    SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32(reader.GetOrdinal("sort_order"))
                };

                // 读取BLOB数据(封面图片)
                int coverImageIdx = reader.GetOrdinal("cover_image");
                if (!reader.IsDBNull(coverImageIdx))
                {
                    Debug.WriteLine($"MapRouteInfo: 检测到路线ID={route.Id}的图片数据");
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        long bytesRead;
                        long fieldOffset = 0;
                        using (var stream = reader.GetStream(coverImageIdx))
                        {
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, (int)bytesRead);
                                fieldOffset += bytesRead;
                            }
                        }
                        route.CoverImage = ms.ToArray();
                        Debug.WriteLine($"MapRouteInfo: 已加载路线ID={route.Id}的图片数据，大小={route.CoverImage.Length}字节");
                    }
                }
                else
                {
                    Debug.WriteLine($"MapRouteInfo: 路线ID={route.Id}没有图片数据");
                }

                return route;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MapRouteInfo异常: {ex.Message}");
                LogHelper.LogError($"映射路线信息异常: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 添加路线
        /// </summary>
        /// <param name="route">路线信息</param>
        /// <returns>添加是否成功</returns>
        public async Task<bool> AddRouteAsync(RouteInfo route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"INSERT INTO route_info 
                                    (route_name, description, cover_image, create_time, update_time, total_distance, is_favorite, sort_order) 
                                    VALUES 
                                    (@RouteName, @Description, @CoverImage, @CreateTime, @UpdateTime, @TotalDistance, @IsFavorite, @SortOrder)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteName", route.RouteName);
                        command.Parameters.AddWithValue("@Description", route.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CoverImage", route.CoverImage ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreateTime", route.CreateTime);
                        command.Parameters.AddWithValue("@UpdateTime", route.UpdateTime);
                        command.Parameters.AddWithValue("@TotalDistance", route.TotalDistance);
                        command.Parameters.AddWithValue("@IsFavorite", route.IsFavorite);
                        command.Parameters.AddWithValue("@SortOrder", route.SortOrder);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"添加路线信息失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 更新路线信息
        /// </summary>
        /// <param name="route">要更新的路线信息</param>
        /// <returns>更新是否成功</returns>
        public async Task<bool> UpdateRouteAsync(RouteInfo route)
        {
            if (route == null || route.Id <= 0)
            {
                return false;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"UPDATE route_info 
                                   SET route_name = @RouteName, 
                                       description = @Description, 
                                       cover_image = @CoverImage, 
                                       update_time = @UpdateTime, 
                                       total_distance = @TotalDistance,
                                       is_favorite = @IsFavorite,
                                       sort_order = @SortOrder 
                                   WHERE id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", route.Id);
                        command.Parameters.AddWithValue("@RouteName", route.RouteName);
                        command.Parameters.AddWithValue("@Description", route.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CoverImage", route.CoverImage ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UpdateTime", route.UpdateTime);
                        command.Parameters.AddWithValue("@TotalDistance", route.TotalDistance);
                        command.Parameters.AddWithValue("@IsFavorite", route.IsFavorite);
                        command.Parameters.AddWithValue("@SortOrder", route.SortOrder);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新路线信息失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 根据ID列表删除路线
        /// </summary>
        /// <param name="routeIds">路线ID列表</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteRoutesByIdsAsync(List<int> routeIds)
        {
            if (routeIds == null || routeIds.Count == 0)
            {
                return true; // 没有要删除的ID，视为成功
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建包含所有ID的IN子句
                    string idList = string.Join(",", routeIds);
                    string query = $"DELETE FROM route_info WHERE id IN ({idList})";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"批量删除路线失败: {ex.Message}", ex);
                Debug.WriteLine($"批量删除路线失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据自定义SQL查询获取路线数量
        /// </summary>
        /// <param name="query">SQL查询语句</param>
        /// <returns>路线数量</returns>
        public async Task<int> GetRouteCountByCustomQueryAsync(string query)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 设置超时时间
                        command.CommandTimeout = 30; // 30秒

                        // 执行查询并返回结果
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"根据自定义查询获取路线数量失败: {ex.Message}", ex);
                throw new Exception($"获取路线数量时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据自定义SQL查询获取路线列表
        /// </summary>
        /// <param name="query">SQL查询语句</param>
        /// <param name="pageNumber">页码（如果查询已包含分页，可忽略）</param>
        /// <param name="pageSize">每页大小（如果查询已包含分页，可忽略）</param>
        /// <returns>路线列表</returns>
        public async Task<List<RouteInfo>> GetRoutesByCustomQueryAsync(string query, int pageNumber = 1, int pageSize = 10)
        {
            var items = new List<RouteInfo>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 设置超时时间
                        command.CommandTimeout = 30; // 30秒

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(MapRouteInfo(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"根据自定义查询获取路线列表失败: {ex.Message}", ex);
                throw new Exception($"获取路线列表时出错: {ex.Message}", ex);
            }

            return items;
        }

        /// <summary>
        /// 根据名称模糊搜索路线
        /// </summary>
        /// <param name="searchText">搜索关键词</param>
        /// <returns>匹配的路线列表</returns>
        public async Task<List<RouteInfo>> SearchRoutesByNameAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new List<RouteInfo>();
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"SELECT * FROM route_info 
                                    WHERE route_name LIKE @SearchText 
                                    ORDER BY sort_order ASC, update_time DESC
                                    LIMIT 10";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SearchText", $"%{searchText}%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var routes = new List<RouteInfo>();

                            while (await reader.ReadAsync())
                            {
                                routes.Add(MapRouteInfo(reader));
                            }

                            return routes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索路线失败: {ex.Message}", ex);
                return new List<RouteInfo>();
            }
        }

        /// <summary>
        /// 获取指定路线的车票总数
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>车票总数</returns>
        public async Task<int> GetRouteTicketsCountAsync(int routeId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"SELECT COUNT(*) 
                                    FROM route_ticket_mapping 
                                    WHERE route_id = @RouteId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        object result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线车票总数失败: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 获取指定路线的车票列表（包含车票详细信息）
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>车票映射列表</returns>
        public async Task<List<RouteTicketMapping>> GetRouteTicketsAsync(int routeId, int pageNumber = 1, int pageSize = 10)
        {
            var items = new List<RouteTicketMapping>();
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 使用JOIN联合查询获取车票详情和映射信息
                    string query = @"SELECT m.id, m.route_id, m.ticket_id, m.add_time, 
                                    t.ticket_number, t.check_in_location, t.depart_station, t.train_no, t.arrive_station,
                                    t.depart_station_pinyin, t.arrive_station_pinyin, t.depart_date, t.depart_time, 
                                    t.coach_no, t.seat_no, t.money, t.seat_type, t.additional_info, t.ticket_purpose, 
                                    t.hint, t.depart_station_code, t.arrive_station_code, t.ticket_modification_type,
                                    t.ticket_type_flags, t.payment_channel_flags
                                    FROM route_ticket_mapping m
                                    INNER JOIN train_ride_info t ON m.ticket_id = t.id
                                    WHERE m.route_id = @RouteId
                                    ORDER BY m.add_time DESC
                                    LIMIT @Offset, @PageSize";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // 创建映射对象
                                var mapping = new RouteTicketMapping
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    RouteId = reader.GetInt32(reader.GetOrdinal("route_id")),
                                    TicketId = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                                    AddTime = reader.GetDateTime(reader.GetOrdinal("add_time")),

                                    // 映射车票信息
                                    Ticket = new TrainRideInfo
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                                        TicketNumber = reader.IsDBNull(reader.GetOrdinal("ticket_number")) ? null : reader.GetString(reader.GetOrdinal("ticket_number")),
                                        CheckInLocation = reader.IsDBNull(reader.GetOrdinal("check_in_location")) ? null : reader.GetString(reader.GetOrdinal("check_in_location")),
                                        DepartStation = reader.IsDBNull(reader.GetOrdinal("depart_station")) ? null : reader.GetString(reader.GetOrdinal("depart_station")),
                                        TrainNo = reader.IsDBNull(reader.GetOrdinal("train_no")) ? null : reader.GetString(reader.GetOrdinal("train_no")),
                                        ArriveStation = reader.IsDBNull(reader.GetOrdinal("arrive_station")) ? null : reader.GetString(reader.GetOrdinal("arrive_station")),
                                        DepartStationPinyin = reader.IsDBNull(reader.GetOrdinal("depart_station_pinyin")) ? null : reader.GetString(reader.GetOrdinal("depart_station_pinyin")),
                                        ArriveStationPinyin = reader.IsDBNull(reader.GetOrdinal("arrive_station_pinyin")) ? null : reader.GetString(reader.GetOrdinal("arrive_station_pinyin")),
                                        DepartDate = reader.IsDBNull(reader.GetOrdinal("depart_date")) ? null : reader.GetDateTime(reader.GetOrdinal("depart_date")),
                                        CoachNo = reader.IsDBNull(reader.GetOrdinal("coach_no")) ? null : reader.GetString(reader.GetOrdinal("coach_no")),
                                        SeatNo = reader.IsDBNull(reader.GetOrdinal("seat_no")) ? null : reader.GetString(reader.GetOrdinal("seat_no")),
                                        Money = reader.IsDBNull(reader.GetOrdinal("money")) ? null : reader.GetDecimal(reader.GetOrdinal("money")),
                                        SeatType = reader.IsDBNull(reader.GetOrdinal("seat_type")) ? null : reader.GetString(reader.GetOrdinal("seat_type")),
                                        AdditionalInfo = reader.IsDBNull(reader.GetOrdinal("additional_info")) ? null : reader.GetString(reader.GetOrdinal("additional_info")),
                                        TicketPurpose = reader.IsDBNull(reader.GetOrdinal("ticket_purpose")) ? null : reader.GetString(reader.GetOrdinal("ticket_purpose")),
                                        Hint = reader.IsDBNull(reader.GetOrdinal("hint")) ? null : reader.GetString(reader.GetOrdinal("hint")),
                                        DepartStationCode = reader.IsDBNull(reader.GetOrdinal("depart_station_code")) ? null : reader.GetString(reader.GetOrdinal("depart_station_code")),
                                        ArriveStationCode = reader.IsDBNull(reader.GetOrdinal("arrive_station_code")) ? null : reader.GetString(reader.GetOrdinal("arrive_station_code")),
                                        TicketModificationType = reader.IsDBNull(reader.GetOrdinal("ticket_modification_type")) ? null : reader.GetString(reader.GetOrdinal("ticket_modification_type")),
                                        TicketTypeFlags = reader.IsDBNull(reader.GetOrdinal("ticket_type_flags")) ? 0 : reader.GetInt32(reader.GetOrdinal("ticket_type_flags")),
                                        PaymentChannelFlags = reader.IsDBNull(reader.GetOrdinal("payment_channel_flags")) ? 0 : reader.GetInt32(reader.GetOrdinal("payment_channel_flags"))
                                    }
                                };

                                // 处理时间字段
                                if (!reader.IsDBNull(reader.GetOrdinal("depart_time")))
                                {
                                    try
                                    {
                                        // 尝试从MySQL读取时间
                                        if (reader is MySqlDataReader mysqlReader)
                                        {
                                            mapping.Ticket.DepartTime = mysqlReader.GetTimeSpan(reader.GetOrdinal("depart_time"));
                                        }
                                        else
                                        {
                                            // 尝试从字符串解析
                                            string timeStr = reader.GetString(reader.GetOrdinal("depart_time"));
                                            mapping.Ticket.DepartTime = TimeSpan.Parse(timeStr);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"解析时间出错: {ex.Message}");
                                    }
                                }

                                items.Add(mapping);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线车票列表失败: {ex.Message}", ex);
                throw;
            }

            return items;
        }

        /// <summary>
        /// 获取路线中已有的车票ID列表
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>车票ID列表</returns>
        public async Task<List<int>> GetRouteTicketIdsAsync(int routeId)
        {
            var ticketIds = new List<int>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT ticket_id 
                        FROM route_ticket_mapping 
                        WHERE route_id = @routeId";

                    // 输出完整的SQL查询（替换参数值）
                    string debugSql = query.Replace("@routeId", routeId.ToString());
                    Debug.WriteLine($"SQL查询路线车票IDs: {debugSql}");

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@routeId", routeId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ticketIds.Add(reader.GetInt32(0));
                            }
                        }
                    }

                    Debug.WriteLine($"找到路线ID={routeId}的车票IDs: {string.Join(", ", ticketIds)}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线车票ID列表失败: {ex.Message}", ex);
                throw;
            }

            return ticketIds;
        }

        /// <summary>
        /// 批量添加车票到路线
        /// </summary>
        /// <param name="mappings">路线车票映射列表</param>
        /// <returns>添加成功的记录数</returns>
        public async Task<int> AddTicketsToRouteAsync(List<RouteTicketMapping> mappings)
        {
            if (mappings == null || mappings.Count == 0)
                return 0;

            int addedCount = 0;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        string query = @"
                            INSERT INTO route_ticket_mapping 
                            (route_id, ticket_id, add_time) 
                            VALUES 
                            (@routeId, @ticketId, @addTime)";

                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            // 添加参数，但不设置值（将在循环中设置）
                            command.Parameters.Add("@routeId", MySqlDbType.Int32);
                            command.Parameters.Add("@ticketId", MySqlDbType.Int32);
                            command.Parameters.Add("@addTime", MySqlDbType.DateTime);

                            // 依次处理每个映射
                            foreach (var mapping in mappings)
                            {
                                command.Parameters["@routeId"].Value = mapping.RouteId;
                                command.Parameters["@ticketId"].Value = mapping.TicketId;
                                command.Parameters["@addTime"].Value = mapping.AddTime;

                                // 执行插入
                                addedCount += await command.ExecuteNonQueryAsync();
                            }
                        }

                        // 提交事务
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        // 回滚事务
                        await transaction.RollbackAsync();
                        LogHelper.LogError($"添加车票到路线失败: {ex.Message}", ex);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"添加车票到路线失败: {ex.Message}", ex);
                throw;
            }

            return addedCount;
        }

        /// <summary>
        /// 从路线中移除车票
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <param name="ticketIds">要移除的车票ID列表</param>
        /// <returns>是否移除成功</returns>
        public async Task<bool> RemoveTicketsFromRouteAsync(int routeId, List<int> ticketIds)
        {
            if (ticketIds == null || ticketIds.Count == 0)
            {
                return true; // 没有要删除的记录，视为成功
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 构建包含所有车票ID的IN子句
                    string idList = string.Join(",", ticketIds);
                    string query = $"DELETE FROM route_ticket_mapping WHERE route_id = @RouteId AND ticket_id IN ({idList})";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"从路线中移除车票失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取路线车站映射数据总数
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>映射数据总数</returns>
        public async Task<int> GetRouteStationsCountAsync(int routeId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string sql = @"SELECT COUNT(*) FROM route_station_mapping WHERE route_id = @RouteId";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取路线车站数据总数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取路线车站映射数据
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>车站映射列表</returns>
        public async Task<List<RouteStationMapping>> GetRouteStationsAsync(int routeId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = new List<RouteStationMapping>();

                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 计算跳过的记录数
                    int skip = (pageNumber - 1) * pageSize;

                    string sql = @"
                        SELECT rsm.*, si.* 
                        FROM route_station_mapping rsm
                        LEFT JOIN station_info si ON rsm.station_id = si.id
                        WHERE rsm.route_id = @RouteId
                        ORDER BY rsm.add_time ASC
                        LIMIT @Skip, @PageSize";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        command.Parameters.AddWithValue("@Skip", skip);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // 映射车站数据
                                var mapping = new RouteStationMapping
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    RouteId = reader.GetInt32(reader.GetOrdinal("route_id")),
                                    StationId = reader.GetInt32(reader.GetOrdinal("station_id")),
                                    StationRole = reader.IsDBNull(reader.GetOrdinal("station_role")) ? (byte)0 : reader.GetByte(reader.GetOrdinal("station_role")),
                                    StayTime = reader.IsDBNull(reader.GetOrdinal("stay_time")) ? 0 : reader.GetInt32(reader.GetOrdinal("stay_time")),
                                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                                    AddTime = reader.IsDBNull(reader.GetOrdinal("add_time")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("add_time")),
                                    DistanceFromPrev = reader.IsDBNull(reader.GetOrdinal("distance_from_prev")) ? 0m : reader.GetDecimal(reader.GetOrdinal("distance_from_prev")),
                                    DistanceFromStart = reader.IsDBNull(reader.GetOrdinal("distance_from_start")) ? 0m : reader.GetDecimal(reader.GetOrdinal("distance_from_start")),
                                    IsSelected = false
                                };

                                // 映射关联的车站数据
                                mapping.Station = MapStationInfo(reader);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取路线车站数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 向路线添加车站
        /// </summary>
        /// <param name="mapping">路线车站映射数据</param>
        /// <returns>添加是否成功</returns>
        public async Task<bool> AddStationToRouteAsync(RouteStationMapping mapping)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"INSERT INTO route_station_mapping 
                                    (route_id, station_id, station_role, stay_time, notes, add_time, distance_from_prev, distance_from_start) 
                                    VALUES 
                                    (@RouteId, @StationId, @StationRole, @StayTime, @Notes, @AddTime, @DistanceFromPrev, @DistanceFromStart)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", mapping.RouteId);
                        command.Parameters.AddWithValue("@StationId", mapping.StationId);
                        command.Parameters.AddWithValue("@StationRole", mapping.StationRole);
                        command.Parameters.AddWithValue("@StayTime", mapping.StayTime);
                        command.Parameters.AddWithValue("@Notes", mapping.Notes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AddTime", mapping.AddTime);
                        command.Parameters.AddWithValue("@DistanceFromPrev", mapping.DistanceFromPrev);
                        command.Parameters.AddWithValue("@DistanceFromStart", mapping.DistanceFromStart);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"添加车站到路线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 更新路线车站映射信息
        /// </summary>
        /// <param name="mapping">要更新的路线车站映射</param>
        /// <returns>更新是否成功</returns>
        public async Task<bool> UpdateRouteStationAsync(RouteStationMapping mapping)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"UPDATE route_station_mapping 
                                    SET station_role = @StationRole,
                                        stay_time = @StayTime,
                                        notes = @Notes,
                                        distance_from_prev = @DistanceFromPrev,
                                        distance_from_start = @DistanceFromStart
                                    WHERE id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", mapping.Id);
                        command.Parameters.AddWithValue("@StationRole", mapping.StationRole);
                        command.Parameters.AddWithValue("@StayTime", mapping.StayTime);
                        command.Parameters.AddWithValue("@Notes", mapping.Notes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DistanceFromPrev", mapping.DistanceFromPrev);
                        command.Parameters.AddWithValue("@DistanceFromStart", mapping.DistanceFromStart);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新路线车站映射失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 根据ID获取车站信息
        /// </summary>
        /// <param name="stationId">车站ID</param>
        /// <returns>车站信息</returns>
        public async Task<StationInfo> GetStationByIdAsync(int stationId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM station_info WHERE id = @stationId";
                        command.Parameters.AddWithValue("@stationId", stationId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapStationInfo(reader);
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"根据ID获取车站信息失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 删除指定ID的路线车站映射
        /// </summary>
        /// <param name="mappingIds">要删除的映射ID列表</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteRouteStationsByIdsAsync(List<int> mappingIds)
        {
            if (mappingIds == null || mappingIds.Count == 0)
                return true;

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = connection.CreateCommand())
                    {
                        // 构建IN查询语句
                        string idList = string.Join(",", mappingIds);
                        command.CommandText = $"DELETE FROM route_station_mapping WHERE id IN ({idList})";

                        int affectedRows = await command.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"删除路线车站映射失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 根据车站代码获取车站信息
        /// </summary>
        /// <param name="stationCode">车站代码</param>
        /// <returns>车站信息，如果不存在则返回null</returns>
        public async Task<StationInfo> GetStationByCodeAsync(string stationCode)
        {
            if (string.IsNullOrWhiteSpace(stationCode))
            {
                return null;
            }

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "@StationCode", stationCode }
                    };

                    using (var command = new MySqlCommand(null, connection))
                    {
                        var results = await QueryStationsAsync(command, "station_code = @StationCode", parameters);
                        return results.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"根据代码获取车站信息失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取路线的统计信息
        /// </summary>
        public async Task<RouteStatisticsInfo> GetRouteStatisticsAsync(int routeId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    var command = new MySqlCommand("SELECT * FROM route_statistics WHERE route_id = @routeId LIMIT 1", connection);
                    command.Parameters.AddWithValue("@routeId", routeId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapRouteStatisticsInfo(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线统计信息失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 创建或更新路线统计信息
        /// </summary>
        public async Task<bool> CreateOrUpdateRouteStatisticsAsync(RouteStatisticsInfo statistics)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    // 检查是否已存在记录
                    var checkCommand = new MySqlCommand("SELECT id FROM route_statistics WHERE route_id = @routeId LIMIT 1", connection);
                    checkCommand.Parameters.AddWithValue("@routeId", statistics.RouteId);
                    var existingId = await checkCommand.ExecuteScalarAsync();

                    if (existingId != null)
                    {
                        // 更新现有记录
                        var updateCommand = new MySqlCommand(
                            "UPDATE route_statistics SET " +
                            "total_cost = @totalCost, " +
                            "provinces_passed = @provincesPassed, " +
                            "cities_passed = @citiesPassed, " +
                            "seat_type_stats = @seatTypeStats, " +
                            "railway_bureau_stats = @railwayBureauStats, " +
                            "update_time = CURRENT_TIMESTAMP " +
                            "WHERE id = @id", connection);

                        updateCommand.Parameters.AddWithValue("@id", existingId);
                        updateCommand.Parameters.AddWithValue("@totalCost", statistics.TotalCost);
                        updateCommand.Parameters.AddWithValue("@provincesPassed", statistics.ProvincesPassed ?? "");
                        updateCommand.Parameters.AddWithValue("@citiesPassed", statistics.CitiesPassed ?? "");
                        updateCommand.Parameters.AddWithValue("@seatTypeStats", statistics.SeatTypeStats ?? "{}");
                        updateCommand.Parameters.AddWithValue("@railwayBureauStats", statistics.RailwayBureauStats ?? "{}");

                        return await updateCommand.ExecuteNonQueryAsync() > 0;
                    }
                    else
                    {
                        // 创建新记录
                        var insertCommand = new MySqlCommand(
                            "INSERT INTO route_statistics " +
                            "(route_id, total_cost, provinces_passed, cities_passed, seat_type_stats, railway_bureau_stats) " +
                            "VALUES (@routeId, @totalCost, @provincesPassed, @citiesPassed, @seatTypeStats, @railwayBureauStats)",
                            connection);

                        insertCommand.Parameters.AddWithValue("@routeId", statistics.RouteId);
                        insertCommand.Parameters.AddWithValue("@totalCost", statistics.TotalCost);
                        insertCommand.Parameters.AddWithValue("@provincesPassed", statistics.ProvincesPassed ?? "");
                        insertCommand.Parameters.AddWithValue("@citiesPassed", statistics.CitiesPassed ?? "");
                        insertCommand.Parameters.AddWithValue("@seatTypeStats", statistics.SeatTypeStats ?? "{}");
                        insertCommand.Parameters.AddWithValue("@railwayBureauStats", statistics.RailwayBureauStats ?? "{}");

                        return await insertCommand.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"创建或更新路线统计信息失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 将数据库记录映射为路线统计信息对象
        /// </summary>
        private RouteStatisticsInfo MapRouteStatisticsInfo(DbDataReader reader)
        {
            return new RouteStatisticsInfo
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                RouteId = reader.GetInt32(reader.GetOrdinal("route_id")),
                TotalCost = reader.GetDecimal(reader.GetOrdinal("total_cost")),
                ProvincesPassed = reader.IsDBNull(reader.GetOrdinal("provinces_passed")) ? null : reader.GetString(reader.GetOrdinal("provinces_passed")),
                CitiesPassed = reader.IsDBNull(reader.GetOrdinal("cities_passed")) ? null : reader.GetString(reader.GetOrdinal("cities_passed")),
                SeatTypeStats = reader.IsDBNull(reader.GetOrdinal("seat_type_stats")) ? null : reader.GetString(reader.GetOrdinal("seat_type_stats")),
                RailwayBureauStats = reader.IsDBNull(reader.GetOrdinal("railway_bureau_stats")) ? null : reader.GetString(reader.GetOrdinal("railway_bureau_stats")),
                UpdateTime = reader.GetDateTime(reader.GetOrdinal("update_time"))
            };
        }

        /// <summary>
        /// 获取路线总花费
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>总花费（元）</returns>
        public async Task<decimal> GetRouteTotalCostAsync(int routeId)
        {
            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT SUM(tri.money) AS total_cost
                        FROM train_ride_info tri
                        JOIN route_ticket_mapping rtm ON tri.id = rtm.ticket_id
                        WHERE rtm.route_id = @RouteId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        object result = await command.ExecuteScalarAsync();
                        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线总花费失败: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 获取席别统计数据
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>席别统计字典(键为席别名称，值为{里程,百分比}元组)</returns>
        public async Task<Dictionary<string, (decimal Distance, decimal Percentage)>> GetSeatTypeStatisticsAsync(int routeId)
        {
            var result = new Dictionary<string, (decimal Distance, decimal Percentage)>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT
                            tri.seat_type AS seat_type,
                            SUM(
                                CASE
                                    WHEN rsm1.distance_from_start <= rsm2.distance_from_start 
                                    THEN rsm2.distance_from_start - rsm1.distance_from_start
                                    ELSE 0
                                END
                            ) AS total_distance,
                            ROUND(
                                SUM(
                                    CASE
                                        WHEN rsm1.distance_from_start <= rsm2.distance_from_start 
                                        THEN rsm2.distance_from_start - rsm1.distance_from_start
                                        ELSE 0
                                    END
                                ) / (
                                    SELECT total_distance 
                                    FROM route_info 
                                    WHERE id = @RouteId
                                ) * 100, 
                                1
                            ) AS percentage
                        FROM
                            train_ride_info tri
                        JOIN
                            route_ticket_mapping rtm ON tri.id = rtm.ticket_id
                        JOIN
                            station_info si_depart ON tri.depart_station_code = si_depart.station_code
                        JOIN
                            station_info si_arrive ON tri.arrive_station_code = si_arrive.station_code
                        JOIN
                            route_station_mapping rsm1 ON rtm.route_id = rsm1.route_id AND si_depart.id = rsm1.station_id
                        JOIN
                            route_station_mapping rsm2 ON rtm.route_id = rsm2.route_id AND si_arrive.id = rsm2.station_id
                        WHERE
                            rtm.route_id = @RouteId
                            AND tri.seat_type IS NOT NULL
                        GROUP BY
                            tri.seat_type
                        ORDER BY
                            total_distance DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string seatType = reader.IsDBNull(0) ? "未知" : reader.GetString(0);
                                decimal distance = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                                decimal percentage = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);

                                result[seatType] = (distance, percentage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取席别统计数据失败: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// 获取路线经过的省份列表
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>省份列表</returns>
        public async Task<List<string>> GetRouteProvinceListAsync(int routeId)
        {
            var provinces = new List<string>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT DISTINCT si.province
                        FROM route_station_mapping rsm
                        JOIN station_info si ON rsm.station_id = si.id
                        WHERE rsm.route_id = @RouteId
                        AND si.province IS NOT NULL
                        AND si.province <> ''
                        ORDER BY si.province";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    provinces.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线经过的省份列表失败: {ex.Message}", ex);
            }

            return provinces;
        }

        /// <summary>
        /// 获取路线经过的城市列表
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>城市列表</returns>
        public async Task<List<string>> GetRouteCityListAsync(int routeId)
        {
            var cities = new List<string>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT DISTINCT si.province, si.city
                        FROM route_station_mapping rsm
                        JOIN station_info si ON rsm.station_id = si.id
                        WHERE rsm.route_id = @RouteId
                        AND si.city IS NOT NULL
                        AND si.city <> ''
                        ORDER BY si.province, si.city";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                {
                                    string province = reader.GetString(0);
                                    string city = reader.GetString(1);
                                    cities.Add($"{city}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线经过的城市列表失败: {ex.Message}", ex);
            }

            return cities;
        }

        /// <summary>
        /// 获取铁路局统计数据
        /// </summary>
        /// <param name="routeId">路线ID</param>
        /// <returns>铁路局统计字典(键为铁路局名称，值为{里程,百分比}元组)</returns>
        public async Task<Dictionary<string, (decimal Distance, decimal Percentage)>> GetRailwayBureauStatisticsAsync(int routeId)
        {
            var result = new Dictionary<string, (decimal Distance, decimal Percentage)>();

            try
            {
                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    string query = @"
                        SELECT 
                            si.railway_bureau AS bureau,
                            SUM(rsm.distance_from_prev) AS total_distance,
                            ROUND(SUM(rsm.distance_from_prev) / (
                                SELECT total_distance 
                                FROM route_info 
                                WHERE id = @RouteId
                            ) * 100, 2) AS percentage
                        FROM route_station_mapping rsm
                        JOIN station_info si ON rsm.station_id = si.id
                        WHERE rsm.route_id = @RouteId
                        AND si.railway_bureau IS NOT NULL
                        AND si.railway_bureau <> ''
                        GROUP BY si.railway_bureau
                        ORDER BY SUM(rsm.distance_from_prev) DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string bureau = reader.IsDBNull(0) ? "未知" : reader.GetString(0);
                                decimal distance = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                                decimal percentage = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);

                                result[bureau] = (distance, percentage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取铁路局统计数据失败: {ex.Message}", ex);
            }

            return result;
        }
    }
}