using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Linq;

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
        /// 检测Python是否已安装
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
        /// 检测指定的Python包是否已安装
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
        /// 检测OCR模型是否已安装
        /// </summary>
        public async Task<bool> CheckOcrModelInstalled()
        {
            try
            {
                // 检测多个可能的模型路径

                // 1. 检测用户主目录下的.cnocr目录
                string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string modelDir1 = Path.Combine(userDir, ".cnocr");

                // 2. 检测AppData/Roaming下的cnocr目录
                string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string modelDir2 = Path.Combine(appDataDir, "cnocr");
                string versionModelDir = Path.Combine(modelDir2, "2.3"); // 特定版本目录

                // 检测所有可能的模型目录
                foreach (string dir in new[] { modelDir1, modelDir2, versionModelDir })
                {
                    if (Directory.Exists(dir))
                    {
                        // 检测是否包含模型文件
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
import sys
import os
import time

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

# 检查是否需要下载模型
def is_model_downloading():
    try:
        user_dir = os.path.expanduser('~')
        cnocr_dir = os.path.join(user_dir, '.cnocr')
        app_data_dir = os.path.join(os.getenv('APPDATA') if os.name == 'nt' else os.path.join(user_dir, '.local', 'share'), 'cnocr')
        
        # 检查模型目录是否存在
        model_dirs = [cnocr_dir, app_data_dir]
        for model_dir in model_dirs:
            if os.path.exists(model_dir):
                model_files = []
                # 递归查找所有.onnx文件
                for root, dirs, files in os.walk(model_dir):
                    model_files.extend([f for f in files if f.endswith('.onnx')])
                
                if not model_files:
                    return True
        
        return False
    except Exception as e:
        sys.stderr.write(f'Error checking model download: {{e}}\n')
        return False

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
    
    # 检查是否需要下载模型
    need_download = is_model_downloading()
    
    # 进度分配:
    # 如果需要下载模型：模型下载占0-70%，OCR占70-100%
    # 如果不需要下载模型：OCR占整个0-100%
    
    # 初始化进度
    sys.stdout.write('PROGRESS:0\n')
    sys.stdout.flush()
    sys.stdout.write('STATUS:准备OCR识别引擎...\n')
    sys.stdout.flush()
    
    if need_download:
        sys.stdout.write('DOWNLOAD:START\n')
        sys.stdout.flush()
        sys.stdout.write('STATUS:首次使用需要下载OCR模型...\n')
        sys.stdout.flush()
    
    # 创建CnOcr实例
    # 这一步如果需要下载模型，CnOcr会自动下载
    # 模拟下载进度（如果需要）
    if need_download:
        for i in range(0, 70, 5):
            sys.stdout.write(f'PROGRESS:{{i}}\n')
            sys.stdout.flush()
            time.sleep(0.3)  # 适当延迟，让用户能看到进度
    else:
        # 如果不需要下载，让初始化占20%的进度
        sys.stdout.write('PROGRESS:20\n')
        sys.stdout.flush()
    
    sys.stdout.write('STATUS:加载OCR引擎...\n')
    sys.stdout.flush()
    ocr = CnOcr()
    
    # 下载完成或不需要下载时，进入OCR识别阶段
    progress_start = 70 if need_download else 20
    sys.stdout.write(f'PROGRESS:{{progress_start}}\n')
    sys.stdout.flush()
    sys.stdout.write('STATUS:正在执行OCR文本识别...\n')
    sys.stdout.flush()
    
    # 执行OCR识别
    out = ocr.ocr(img_fp)
    
    # OCR完成，处理结果
    progress_end = 90
    sys.stdout.write(f'PROGRESS:{{progress_end}}\n')
    sys.stdout.flush()
    sys.stdout.write('STATUS:正在处理识别结果...\n')
    sys.stdout.flush()
    
    # 处理OCR结果
    processed_out = process_ocr_result(out)
    
    # 完成
    sys.stdout.write('PROGRESS:100\n')
    sys.stdout.flush()
    sys.stdout.write('STATUS:OCR识别完成\n')
    sys.stdout.flush()
    
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

                    // 添加进度处理
                    StringBuilder outputBuilder = new StringBuilder();
                    StringBuilder errorBuilder = new StringBuilder();
                    bool isDownloading = false;
                    string statusMessage = "正在准备OCR引擎...";

                    process.OutputDataReceived += (sender, e) => 
                    {
                        if (e.Data != null)
                        {
                            if (e.Data.StartsWith("PROGRESS:"))
                            {
                                // 处理进度信息
                                string progressStr = e.Data.Substring(9);
                                if (int.TryParse(progressStr, out int progress))
                                {
                                    // 使用Application.Current.Dispatcher确保在UI线程上更新进度
                                    Application.Current.Dispatcher.Invoke(() => {
                                        // 获取当前OcrTicketViewModel实例
                                        var windows = Application.Current.Windows.OfType<Window>();
                                        var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                                        if (ocrWindow != null && ocrWindow.DataContext is ViewModels.OcrTicketViewModel viewModel)
                                        {
                                            // 更新进度属性
                                            viewModel.Progress = progress;
                                            // 更新状态消息
                                            if (!string.IsNullOrEmpty(statusMessage))
                                            {
                                                viewModel.LoadingMessage = statusMessage;
                                            }
                                        }
                                    });
                                }
                            }
                            else if (e.Data.StartsWith("STATUS:"))
                            {
                                // 处理状态信息
                                statusMessage = e.Data.Substring(7);
                                // 使用Application.Current.Dispatcher确保在UI线程上更新状态
                                Application.Current.Dispatcher.Invoke(() => {
                                    var windows = Application.Current.Windows.OfType<Window>();
                                    var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                                    if (ocrWindow != null && ocrWindow.DataContext is ViewModels.OcrTicketViewModel viewModel)
                                    {
                                        viewModel.LoadingMessage = statusMessage;
                                    }
                                });
                            }
                            else if (e.Data.StartsWith("DOWNLOAD:START"))
                            {
                                isDownloading = true;
                                statusMessage = "首次使用需要下载OCR模型，请耐心等待...";
                                
                                // 使用Application.Current.Dispatcher确保在UI线程上更新状态
                                Application.Current.Dispatcher.Invoke(() => {
                                    var windows = Application.Current.Windows.OfType<Window>();
                                    var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                                    if (ocrWindow != null && ocrWindow.DataContext is ViewModels.OcrTicketViewModel viewModel)
                                    {
                                        viewModel.Progress = 0;
                                        viewModel.LoadingMessage = statusMessage;
                                        viewModel.IsDownloadingModel = true;
                                    }
                                });
                            }
                            else
                            {
                                // 收集普通输出
                                outputBuilder.AppendLine(e.Data);
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) => 
                    {
                        if (e.Data != null)
                        {
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    // 重置下载状态
                    if (isDownloading)
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            var windows = Application.Current.Windows.OfType<Window>();
                            var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                            if (ocrWindow != null && ocrWindow.DataContext is ViewModels.OcrTicketViewModel viewModel)
                            {
                                viewModel.IsDownloadingModel = false;
                            }
                        });
                    }

                    // 删除临时脚本
                    try { File.Delete(tempScriptPath); } catch { }

                    if (process.ExitCode != 0)
                    {
                        return JsonConvert.SerializeObject(new { error = errorBuilder.ToString() });
                    }

                    return outputBuilder.ToString().Trim();
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

        /// <summary>
        /// 检测CNOCR是否已安装
        /// </summary>
        public async Task<bool> CheckCnocrInstalled()
        {
            return await CheckPackageInstalled("cnocr");
        }

        /// <summary>
        /// 检测任意Python包是否已安装（通用方法）
        /// </summary>
        /// <param name="packageName">包名</param>
        public async Task<bool> CheckPythonPackageInstalled(string packageName)
        {
            return await CheckPackageInstalled(packageName);
        }
    }
}