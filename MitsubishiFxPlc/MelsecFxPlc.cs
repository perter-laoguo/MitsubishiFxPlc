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
    public class FxPlc
    {
        SerialPort serialPort;
        public FxPlc() { }
        public FxPlc(SerialPort serialPort)
        {
            this.serialPort = serialPort;
        }
        /// <summary>
        /// 设置等待回复时的轮询时间，值越大性能越好，但可能会回复延迟
        /// </summary>
        public int LoopDelayTime { get; set; } = 20;

        /// <summary>
        /// 初始化串口对象
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBit">数据位</param>
        /// <param name="stopBit">停止位</param>
        /// <param name="parity">校验协议</param>
        /// <exception cref="NotImplementedException"></exception>
        public void SerialiInit(string portName, int baudRate, int dataBit, StopBits stopBit, Parity parity)
        {
            serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = dataBit;
            serialPort.StopBits = stopBit;
            serialPort.Parity = parity;
        }

        /// <summary>
        /// 打开连接
        /// </summary>
        public void Open()
        {
            serialPort.Open();
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            serialPort.Close();
        }

        /// <summary>
        /// 写入plc数据
        /// </summary>
        /// <param name="plcAddr">要写入的plc的地址</param>
        /// <param name="value">要写入的值</param>
        public bool Write(string plcAddr, object value)
        {
            // 计算位元地址
            byte[] addrBytes = ParsePlcAddr(plcAddr);
            // 获取要写入的字节数，并将要写入的字节数转换位ascii数组
            byte len = Common.GetNumSize(value);
            byte[] lenBytes = Common.Num2AsciiArr(len);
            // 准备问询帧
            byte[] buffer = new byte[]
            {
                0x02,
                0x31,                   // 功能码，写入
                addrBytes[0], addrBytes[1], addrBytes[2], addrBytes[3],    // 写入的位元地址
                lenBytes[0], lenBytes[1],                // 写入的字节数
            };
            // 添加数据和终止码
            byte[] valueBytes = Common.Num2AsciiArr(value);
            buffer = buffer.Concat(valueBytes).Append((byte)0x03).ToArray();
            // 添加校验位
            Common.AddCheckSum(ref buffer, 1, buffer.Length - 1);
            // 发送
            serialPort.Write(buffer, 0, buffer.Length);
            // 读取一个字节（操作结果）
            byte[] resBytes = new byte[1];
            while (serialPort.BytesToRead < resBytes.Length) { Thread.Sleep(LoopDelayTime); }
            // 读取数据
            serialPort.Read(resBytes, 0, resBytes.Length);
            // 判断结果是否位6，6表示成功
            return resBytes[0] == 6;
        }
        /// <summary>
        /// 进行置位和复位操作
        /// </summary>
        /// <param name="plcAddr">要写入的plc的地址</param>
        /// <param name="value">要写入的值</param>
        public bool Write(string plcAddr, bool value)
        {
            // 计算位元地址
            byte[] addrBytes = ParsePlcAddr(plcAddr, false);
            byte[] buffer = new byte[]
            {
                0x02,
                (byte)(value ? 0x37 : 0x38),                   // 功能码，根据value决定进行置位或者复位
                addrBytes[0], addrBytes[1], addrBytes[2], addrBytes[3],    // 操作的位元地址
                0x03                    // 终止码
            };
            // 添加校验位
            Common.AddCheckSum(ref buffer, 1, 6);
            // 发送
            serialPort.Write(buffer, 0, buffer.Length);
            // 读取一个字节（操作结果）
            byte[] resBytes = new byte[1];
            while (serialPort.BytesToRead < resBytes.Length) { Thread.Sleep(LoopDelayTime); }
            // 读取数据
            serialPort.Read(resBytes, 0, resBytes.Length);
            // 判断结果是否位6，6表示成功
            return resBytes[0] == 6;
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="plcAddr">读取的plc地址</param>
        /// <param name="len">读取的字节数</param>
        /// <returns>结果的字节数组</returns>
        public byte[] Read(string plcAddr, byte len)
        {
            // 计算位元地址
            byte[] addrBytes = ParsePlcAddr(plcAddr);
            // 将读取的数据长度转换为字节数组
            byte[] lenBytes = Common.Num2AsciiArr(len);
            byte[] buffer = new byte[]
            {
                0x02,
                0x30,
                addrBytes[0], addrBytes[1], addrBytes[2], addrBytes[3], // 读取的地址
                lenBytes[0], lenBytes[1],                                // 读取字节数
                0x03
            };
            // 添加校验位
            Common.AddCheckSum(ref buffer, 1, 8);
            // 发送
            serialPort.Write(buffer, 0, buffer.Length);
            // 等待缓冲区的数据足够      
            byte[] resBytes = new byte[1 + len * 2 + 1 + 2];  // 起始1个 + 数据N个 + 终止1个 + 校验2个        // 30 30 30 30  =>  { 0x00, 0x00 }
            while (serialPort.BytesToRead < resBytes.Length) { Thread.Sleep(LoopDelayTime); }
            // 读取数据
            serialPort.Read(resBytes, 0, resBytes.Length);
            // 截取并将ascii数组转换为byte数组
            return Common.AsciiArr2ByteArr(resBytes, 1, len * 2);
        }
        /// <summary>
        /// 异步读取数据
        /// </summary>
        /// <param name="plcAddr">读取的plc地址</param>
        /// <param name="len">读取的字节数</param>
        /// <returns>结果的字节数组</returns>
        public Task<byte[]> ReadAsync(string plcAddr, byte len)
        {
            return Task.Run(() => Read(plcAddr, len));
        }

        /// <summary>
        /// 读取uint16类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public ushort ReadUInt16(string plcAddr)
        {
            return BitConverter.ToUInt16(Read(plcAddr, 2), 0);
        }
        /// <summary>
        /// 异步读取uint16类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public Task<ushort> ReadUInt16Async(string plcAddr)
        {
            return Task.Run(() => ReadUInt16(plcAddr));
        }
        /// <summary>
        /// 读取uint32类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public uint ReadUInt32(string plcAddr)
        {
            return BitConverter.ToUInt32(Read(plcAddr, 4), 0);
        }
        /// <summary>
        /// 异步读取uint32类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public Task<uint> ReadUInt32Async(string plcAddr)
        {
            return Task.Run(() => ReadUInt32(plcAddr));
        }
        /// <summary>
        /// 读取float类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public float ReadFloat(string plcAddr)
        {
            return BitConverter.ToSingle(Read(plcAddr, 4), 0);
        }
        /// <summary>
        /// 异步读取uint32类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public Task<float> ReadFloatAsync(string plcAddr)
        {
            return Task.Run(() => ReadFloat(plcAddr));
        }
        /// <summary>
        /// 读取bool类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public bool ReadBool(string plcAddr)
        {
            byte[] resBytes = Read(plcAddr, 1);
            addr2AreaAddr(plcAddr, out PlcArea _, out int addr);
            return Convert.ToBoolean(resBytes[0] >> (addr % 8) & 1);
        }
        /// <summary>
        /// 异步读取bool类型数据
        /// </summary>
        /// <param name="plcAddr">地址</param>
        /// <returns>结果</returns>
        public Task<bool> ReadBoolAsync(string plcAddr)
        {
            return Task.Run(() => ReadBool(plcAddr));
        }

        public enum PlcArea
        {
            D,
            M,
            X,
            Y,
            S,
            T
        }

        /// <summary>
        /// 将字符串类型的plc地址解析为区和地址
        /// </summary>
        /// <param name="plcAddr">plc地址</param>
        /// <param name="area">输出所在的区</param>
        /// <param name="addr">输出所在的地址</param>
        private void addr2AreaAddr(string plcAddr, out PlcArea area, out int addr)
        {
            // 解析地址所在的区
            try
            {
                area = (PlcArea)Enum.Parse(typeof(PlcArea), plcAddr.Substring(0, 1));
            }
            catch
            {
                throw new ArgumentException("仅支持D、M、X、Y、S、T区的操作");
            }
            // 解析地址所在的号
            if (area == PlcArea.X || area == PlcArea.Y)  // 如果是X或者Y区，说明该字符串是8进制字符串
                addr = Convert.ToInt32(plcAddr.Substring(1), 8);
            else
                addr = Convert.ToInt32(plcAddr.Substring(1));
        }
        /// <summary>
        /// 根据输入的plc字符串计算所在位元的地址
        /// </summary>
        /// <param name="plcAddr">plc地址</param>
        /// <param name="isByteOperator">操作单位是否为字节</param>
        /// <returns></returns>
        private byte[] ParsePlcAddr(string plcAddr, bool isByteOperator = true)
        {
            // 1、解析地址所在区和位置
            addr2AreaAddr(plcAddr, out PlcArea area, out int addr);
            // 2、根据所在的区使用不同的公式计算位元地址
            if (isByteOperator)
            {
                switch (area)
                {
                    case PlcArea.D:
                        addr = addr * 2 + 0x1000;
                        break;
                    case PlcArea.M:
                        addr = addr / 8 + 0x0100;
                        break;
                    case PlcArea.X:
                        addr = addr / 8 + 0x0080;
                        break;
                    case PlcArea.Y:
                        addr = addr / 8 + 0x00A0;
                        break;
                    case PlcArea.S:
                        addr = addr / 8;
                        break;
                    case PlcArea.T:
                        addr = addr / 8 + 0x00C0;
                        break;
                }
            }
            else
            {
                switch (area)
                {
                    case PlcArea.M:
                        addr = addr + 0x0800;
                        break;
                    case PlcArea.Y:
                        addr = addr + 0x0500;
                        break;
                    case PlcArea.S:
                        addr = addr + 0x0000;
                        break;
                    case PlcArea.T:
                        addr = addr + 0x0600;
                        break;
                }
            }
            // 3、数字转ASCII字符串(如果是字节操作，应该是大端模式，如果是置位复位操作，应该是小端模式)
            // isByteOperator为true时，应该是大端模式，isByteOperator为false时，应该是小端端模式，
            // Num2AsciiArr的参数2指定是否以小端模式转换，true表示小端，false表示大端
            return Common.Num2AsciiArr((ushort)addr, !isByteOperator);
        }

    }
}
