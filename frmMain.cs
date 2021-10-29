using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RaspberryHelper
{
    public partial class frmMain : Form, frmMain.IView
    {
        public interface IView
        {
            void SetController(IController controller);
            //Open serial port event
            void OpenComEvent(Object sender, SerialPortEventArgs e);
            //Close serial port event
            void CloseComEvent(Object sender, SerialPortEventArgs e);
            //Serial port receive data event
            void ComReceiveDataEvent(Object sender, SerialPortEventArgs e);
        }
        public frmMain()
        {
            InitializeComponent();
            IController controller = new IController(this);
            InitializeCOMCombox();
            this.toolStripStatusRx.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            this.toolStripStatusRx.Text = "Sent: 0";
            this.toolStripStatusRx.Text = "Received: 0";
        }
        private IController controller;
        private int sendBytesCount = 0;
        private int receiveBytesCount = 0;
        public string SSID
        {
            get
            {
                return _SSID;
            }
            set
            {
                _SSID = value;
            }
        }
        private string _SSID = "";
        private string _Psswd = "";

        static HttpListener sSocket = null;
        private delegate void WriteLogDelegate(string str);
        //C#关机代码

        // 这个结构体将会传递给API。使用StructLayout 

        //(...特性，确保其中的成员是按顺序排列的，C#编译器不会对其进行调整。

        [StructLayout(LayoutKind.Sequential, Pack = 1)]

        internal struct TokPriv1Luid { public int Count; public long Luid; public int Attr; }

        // 以下使用DllImport特性导入了所需的Windows API。 

        // 导入的方法必须是static extern的，并且没有方法体。

        //调用这些方法就相当于调用Windows API。 

        [DllImport("kernel32.dll", ExactSpelling = true)]

        internal static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]

        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]

        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]

        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]

        internal static extern bool ExitWindowsEx(int flg, int rea);

        //C#关机代码 // 以下定义了在调用WinAPI时需要的常数。 //这些常数通常可以从Platform SDK的包含文件（头文件）中找到 

        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;

        internal const int TOKEN_QUERY = 0x00000008;

        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        internal const int EWX_LOGOFF = 0x00000000;

        internal const int EWX_SHUTDOWN = 0x00000001;

        internal const int EWX_REBOOT = 0x00000002;

        internal const int EWX_FORCE = 0x00000004;

        internal const int EWX_POWEROFF = 0x00000008;

        internal const int EWX_FORCEIFHUNG = 0x00000010;
        private void frmMain_Load(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
            SSID = WifiHelper.GetCurrentSSID();
            if(!string.IsNullOrEmpty(SSID))
            {
                _Psswd = WifiHelper.GetWiFiKey(SSID);
            }
            txtLog.AppendText("本机：\r\n");
            txtLog.AppendText("SSID:" + SSID + "\r\n");
            txtLog.AppendText("密码:" + _Psswd + "\r\n");
            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string ip = config.AppSettings.Settings["IP"].Value;
            chromiumWebBrowser1.Load("http://"+ ip + "/");

            txtLog.AppendText("开始监听关机事件……\r\n");
            sSocket = new HttpListener();
            sSocket.Prefixes.Add("http://*:8888/shutwodn/");
            sSocket.Start();
            sSocket.BeginGetContext(new AsyncCallback(GetContextCallBack), sSocket);
            tabControl1.SelectedIndex = 0;
            txtLog.AppendText("切换到打印机页面……\r\n");
        }
        void GetContextCallBack(IAsyncResult ar)
        {
            try
            {
                sSocket = ar.AsyncState as HttpListener;
                HttpListenerContext context = sSocket.EndGetContext(ar);
                sSocket.BeginGetContext(new AsyncCallback(GetContextCallBack), sSocket);
                Console.WriteLine(context.Request.Url.PathAndQuery);
                WriteLogDelegate wl = new WriteLogDelegate(WriteLog);
                txtLog.BeginInvoke(wl, "Http请求:" + context.Request.Url.PathAndQuery);
                //txtLog.AppendText("Http请求:" + context.Request.Url.PathAndQuery + "\r\n");
                txtLog.BeginInvoke(wl, "关机");
                //txtLog.AppendText("关机");
                int delay = 10 * 60 * 1000;
                do
                {
                    Thread.Sleep(1000);
                    txtLog.BeginInvoke(wl, "关机剩余时间"+ delay/1000+"秒……");
                    delay -= 1000;
                } while (delay>0);
                DoExitWin(EWX_SHUTDOWN);
            }
            catch { }

        }

        // 通过调用WinAPI实现关机，主要代码再最后一行ExitWindowsEx  //这调用了同名的WinAPI，正好是关机用的。
        //C#关机代码

        private static void DoExitWin(int flg)
        {
            bool ok;
            TokPriv1Luid tp;
            IntPtr hproc = GetCurrentProcess();
            IntPtr htok = IntPtr.Zero;
            ok = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
            tp.Count = 1;
            tp.Luid = 0;
            tp.Attr = SE_PRIVILEGE_ENABLED;
            ok = LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tp.Luid);
            ok = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            ok = ExitWindowsEx(flg, 0);
        }

        private void WriteLog(string str)
        {
            tabControl1.SelectedIndex = 1;
            txtLog.AppendText(str+"\r\n");
        }


        /// <summary>
        /// Set controller
        /// </summary>
        /// <param name="controller"></param>
        public void SetController(IController controller)
        {
            this.controller = controller;
        }

        /// <summary>
        /// Initialize serial port information
        /// </summary>
        private void InitializeCOMCombox()
        {
            //BaudRate
            baudRateCbx.Items.Add(4800);
            baudRateCbx.Items.Add(9600);
            baudRateCbx.Items.Add(19200);
            baudRateCbx.Items.Add(38400);
            baudRateCbx.Items.Add(57600);
            baudRateCbx.Items.Add(115200);
            baudRateCbx.Items.ToString();
            //get 9600 print in text
            baudRateCbx.Text = baudRateCbx.Items[5].ToString();

            //Data bits
            dataBitsCbx.Items.Add(7);
            dataBitsCbx.Items.Add(8);
            //get the 8bit item print it in the text 
            dataBitsCbx.Text = dataBitsCbx.Items[1].ToString();

            //Stop bits
            stopBitsCbx.Items.Add("One");
            stopBitsCbx.Items.Add("OnePointFive");
            stopBitsCbx.Items.Add("Two");
            //get the One item print in the text
            stopBitsCbx.Text = stopBitsCbx.Items[0].ToString();

            //Parity
            parityCbx.Items.Add("None");
            parityCbx.Items.Add("Even");
            parityCbx.Items.Add("Mark");
            parityCbx.Items.Add("Odd");
            parityCbx.Items.Add("Space");
            //get the first item print in the text
            parityCbx.Text = parityCbx.Items[0].ToString();

            //Handshaking
            handshakingcbx.Items.Add("None");
            handshakingcbx.Items.Add("XOnXOff");
            handshakingcbx.Items.Add("RequestToSend");
            handshakingcbx.Items.Add("RequestToSendXOnXOff");
            handshakingcbx.Text = handshakingcbx.Items[0].ToString();

            //Com Ports
            string[] ArrayComPortsNames = SerialPort.GetPortNames();
            if (ArrayComPortsNames.Length == 0)
            {
                statuslabel.Text = "No COM found !";
                openCloseSpbtn.Enabled = false;
            }
            else
            {
                Array.Sort(ArrayComPortsNames);
                for (int i = 0; i < ArrayComPortsNames.Length; i++)
                {
                    comListCbx.Items.Add(ArrayComPortsNames[i]);
                }
                comListCbx.Text = ArrayComPortsNames[ArrayComPortsNames.Length - 1];
                openCloseSpbtn.Enabled = true;
            }
        }

        /// <summary>
        /// update status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenComEvent(Object sender, SerialPortEventArgs e)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action<Object, SerialPortEventArgs>(OpenComEvent), sender, e);
                return;
            }

            if (e.isOpend)  //Open successfully
            {
                statuslabel.Text = comListCbx.Text + " Opend";
                openCloseSpbtn.Text = "Close";
                sendbtn.Enabled = true;
                autoSendcbx.Enabled = true;
                autoReplyCbx.Enabled = true;

                comListCbx.Enabled = false;
                baudRateCbx.Enabled = false;
                dataBitsCbx.Enabled = false;
                stopBitsCbx.Enabled = false;
                parityCbx.Enabled = false;
                handshakingcbx.Enabled = false;
                refreshbtn.Enabled = false;

                if (autoSendcbx.Checked)
                {
                    autoSendtimer.Start();
                    sendtbx.ReadOnly = true;
                }
            }
            else    //Open failed
            {
                statuslabel.Text = "Open failed !";
                sendbtn.Enabled = false;
                autoSendcbx.Enabled = false;
                autoReplyCbx.Enabled = false;
            }
        }

        /// <summary>
        /// update status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CloseComEvent(Object sender, SerialPortEventArgs e)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action<Object, SerialPortEventArgs>(CloseComEvent), sender, e);
                return;
            }

            if (!e.isOpend) //close successfully
            {
                statuslabel.Text = comListCbx.Text + " Closed";
                openCloseSpbtn.Text = "Open";

                sendbtn.Enabled = false;
                sendtbx.ReadOnly = false;
                autoSendcbx.Enabled = false;
                autoSendtimer.Stop();

                comListCbx.Enabled = true;
                baudRateCbx.Enabled = true;
                dataBitsCbx.Enabled = true;
                stopBitsCbx.Enabled = true;
                parityCbx.Enabled = true;
                handshakingcbx.Enabled = true;
                refreshbtn.Enabled = true;
            }
        }

        /// <summary>
        /// Display received data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComReceiveDataEvent(Object sender, SerialPortEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    Invoke(new Action<Object, SerialPortEventArgs>(ComReceiveDataEvent), sender, e);
                }
                catch (System.Exception)
                {
                    //disable form destroy exception
                }
                return;
            }

            if (recStrRadiobtn.Checked) //display as string
            {
                receivetbx.AppendText(Encoding.Default.GetString(e.receivedBytes));
            }
            else //display as hex
            {
                if (receivetbx.Text.Length > 0)
                {
                    receivetbx.AppendText("-");
                }
                receivetbx.AppendText(IController.Bytes2Hex(e.receivedBytes));
            }
            //update status bar
            receiveBytesCount += e.receivedBytes.Length;
            toolStripStatusRx.Text = "Received: " + receiveBytesCount.ToString();
            isbusy = false;
            //auto reply
            if (autoReplyCbx.Checked)
            {
                sendbtn_Click(this, new EventArgs());
            }

        }

        /// <summary>
        /// Auto scroll in receive textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void receivetbx_TextChanged(object sender, EventArgs e)
        {
            receivetbx.SelectionStart = receivetbx.Text.Length;
            receivetbx.ScrollToCaret();
        }

        /// <summary>
        /// update time in status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void statustimer_Tick(object sender, EventArgs e)
        {
            this.toolStripStatusRx.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }

        /// <summary>
        /// open or close serial port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openCloseSpbtn_Click(object sender, EventArgs e)
        {
            if (openCloseSpbtn.Text == "Open")
            {
                controller.OpenSerialPort(comListCbx.Text, baudRateCbx.Text,
                    dataBitsCbx.Text, stopBitsCbx.Text, parityCbx.Text,
                    handshakingcbx.Text);
            }
            else
            {
                controller.CloseSerialPort();
            }
        }

        /// <summary>
        /// Refresh soft to find Serial port device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshbtn_Click(object sender, EventArgs e)
        {
            comListCbx.Items.Clear();
            //Com Ports
            string[] ArrayComPortsNames = SerialPort.GetPortNames();
            if (ArrayComPortsNames.Length == 0)
            {
                statuslabel.Text = "No COM found !";
                openCloseSpbtn.Enabled = false;
            }
            else
            {
                Array.Sort(ArrayComPortsNames);
                for (int i = 0; i < ArrayComPortsNames.Length; i++)
                {
                    comListCbx.Items.Add(ArrayComPortsNames[i]);
                }
                comListCbx.Text = ArrayComPortsNames[ArrayComPortsNames.Length-1];
                openCloseSpbtn.Enabled = true;
                statuslabel.Text = "OK !";
            }

        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendbtn_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private bool isbusy = false;
        private void SendMessage()
        {
            isbusy = true;
            String sendText = sendtbx.Text + "\r\n";
            bool flag = false;
            //set select index to the end
            sendtbx.SelectionStart = sendtbx.TextLength;

            if (sendHexRadiobtn.Checked)
            {
                //If hex radio checked
                //send bytes to serial port
                Byte[] bytes = IController.Hex2Bytes(sendText);
                sendbtn.Enabled = false; //wait return
                flag = controller.SendDataToCom(bytes);
                sendbtn.Enabled = true;
                sendBytesCount += bytes.Length;
            }
            else
            {
                //send String to serial port
                sendbtn.Enabled = false; //wait return
                flag = controller.SendDataToCom(sendText);
                sendbtn.Enabled = true;
                sendBytesCount += sendText.Length;
            }

            if (flag)
            {
                statuslabel.Text = "Send OK !";
            }
            else
            {
                statuslabel.Text = "Send failed !";
            }

            //update status bar
            toolStripStatusRx.Text = "Sent: " + sendBytesCount.ToString();
            while (isbusy)
            {
                Delay.DelyNotSleep(1000);
            }
        }

        /// <summary>
        /// clear text in send area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearSendbtn_Click(object sender, EventArgs e)
        {
            sendtbx.Text = "";
            toolStripStatusRx.Text = "Sent: 0";
            sendBytesCount = 0;
            addCRCcbx.Checked = false;
        }

        /// <summary>
        /// clear receive text in receive area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearReceivebtn_Click(object sender, EventArgs e)
        {
            receivetbx.Text = "";
            toolStripStatusRx.Text = "Received: 0";
            receiveBytesCount = 0;
        }

        /// <summary>
        /// String to hex
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recHexRadiobtn_CheckedChanged(object sender, EventArgs e)
        {
            if (recHexRadiobtn.Checked)
            {
                if (receivetbx.Text == null)
                {
                    return;
                }
                receivetbx.Text = IController.String2Hex(receivetbx.Text);
            }
        }

        /// <summary>
        /// Hex to string
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recStrRadiobtn_CheckedChanged(object sender, EventArgs e)
        {
            if (recStrRadiobtn.Checked)
            {
                if (receivetbx.Text == null)
                {
                    return;
                }
                receivetbx.Text = IController.Hex2String(receivetbx.Text);
            }
        }

        /// <summary>
        /// String to Hex
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendHexRadiobtn_CheckedChanged(object sender, EventArgs e)
        {
            if (sendHexRadiobtn.Checked)
            {
                if (sendtbx.Text == null)
                {
                    return;
                }
                sendtbx.Text = IController.String2Hex(sendtbx.Text);
                addCRCcbx.Enabled = true;
            }
        }

        /// <summary>
        /// Hex to string
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendStrRadiobtn_CheckedChanged(object sender, EventArgs e)
        {
            if (sendStrRadiobtn.Checked)
            {
                if (sendtbx.Text == null)
                {
                    return;
                }
                sendtbx.Text = IController.Hex2String(sendtbx.Text);
                addCRCcbx.Enabled = false;
            }
        }

        /// <summary>
        /// Filter illegal input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendtbx_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Input Hex, should like: AF-1B-09
            if (sendHexRadiobtn.Checked)
            {
                e.Handled = true;
                int length = sendtbx.SelectionStart;
                switch (length % 3)
                {
                    case 0:
                    case 1:
                        if ((e.KeyChar >= 'a' && e.KeyChar <= 'f')
                            || (e.KeyChar >= 'A' && e.KeyChar <= 'F')
                            || char.IsDigit(e.KeyChar)
                            || (char.IsControl(e.KeyChar) && e.KeyChar != (char)13))
                        {
                            e.Handled = false;
                        }
                        break;
                    case 2:
                        if (e.KeyChar == '-'
                            || (char.IsControl(e.KeyChar) && e.KeyChar != (char)13))
                        {
                            e.Handled = false;
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// Auto send data to serial port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autoSendcbx_CheckedChanged(object sender, EventArgs e)
        {
            if (autoSendcbx.Checked)
            {
                autoSendtimer.Enabled = true;
                autoSendtimer.Interval = int.Parse(sendIntervalTimetbx.Text);
                autoSendtimer.Start();

                //disable send botton and textbox
                sendIntervalTimetbx.Enabled = false;
                sendtbx.ReadOnly = true;
                sendbtn.Enabled = false;
            }
            else
            {
                autoSendtimer.Enabled = false;
                autoSendtimer.Stop();

                //enable send botton and textbox
                sendIntervalTimetbx.Enabled = true;
                sendtbx.ReadOnly = false;
                sendbtn.Enabled = true;
            }
        }

        private void autoSendtimer_Tick(object sender, EventArgs e)
        {
            sendbtn_Click(sender, e);
        }

        /// <summary>
        /// filter illegal input of auto send interval time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendIntervalTimetbx_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar) || e.KeyChar == '\b')
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Add CRC checkbox changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addCRCcbx_CheckedChanged(object sender, EventArgs e)
        {
            String sendText = sendtbx.Text;
            if (sendText == null || sendText == "")
            {
                addCRCcbx.Checked = false;
                return;
            }
            if (addCRCcbx.Checked)
            {
                //Add 2 bytes CRC to the end of the data
                Byte[] senddata = IController.Hex2Bytes(sendText);
                Byte[] crcbytes = BitConverter.GetBytes(CRC16.Compute(senddata));
                sendText += "-" + BitConverter.ToString(crcbytes, 1, 1);
                sendText += "-" + BitConverter.ToString(crcbytes, 0, 1);
            }
            else
            {
                //Delete 2 bytes CRC to the end of the data
                if (sendText.Length >= 6)
                {
                    sendText = sendText.Substring(0, sendText.Length - 6);
                }
            }
            sendtbx.Text = sendText;
        }

        private void btnSendWifi_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
            openCloseSpbtn_Click(null, null);
            //登陆
            sendtbx.Text = "\n";
            SendMessage();
            sendtbx.Text = "pi";
            SendMessage();
            sendtbx.Text = "raspberry";
            SendMessage();
            sendtbx.Text = "sudo rm -f /boot/nanodlp-dhcp.txt";
            SendMessage();
            sendtbx.Text = "sudo bash -c 'echo \"\" > /boot/nanodlp-dhcp.txt'";
            SendMessage(global::RaspberryHelper.Properties.Resources.DHCP1, "/boot/nanodlp-dhcp.txt");
            sendtbx.Text = "sudo bash -c 'echo static ip_address="+txtIP.Text.Trim()+"/24 >> /boot/nanodlp-dhcp.txt'";
            SendMessage();
            sendtbx.Text = "sudo bash -c 'echo static ip6_address=  >> /boot/nanodlp-dhcp.txt'";
            SendMessage();
            sendtbx.Text = "sudo bash -c 'echo static routers=" + txtrouters.Text.Trim()+ " >> /boot/nanodlp-dhcp.txt'";
            SendMessage();
            sendtbx.Text = "sudo bash -c 'echo static domain_name_servers=" + txtDNS.Text.Trim() +
                           " 114.114.114.114 fd51:42f8:caae:d92e::1 >> /boot/nanodlp-dhcp.txt'";
            SendMessage();
            SendMessage(global::RaspberryHelper.Properties.Resources.DHCP2, "/boot/nanodlp-dhcp.txt");


            sendtbx.Text = "sudo rm -f /boot/nanodlp-wifi.txt";
            SendMessage();
            sendtbx.Text = "sudo bash -c 'echo \"\" > /boot/nanodlp-wifi.txt'";
            SendMessage(global::RaspberryHelper.Properties.Resources.WIFI1, "/boot/nanodlp-wifi.txt");
            sendtbx.Text = "sudo bash -c 'echo ssid=\\\""+ SSID + "\\\" >> /boot/nanodlp-wifi.txt'";
            SendMessage();
            sendtbx.Text = "sudo bash -c 'echo psk=\\\"" + _Psswd + "\\\" >> /boot/nanodlp-wifi.txt'";
            SendMessage();
            SendMessage(global::RaspberryHelper.Properties.Resources.WIFI2, "/boot/nanodlp-wifi.txt");
            sendtbx.Text = "sudo reboot";
            SendMessage();

            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //写入<add>元素的Value
            config.AppSettings.Settings["IP"].Value = txtIP.Text.Trim();
            //一定要记得保存，写不带参数的config.Save()也可以
            config.Save(ConfigurationSaveMode.Modified);
            //刷新，否则程序读取的还是之前的值（可能已装入内存）
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");
        }

        private void SendMessage(string str,string fileName)
        {
            var strs = str.Split("\n".ToCharArray());
            foreach (string s in strs)
            {
                var ss = s.Replace("#", "\"#\"");
                sendtbx.Text = "sudo bash -c ' echo " + ss +
                               " >> "+ fileName+"'";
                SendMessage();
            }
        }

    }
}
