# MitsubishiFxPlc

���ں������FXϵ��PLCͨ�ţ���֧��D��M��S��Y��X���Ķ�д�������ֽ���������FX3U�豸���⼸��������д��������������ύisuues���п��˸ģ�������

## ʹ��

clone���ֿ⣬������������Ŀ����Ӷ�`MitsubishiFxPlc`��Ŀdll������

��д��ʾ

```CSharp
private MelsecFxPlc plc;

public Form1()
{
    InitializeComponent();
    // ʵ����
    plc = new MelsecFxPlc();
    // ������Ϣ�ĳ�ʼ��
    plc.SerialiInit("COM2", 9600, 7, StopBits.One, Parity.Even);
}

private void button1_Click(object sender, EventArgs e)
{
    // д�����
    plc.Write("D2", (ushort)258);
    plc.Write("D10", (uint)65536);
    plc.Write("D20", (float)3.45);
    plc.Write("M0", true);
    plc.Write("S0", true);
    plc.Write("Y0", true);
    
    // ��ȡ����
    byte[] a = plc.Read("D0", 2);
    Console.WriteLine("D0: " + BitConverter.ToUInt16(a, 0));
    Console.WriteLine("D2: " + plc.ReadUInt16("D2"));
    Console.WriteLine("D10: " + plc.ReadUInt32("D10"));
    Console.WriteLine("D20: " + plc.ReadFloat("D20"));
    Console.WriteLine("X0: " + plc.ReadBool("X0"));
    
    // ��֧���첽��ȡ
    bool b = await plc.ReadBoolAsync("M1");
    await Console.Out.WriteLineAsync("M1: " + b.ToString ());
}
```