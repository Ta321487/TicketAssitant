using MySql.Data.MySqlClient;
using System.Data.Common;
using TA_WPF.Models;
using TA_WPF.Utils;

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
                    string direction = ascending ? "ASC" : "DESC";
                    // Basic validation for orderBy to prevent injection, allow only known columns
                    string[] allowedColumns = { "id", "station_name", "province", "city", "district", "station_code", "station_pinyin" };
                    if (!allowedColumns.Contains(orderBy.ToLower()))
                    {
                        orderBy = "id"; // Default to id if invalid column
                    }

                    string query = "SELECT * FROM station_info ";
                    
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
                           (station_name, province, city, district, longitude, latitude, station_code, station_pinyin) 
                           VALUES 
                           (@StationName, @Province, @City, @District, @Longitude, @Latitude, @StationCode, @StationPinyin)";

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
                StationPinyin = reader.IsDBNull(reader.GetOrdinal("station_pinyin")) ? null : reader.GetString(reader.GetOrdinal("station_pinyin"))
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
                    System.Diagnostics.Debug.WriteLine($"数据库错误: {ex.Message}, 错误代码: {ex.Number}");
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

        /// <summary>
        /// 根据筛选条件获取车票总数
        /// </summary>
        /// <param name="departStation">出发站</param>
        /// <param name="trainNo">车次号</param>
        /// <param name="year">出发年份</param>
        /// <param name="isAndCondition">是否使用AND条件</param>
        /// <returns>符合条件的车票总数</returns>
        public async Task<int> GetFilteredTrainRideInfoCountAsync(string departStation, string trainNo, int? year, bool isAndCondition)
        {
            try
            {
                // 构建查询条件
                var conditions = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (isAndCondition)
                {
                    // 如果没有任何条件，返回所有记录
                    if (string.IsNullOrWhiteSpace(departStation) && string.IsNullOrWhiteSpace(trainNo) && !year.HasValue)
                    {
                        return await GetTotalTrainRideInfoCountAsync();
                    }

                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        // 确保站名以"站"结尾
                        string stationName = StationNameHelper.EnsureStationSuffix(departStation);
                        conditions.Add("depart_station = @DepartStation");
                        parameters.Add("@DepartStation", stationName);
                    }
                    else
                    {
                        // 对于AND条件，如果站点为空，使用IS NULL条件
                        conditions.Add("depart_station IS NULL");
                    }

                    // 添加车次号筛选条件
                    if (!string.IsNullOrWhiteSpace(trainNo))
                    {
                        conditions.Add("train_no = @TrainNo");
                        parameters.Add("@TrainNo", trainNo);
                    }
                    else
                    {
                        // 对于AND条件，如果车次为空，使用IS NULL条件
                        conditions.Add("train_no IS NULL");
                    }

                    // 添加出发年份筛选条件
                    if (year.HasValue)
                    {
                        conditions.Add("YEAR(depart_date) = @Year");
                        parameters.Add("@Year", year.Value);
                    }
                    else
                    {
                        // 对于AND条件，如果年份为空，使用IS NULL条件
                        // 使用YEAR()函数对NULL值返回NULL，所以需要检测日期是否为NULL
                        conditions.Add("depart_date IS NULL");
                    }
                }
                else // OR 条件
                {
                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        // 确保站名以"站"结尾
                        string stationName = StationNameHelper.EnsureStationSuffix(departStation);
                        conditions.Add("depart_station = @DepartStation");
                        parameters.Add("@DepartStation", stationName);
                    }

                    // 添加车次号筛选条件
                    if (!string.IsNullOrWhiteSpace(trainNo))
                    {
                        conditions.Add("train_no = @TrainNo");
                        parameters.Add("@TrainNo", trainNo);
                    }

                    // 添加出发年份筛选条件
                    if (year.HasValue)
                    {
                        conditions.Add("YEAR(depart_date) = @Year");
                        parameters.Add("@Year", year.Value);
                    }
                }

                // 如果没有任何条件，返回所有记录数
                if (conditions.Count == 0)
                {
                    return await GetTotalTrainRideInfoCountAsync();
                }

                // 构建SQL查询语句
                string query;
                // 如果只有一个条件，不需要使用AND或OR
                if (conditions.Count == 1)
                {
                    query = $"SELECT COUNT(*) FROM train_ride_info WHERE {conditions[0]}";
                }
                else
                {
                    string conditionOperator = isAndCondition ? " AND " : " OR ";
                    query = $"SELECT COUNT(*) FROM train_ride_info WHERE {string.Join(conditionOperator, conditions)}";
                }

                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 添加参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取筛选车票总数时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据筛选条件获取分页车票信息
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页记录数</param>
        /// <param name="departStation">出发站</param>
        /// <param name="trainNo">车次号</param>
        /// <param name="year">出发年份</param>
        /// <param name="isAndCondition">是否使用AND条件</param>
        /// <returns>符合条件的车票列表</returns>
        public async Task<List<TrainRideInfo>> GetFilteredTrainRideInfosAsync(int pageNumber, int pageSize, string departStation, string trainNo, int? year, bool isAndCondition)
        {
            try
            {
                var items = new List<TrainRideInfo>();

                // 构建查询条件
                var conditions = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (isAndCondition)
                {
                    // 如果没有任何条件，返回所有记录
                    if (string.IsNullOrWhiteSpace(departStation) && string.IsNullOrWhiteSpace(trainNo) && !year.HasValue)
                    {
                        return await GetPagedTrainRideInfosAsync(pageNumber, pageSize);
                    }

                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        // 确保站名以"站"结尾
                        string stationName = StationNameHelper.EnsureStationSuffix(departStation);
                        conditions.Add("depart_station = @DepartStation");
                        parameters.Add("@DepartStation", stationName);
                    }
                    else
                    {
                        // 对于AND条件，如果站点为空，使用IS NULL条件
                        conditions.Add("depart_station IS NULL");
                    }

                    // 添加车次号筛选条件
                    if (!string.IsNullOrWhiteSpace(trainNo))
                    {
                        conditions.Add("train_no = @TrainNo");
                        parameters.Add("@TrainNo", trainNo);
                    }
                    else
                    {
                        // 对于AND条件，如果车次为空，使用IS NULL条件
                        conditions.Add("train_no IS NULL");
                    }

                    // 添加出发年份筛选条件
                    if (year.HasValue)
                    {
                        conditions.Add("YEAR(depart_date) = @Year");
                        parameters.Add("@Year", year.Value);
                    }
                    else
                    {
                        // 对于AND条件，如果年份为空，使用IS NULL条件
                        // 使用YEAR()函数对NULL值返回NULL，所以需要检测日期是否为NULL
                        conditions.Add("depart_date IS NULL");
                    }
                }
                else // OR 条件
                {
                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        // 确保站名以"站"结尾
                        string stationName = StationNameHelper.EnsureStationSuffix(departStation);
                        conditions.Add("depart_station = @DepartStation");
                        parameters.Add("@DepartStation", stationName);
                    }

                    // 添加车次号筛选条件
                    if (!string.IsNullOrWhiteSpace(trainNo))
                    {
                        conditions.Add("train_no = @TrainNo");
                        parameters.Add("@TrainNo", trainNo);
                    }

                    // 添加出发年份筛选条件
                    if (year.HasValue)
                    {
                        conditions.Add("YEAR(depart_date) = @Year");
                        parameters.Add("@Year", year.Value);
                    }
                }

                // 如果没有任何条件，使用常规查询
                if (conditions.Count == 0)
                {
                    return await GetPagedTrainRideInfosAsync(pageNumber, pageSize);
                }

                // 构建SQL查询语句
                string query;
                // 如果只有一个条件，不需要使用AND或OR
                if (conditions.Count == 1)
                {
                    query = $@"SELECT * FROM train_ride_info 
                            WHERE {conditions[0]} 
                            ORDER BY id 
                            LIMIT @Offset, @PageSize";
                }
                else
                {
                    string conditionOperator = isAndCondition ? " AND " : " OR ";
                    query = $@"SELECT * FROM train_ride_info 
                            WHERE {string.Join(conditionOperator, conditions)} 
                            ORDER BY id 
                            LIMIT @Offset, @PageSize";
                }

                using (var connection = await GetOpenConnectionWithRetryAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 添加分页参数
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        // 添加筛选参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

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
            catch (Exception ex)
            {
                throw new Exception($"获取筛选车票信息时出错: {ex.Message}", ex);
            }
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
                    System.Diagnostics.Debug.WriteLine($"解析时间出错: {ex.Message}");
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
                    `station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站拼音',
                    PRIMARY KEY (`id`) USING BTREE,
                    INDEX `station_name`(`station_name` ASC) USING BTREE,
                    INDEX `fk_arrive_code`(`station_code` ASC) USING BTREE,
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
                    CONSTRAINT `fc_dp_arrive` FOREIGN KEY (`arrive_station_pinyin`) REFERENCES `station_info` (`station_pinyin`) ON DELETE CASCADE ON UPDATE CASCADE,
                    CONSTRAINT `fc_sp_depart` FOREIGN KEY (`depart_station_pinyin`) REFERENCES `station_info` (`station_pinyin`) ON DELETE CASCADE ON UPDATE CASCADE,
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
                    System.Diagnostics.Debug.WriteLine($"数据库错误: {ex.Message}, 错误代码: {ex.Number}");
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
                System.Diagnostics.Debug.WriteLine($"删除车票时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有不同的出发站
        /// </summary>
        /// <returns>不同出发站列表</returns>
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
                System.Diagnostics.Debug.WriteLine($"获取出发站列表时出错: {ex.Message}");
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
                            station_code = @StationCode
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
    }
}