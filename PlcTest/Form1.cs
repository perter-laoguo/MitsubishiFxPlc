using MitsubishiFxPlc;
using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlcTest
{
    public partial class Form1 : Form
    {
        private FxPlc plc;

        public Form1()
        {
            InitializeComponent();
            plc = new FxPlc();
            // 串口信息的初始化
            plc.SerialiInit("COM2", 9600, 7, StopBits.One, Parity.Even);
            plc.Open();
        }

        private async void button1_ClickAsync(object sender, EventArgs e)
        {
            plc.Write("D2", (ushort)258);
            plc.Write("D10", (uint)65536);
            plc.Write("D20", (float)3.45);
            plc.Write("M0", true);
            plc.Write("S0", true);
            plc.Write("Y0", true);

            byte[] a = plc.Read("D0", 2);
            Console.WriteLine("D0: " + BitConverter.ToUInt16(a, 0));
            Console.WriteLine("D2: " + plc.ReadUInt16("D2"));
            Console.WriteLine("D10: " + plc.ReadUInt32("D10"));
            Console.WriteLine("D20: " + plc.ReadFloat("D20"));
            Console.WriteLine("X0: " + plc.ReadBool("X0"));

            bool b = await plc.ReadBoolAsync("M1");
            await Console.Out.WriteLineAsync("M1: " + b.ToString ());
        }
    }
}
