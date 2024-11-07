using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace MitsubishiFxPlc
{
    /// <summary>
    /// 用于和三菱FX-CPU的plc进行通讯
    /// </summary>
    public class FxPlc
    {
        public SerialPort SerialPort;

        /// <summary>
        /// 设置等待回复时的轮询时间，值越大性能越好，但可能会回复延迟
        /// </summary>
        public int LoopDelayTime { get; set; } = 10;

        public FxPlc() { }

        public FxPlc(SerialPort serialPort)
        {
            SerialPort = serialPort;
        }

        /// <summary>
        /// 初始化串口对象
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBit">数据位</param>
        /// <param name="stopBit">停止位</param>
        /// <param name="parity">校验协议</param>
        public void SerialInit(string portName, int baudRate, int dataBit, StopBits stopBit, Parity parity)
        {
            SerialPort = new SerialPort(portName, baudRate, parity, dataBit, stopBit);
        }

        /// <summary>
        /// 打开连接
        /// </summary>
        public void Open() => SerialPort.Open();

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close() => SerialPort.Close();

        /// <summary>
        /// 写入PLC数据
        /// </summary>
        public bool Write(string plcAddr, object value)
        {
            var request = PrepareWriteRequest(plcAddr, value);
            SendRequest(request);
            return ReceiveResponse();
        }

        /// <summary>
        /// 进行置位和复位操作
        /// </summary>
        public bool Write(string plcAddr, bool value)
        {
            var request = PrepareBitControlRequest(plcAddr, value);
            SendRequest(request);
            return ReceiveResponse();
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        public byte[] Read(string plcAddr, byte len)
        {
            var request = PrepareReadRequest(plcAddr, len);
            SendRequest(request);
            return ReceiveReadResponse(len);
        }

        /// <summary>
        /// 读取指定地址的16位无符号整数
        /// </summary>
        /// <param name="plcAddr"></param>
        /// <returns></returns>
        public ushort ReadUInt16(string plcAddr) => BitConverter.ToUInt16(Read(plcAddr, 2), 0);

        /// <summary>
        /// 读取指定地址的32位无符号整数
        /// </summary>
        /// <param name="plcAddr"></param>
        /// <returns></returns>
        public uint ReadUInt32(string plcAddr) => BitConverter.ToUInt32(Read(plcAddr, 4), 0);

        /// <summary>
        /// 读取指定地址的浮点数
        /// </summary>
        /// <param name="plcAddr"></param>
        /// <returns></returns>
        public float ReadFloat(string plcAddr) => BitConverter.ToSingle(Read(plcAddr, 4), 0);

        /// <summary>
        /// 读取指定地址的布尔值
        /// </summary>
        /// <param name="plcAddr"></param>
        /// <returns></returns>
        public bool ReadBool(string plcAddr)
        {
            byte[] resBytes = Read(plcAddr, 1);
            addr2AreaAddr(plcAddr, out PlcArea _, out int addr);
            return Convert.ToBoolean(resBytes[0] >> (addr % 8) & 1);
        }

        private void SendRequest(byte[] request)
        {
            SerialPort.Write(request, 0, request.Length);
        }

        /// <summary>
        /// 接收响应
        /// </summary>
        /// <returns></returns>
        private bool ReceiveResponse()
        {
            byte[] resBytes = new byte[1];
            WaitForData(resBytes.Length);
            SerialPort.Read(resBytes, 0, resBytes.Length);
            return resBytes[0] == 6; // 判断结果是否为6，6表示成功
        }

        /// <summary>
        /// 接收读响应
        /// </summary>
        private byte[] ReceiveReadResponse(byte len)
        {
            byte[] resBytes = new byte[1 + len * 2 + 1 + 2];
            WaitForData(resBytes.Length);
            SerialPort.Read(resBytes, 0, resBytes.Length);
            return Common.AsciiArr2ByteArr(resBytes, 1, len * 2); // 转换为byte数组
        }
        /// <summary>
        /// 等待数据
        /// </summary>
        /// <param name="length">目标数据长度</param>
        private void WaitForData(int length)
        {
            while (SerialPort.BytesToRead < length)
            {
                Thread.Sleep(LoopDelayTime);
            }
        }
        /// <summary>
        /// 准备写入请求的数据帧
        /// </summary>
        /// <param name="plcAddr">plc地址</param>
        /// <param name="value">要写入的值</param>
        /// <returns></returns>
        private byte[] PrepareWriteRequest(string plcAddr, object value)
        {
            byte[] addrBytes = ParsePlcAddr(plcAddr);
            byte len = Common.GetNumSize(value);
            byte[] lenBytes = Common.Num2AsciiArr(len);
            byte[] valueBytes = Common.Num2AsciiArr(value);

            var bufferList = new List<byte>
            {
                0x02, 0x31,
                addrBytes[0], addrBytes[1], addrBytes[2], addrBytes[3],
                lenBytes[0], lenBytes[1],
            };
            bufferList.AddRange(valueBytes);
            bufferList.Add(0x03); // 添加结束符
            byte[] buffer = bufferList.ToArray();
            Common.AddCheckSum(ref buffer, 1, buffer.Length - 1);
            return buffer;
        }
        /// <summary>
        /// 准备位控制请求的数据帧
        /// </summary>
        /// <param name="plcAddr">要控制的位地址</param>
        /// <param name="value">要操作的值</param>
        /// <returns></returns>
        private byte[] PrepareBitControlRequest(string plcAddr, bool value)
        {
            byte[] addrBytes = ParsePlcAddr(plcAddr, false);
            byte[] buffer = new byte[]
            {
                0x02,
                (byte)(value ? 0x37 : 0x38),
                addrBytes[0], addrBytes[1], addrBytes[2], addrBytes[3],
                0x03
            };
            Common.AddCheckSum(ref buffer, 1, 6);
            return buffer;
        }
        /// <summary>
        /// 准备读取请求的数据帧
        /// </summary>
        /// <param name="plcAddr">要读取的plc地址</param>
        /// <param name="len">目标数据长度</param>
        /// <returns></returns>
        private byte[] PrepareReadRequest(string plcAddr, byte len)
        {
            byte[] addrBytes = ParsePlcAddr(plcAddr);
            byte[] lenBytes = Common.Num2AsciiArr(len);
            var bufferList = new List<byte>
            {
                0x02,
                0x30,
                addrBytes[0], addrBytes[1], addrBytes[2], addrBytes[3],
                lenBytes[0], lenBytes[1],
                0x03
            };
            byte[] buffer = bufferList.ToArray();
            Common.AddCheckSum(ref buffer, 1, buffer.Length - 1);
            return buffer;
        }

        private void addr2AreaAddr(string plcAddr, out PlcArea area, out int addr)
        {
            if (!Enum.TryParse(plcAddr.Substring(0, 1), out area))
                throw new ArgumentException("仅支持D、M、X、Y、S、T区的操作");

            addr = area == PlcArea.X || area == PlcArea.Y
                ? Convert.ToInt32(plcAddr.Substring(1), 8)
                : Convert.ToInt32(plcAddr.Substring(1));
        }

        private byte[] ParsePlcAddr(string plcAddr, bool isByteOperator = true)
        {
            addr2AreaAddr(plcAddr, out PlcArea area, out int addr);
            addr = GetAdjustedAddress(area, addr, isByteOperator);
            return Common.Num2AsciiArr((ushort)addr, !isByteOperator);
        }

        private int GetAdjustedAddress(PlcArea area, int addr, bool isByteOperator)
        {
            if (isByteOperator)
            {
                switch (area)
                {
                    case PlcArea.D: return addr * 2 + 0x1000;
                    case PlcArea.M: return addr / 8 + 0x0100;
                    case PlcArea.X: return addr / 8 + 0x0080;
                    case PlcArea.Y: return addr / 8 + 0x00A0;
                    case PlcArea.S: return addr / 8;
                    case PlcArea.T: return addr / 8 + 0x00C0;
                }
            }
            else
            {
                switch (area)
                {
                    case PlcArea.M: return addr + 0x0800;
                    case PlcArea.Y: return addr + 0x0500;
                    case PlcArea.S: return addr;
                    case PlcArea.T: return addr + 0x0600;
                }
            }
            return addr; // default return statement
        }

        public enum PlcArea
        {
            D, M, X, Y, S, T
        }
    }
}
