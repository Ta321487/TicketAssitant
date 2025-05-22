using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// Python服务，负责与Python环境交互
    /// </summary>
    public class PythonService
    {
        private string _pythonExePath;
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
            // 尝试检测的Python命令列表
            string[] pythonCommands = new string[] { "python", "python3", "py" };
            
            // 在Windows系统上，还可能需要检查特定路径
            List<string> possiblePaths = new List<string>();
            try 
            {
                // 获取PATH环境变量中的路径，寻找Python安装
                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                string[] paths = pathEnv.Split(Path.PathSeparator);
                
                // 收集系统中可能的Python安装路径
                foreach (string path in paths)
                {
                    if (path.ToLower().Contains("python"))
                    {
                        // 检查可能的Python可执行文件
                        string pythonExe = Path.Combine(path, "python.exe");
                        string python3Exe = Path.Combine(path, "python3.exe");
                        string pyExe = Path.Combine(path, "py.exe");
                        
                        if (File.Exists(pythonExe)) possiblePaths.Add(pythonExe);
                        if (File.Exists(python3Exe)) possiblePaths.Add(python3Exe);
                        if (File.Exists(pyExe)) possiblePaths.Add(pyExe);
                    }
                }
                
                // 检查常见的Python安装目录
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                
                // 添加可能的Python安装路径
                string[] commonPaths = new[]
                {
                    Path.Combine(programFiles, "Python*"),
                    Path.Combine(programFilesX86, "Python*"),
                    @"C:\Python*"
                };
                
                foreach (string pattern in commonPaths)
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(pattern) ?? string.Empty;
                        string searchPattern = Path.GetFileName(pattern);
                        
                        if (Directory.Exists(directory))
                        {
                            foreach (string dir in Directory.GetDirectories(directory, searchPattern))
                            {
                                string pythonExe = Path.Combine(dir, "python.exe");
                                if (File.Exists(pythonExe))
                                    possiblePaths.Add(pythonExe);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"搜索Python安装路径时出错: {ex.Message}");
                        LogHelper.LogSystemError("Python环境", $"搜索Python安装路径时出错", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取Python路径时出错: {ex.Message}");
            }
            
            // 添加找到的路径到检测列表
            pythonCommands = pythonCommands.Concat(possiblePaths).ToArray();
            
            // 记录诊断信息
            Debug.WriteLine($"开始检测Python安装状态...");
            Debug.WriteLine($"将尝试以下命令/路径: {string.Join(", ", pythonCommands)}");
            
            // 依次尝试所有可能的Python命令
            foreach (string command in pythonCommands)
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = command;
                        process.StartInfo.Arguments = "--version";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.CreateNoWindow = true;

                        StringBuilder output = new StringBuilder();
                        StringBuilder error = new StringBuilder();
                        
                        process.OutputDataReceived += (sender, e) => {
                            if (!string.IsNullOrEmpty(e.Data))
                                output.AppendLine(e.Data);
                        };
                        
                        process.ErrorDataReceived += (sender, e) => {
                            if (!string.IsNullOrEmpty(e.Data))
                                error.AppendLine(e.Data);
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        await process.WaitForExitAsync();
                        
                        // 记录诊断信息
                        Debug.WriteLine($"命令 '{command}' 返回: 退出代码={process.ExitCode}, 输出={output}, 错误={error}");

                        if (process.ExitCode == 0)
                        {
                            // 找到有效的Python安装
                            string versionInfo = output.ToString().Trim();
                            if (string.IsNullOrEmpty(versionInfo))
                                versionInfo = error.ToString().Trim();
                            
                            Debug.WriteLine($"已检测到Python安装: {versionInfo}");
                            LogHelper.LogSystem("Python", $"已检测到Python安装: {versionInfo}");
                            
                            // 更新当前Python路径
                            _pythonExePath = command;
                            
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"检测Python时出错 ({command}): {ex.Message}");
                    // 继续尝试下一个命令
                }
            }
            
            Debug.WriteLine("未检测到Python安装");
            return false;
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
                
                // 3. 检测检测模型目录 (cnstd)
                string cnstdDir = Path.Combine(appDataDir, "cnstd");
                string cnstdVersionDir = Path.Combine(cnstdDir, "1.2"); // 特定版本目录
                
                // 4. 检测其他可能的目录
                string versionModelDir1 = Path.Combine(modelDir2, "1"); // 另一个可能的版本目录
                string pythonSitePackages = GetPythonSitePackagesDir(); // Python site-packages目录

                // 记录搜索的目录信息
                Debug.WriteLine($"检查OCR模型安装 - 搜索目录: {modelDir1}, {modelDir2}, {versionModelDir}, {versionModelDir1}, {cnstdDir}, {cnstdVersionDir}");

                // 检测所有可能的模型目录
                foreach (string dir in new[] { modelDir1, modelDir2, versionModelDir, versionModelDir1, cnstdDir, cnstdVersionDir, pythonSitePackages })
                {
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        try
                        {
                            // 检测是否包含模型文件
                            string[] modelFiles = Directory.GetFiles(dir, "*.onnx", SearchOption.AllDirectories);
                            if (modelFiles.Length > 0)
                            {
                                // 记录找到的模型文件
                                foreach (var file in modelFiles)
                                {
                                    Debug.WriteLine($"找到OCR模型文件: {file}");
                                }
                                return true;
                            }
                            
                            // 尝试搜索特定的模型子目录
                            string[] potentialModelDirs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                            foreach (var potentialDir in potentialModelDirs)
                            {
                                try
                                {
                                    string[] subModelFiles = Directory.GetFiles(potentialDir, "*.onnx", SearchOption.AllDirectories);
                                    if (subModelFiles.Length > 0)
                                    {
                                        // 记录找到的模型文件
                                        foreach (var file in subModelFiles)
                                        {
                                            Debug.WriteLine($"在子目录中找到OCR模型文件: {file}");
                                            LogHelper.LogSystem("Python环境", $"在子目录中找到OCR模型文件: {file}");
                                        }
                                        return true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"搜索子目录时出错: {ex.Message}");
                                    LogHelper.LogSystemError("Python", $"搜索子目录时出错: {ex.Message}");
                                    // 继续搜索其他目录
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"搜索模型文件时出错: {ex.Message}");
                            LogHelper.LogSystemError("模型",$"搜索模型文件时出错: {ex.Message}", ex);

                            // 继续搜索其他目录
                        }
                    }
                }

                Debug.WriteLine("未找到任何OCR模型文件");
                LogHelper.LogSystemWarning("模型", "未找到任何OCR模型文件");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查OCR模型安装状态时出错: {ex.Message}");
                LogHelper.LogSystemError("模型", $"检查OCR模型安装状态时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取Python的site-packages目录
        /// </summary>
        /// <returns>site-packages目录路径</returns>
        private string GetPythonSitePackagesDir()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _pythonExePath;
                    process.StartInfo.Arguments = "-c \"import site; print(site.getsitepackages()[0])\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output) && Directory.Exists(output))
                    {
                        string cnocrPath = Path.Combine(output, "cnocr");
                        if (Directory.Exists(cnocrPath))
                        {
                            Debug.WriteLine($"找到cnocr包路径: {cnocrPath}");
                            LogHelper.LogSystem("模型", $"找到cnocr包路径: {cnocrPath}");
                            return cnocrPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取Python site-packages目录时出错: {ex.Message}");
                LogHelper.LogSystemError("Python环境", $"获取Python site-packages目录时出错", ex);
            }
            
            return null;
        }

        /// <summary>
        /// 通过外部进程运行OCR识别
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        public async Task<string> RunOcrWithExternalProcess(string imagePath)
        {
            try
            {
                // 首先检查模型是否已安装，如果没有则尝试从Assets复制
                bool modelInstalled = await CheckOcrModelInstalled();
                if (!modelInstalled)
                {
                    Debug.WriteLine("OCR模型不存在，尝试从内置资源复制模型文件");
                    LogHelper.LogSystemWarning("模型", "OCR模型不存在，尝试从内置资源复制模型文件");
                    bool copySuccess = await CopyModelFilesFromAssets();
                    Debug.WriteLine($"从内置资源复制模型文件{(copySuccess ? "成功" : "失败")}");

                }

                // 创建一个临时Python脚本
                string tempScriptPath = Path.GetTempFileName() + ".py";
                string tempScriptContent = $@"
# -*- coding: utf-8 -*-
from cnocr import CnOcr
import json
import numpy as np
import sys
import os
import time
import io

# 设置标准输出为UTF-8编码
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

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
        cnstd_dir = os.path.join(os.getenv('APPDATA') if os.name == 'nt' else os.path.join(user_dir, '.local', 'share'), 'cnstd')
        
        # 检查关键模型文件路径
        cnocr_model_file = os.path.join(app_data_dir, '2.3', 'densenet_lite_136-gru', 'cnocr-v2.3-densenet_lite_136-gru-epoch=004-ft-model.onnx')
        cnstd_model_file = os.path.join(cnstd_dir, '1.2', 'ppocr', 'ch_PP-OCRv4_det', 'ch_PP-OCRv4_det_infer.onnx')
        
        # 如果任一关键模型文件存在，则不需要下载
        if os.path.exists(cnocr_model_file) or os.path.exists(cnstd_model_file):
            return False
        
        # 检查模型目录是否存在
        model_dirs = [cnocr_dir, app_data_dir, cnstd_dir]
        for model_dir in model_dirs:
            if os.path.exists(model_dir):
                model_files = []
                # 递归查找所有.onnx文件
                for root, dirs, files in os.walk(model_dir):
                    model_files.extend([os.path.join(root, f) for f in files if f.endswith('.onnx')])
                
                if model_files:
                    return False
        
        return True
    except Exception as e:
        sys.stderr.write(f'Error checking model download: {{e}}\n')
        return False

# 查找可用的模型文件路径
def find_model_file_path():
    try:
        # 检查可能的模型目录
        user_dir = os.path.expanduser('~')
        app_data_dir = os.path.join(os.getenv('APPDATA') if os.name == 'nt' else os.path.join(user_dir, '.local', 'share'))
        
        # 可能的目录列表
        model_dirs = [
            os.path.join(user_dir, '.cnocr'),
            os.path.join(app_data_dir, 'cnocr'),
            os.path.join(app_data_dir, 'cnocr', '2.3'),
            os.path.join(app_data_dir, 'cnocr', '1')
        ]
        
        # 遍历所有可能的目录，查找模型文件
        for model_dir in model_dirs:
            if os.path.exists(model_dir):
                for root, dirs, files in os.walk(model_dir):
                    # 寻找.onnx文件
                    onnx_files = [f for f in files if f.endswith('.onnx')]
                    # 优先使用包含densenet_lite_136-gru的文件
                    for f in onnx_files:
                        if 'densenet_lite_136-gru' in f:
                            model_path = os.path.join(root, f)
                            model_name = 'densenet_lite_136-gru'
                            sys.stdout.write(f'INFO:找到模型文件: {{model_path}}\n')
                            sys.stdout.flush()
                            return model_name, model_path
                    
                    # 如果没有找到指定模型，使用任何.onnx文件
                    if onnx_files:
                        model_path = os.path.join(root, onnx_files[0])
                        sys.stdout.write(f'INFO:找到其他模型文件: {{model_path}}\n')
                        sys.stdout.flush()
                        # 尝试从文件名推断模型名称
                        model_name = None
                        for name in ['densenet_lite_136-gru', 'ch_PP-OCRv3', 'ch_PP-OCRv4']:
                            if name in onnx_files[0]:
                                model_name = name
                                break
                        
                        return model_name, model_path
        
        # 没有找到任何模型文件
        sys.stderr.write('ERROR:未找到OCR识别模型文件，请检查网络连接或联系技术支持\n')
        sys.stderr.flush()
        return None, None
    except Exception as e:
        sys.stderr.write(f'Error finding model path: {{e}}\n')
        sys.stdout.flush()
        return None, None

# 确保检测模型已初始化
def ensure_detection_model():
    try:
        # 引入cnstd模块 (懒加载，仅在需要时导入)
        import importlib
        cnstd_spec = importlib.util.find_spec('cnstd')
        
        if cnstd_spec is not None:
            from cnstd import CnStd
            
            # 检查是否有已安装的模型文件
            app_data_dir = os.path.join(os.getenv('APPDATA') if os.name == 'nt' else os.path.join(os.path.expanduser('~'), '.local', 'share'))
            cnstd_dir = os.path.join(app_data_dir, 'cnstd')
            
            # 尝试初始化检测器，这会触发自动下载检测模型
            sys.stdout.write('STATUS:检查文本检测模型...\n')
            sys.stdout.flush()
            
            try:
                # 首先确保目录存在
                os.makedirs(cnstd_dir, exist_ok=True)
                
                # 检查是否已有检测模型，如果没有则手动触发下载
                model_dir = os.path.join(cnstd_dir, '1.2')
                model_file = os.path.join(model_dir, 'ppocr', 'ch_PP-OCRv4_det', 'ch_PP-OCRv4_det_infer.onnx')
                
                if not os.path.exists(model_file):
                    sys.stdout.write('STATUS:正在下载文本检测模型...\n')
                    sys.stdout.flush()
                    
                    # 使用PP-OCRv4检测模型，强制重新下载
                    sys.stdout.write('INFO:准备下载检测模型: ch_PP-OCRv4_det\n')
                    sys.stdout.flush()
                    
                    # 这里先导入所需的包
                    from cnstd.utils import get_model_file
                    
                    try:
                        # 手动触发模型下载 - 移除不支持的参数
                        get_model_file('ch_PP-OCRv4_det', 'cnstd')
                        
                        sys.stdout.write('INFO:检测模型下载完成\n')
                        sys.stdout.flush()
                    except Exception as model_ex:
                        # 检测模型下载失败，可能是网络问题
                        sys.stderr.write(f'下载检测模型失败: {{model_ex}}\n')
                        sys.stderr.write('ERROR:文本检测模型下载失败，可能是网络问题，应用程序将尝试使用内置模型\n')
                        sys.stderr.flush()
                        # 即使下载失败，也继续执行，应用程序将尝试使用内置模型
                
                # 检查确认模型是否存在
                if not os.path.exists(model_file):
                    sys.stderr.write(f'ERROR:检测模型文件不存在: {{model_file}}\n')
                    sys.stderr.flush()
                
                # 使用检测模型初始化CnStd
                detector = CnStd(model_name='ch_PP-OCRv4_det', rotated_bbox=True, det_model_backend='onnx')
                
                # 创建一个小的测试图像，确保模型加载完成
                import numpy as np
                test_img = np.ones((32, 100, 3), dtype=np.uint8) * 255
                # 执行一次检测，这会触发模型加载
                detector.detect(test_img)
                
                sys.stdout.write('INFO:文本检测模型已准备就绪\n')
                sys.stdout.flush()
                return True
            except Exception as exc:
                sys.stderr.write(f'初始化文本检测模型失败: {{exc}}\n')
                sys.stderr.flush()
                
                # 尝试更多调试信息
                import traceback as tb
                sys.stderr.write(f'详细错误: {{tb.format_exc()}}\n')
                sys.stderr.flush()
                
                # 即使失败也继续OCR流程
                return False
        else:
            sys.stdout.write('INFO:未安装cnstd模块，仅使用单文本识别\n')
            sys.stdout.flush()
            return False
    except Exception as exc:
        sys.stderr.write(f'检查文本检测模型时出错: {{exc}}\n')
        sys.stderr.flush()
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
        
        # 处理置信度，确保不会有无效值
        if isinstance(item, dict) and 'score' in item:
            score = item['score']
            # 确保分数是有效的数字
            if isinstance(score, (int, float)) and not (float('-inf') < score < float('inf')):
                processed_item['score'] = 0.95  # 使用默认值替代无效值
            else:
                processed_item['score'] = score
        elif hasattr(item, 'score'):
            score = item.score
            # 确保分数是有效的数字
            if isinstance(score, (int, float)) and not (float('-inf') < score < float('inf')):
                processed_item['score'] = 0.95  # 使用默认值替代无效值
            else:
                processed_item['score'] = score
        else:
            processed_item['score'] = 0.95  # 默认置信度
        
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
    
    # 确保检测模型初始化 (会自动下载检测模型) - 移动到这里，确保在OCR过程一开始就主动下载
    sys.stdout.write('STATUS:检查文本检测模型...\n')
    sys.stdout.flush()
    
    # 如果需要下载，为检测模型分配进度
    if need_download:
        for i in range(0, 40, 5):
            sys.stdout.write(f'PROGRESS:{{i}}\n')
            sys.stdout.flush()
            time.sleep(0.3)  # 适当延迟
    
    # 确保检测模型已准备就绪 - 主动调用来触发模型下载
    detection_ready = ensure_detection_model()
    
    # 查找模型文件位置
    model_name, model_path = find_model_file_path()
    
    # 创建CnOcr实例
    # 这一步如果需要下载模型，CnOcr会自动下载
    # 模拟下载进度（如果需要）
    if need_download:
        for i in range(40, 70, 5):
            sys.stdout.write(f'PROGRESS:{{i}}\n')
            sys.stdout.flush()
            time.sleep(0.3)  # 适当延迟，让用户能看到进度
    else:
        # 如果不需要下载，让初始化占20%的进度
        sys.stdout.write('PROGRESS:20\n')
        sys.stdout.flush()
    
    sys.stdout.write('STATUS:加载OCR引擎...\n')
    sys.stdout.flush()
    
    # 如果找到了模型文件，使用指定的模型
    if model_name and model_path:
        sys.stdout.write(f'INFO:使用模型: {{model_name}}, 路径: {{model_path}}\n')
        sys.stdout.flush()
        ocr = CnOcr(rec_model_name=model_name, rec_model_backend='onnx', rec_model_fp=model_path)
    else:
        # 如果没找到，则使用默认设置
        sys.stdout.write('INFO:使用默认模型设置\n')
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
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

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
                            else if (e.Data.StartsWith("INFO:"))
                            {
                                // 记录信息性消息到日志
                                string infoMessage = e.Data.Substring(5);
                                Debug.WriteLine($"OCR信息: {infoMessage}");
                                // 收集输出
                                outputBuilder.AppendLine(e.Data);
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
                            // 过滤掉一些常见的无关紧要的警告或错误，不显示在日志中
                            if (e.Data.Contains("FutureWarning") || 
                                e.Data.Contains("symlinks") || 
                                e.Data.Contains("Ignoring --local-dir-use-symlinks") ||
                                e.Data.Contains("warnings.warn") ||
                                e.Data.Contains("huggingface_hub") ||
                                e.Data.Contains("UserWarning"))
                            {
                                // 不记录这些常见警告
                                return;
                            }
                            
                            // 过滤网络连接相关错误，仅保留核心错误信息
                            if (e.Data.Contains("ConnectTimeoutError") || 
                                e.Data.Contains("Connection to huggingface.co timed out") ||
                                e.Data.Contains("Max retries exceeded") ||
                                e.Data.Contains("10060") ||
                                e.Data.Contains("WinError"))
                            {
                                // 替换为更友好的中文提示
                                errorBuilder.AppendLine("网络连接超时：连接到模型服务器失败，但不影响使用已下载的模型");
                                Debug.WriteLine("OCR错误: 网络连接超时，连接到模型服务器失败");
                                LogHelper.LogSystemError("网络", "网络连接超时：连接到模型服务器失败，但不影响使用已下载的模型");
                                return;
                            }
                            
                            errorBuilder.AppendLine(e.Data);
                            Debug.WriteLine($"OCR错误: {e.Data}");
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

                    // 修改输出处理
                    string output = outputBuilder.ToString().Trim();
                    Debug.WriteLine($"OCR原始输出: {output}");
                    
                    if (process.ExitCode != 0)
                    {
                        string error = errorBuilder.ToString();
                        Debug.WriteLine($"OCR进程退出错误: {error}");
                        return JsonConvert.SerializeObject(new { error = error });
                    }

                    // 尝试解析JSON格式
                    try 
                    {
                        // 清理输出中的非JSON部分
                        string jsonPart = ExtractJsonFromOutput(output);
                        if (!string.IsNullOrEmpty(jsonPart))
                        {
                            Debug.WriteLine($"提取的JSON部分: {jsonPart}");
                            
                            // 使用更安全的设置解析JSON
                            var settings = new JsonSerializerSettings
                            {
                                FloatParseHandling = FloatParseHandling.Double,
                                FloatFormatHandling = FloatFormatHandling.DefaultValue,
                                NullValueHandling = NullValueHandling.Ignore
                            };
                            
                            // 尝试验证JSON是否有效
                            object jsonObj = JsonConvert.DeserializeObject(jsonPart, settings);
                            string result = JsonConvert.SerializeObject(jsonObj, settings);
                            Debug.WriteLine($"最终JSON输出: {result.Substring(0, Math.Min(100, result.Length))}...");
                            return result;
                        }
                        return output;
                    }
                    catch (Exception jsonEx)
                    {
                        Debug.WriteLine($"JSON解析错误: {jsonEx.Message}");
                        // 返回原始输出，让前端尝试处理
                        return output;
                    }
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
        /// 检测CNSTD文本检测库是否已安装
        /// </summary>
        public async Task<bool> CheckCnstdInstalled()
        {
            return await CheckPackageInstalled("cnstd");
        }

        /// <summary>
        /// 检测任意Python包是否已安装（通用方法）
        /// </summary>
        /// <param name="packageName">包名</param>
        public async Task<bool> CheckPythonPackageInstalled(string packageName)
        {
            return await CheckPackageInstalled(packageName);
        }

        /// <summary>
        /// 从Python输出中提取JSON部分
        /// </summary>
        private string ExtractJsonFromOutput(string output)
        {
            try
            {
                // 查找第一个[或{作为JSON开始
                int jsonStart = output.IndexOfAny(new char[] { '[', '{' });
                if (jsonStart >= 0)
                {
                    // 从这里开始提取到结尾作为JSON部分
                    return output.Substring(jsonStart);
                }
                return output;
            }
            catch
            {
                return output;
            }
        }

        /// <summary>
        /// 复制内置模型文件到目标位置
        /// </summary>
        /// <returns>是否成功复制任何模型文件</returns>
        public async Task<bool> CopyModelFilesFromAssets()
        {
            try
            {
                bool copiedAnyFile = false;
                string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string assetsOnnxDir = Path.Combine(appDir, "Assets", "onnx");
                
                Debug.WriteLine($"尝试从内置目录复制模型文件: {assetsOnnxDir}");
                
                // 检查Assets/onnx目录是否存在
                if (!Directory.Exists(assetsOnnxDir))
                {
                    Debug.WriteLine("错误: Assets/onnx目录不存在");
                    return false;
                }
                
                // 1. 检查并复制CnOCR识别模型
                string cnocrModelSource = Path.Combine(assetsOnnxDir, "cnocr-v2.3-densenet_lite_136-gru-epoch=004-ft-model.onnx");
                if (File.Exists(cnocrModelSource))
                {
                    // 目标路径: C:\Users\<username>\AppData\Roaming\cnocr\2.3\densenet_lite_136-gru
                    string cnocrDir = Path.Combine(appDataDir, "cnocr");
                    string cnocrVersionDir = Path.Combine(cnocrDir, "2.3");
                    string cnocrModelDir = Path.Combine(cnocrVersionDir, "densenet_lite_136-gru");
                    string cnocrModelTarget = Path.Combine(cnocrModelDir, "cnocr-v2.3-densenet_lite_136-gru-epoch=004-ft-model.onnx");
                    
                    // 检查目标文件是否已存在
                    if (!File.Exists(cnocrModelTarget))
                    {
                        // 确保目标目录存在
                        Directory.CreateDirectory(cnocrModelDir);
                        
                        // 复制模型文件
                        File.Copy(cnocrModelSource, cnocrModelTarget, true);
                        Debug.WriteLine($"已复制CnOCR识别模型: {cnocrModelTarget}");
                        copiedAnyFile = true;
                    }
                    else
                    {
                        Debug.WriteLine($"CnOCR识别模型已存在: {cnocrModelTarget}");
                    }
                }
                else
                {
                    Debug.WriteLine($"警告: CnOCR识别模型源文件不存在: {cnocrModelSource}");
                }
                
                // 2. 检查并复制CNSTD检测模型
                string cnstdModelSource = Path.Combine(assetsOnnxDir, "ch_PP-OCRv4_det_infer.onnx");
                if (File.Exists(cnstdModelSource))
                {
                    // 目标路径: C:\Users\<username>\AppData\Roaming\cnstd\1.2\ppocr\ch_PP-OCRv4_det
                    string cnstdDir = Path.Combine(appDataDir, "cnstd");
                    string cnstdVersionDir = Path.Combine(cnstdDir, "1.2");
                    string ppocrDir = Path.Combine(cnstdVersionDir, "ppocr");
                    string cnstdModelDir = Path.Combine(ppocrDir, "ch_PP-OCRv4_det");
                    string cnstdModelTarget = Path.Combine(cnstdModelDir, "ch_PP-OCRv4_det_infer.onnx");
                    
                    // 检查目标文件是否已存在
                    if (!File.Exists(cnstdModelTarget))
                    {
                        // 确保目标目录存在
                        Directory.CreateDirectory(cnstdModelDir);
                        
                        // 复制模型文件
                        File.Copy(cnstdModelSource, cnstdModelTarget, true);
                        Debug.WriteLine($"已复制CNSTD检测模型: {cnstdModelTarget}");
                        copiedAnyFile = true;
                    }
                    else
                    {
                        Debug.WriteLine($"CNSTD检测模型已存在: {cnstdModelTarget}");
                    }
                }
                else
                {
                    Debug.WriteLine($"警告: CNSTD检测模型源文件不存在: {cnstdModelSource}");
                }
                
                return copiedAnyFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"复制模型文件时出错: {ex.Message}");
                return false;
            }
        }
    }
}