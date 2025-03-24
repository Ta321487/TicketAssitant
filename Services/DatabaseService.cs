using System.Data.Common;
using MySql.Data.MySqlClient;
using TA_WPF.Models;
using System.Text;
using TA_WPF.Utils;
using System.Diagnostics;

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
                        LogHelper.LogError($"数据库连接失败。错误: {ex.Message}, 错误代码: {ex.Number}");
                        throw; // 重新抛出异常
                    }
                    
                    // 记录重试信息
                    LogHelper.LogWarning($"数据库连接失败，正在重试。错误: {ex.Message}");
                    
                    // 等待一段时间后重试
                    await Task.Delay(RetryDelayMs);
                }
            }
        }

        public async Task<(List<TrainRideInfo> Items, int TotalCount)> GetTrainRideInfoAsync(int pageSize, int pageNumber)
        {
            var items = new List<TrainRideInfo>();
            int totalCount = 0;

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                // 获取总记录数
                using (var countCommand = new MySqlCommand("SELECT COUNT(*) FROM train_ride_info", connection))
                {
                    totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                // 获取分页数据
                string query = @"SELECT * FROM train_ride_info 
                               ORDER BY id 
                               LIMIT @Offset, @PageSize";

                using (var command = new MySqlCommand(query, connection))
                {
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

            return (items, totalCount);
        }

        public async Task<List<StationInfo>> GetStationsAsync()
        {
            var stations = new List<StationInfo>();

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                string query = "SELECT * FROM station_info ORDER BY station_name";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            stations.Add(new StationInfo
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
                            });
                        }
                    }
                }
            }

            return stations;
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
                                    depart_station_code, arrive_station_code, ticket_modification_type)
                                  VALUES (
                                    @TicketNumber, @CheckInLocation, @DepartStation, @TrainNo, 
                                    @ArriveStation, @DepartStationPinyin, @ArriveStationPinyin, 
                                    @DepartDate, @DepartTime, @CoachNo, @SeatNo, @Money, 
                                    @SeatType, @AdditionalInfo, @TicketPurpose, @Hint, 
                                    @DepartStationCode, @ArriveStationCode, @TicketModificationType)";

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
                        // 如果用户输入的站名不以"站"结尾，自动添加"站"字
                        string stationName = departStation.EndsWith("站") ? departStation : departStation + "站";
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
                        // 使用YEAR()函数对NULL值返回NULL，所以需要检查日期是否为NULL
                        conditions.Add("depart_date IS NULL");
                    }
                }
                else // OR 条件
                {
                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        // 如果用户输入的站名不以"站"结尾，自动添加"站"字
                        string stationName = departStation.EndsWith("站") ? departStation : departStation + "站";
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
                        // 如果用户输入的站名不以"站"结尾，自动添加"站"字
                        string stationName = departStation.EndsWith("站") ? departStation : departStation + "站";
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
                        // 使用YEAR()函数对NULL值返回NULL，所以需要检查日期是否为NULL
                        conditions.Add("depart_date IS NULL");
                    }
                }
                else // OR 条件
                {
                    // 添加出发站筛选条件
                    if (!string.IsNullOrWhiteSpace(departStation))
                    {
                        // 如果用户输入的站名不以"站"结尾，自动添加"站"字
                        string stationName = departStation.EndsWith("站") ? departStation : departStation + "站";
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
                TicketModificationType = reader.IsDBNull(reader.GetOrdinal("ticket_modification_type")) ? null : reader.GetString(reader.GetOrdinal("ticket_modification_type"))
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
                    Console.WriteLine($"解析时间出错: {ex.Message}");
                }
            }

            return trainRideInfo;
        }

        public async Task<List<StationInfo>> SearchStationsByNameAsync(string partialName)
        {
            var stations = new List<StationInfo>();

            if (string.IsNullOrWhiteSpace(partialName))
                return stations;

            // 创建带有超时设置的连接字符串
            var builder = new MySqlConnectionStringBuilder(_connectionString)
            {
                ConnectionTimeout = 5 // 设置连接超时为5秒
            };

            using (var connection = await GetOpenConnectionWithRetryAsync())
            {
                try
                {
                    string query = "SELECT * FROM station_info WHERE station_name LIKE @PartialName ORDER BY station_name LIMIT 10";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // 设置命令超时为3秒
                        command.CommandTimeout = 10;
                        
                        command.Parameters.AddWithValue("@PartialName", partialName + "%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stations.Add(new StationInfo
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
                                });
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    // 记录详细的数据库错误信息
                    System.Diagnostics.Debug.WriteLine($"搜索车站时数据库错误: {ex.Message}, 错误代码: {ex.Number}");
                    throw; // 重新抛出异常以便上层处理
                }
            }

            return stations;
        }

        /// <summary>
        /// 检查数据库中是否存在指定的表
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
                string query = @"
                CREATE TABLE IF NOT EXISTS `station_info` (
                  `id` int NOT NULL AUTO_INCREMENT,
                  `station_name` varchar(50) DEFAULT NULL,
                  `province` varchar(20) DEFAULT NULL,
                  `city` varchar(20) DEFAULT NULL,
                  `district` varchar(20) DEFAULT NULL,
                  `longitude` varchar(20) DEFAULT NULL,
                  `latitude` varchar(20) DEFAULT NULL,
                  `station_code` varchar(10) DEFAULT NULL,
                  `station_pinyin` varchar(50) DEFAULT NULL,
                  PRIMARY KEY (`id`),
                  UNIQUE KEY `station_name` (`station_name`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
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
                //TODO: 修改这个表结构
                
                string query = @"
                CREATE TABLE IF NOT EXISTS `train_ride_info` (
                  `id` int NOT NULL AUTO_INCREMENT,
                  `train_number` varchar(20) DEFAULT NULL,
                  `departure_station` varchar(50) DEFAULT NULL,
                  `arrival_station` varchar(50) DEFAULT NULL,
                  `departure_time` datetime DEFAULT NULL,
                  `arrival_time` datetime DEFAULT NULL,
                  `seat_type` varchar(20) DEFAULT NULL,
                  `seat_number` varchar(20) DEFAULT NULL,
                  `passenger_name` varchar(50) DEFAULT NULL,
                  `passenger_id` varchar(50) DEFAULT NULL,
                  `ticket_price` decimal(10,2) DEFAULT NULL,
                  `purchase_time` datetime DEFAULT NULL,
                  `ticket_status` varchar(20) DEFAULT NULL,
                  `carriage_number` varchar(10) DEFAULT NULL,
                  `notes` text,
                  PRIMARY KEY (`id`),
                  KEY `idx_train_number` (`train_number`),
                  KEY `idx_departure_station` (`departure_station`),
                  KEY `idx_arrival_station` (`arrival_station`),
                  KEY `idx_passenger_name` (`passenger_name`),
                  KEY `idx_passenger_id` (`passenger_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
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
                                       ticket_modification_type = @TicketModificationType
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
                Console.WriteLine($"删除车票时出错: {ex.Message}");
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
                                // 如果站名不以"站"结尾，添加"站"字
                                if (!stationName.EndsWith("站"))
                                {
                                    stationName += "站";
                                }
                                stations.Add(stationName);
                            }
                        }
                    }
                }
                
                return stations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取出发站列表时出错: {ex.Message}");
                return new List<string>();
            }
        }
    }
} 