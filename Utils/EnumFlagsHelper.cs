namespace TA_WPF.Utils
{
    public static class EnumFlagsHelper
    {
        /// <summary>
        /// 获取枚举标志位对应的描述列表
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="flags">标志位整数值</param>
        /// <returns>描述列表</returns>
        public static List<string> GetFlagDescriptions<T>(int flags) where T : Enum
        {
            var descriptions = new List<string>();
            
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                int flagValue = Convert.ToInt32(value);
                
                // 跳过None(0)值
                if (flagValue == 0)
                    continue;
                
                // 检查该位是否设置
                if ((flags & flagValue) == flagValue)
                {
                    descriptions.Add(value.ToString());
                }
            }
            
            return descriptions;
        }
        
        /// <summary>
        /// 获取描述字符串（以逗号分隔）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="flags">标志位整数值</param>
        /// <returns>描述字符串</returns>
        public static string GetFlagDescriptionString<T>(int flags) where T : Enum
        {
            var descriptions = GetFlagDescriptions<T>(flags);
            return string.Join("、", descriptions);
        }
        
        /// <summary>
        /// 从描述列表获取标志位整数值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="selectedDescriptions">选中的描述列表</param>
        /// <returns>标志位整数值</returns>
        public static int GetFlagsFromDescriptions<T>(IEnumerable<string> selectedDescriptions) where T : Enum
        {
            int flags = 0;
            
            foreach (string description in selectedDescriptions)
            {
                if (Enum.TryParse(typeof(T), description, out object? value) && value != null)
                {
                    flags |= Convert.ToInt32(value);
                }
            }
            
            return flags;
        }
        
        /// <summary>
        /// 获取枚举所有可用的描述列表
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>所有描述列表</returns>
        public static List<string> GetAllDescriptions<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Where(v => Convert.ToInt32(v) != 0) // 排除None
                .Select(v => v.ToString())
                .ToList();
        }
    }
} 