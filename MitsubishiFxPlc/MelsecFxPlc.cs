using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MitsubishiFxPlc
{
    /// <summary>
    /// 用于和三菱FX-CPU的plc进行通讯
    /// </summary>
    public class MelsecFxPlc
    {
        private static object lockObj = new object();
        private SerialPort port;


        /// <summary>
        /// 初始化
        /// </summary>
        public MelsecFxPlc()
        {
            port = new SerialPort();
        }
        /// <summary>
        /// 使用指定的串口对象初始化
        /// </summary>
        public MelsecFxPlc(SerialPort serialPort)
        {
            port = serialPort;
        }

        /// <summary>
        /// 设置串口的信息
        /// </summary>
        /// <param name="portName">串口号</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBit">数据位</param>
        /// <param name="stopBit">停止位</param>
        /// <param name="parity">校验协议</param>
        public void SerialiInit(string portName, int baudRate, int dataBit, StopBits stopBit, Parity parity)
        {
            port.PortName = portName;
            port.BaudRate = baudRate;
            port.DataBits = dataBit;
            port.Parity = parity;
            port.StopBits = stopBit;
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; } = 5000;

        /// <summary>
        /// 读取位
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns>bool值</returns>
        public bool ReadBool(string address)
        {
            byte v = ReadByte(address);
            // 计算出该地址中的数字
            Addr2AreaAddr(address, out AreaType _, out ushort addr);
            return Convert.ToBoolean(v >> (addr % 8) & 1);
        }
        /// <summary>
        /// 异步读取位
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns>bool值</returns>
        public Task<bool> ReadBoolAsync(string address)
        {
            return Task.Run(() => ReadBool(address));
        }

        /// <summary>
        /// 读取字节
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns></returns>
        public byte ReadByte(string address)
        {
            byte[] values = ReadPort(address, 1);
            return values[0];
        }
        /// <summary>
        /// 异步读取字节
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns></returns>
        public Task<byte> ReadByteAsync(string address)
        {
            return Task.Run(() => ReadByte(address));
        }

        /// <summary>
        /// 读取ushort类型
        /// </summary>
        /// <param name="address">地址</param>
        /// <returns></returns>
        public ushort ReadUshort(string address)
        {
            byte[] values = ReadPort(address, 2);

            return BitConverter.ToUInt16(values, 0);
        }
        /// <summary>
        /// 异步读取ushort类型
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns></returns>
        public Task<ushort> ReadUshortAsync(string address)
        {
            return Task.Run(() => ReadUshort(address));
        }
        /// <summary>
        /// 读取uint类型
        /// </summary>
        /// <param name="address">地址</param>
        /// <returns></returns>
        public uint ReadUint(string address)
        {
            byte[] values = ReadPort(address, 4);

            return BitConverter.ToUInt32(values, 0);
        }
        /// <summary>
        /// 异步读取uint类型
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns></returns>
        public Task<uint> ReadUintAsync(string address)
        {
            return Task.Run(() => ReadUint(address));
        }
        /// <summary>
        /// 读取float
        /// </summary>
        /// <param name="address">float</param>
        /// <returns></returns>
        public float ReadSingle(string address)
        {
            byte[] values = ReadPort(address, 4);

            return BitConverter.ToSingle(values, 0);
        }
        /// <summary>
        /// 异步读取uint类型
        /// </summary>
        /// <param name="address">读取的数据地址</param>
        /// <returns></returns>
        public Task<float> ReadSingleAsync(string address)
        {
            return Task.Run(() => ReadSingle(address));
        }
        #region 写入bool
        public void Write(string address, bool value)
        {
            // 获取区和地址
            Addr2AreaAddr(address, out AreaType area, out ushort addr);
            // 获取元件地址
            byte[] addrByte = plcAddrToBytes(area, addr, false);

            byte[] buffer = addrByte.Prepend(value ? (byte)0x37 : (byte)0x38).ToArray();
            getReqBuffer(ref buffer);
            WriteAndRead(buffer, 1);
        }
        #endregion
        #region 写入数字
        /// <summary>
        /// 向指定的地址写入byte
        /// </summary>
        /// <param name="address">地址位</param>
        /// <param name="value">写入数据</param>
        /// <returns>是否写入成功</returns>
        public void Write(string address, byte value)
        {
            WriteBytes(address, Common.NumToAsciiBytes(value, 1 * 2));
        }
        /// <summary>
        /// 向指定的地址写入ushort
        /// </summary>
        /// <param name="address">地址位</param>
        /// <param name="value">写入数据</param>
        /// <returns>是否写入成功</returns>
        public void Write(string address, ushort value)
        {
            WriteBytes(address, Common.NumToAsciiBytes(value, 2 * 2));
        }
        /// <summary>
        /// 向指定的地址写入uint
        /// </summary>
        /// <param name="address">地址位</param>
        /// <param name="value">写入数据</param>
        /// <returns>是否写入成功</returns>
        public void Write(string address, uint value)
        {
            WriteBytes(address, Common.NumToAsciiBytes(value, 4 * 2));
        }
        /// <summary>
        /// 向指定的地址写入float
        /// </summary>
        /// <param name="address">地址位</param>
        /// <param name="value">写入数据</param>
        /// <returns>是否写入成功</returns>
        public void Write(string address, float value)
        {
            WriteBytes(address, Common.NumToAsciiBytes(value));
        }
        private void WriteBytes(string address, byte[] values)
        {
            // 获取区和地址
            Addr2AreaAddr(address, out AreaType area, out ushort addr);
            // 获取元件地址
            byte[] addrByte = plcAddrToBytes(area, addr);
            // 获取要写入数据的长度
            byte[] lenByte = Common.NumToAsciiBytes(values.Length / 2, 2);

            byte[] buffer = addrByte.Prepend<byte>(0x31).Concat(lenByte).Concat(values).ToArray();
            getReqBuffer(ref buffer);
            WriteAndRead(buffer, 1);
        }
        #endregion

        #region private

        /// <summary>
        /// 根据plc地址和要读取的数据长度读取数据
        /// </summary>
        /// <param name="address">plc地址，如：D0、M0</param>
        /// <param name="len">要读取的字节数</param>
        /// <returns>结果数组</returns>
        private byte[] ReadPort(string address, byte len)
        {
            // 获取询问帧
            byte[] buffer = getReadBuffer(address, len);

            // 写入串口并获取返回结果
            byte[] res = WriteAndRead(buffer, 4 + len * 2);

            // 将响应帧中的数据解析为字节
            byte[] values = res.Skip(1).Take(len * 2).ToArray().AsciiBytesToNumBytes();

            return values;
        }

        /// <summary>
        /// 写入并读取串口响应结果
        /// </summary>
        /// <param name="bytes">要写入的数据</param>
        /// <param name="expectLen">预期返回的字节数</param>
        /// <returns>读取的结果</returns>
        private byte[] WriteAndRead(byte[] bytes, int expectLen)
        {
            lock (lockObj)
            {
                if (port.IsOpen == false) port.Open();
                // 写入
                port.Write(bytes, 0, bytes.Length);
                DateTime dt = DateTime.Now;
                // 读取结果
                while (port.BytesToRead < expectLen)
                {
                    Thread.Sleep(20);
                    // 超时判断
                    if (DateTime.Now - dt > new TimeSpan(0, 0, Timeout / 1000))
                    {
                        throw new TimeoutException("操作超时");
                    }
                } // 如果串口的读取缓冲区中数据量小于预期的字节数，就一直循环
                byte[] body = new byte[expectLen];
                port.Read(body, 0, body.Length);
                // 返回读取结果
                return body;
            }
        }

        // 数据区块的类型
        private enum AreaType
        {
            D,
            M,
            S,
            /// <summary>
            /// 地址为8进制
            /// </summary>
            Y,
            /// <summary>
            /// 该区只读，地址为8进制
            /// </summary>
            X
        }

        /// <summary>
        /// 将地址字符串拆分为区和地址位
        /// </summary>
        /// <param name="address">plc地址，如：M0,X10。注意：X和Y地址位8进制</param>
        /// <param name="area">当前地址所在的区</param>
        /// <param name="addr">当前地址的地址位</param>
        private void Addr2AreaAddr(string address, out AreaType area, out ushort addr)
        {
            // 将传入的地址拆分为数据区和地址   "M20"   =>   "M" "20"
            Regex reg = new Regex(@"^[a-zA-Z]");
            string areaString = reg.Match(address).Value;
            if (!Enum.TryParse(areaString, true, out area))    // 区块
            {
                throw new ArgumentException("地址错误，仅支持D、M、S、Y、X区域");
            }

            // 如果是X区和Y区，地址是8进制      X10 => 10 ==10进制=> 8
            int jinZhi = (area == AreaType.X || area == AreaType.Y) ? 8 : 10;
            addr = Convert.ToUInt16(address.Substring(1), jinZhi); // 地址
        }

        /// <summary>
        /// 将plc地址计算转换为元件地址
        /// </summary>
        /// <param name="area">所在的区</param>
        /// <param name="addr">地址位</param>
        /// <param name="isByteOperator">是否为字节操作</param>
        /// <returns>对应plc地址的元件地址，置位复位操作和字节操作结果是不同的</returns>
        private byte[] plcAddrToBytes(AreaType area, ushort addr, bool isByteOperator = true)
        {
            int res = 0;
            bool reverse = false;
            if (isByteOperator)
            {
                switch (area)
                {
                    case AreaType.D:
                        res = addr * 2 + 0x1000;
                        break;

                    case AreaType.M:
                        res = addr / 8 + 0x0100;
                        break;

                    case AreaType.S:
                        res = addr / 8 + 0x0000;
                        break;

                    case AreaType.Y:
                        res = addr / 8 + 0x00A0;
                        break;

                    case AreaType.X:
                        res = addr / 8 + 0x0080;
                        break;
                }
            }
            else
            {
                reverse = true;
                switch (area)
                {
                    case AreaType.M:
                        if (addr >= 8000) res = addr - 8000 + 0x0F00;   // 特M
                        else res = addr + 0x0800;
                        break;

                    case AreaType.S:
                        res = addr + 0x0000;
                        break;

                    case AreaType.Y:
                        res = addr + 0x0500;
                        break;

                    case AreaType.X:
                        res = addr + 0x0400;
                        break;

                    default:
                        throw new Exception("仅支持对M、S、Y、X区进行置位复位操作");
                }
            }
            return Common.NumToAsciiBytes(res, 4, reverse);
        }

        /// <summary>
        /// 获取读取操作的字节数组
        /// </summary>
        /// <param name="address">要读取的地址</param>
        /// <param name="len">要读取的字节数</param>
        /// <returns>询问帧字节数组</returns>
        private byte[] getReadBuffer(string address, byte len)
        {
            // 获取区和地址位
            Addr2AreaAddr(address, out AreaType area, out ushort addr);

            // 将PLC地址转换为元件地址
            byte[] addrByte = plcAddrToBytes(area, addr);

            // 将要读取的数据长度转换为字节数组
            byte[] lenByte = Common.NumToAsciiBytes(len, 2);

            // 拼接并计算询问帧
            byte[] buffer = new byte[] { 0x30 }.Concat(addrByte).Concat(lenByte).ToArray();
            getReqBuffer(ref buffer);

            return buffer;
        }

        /// <summary>
        /// 将给定数组加上开始位 0x02, 结束位 0x03，计算和校验并拼接
        /// </summary>
        /// <param name="content"></param>
        private void getReqBuffer(ref byte[] content)
        {
            IEnumerable<byte> buffer = content.Prepend<byte>(0x02).Append<byte>(0x03);
            // 计算和校验
            byte[] checkBytes = buffer.Skip(1).GetCheckSum();
            // 拼接并赋值
            content = buffer.Concat(checkBytes).ToArray();
        }

        #endregion private
    }
}
