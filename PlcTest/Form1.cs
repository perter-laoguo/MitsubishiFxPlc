using MitsubishiFxPlc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlcTest
{
    public partial class Form1 : Form
    {
        private MelsecFxPlc plc;

        public Form1()
        {
            InitializeComponent();
            plc = new MelsecFxPlc();
            // 串口信息的初始化
            plc.SerialiInit("COM2", 9600, 7, StopBits.One, Parity.Even);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            plc.Write("M0", true);
            plc.Write("Y0", true);
            plc.Write("D0", (byte)22);
            plc.Write("D0", (ushort)258);
            plc.Write("D0", (uint)65536);
            plc.Write("D0", (float)3.45);

            Console.WriteLine("M0：" + plc.ReadBool("M0"));
            Console.WriteLine("X6:" + plc.ReadBool("X6"));
            Console.WriteLine("M146: " + plc.ReadBool("M146"));

            Console.WriteLine("D0:" + plc.ReadUshort("D0"));
            Console.WriteLine("D0:" + plc.ReadUint("D0"));
            Console.WriteLine("D0:" + plc.ReadSingle("D0"));
        }
    }
}
