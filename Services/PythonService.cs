using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace TA_WPF.Services
{
    /// <summary>
    /// Python服务，负责与Python环境交互
    /// </summary>
    public class PythonService
    {
        private readonly string _pythonExePath;
        private readonly string _pythonScriptPath;
        private readonly ScriptEngine _engine;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public PythonService()
        {
            _pythonExePath = "python";
            _pythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "ocr.py");
            
            // 初始化IronPython引擎
            _engine = Python.CreateEngine();
        }
        
        /// <summary>
        /// 检查Python是否已安装
        /// </summary>
        public async Task<bool> CheckPythonInstalled()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _pythonExePath;
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    await process.WaitForExitAsync();
                    
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 检查指定的Python包是否已安装
        /// </summary>
        /// <param name="packageName">包名</param>
        public async Task<bool> CheckPackageInstalled(string packageName)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _pythonExePath;
                    process.StartInfo.Arguments = $"-c \"import {packageName}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    await process.WaitForExitAsync();
                    
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 检查OCR模型是否已安装
        /// </summary>
        public async Task<bool> CheckOcrModelInstalled()
        {
            try
            {
                // 检查多个可能的模型路径
                
                // 1. 检查用户主目录下的.cnocr目录
                string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string modelDir1 = Path.Combine(userDir, ".cnocr");
                
                // 2. 检查AppData/Roaming下的cnocr目录
                string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string modelDir2 = Path.Combine(appDataDir, "cnocr");
                string versionModelDir = Path.Combine(modelDir2, "2.3"); // 特定版本目录
                
                // 检查所有可能的模型目录
                foreach (string dir in new[] { modelDir1, modelDir2, versionModelDir })
                {
                    if (Directory.Exists(dir))
                    {
                        // 检查是否包含模型文件
                        string[] modelFiles = Directory.GetFiles(dir, "*.onnx", SearchOption.AllDirectories);
                        if (modelFiles.Length > 0)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 通过外部进程运行OCR识别
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        public async Task<string> RunOcrWithExternalProcess(string imagePath)
        {
            try
            {
                // 创建一个临时Python脚本
                string tempScriptPath = Path.GetTempFileName() + ".py";
                string tempScriptContent = $@"
from cnocr import CnOcr
import json
import numpy as np

# 将numpy数组转换为可JSON序列化的Python列表
class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        if isinstance(obj, np.integer):
            return int(obj)
        if isinstance(obj, np.floating):
            return float(obj)
        return json.JSONEncoder.default(self, obj)

def process_ocr_result(result):
    # 处理OCR结果，转换为易于理解的格式
    processed_results = []
    
    for item in result:
        # CnOCR返回的每个项目通常包含text和position
        processed_item = {{}}
        
        # 处理文本，确保是字符串
        if isinstance(item, dict) and 'text' in item:
            processed_item['text'] = item['text']
        elif hasattr(item, 'text'):
            processed_item['text'] = item.text
        elif isinstance(item, (list, tuple)) and len(item) > 0 and isinstance(item[0], str):
            processed_item['text'] = item[0]
        else:
            processed_item['text'] = str(item)
        
        # 处理位置信息
        if isinstance(item, dict) and 'position' in item:
            processed_item['position'] = item['position']
        elif hasattr(item, 'position'):
            processed_item['position'] = item.position
        
        # 处理置信度
        if isinstance(item, dict) and 'score' in item:
            processed_item['score'] = item['score']
        elif hasattr(item, 'score'):
            processed_item['score'] = item.score
        else:
            processed_item['score'] = 1.0  # 默认置信度
        
        processed_results.append(processed_item)
    
    return processed_results

try:
    img_fp = '{imagePath.Replace("\\", "\\\\")}'
    ocr = CnOcr()
    out = ocr.ocr(img_fp)
    
    # 处理OCR结果
    processed_out = process_ocr_result(out)
    
    # 转换为JSON
    print(json.dumps(processed_out, cls=NumpyEncoder))
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";
                File.WriteAllText(tempScriptPath, tempScriptContent);

                using (var process = new Process())
                {
                    process.StartInfo.FileName = _pythonExePath;
                    process.StartInfo.Arguments = tempScriptPath;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    // 删除临时脚本
                    try { File.Delete(tempScriptPath); } catch { }
                    
                    if (process.ExitCode != 0)
                    {
                        return JsonConvert.SerializeObject(new { error = error });
                    }
                    
                    return output;
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }
        
        /// <summary>
        /// 使用IronPython引擎运行简单Python代码
        /// </summary>
        /// <param name="code">Python代码</param>
        /// <returns>执行结果</returns>
        public string RunSimplePythonCode(string code)
        {
            try
            {
                var scope = _engine.CreateScope();
                var source = _engine.CreateScriptSourceFromString(code);
                var output = new StringWriter();
                
                // 使用内存流作为中间转换
                var memoryStream = new MemoryStream();
                var streamWriter = new StreamWriter(memoryStream);
                streamWriter.AutoFlush = true;
                _engine.Runtime.IO.SetOutput(memoryStream, System.Text.Encoding.UTF8);
                
                source.Execute(scope);
                
                // 读取内存流中的结果
                memoryStream.Position = 0;
                var reader = new StreamReader(memoryStream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }
    }
} 