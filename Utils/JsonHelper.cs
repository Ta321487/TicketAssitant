using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TA_WPF.Models;

namespace TA_WPF.Utils
{
    /// <summary>
    /// JSON辅助类，用于处理JSON数据
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 尝试将JSON字符串解析为OCR结果列表
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>OCR结果列表</returns>
        public static List<OcrResult> TryParseOcrResults(string json)
        {
            try
            {
                // 首先尝试直接解析
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => args.ErrorContext.Handled = true,
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                
                var results = JsonConvert.DeserializeObject<List<OcrResult>>(json, settings);
                if (results != null && results.Count > 0)
                {
                    return results;
                }
                
                // 如果直接解析失败，尝试分析JSON结构并手动创建OcrResult对象
                var jToken = JToken.Parse(json);
                
                if (jToken is JArray jArray)
                {
                    results = new List<OcrResult>();
                    
                    foreach (var item in jArray)
                    {
                        if (item is JObject jObject)
                        {
                            var result = new OcrResult();
                            
                            // 处理text字段
                            if (jObject.TryGetValue("text", out var textToken) && textToken.Type == JTokenType.String)
                            {
                                result.Text = textToken.Value<string>();
                            }
                            // cnocr有时可能将text放在单独的结构中
                            else if (jObject.TryGetValue("text", out var textObj) && textObj is JObject textJObj)
                            {
                                result.Text = textJObj.ToString(Formatting.None);
                            }
                            
                            // 处理score字段
                            if (jObject.TryGetValue("score", out var scoreToken) && 
                                (scoreToken.Type == JTokenType.Float || scoreToken.Type == JTokenType.Integer))
                            {
                                result.Score = scoreToken.Value<double>();
                            }
                            
                            // 处理position字段
                            if (jObject.TryGetValue("position", out var posToken) && posToken is JArray posArray)
                            {
                                result.Position = new List<List<double>>();
                                foreach (var posItem in posArray)
                                {
                                    if (posItem is JArray coords)
                                    {
                                        var coordList = new List<double>();
                                        foreach (var coord in coords)
                                        {
                                            coordList.Add(coord.Value<double>());
                                        }
                                        result.Position.Add(coordList);
                                    }
                                }
                            }
                            
                            // 如果有文本则添加结果
                            if (!string.IsNullOrEmpty(result.Text))
                            {
                                results.Add(result);
                            }
                        }
                    }
                    
                    return results;
                }
            }
            catch
            {
                // 解析失败，返回空列表
            }
            
            return new List<OcrResult>();
        }
    }
} 