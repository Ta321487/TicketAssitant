using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using TA_WPF.Utils;
using System.Text;
using System.Linq;

namespace TA_WPF.Services
{
    /// <summary>
    /// PDF服务，负责处理PDF文件的读取和解析
    /// </summary>
    public class PdfService
    {
        /// <summary>
        /// 异步读取PDF文件内容
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>PDF文件内容字符串</returns>
        public async Task<string> ReadPdfContentAsync(string filePath)
        {
            return await Task.Run(() => ReadPdfContent(filePath));
        }

        /// <summary>
        /// 读取PDF文件内容
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>PDF文件内容字符串</returns>
        public string ReadPdfContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"找不到文件: {filePath}");
                }

                var content = new System.Text.StringBuilder();

                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    // 获取PDF文档页数
                    int pageCount = document.NumberOfPages;
                    
                    for (int i = 1; i <= pageCount; i++)
                    {
                        // 获取指定页的内容
                        Page page = document.GetPage(i);
                        
                        content.AppendLine($"--- 第 {i} 页 ---");
                        
                        // 改为按行提取文本，以保留原始格式
                        var words = page.GetWords();
                        
                        if (words != null && words.Any())
                        {
                            // 将IEnumerable<Word>转换为List<Word>以便使用索引
                            var wordsList = words.ToList();
                            float currentY = (float)wordsList[0].BoundingBox.Bottom;
                            float lineSpacingThreshold = 5.0f; // 行间距阈值，可以根据实际PDF调整
                            StringBuilder lineText = new StringBuilder();
                            
                            foreach (var word in wordsList)
                            {
                                // 如果Y坐标差异大于阈值，认为是新的一行
                                if (Math.Abs((float)word.BoundingBox.Bottom - currentY) > lineSpacingThreshold)
                                {
                                    // 完成当前行，添加换行
                                    content.AppendLine(lineText.ToString().Trim());
                                    lineText.Clear();
                                    currentY = (float)word.BoundingBox.Bottom;
                                }
                                
                                // 添加词与前一个词之间的空格
                                if (lineText.Length > 0)
                                {
                                    lineText.Append(" ");
                                }
                                
                                lineText.Append(word.Text);
                            }
                            
                            // 添加最后一行
                            if (lineText.Length > 0)
                            {
                                content.AppendLine(lineText.ToString().Trim());
                            }
                        }
                        else
                        {
                            // 如果无法获取单词列表，则使用原始方法
                            content.AppendLine(page.Text);
                        }
                        
                        content.AppendLine();
                    }
                }

                return content.ToString();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"读取PDF文件时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从PDF文件中提取特定关键信息 (可根据需要扩展)
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>提取的关键信息字典</returns>
        public async Task<Dictionary<string, string>> ExtractPdfInfoAsync(string filePath)
        {
            return await Task.Run(() => ExtractPdfInfo(filePath));
        }

        /// <summary>
        /// 从PDF文件中提取特定关键信息
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>提取的关键信息字典</returns>
        public Dictionary<string, string> ExtractPdfInfo(string filePath)
        {
            try
            {
                var info = new Dictionary<string, string>();
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"找不到文件: {filePath}");
                }

                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    // 获取PDF的元数据信息
                    if (document.Information.Title != null)
                        info["标题"] = document.Information.Title;
                    
                    if (document.Information.Author != null)
                        info["作者"] = document.Information.Author;
                    
                    if (document.Information.Creator != null)
                        info["创建者"] = document.Information.Creator;
                    
                    // 读取文档页面内容
                    if (document.NumberOfPages > 0)
                    {
                        Page page = document.GetPage(1);
                        info["内容"] = page.Text;
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"提取PDF信息时出错: {ex.Message}");
                throw;
            }
        }
    }
} 