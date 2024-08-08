using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            // 2、转换位16进制字符串
            string sumStr = sum.ToString("X2");
            // 3、将16进制字符串转换位ascii码数组
            byte[] res = Encoding.ASCII.GetBytes(sumStr);
            return res;
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

            Console.WriteLine("发送：" + BitConverter.ToString(bytes, 0).Replace("-", " "));
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

            // 1、将每一个数字转换为ascii字符串 
            string s = Encoding.ASCII.GetString(bytes, startIndex, len);

            // 2、字符串两两分割, 存储到数组中         
            // 3、每一个字符串变成数字           
            byte[] resBytes = new byte[len / 2];
            for (int i = 0; i < resBytes.Length; i++)
            {
                resBytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
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
            byte[] bytes = new byte[16]; // 存储转换后的字节数组
            MemoryStream stream = new MemoryStream(bytes);   // 为数组创建流
            BinaryWriter w = new BinaryWriter(stream);   // 为流创建“按位写入”对象
            if (v is float) w.Write(Convert.ToSingle(v));
            else if (v is double) w.Write(Convert.ToDouble(v));
            else w.Write(Convert.ToDecimal(v));     // 将变量写入流
            bytes = bytes.Take(len).ToArray();      // 截取部分


            if (isLittle == false)   // 如果不是小端模式，则反转数组变成大端
            {
                bytes = bytes.Reverse().ToArray();
            }
            string s = BitConverter.ToString(bytes, 0, bytes.Length);      // 字节数组转换为16进制的字符串表达方式
            s = s.Replace("-", "");     // 替换字符串中的 - 为空字符串
            byte[] sbytes = Encoding.ASCII.GetBytes(s); // 转换为ascii数组
            return sbytes;
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
