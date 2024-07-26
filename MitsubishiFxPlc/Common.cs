using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MitsubishiFxPlc
{
    public static class Common
    {
        static Regex reg = new Regex(".{2}");

        /// <summary>
        /// 将ascii编码的字节数组，两两结合，转换为字节数组
        /// 如：{ 0x30,0x31 } => "01" => {"01"} => { 1 }        {0x31,0x31,0x30,0x31} => "1101" => {"11","01"} => {17,1}
        /// </summary>
        /// <param name="bytes">要转换的数组</param>
        /// <returns></returns>
        public static byte[] AsciiBytesToNumBytes(this byte[] bytes)
        {
            string value = Encoding.ASCII.GetString(bytes); // {0x31,0x31,0x30,0x31} => "1101"
            List<string> strings = new List<string>();  // "1101" => {"11","01"}
            for (int i = 0; i < value.Length; i += 2)
            {
                strings.Add(value.Substring(i, 2));
            }
            // {"11","01"} => {17,1}
            return strings.Select(v => Convert.ToByte(v, 16)).ToArray();
        }
        /// <summary>
        /// 计算和校验
        /// </summary>
        /// <param name="bytes">要参与计算的数组</param>
        /// <param name="startIndex">开始计算的索引位置</param>
        /// <returns>计算结果, 高位在前低位在后</returns>
        public static byte[] GetCheckSum(this IEnumerable<byte> bytes, int startIndex = 0)
        {
            // 计算和。并转换为16进制字符串
            string v = bytes.Skip(startIndex).Sum(_ => _).ToString("X4");
            // 取得后两文
            string sv = v.Substring(v.Length - 2, 2);
            // 转换为ascii字节数组
            return Encoding.ASCII.GetBytes(sv);
        }

        /// <summary>
        /// 将数字转换为16进制字符串，并以ascii格式编码为字节数组
        /// 如：258 ==转16进制=> "0102" => 反转 => "0201" ==ascii编码==> {0x30,0x32,0x30,0x31} = { 48,50,48,49 }
        /// </summary>
        /// <param name="num">要转换的数字</param>
        /// <param name="isSmallEnd">是否为小端模式</param>
        /// <returns>字节数组</returns>
        public static byte[] NumToAsciiBytes(float num, bool isSmallEnd = true)
        {
            string v = BitConverter.ToString(BitConverter.GetBytes(num)).Replace("-", ""); // 先转16进制
            // 反转
            if (!isSmallEnd)
            {
                MatchCollection matches = reg.Matches(v);
                v = "";
                for (int i = matches.Count - 1; i > -1; i--)
                {
                    v += matches[i].Value;
                }
            }
            return Encoding.ASCII.GetBytes(v);    // 编码并返回
        }
        /// <summary>
        /// 将数字转换为16进制字符串，并以ascii格式编码为字节数组
        /// 如：258 ==转16进制=> "0102" => 反转 => "0201" ==ascii编码==> {0x30,0x32,0x30,0x31} = { 48,50,48,49 }
        /// </summary>
        /// <param name="num">要转换的数字</param>
        /// <param name="len">目标数组的长度</param>
        /// <param name="isSmallEnd">是否为小端模式</param>
        /// <returns>字节数组</returns>
        public static byte[] NumToAsciiBytes(long num, int len, bool isSmallEnd = true)
        {
            string v = num.ToString("X" + len); // 先转16进制
            // 反转
            if (isSmallEnd)
            {
                MatchCollection matches = reg.Matches(v);
                v = "";
                for (int i = matches.Count - 1; i > -1; i--)
                {
                    v += matches[i].Value;
                }
            }
            return Encoding.ASCII.GetBytes(v);
        }
    }
}
