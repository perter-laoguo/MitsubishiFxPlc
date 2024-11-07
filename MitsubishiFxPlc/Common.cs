using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MitsubishiFxPlc
{
    public static class Common
    {
        /// <summary>
        /// 计算和校验
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="start">开始位置</param>
        /// <param name="len">长度</param>
        /// <returns>计算结果，高位在前低位在后</returns>
        public static byte[] CalcCheckSum(byte[] bytes, int start, int len)
        {
            // 1、将所有数据相加
            byte sum = 0;
            for (int i = start; i < start + len; i++)
            {
                sum += bytes[i];
            }
            // 2、转换为16进制字符串并生成字节数组
            return Encoding.ASCII.GetBytes(sum.ToString("X2"));
        }
        /// <summary>
        /// 为字节数组添加和校验
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="start">开始位置</param>
        /// <param name="len">长度</param>
        public static void AddCheckSum(ref byte[] bytes, int start, int len)
        {
            byte[] sum = CalcCheckSum(bytes, start, len);   // 计算校验和
            bytes = bytes.Concat(sum).ToArray();// 拼接数组
        }
        /// <summary>
        /// 将ascii数组两两组合，转换为byte数组
        /// 如：byte[] { 0x30, 0x31, 0x32, 0x33 }  => byte[] { 0x01, 0x23 }
        /// </summary>
        /// <param name="bytes">数组</param>
        /// <param name="startIndex">开始索引</param>
        /// <param name="len">解析的长度，注意必须是偶数</param>
        /// <returns>字节数组</returns>
        /// <exception cref="ArgumentException">解析长度必须是偶数</exception>
        public static byte[] AsciiArr2ByteArr(byte[] bytes, int startIndex, int len)
        {
            // 长度必须是偶数
            if (len % 2 != 0) throw new ArgumentException("解析长度必须是偶数");

            byte[] resBytes = new byte[len / 2];
            for (int i = 0; i < resBytes.Length; i++)
            {
                resBytes[i] = Convert.ToByte(Encoding.ASCII.GetString(bytes, startIndex + i * 2, 2), 16);
            }
            return resBytes;
        }
        /// <summary>
        /// 将数字转换为ascii数组
        /// </summary>
        /// <param name="v">要转换的数字</param>
        /// <param name="isLittle">是否为小端模式转换</param>
        /// <returns>转换后的ascii数组</returns>
        public static byte[] Num2AsciiArr(object v, bool isLittle = true)
        {
            int len = GetNumSize(v);
            byte[] bytes = new byte[16];
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(stream))
                {
                    if (v is float) w.Write(Convert.ToSingle(v));
                    else if (v is double) w.Write(Convert.ToDouble(v));
                    else w.Write(Convert.ToDecimal(v));
                }
                bytes = stream.GetBuffer().Take(len).ToArray();
            }

            if (!isLittle) Array.Reverse(bytes);

            string hexString = BitConverter.ToString(bytes).Replace("-", "");
            return Encoding.ASCII.GetBytes(hexString);
        }

        public static byte GetNumSize(object v)
        {
            if (v is byte || v is sbyte) return 1;
            if (v is short || v is ushort) return 2;
            if (v is int || v is uint || v is float) return 4;
            if (v is long || v is ulong || v is double) return 8;
            if (v is decimal) return 16;
            throw new ArgumentException("只能获取数值类型大小");
        }
    }
}
