# MitsubishiFxPlc

用于和三菱的FX系列PLC通信，现支持D、M、S、Y、X区的读写操作，现仅测试三菱FX3U设备，这几个区均读写正常，有问题可提交isuues，有空了改，嘻嘻。

## 使用

clone本仓库，编译后在你的项目中添加对`MitsubishiFxPlc`项目dll的引用

读写演示

```CSharp
private MelsecFxPlc plc;

public Form1()
{
    InitializeComponent();
    // 实例化
    plc = new MelsecFxPlc();
    // 串口信息的初始化
    plc.SerialiInit("COM2", 9600, 7, StopBits.One, Parity.Even);
}

private void button1_Click(object sender, EventArgs e)
{
    // 写入测试
    plc.Write("D2", (ushort)258);
    plc.Write("D10", (uint)65536);
    plc.Write("D20", (float)3.45);
    plc.Write("M0", true);
    plc.Write("S0", true);
    plc.Write("Y0", true);
    
    // 读取测试
    byte[] a = plc.Read("D0", 2);
    Console.WriteLine("D0: " + BitConverter.ToUInt16(a, 0));
    Console.WriteLine("D2: " + plc.ReadUInt16("D2"));
    Console.WriteLine("D10: " + plc.ReadUInt32("D10"));
    Console.WriteLine("D20: " + plc.ReadFloat("D20"));
    Console.WriteLine("X0: " + plc.ReadBool("X0"));
    
    // 均支持异步读取
    bool b = await plc.ReadBoolAsync("M1");
    await Console.Out.WriteLineAsync("M1: " + b.ToString ());
}
```