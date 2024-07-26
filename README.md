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
    plc.Write("D0", (byte)22);
    plc.Write("D0", (ushort)258);
    plc.Write("D0", (uint)65536);
    plc.Write("D0", (float)3.45);
    plc.Write("M0", true);
    plc.Write("S0", true);
    plc.Write("Y0", true);
    
    // 读取测试
    Console.WriteLine("M0：" + plc.ReadBool("M0"));
    Console.WriteLine("M146: " + plc.ReadBool("M146"));
    Console.WriteLine("S1: " + plc.ReadBool("S1"));
    Console.WriteLine("Y1: " + plc.ReadBool("Y1"));
    Console.WriteLine("X6:" + plc.ReadBool("X6"));
    Console.WriteLine("D0:" + plc.ReadUshort("D0"));
    Console.WriteLine("D0:" + plc.ReadUint("D0"));
    Console.WriteLine("D0:" + plc.ReadSingle("D0"));
}
```