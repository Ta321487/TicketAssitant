using Newtonsoft.Json;

namespace TA_WPF.Models
{
    /// <summary>
    /// OCR识别结果类
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// 识别出的文本
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// 识别的可信度得分
        /// </summary>
        [JsonProperty("score")]
        public double Score { get; set; }

        /// <summary>
        /// 文本在图像中的位置坐标
        /// </summary>
        [JsonProperty("position")]
        public List<List<double>> Position { get; set; }

        /// <summary>
        /// 重写ToString方法，便于调试
        /// </summary>
        public override string ToString()
        {
            return $"文本: {Text}, 可信度: {Score:P2}";
        }
    }

    /// <summary>
    /// OCR错误信息类
    /// </summary>
    public class OcrError
    {
        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}