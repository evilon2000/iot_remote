

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualBasic;
using COMDBG.View;

namespace COMDBG
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
    
    public partial class MainForm : Form, IView
    {
        public static MainForm mainForm = null;
        private IController controller;
        private int sendBytesCount = 0;
        private int receiveBytesCount = 0;
        public static OleDbConnection Conn;
        private List<Instruction> instructionSets;
        private List<AtSet> instructionAT;
        AutoSizeFormUtil autoSize = new AutoSizeFormUtil();

        public MainForm()
        {
            InitializeComponent();
            InitializeCOMCombox();
            this.statusTimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            this.toolStripStatusTx.Text = "Sent: 0";
            this.toolStripStatusRx.Text = "Received: 0";
            Conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source = " + AppDomain.CurrentDomain.BaseDirectory + "Instruction.mdb;jet oledb:database password=bove123456");
            GetInstuctionSets();
            mainForm = this;
        }

        private void SetHexInstuction()
        {
            InstuctionSetCbx.DataSource = instructionSets.Select(x => x.指令名称).ToList();
            InstuctionSetCbx.SelectedItem = null;
        }

        private void GetInstuctionSets()
        {
            Conn.Open();
            OleDbDataAdapter da_cont = new OleDbDataAdapter("SELECT * FROM InstructionSet", Conn);
            DataTable ds_cont = new DataTable();
            da_cont.Fill(ds_cont);
            instructionSets = DatatableToList<Instruction>(ds_cont);
            da_cont = new OleDbDataAdapter("SELECT * FROM ATSets", Conn);
            ds_cont = new DataTable();
            da_cont.Fill(ds_cont);
            instructionAT = DatatableToList<AtSet>(ds_cont);

        }
        /// <summary>
        /// 将table转化为实体类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<T> DatatableToList<T>(DataTable table)
        {
            List<T> list = new List<T>();
            T t = default(T);
            PropertyInfo[] properties = null;
            string tempName = string.Empty;
            foreach (DataRow row in table.Rows)
            {
                t = Activator.CreateInstance<T>();
                properties = t.GetType().GetProperties();
                foreach (PropertyInfo pro in properties)
                {
                    tempName = pro.Name;
                    if (table.Columns.Contains(tempName))
                    {
                        if (!pro.CanWrite) continue;
                        object value = row[tempName];
                        if (value != DBNull.Value)
                            pro.SetValue(t, value, null);
                    }
                }
                list.Add(t);
            }
            return list;
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
            baudRateCbx.Items.Add(2400);
            baudRateCbx.Items.Add(4800);
            baudRateCbx.Items.Add(9600);
            baudRateCbx.Items.Add(19200);
            baudRateCbx.Items.Add(38400);
            baudRateCbx.Items.Add(57600);
            baudRateCbx.Items.Add(115200);
            baudRateCbx.Items.ToString();
            //get 9600 print in text
            baudRateCbx.Text = baudRateCbx.Items[0].ToString();

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
            parityCbx.Text = parityCbx.Items[1].ToString();

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
                comListCbx.Text = ArrayComPortsNames[0];
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
                btnMbusSend.Enabled = true;
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
                btnMbusSend.Enabled = false;
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
           
            var hexStr = receivetbx.Text.Length > 0 ? $"{receivetbx.Text}-{IController.Bytes2Hex(e.receivedBytes)}" : $"{IController.Bytes2Hex(e.receivedBytes)}";
            var atStr = GetAtString(hexStr);
            if (sendModBusRadiobtn.Checked)
            {
                var modArr = hexStr.Split('-').ToList();
                if (recHexRadiobtn.Checked)
                {
                    receivetbx.Text = hexStr;
                    return;
                   
                }
                else
                {
                    if (modArr.Count >= 81)
                    {
                        string modStr = ParseMod(modArr);
                        receivetbx.Text = modStr;
                        return;
                    }
                    else
                    {
                        receivetbx.Text = hexStr;
                    }
                    return;
                }
                
            }
            if (recStrRadiobtn.Checked) //display as string
            {
                if (sendATRadioButton.Checked && atStr != null)
                {
                    receivetbx.Text = string.IsNullOrWhiteSpace(atStr) ? hexStr : atStr;
                    return;
                }
                else
                {
                    receivetbx.Text = hexStr;
                }
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
            toolStripStatusRx.Text = "Received: "+receiveBytesCount.ToString();

            //auto reply
            if (autoReplyCbx.Checked)
            {
                sendbtn_Click(this, new EventArgs());
            }

        }
        
        private string ParseMod(List<string> modArr)
        {
            var startIndex = modArr.IndexOf("03") - 1;
            Dictionary<string, UnitRatio> unitDic = new Dictionary<string, UnitRatio>
            {
                { "0B3B", new UnitRatio{ UnitName = "(L/h)" , Ratio = 1} },
                { "0C13", new UnitRatio{ UnitName = "(m3)" , Ratio = 0.001M} },
                { "0C14", new UnitRatio{ UnitName = "(m3)" , Ratio = 0.01M} },
                { "0C15", new UnitRatio{ UnitName = "(m3)" , Ratio = 0.1M} }
            };
            Dictionary<string, string> errorDic = new Dictionary<string, string>()
            {
                { "0000","OK"},
                { "0020","Low Battery"},
                { "0004","Empty Pipe"},
                { "2012","Empty Pipe, Temperature Warning"}
            };
            Dictionary<string, string> baudRateDic = new Dictionary<string, string>
            {
                { "0000","9600bps"},
                { "0001","2400bps"},
                { "0002","4800bps"},
                { "0003","1200bps"},
            };
            var flowRate = Convert.ToInt32($"{modArr[startIndex + 5]}{modArr[startIndex + 6]}{modArr[startIndex + 3]}{modArr[startIndex + 4]}", 16);
            var flowUint = unitDic[$"{modArr[startIndex + 7]}{modArr[startIndex + 8]}"];
            decimal totalizer = (decimal)Convert.ToInt32($"{modArr[startIndex + 17]}{modArr[startIndex + 18]}{modArr[startIndex + 15]}{modArr[startIndex + 16]}", 16);
            var totalizeUint = unitDic[$"{modArr[startIndex + 19]}{modArr[startIndex + 20]}"];
            //var meterState = errorDic.ContainsKey($"{modArr[startIndex + 41]}{modArr[startIndex + 42]}") ? errorDic[$"{modArr[startIndex + 41]}{modArr[startIndex + 42]}"] : "error";//表状态
            //错误示范var meterState = errorDic[$"{modArr[startIndex + 41]}{modArr[startIndex + 42]}"];
            var errorHexLow = Convert.ToInt32($"{modArr[startIndex + 42]}", 16);
            string errorBitLow = Convert.ToString(errorHexLow, 2).PadLeft(8,'0');

            string error2 = "";
            int bitCheck2 = errorBitLow[2] & 1;
            if (bitCheck2 == 1)
            {
                   error2 = "EE Error, ";
            }

            string error3 = "";
            int bitCheck3 = errorBitLow[3]&1; 
            if (bitCheck3 == 1)
            {
                error3 = "Temperature Warning, ";
            }

            string error4 = "";
            int bitCheck4 = errorBitLow[4] & 1;
            if (bitCheck4 == 1)
            {
                error4 = "Over Range, ";
            }

            string error5 = "";
            int bitCheck5 = errorBitLow[5] & 1;
            if (bitCheck5 == 1)
            {
                error5 = "Reverse Flow, ";
            }

            string error6 = "";
            int bitCheck6 = errorBitLow[6] & 1;
            if (bitCheck6 == 1)
            {
                error6 = "Empty Pipe, ";
            }

            string error7 = "";
            int bitCheck7 = errorBitLow[7] & 1;
            if (bitCheck7 == 1)
            {
                error7 = "Low Battery, ";
            }

            //第二通道空管报警，一般不存在1&2通道单独报警，故不启用
            //var errorHexHigh = Convert.ToInt32($"{modArr[startIndex + 41]}", 16);
            //string errorBitHigh = Convert.ToString(errorHexHigh, 2).PadLeft(8, '0');
            //string error13 = "";
            //int bitCheck13 = errorBitHigh[2] & 1;
            //if (bitCheck13 == 1)
            //{
            //    error13 = "Empty Pipe2";
            //}

            string errors;
            if (modArr[startIndex + 42] == "00")
            {
                errors = "normal";
            }
            else
            {
                errors = $"{error2}{error3}{error4}{error5}{error6}{error7}";
                //errors = errors.TrimEnd();
                errors = errors.Substring(0, errors.Length - 2);
            }

            var cumulativeWorkingTime = $"{ Convert.ToInt32($"{modArr[startIndex + 45]}{modArr[startIndex + 46]}{modArr[startIndex + 43]}{modArr[startIndex + 44]}", 16)}h"; //运行时长
            var meterNum = $"{modArr[startIndex + 69]}{modArr[startIndex + 70]}{modArr[startIndex + 67]}{modArr[startIndex + 68]}";
            var meterId = $"{modArr[startIndex + 72]}";
            var baudRate = baudRateDic[$"{modArr[startIndex + 75]}{modArr[startIndex + 76]}"];
            return $"Flow Rate:\r\t{flowRate * flowUint.Ratio}{flowUint.UnitName}\r\nTotalizer:\r\t\r\t{totalizer * totalizeUint.Ratio}{totalizeUint.UnitName}\r\n" +
                $"Error Status:\r\t{errors}\r\nWorking Time:\r\t{cumulativeWorkingTime}\r\nMeter SN:\r\t\r\t{meterNum}\r\nModbus ID:\r\t{meterId}(HEX)\r\nBaud Rate:\r\t{baudRate}";
        }
        /// <summary>
        /// 解析返回数据
        /// </summary>
        /// <param name="hexStr"></param>
        /// <returns></returns>
        private string GetAtString(string hexStr)
        {
            var hexArr = hexStr.Split('-').ToList();
            int countFe = 0;
            for(int i=0; i<hexArr.Count;i++)
            {
                if(hexArr[i].ToUpper() != "FE")
                {
                    countFe = i + 1;
                    break;
                }
            }
            
            if (hexArr.Contains("68") && hexArr.Contains("16") && hexArr.Last().Equals("16") && hexArr.Count > countFe + 12)
            {
                /// 先判断是否是挪威NB-IOT修改间隔的指令
                int nbiot2CheckIndex = hexArr.IndexOf("68") + 9;
                var nbiot2Checktext = $"{hexArr[nbiot2CheckIndex]}{hexArr[nbiot2CheckIndex + 1]}{hexArr[nbiot2CheckIndex + 2]}";
                if (nbiot2Checktext == "A60B0C")
                {
                    int nbiot2intervalIndex = hexArr.IndexOf("68") + 14;
                    var nbiot2IntervalHEX = $"{hexArr[nbiot2intervalIndex + 3]}{hexArr[nbiot2intervalIndex + 2]}{hexArr[nbiot2intervalIndex + 1]}{hexArr[nbiot2intervalIndex]}";
                    var nbiot2IntervalString = (Convert.ToInt32(nbiot2IntervalHEX, 16) + 1).ToString();

                    int nbiot2counterIndex = hexArr.IndexOf("68") + 18;
                    var nbiot2counterHEX = $"{hexArr[nbiot2counterIndex + 3]}{hexArr[nbiot2counterIndex + 2]}{hexArr[nbiot2counterIndex + 1]}{hexArr[nbiot2counterIndex]}";
                    var nbiot2counterString = (Convert.ToInt32(nbiot2counterHEX, 16) + 1).ToString();
                    var nbiot2returnText = $"Interval:\t{nbiot2IntervalString} s\r\nCounter:\t{nbiot2counterString} s";
                    return nbiot2returnText;
                }
                /// 先判断是否是挪威NB-IOT修改间隔的指令

     
                int startDataIndex = hexArr.IndexOf("68") + 10;
                var dataLen = int.Parse(hexArr[startDataIndex],System.Globalization.NumberStyles.HexNumber);
                var lastIndexStopBit = dataLen + startDataIndex + 3;
                if (lastIndexStopBit > hexArr.Count) return null;

                if (hexArr.Count < startDataIndex + 1) return null;
                int dataLength = Convert.ToInt32(hexArr[startDataIndex],16);
                if (hexArr.Count < startDataIndex + dataLength + 1) return null;
                var atStr = "";
                bool start = false;

                var fixBit = 0;
                if (hexArr[hexArr.Count - 3].ToUpper().Equals("0D"))
                {
                    fixBit = 1;
                }

                var endIndex = startDataIndex + dataLength + fixBit;
                for (int i = 0; i < endIndex; i++)
                {
                    if (hexArr[i] == "68")
                    {
                        start = true;
                        i += 13;
                        continue;
                    }
                    //if (hexArr[i] == "16") start = false;
                    if (start)
                    {
                        atStr = string.IsNullOrEmpty(atStr) ? hexArr[i] : $"{atStr}-{hexArr[i]}";
                    }
                }
                //atStr = atStr.Remove(atStr.Length - 6);
                return IController.Hex2String(atStr);
            }
            return null;
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
            this.statusTimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
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
                comListCbx.Text = ArrayComPortsNames[0];
                openCloseSpbtn.Enabled = true;
                statuslabel.Text = "OK !";
            }
            
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void sendbtn_Click(object sender, EventArgs e)
        {
            if (!sendbtn.Enabled)
            {
                MessageBox.Show("Please open com port first");
                return;
            }
            bool flag = false;
            String sendText = "";
            receivetbx.Text = "";
            //write modbus btn
            if (wirteModBusRadiobtn.Checked)
            {
                var defaultAdress = "01";
                var address = Interaction.InputBox("Pleaze input your adress,if you input wrong address,it will use default address(01)", "Title", "01", -1, -1).AddZero(2);
                address = address.AddressChecked() ? address : defaultAdress;
                var sendData = int.Parse(sendtbx.Text).ToString("X").AddZero(8);            
                var tempData = $"{address}100606000204{sendData}";
                var crcString = TrunCrcString(tempData);
                Byte[] bytes = IController.Hex2Bytes(crcString);
                sendbtn.Enabled = false;//wait return
                flag = controller.SendDataToCom(bytes);
                sendbtn.Enabled = true;
                if (flag)
                {
                    statuslabel.Text = "Send OK !";
                }
                else
                {
                    statuslabel.Text = "Send failed !";
                }
                return;
            }
            //Modbus btn
            if (sendModBusRadiobtn.Checked)
            {
                if (string.IsNullOrEmpty(sendtbx.Text))
                {
                    MessageBox.Show("Please enter ModBus ID(HEX)");
                    return;
                }
                if (sendtbx.Text.Length == 1)
                {
                    sendtbx.Text = sendtbx.Text.PadLeft(2, '0');
                }
                receivetbx.Text = "";
                string modId = sendtbx.Text;
                string tempMod = $"{modId}0300010026";
                //sendtbx.Text = sendtbx.Text.Replace("\r", "");
                //sendtbx.Text = sendtbx.Text.Replace("\n", "");
                var crcString = TrunCrcString(tempMod);
                Byte[] bytes = IController.Hex2Bytes(crcString);
                sendbtn.Enabled = false;//wait return
                flag = controller.SendDataToCom(bytes);
                sendbtn.Enabled = true;
                if (flag)
                {
                    statuslabel.Text = "Send OK !";
                }
                else
                {
                    statuslabel.Text = "Send failed !";
                }
                return;
            }
            //AT btn
            if (sendATRadioButton.Checked)
            {
                string packHead = "FEFE";
                string packData = "6820AAAAAAAAAAAAAA22";
                string packZero = "000000";
                string AT = InstuctionSetCbx.Text;
                if (string.IsNullOrWhiteSpace(AT)) return;
                if (!string.IsNullOrWhiteSpace(sendtbx.Text))
                {
                    AT = $"{AT}={sendtbx.Text}";
                }
                if (AT.Contains("SIGFOX-") || AT.Contains("sigfox-"))
                {
                    var tempp = AT.ToUpper().Replace("SIGFOX-", "").Replace("INTERVAL", "interval").Replace("PERIOD", "period");
                    var returnHex = IController.String2Hex(tempp).Replace("-", ""); 
                    packData = "6810AAAAAAAAAAAAAA22";
                    var dataLen = Convert.ToString( $"{ packZero}{returnHex}0D".Length / 2,16).AddZero(2);
                    packData = $"{packData}{dataLen}{packZero}{returnHex}0D";
                    int sum = 0;
                    for (int i = 0; i < packData.Length; i += 2)
                    {
                        sum += int.Parse(packData.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                    }
                    var atSum = Convert.ToString(sum, 16).ToUpper();
                    atSum = atSum.Length <= 2 ? atSum : atSum.Substring(atSum.Length - 2, 2);
                    sendText = $"{packHead}{packData}{atSum}16";
                }
                /// 先判断是否是挪威NB-IOT修改间隔的指令
                else if (AT.Contains("NBIOT2-") || AT.Contains("nbiot2-"))
                {
                    //if (AT.Contains("RDMETER") || AT.Contains("rdmeter"))
                    //{ 
                    //    AT = "AT+interval=period,360";
                    //}
                    if (AT.ToLower().Contains("nbiot2-at+interval,counter=?"))
                    {
                        sendText = "FEFE6810AAAAAAAAAAAAAA26030C00005316";
                    }
                    else
                    {
                        var tempp = AT.Split('=')[1].Split(',');
                        var nbiot2Interval = (int.Parse(tempp[0]) - 1).ToString("X8");
                        var nbiot2counter = (int.Parse(tempp[1]) - 1).ToString("X8");
                        //var nbiot2IntervalHEX111 = IController.String2Hex(nbiot2Interval).Replace("-", "");
                        var nbiot2IntervalHEX = nbiot2Interval.LSBReverse();
                        var nbiot2counterHEX = nbiot2counter.LSBReverse();
                        //var returnHex = IController.String2Hex(tempp).Replace("-", "");
                        packData = "6810AAAAAAAAAAAAAA250B0C0000";
                        //var dataLen = Convert.ToString($"{ packZero}{returnHex}0D".Length / 2, 16).AddZero(2);
                        packData = $"{packData}{nbiot2IntervalHEX}{nbiot2counterHEX}";
                        int sum = 0;
                        for (int i = 0; i < packData.Length; i += 2)
                        {
                            sum += int.Parse(packData.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                        }
                        var atSum = Convert.ToString(sum, 16).ToUpper();
                        atSum = atSum.Length <= 2 ? atSum : atSum.Substring(atSum.Length - 2, 2);
                        sendText = $"{packHead}{packData}{atSum}16";
                    }
                }/// 先判断是否是挪威NB-IOT修改间隔的指令

                else if (AT.Contains("NBIOT-") || AT.Contains("nbiot-"))
                {
                    //if (AT.Contains("RDMETER") || AT.Contains("rdmeter"))
                    //{ 
                    //    AT = "AT+interval=period,360";
                    //}
                    var tempp = AT.ToUpper().Replace("NBIOT-", "").Replace("INTERVAL", "interval").Replace("PERIOD", "period");
                    var returnHex = IController.String2Hex(tempp).Replace("-", "");
                    packData = "6810AAAAAAAAAAAAAA22";
                    var dataLen = Convert.ToString($"{ packZero}{returnHex}0D".Length / 2, 16).AddZero(2);
                    packData = $"{packData}{dataLen}{packZero}{returnHex}0D";
                    int sum = 0;
                    for (int i = 0; i < packData.Length; i += 2)
                    {
                        sum += int.Parse(packData.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                    }
                    var atSum = Convert.ToString(sum, 16).ToUpper();
                    atSum = atSum.Length <= 2 ? atSum : atSum.Substring(atSum.Length - 2, 2);
                    sendText = $"{packHead}{packData}{atSum}16";
                }
                else
                {
                    var returnHex = IController.String2Hex(AT);
                    var atInstructionLen = Convert.ToString(returnHex.Split('-').Length + 3, 16).AddZero(2);
                    var atHex = returnHex.Replace("-", "");
                    packData = $"{packData}{atInstructionLen}{packZero}{atHex}";
                    int sum = 0;
                    for (int i = 0; i < packData.Length; i += 2)
                    {
                        sum += int.Parse(packData.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                    }
                    var atSum = Convert.ToString(sum, 16).ToUpper();
                    atSum = atSum.Length <= 2 ? atSum : atSum.Substring(atSum.Length - 2, 2);
                    sendText = $"{packHead}{packData}{atSum}16";
                }
            }
            else
            {
                sendText = sendtbx.Text.Replace(" ", "");
            }
            //Pulse bt
            if (PulseRadiobtn.Checked)
            {
                //检测输入格式 sample:10-10-99999999-88888888
                try
                {
                    var readingBytes = sendText.ParseToPulseReadingData();
                    var address = Interaction.InputBox("Please type meter ID (14 numbers),if you input wrong meter ID,it will use default meter ID(AAAAAAAAAAAAAA)", "Meter ID", "AAAAAAAAAAAAAA", -1, -1).LSBReverse();
                    //检测地址长度
                    if (address.Length != 14)
                    {
                        MessageBox.Show("Wrong input meter ID,We will user default meter ID (AAAAAAAAAAAAAA)");
                        address = "AAAAAAAAAAAAAA";
                    }
                    var packHead = "FEFEFE";
                    var packData = $"6820{address}250F040100{readingBytes[0]}{readingBytes[1]}{readingBytes[2]}{readingBytes[3]}";

                    int sum = 0;
                    for (int i = 0; i < packData.Length; i += 2)
                    {
                        sum += int.Parse(packData.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                    }
                    var atSum = Convert.ToString(sum, 16).ToUpper();
                    atSum = atSum.Length <= 2 ? atSum : atSum.Substring(atSum.Length - 2, 2);
                    sendText = $"{packHead}{packData}{atSum}16";
                }
                catch(Exception e1)
                {
                    MessageBox.Show("Wrong input format");
                }
            }

            if (sendText == null)
            {
                return;
            }
            //set select index to the end
            sendtbx.SelectionStart = sendtbx.TextLength; 
          
            if (sendHexRadiobtn.Checked || sendATRadioButton.Checked ||PulseRadiobtn.Checked)
            {
                //If hex radio checked
                //send bytes to serial port
                Byte[] bytes = IController.Hex2Bytes(sendText);
                sendbtn.Enabled = false;//wait return
                flag = controller.SendDataToCom(bytes);
                sendbtn.Enabled = true;
                sendBytesCount += bytes.Length;
            }
            else
            {
                //send String to serial port
                sendbtn.Enabled = false;//wait return
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
            toolStripStatusTx.Text = "Sent: " + sendBytesCount.ToString();
        }

        private string TrunCrcString(string tempMod)
        {
            Byte[] senddata = IController.Hex2Bytes(tempMod);
            Byte[] crcbytes = BitConverter.GetBytes(CRC16.Compute(senddata));
            tempMod +=  BitConverter.ToString(crcbytes, 1, 1);
            tempMod +=  BitConverter.ToString(crcbytes, 0, 1);
            return tempMod;
        }

        /// <summary>
        /// clear text in send area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearSendbtn_Click(object sender, EventArgs e)
        {
            sendtbx.Text = "";
            toolStripStatusTx.Text = "Sent: 0";
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
        private void sendATRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (sendATRadioButton.Checked)
            {
                InstuctionSetCbx.DataSource = null;
                InstuctionSetCbx.DataSource = instructionAT.Select(x=>x.ATSet).ToList();
                InstuctionSetCbx.SelectedItem = null;
            }
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
                if (sendATRadioButton.Checked)
                {
                    receivetbx.Text = GetAtString(receivetbx.Text);
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
                InstuctionSetCbx.DataSource = null;
                SetHexInstuction();
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
            if (sendModBusRadiobtn.Checked)
            {
                if (sendtbx.Text == null)
                {
                    return;
                }
                InstuctionSetCbx.DataSource = null;
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
            else
            {
                if (e.KeyChar == (char)13)
                {
                    sendbtn_Click(null,null);


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

        /// <summary>
        /// save received data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void receivedDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.Filter = "txt file|*.txt";
            saveFileDialog.DefaultExt = ".txt";
            saveFileDialog.FileName = "received.txt";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                String fName = saveFileDialog.FileName;
                System.IO.File.WriteAllText(fName, receivetbx.Text);
            }
        }

        /// <summary>
        /// save send data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.Filter = "txt file|*.txt";
            saveFileDialog.DefaultExt = ".txt";
            saveFileDialog.FileName = "send.txt";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                String fName = saveFileDialog.FileName;
                System.IO.File.WriteAllText(fName, sendtbx.Text);
            }
        }

        /// <summary>
        /// Quit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// about me
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.StartPosition = FormStartPosition.CenterParent;
            about.Show();

            if (about.StartPosition == FormStartPosition.CenterParent)
            {
                var x = Location.X + (Width - about.Width) / 2;
                var y = Location.Y + (Height - about.Height) / 2;
                about.Location = new Point(Math.Max(x, 0), Math.Max(y, 0));
            }
        }

        /// <summary>
        /// Help
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HelpForm help = new HelpForm();
            help.StartPosition = FormStartPosition.CenterParent;
            help.Show();

            if (help.StartPosition == FormStartPosition.CenterParent)
            {
                var x = Location.X + (Width - help.Width) / 2;
                var y = Location.Y + (Height - help.Height) / 2;
                help.Location = new Point(Math.Max(x, 0), Math.Max(y, 0));
            }
        }

        private void instToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void InstuctionSetCbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InstuctionSetCbx.SelectedItem != null)
            {
                sendtbx.Text = instructionSets.Where(x => x.指令名称 == InstuctionSetCbx.SelectedItem.ToString()).Select(x => x.指令内容).FirstOrDefault();
            }
            else
            {
                sendtbx.Text = "";
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            autoSize.ControllInitializeSize(this);
        }

        private void sendtbx_TextChanged(object sender, EventArgs e)
        {
            sendtbx.Text = sendtbx.Text.Replace("\r", "");
            sendtbx.Text = sendtbx.Text.Replace("\n", "");
        }

        private void btnMbusSend_Click(object sender, EventArgs e)
        {
            var deviceList = instructionSets.Where(x => x.指令名称.Contains("lock")).OrderBy(x => x.指令名称).ToList();
            controller.StartBatchModle();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            deviceList.ForEach(x =>
            {
                LockDevice(x);
            });
            stopwatch.Stop();
            var dialog = MessageBox.Show("All devices are connected,if you want to go on reading devices,Pleaze enter Y to continue",$"Cost time:{stopwatch.ElapsedMilliseconds}ms",MessageBoxButtons.OKCancel);
            if (dialog == DialogResult.OK)
            {
                var readInstruction = instructionSets.Where(x => x.指令名称 == "read data").First();
                stopwatch.Start();
                var readResult = new Dictionary<string, bool>();
                deviceList.ForEach(x =>
                {
                    LockDevice(x);
                    readResult.Add(x.指令名称,ReadDevice(readInstruction));
                });
                stopwatch.Stop();
                receivetbx.Text = $"Read insturction completed,total time waste:{stopwatch.ElapsedMilliseconds}ms \r\n";
                foreach (var item in readResult)
                {
                    receivetbx.Text += $"{item.Key}:{item.Value.ToString()} \r\n";
                }
                
            }
            //controller.StopBatchModle();
        }

        private bool ReadDevice(Instruction readInstruction)
        {
            if (controller.SendDataToCom(IController.Hex2Bytes(readInstruction.指令内容.Replace(" ", ""))))
            {
                string response = "";
                try
                {
                    response = controller.ReadDataFromCom(203);
                }
                catch (Exception e)
                {
                    response = e.Message;
                }
                if (response.StartsWith("68") && response.EndsWith("16"))
                {
                    return true;
                }
            }
            return false;
        }

        private void LockDevice(Instruction x)
        {
            var result = controller.SendDataToCom(IController.Hex2Bytes(x.指令内容.Replace(" ", "")));
            if (result)
            {
                try
                {
                    var response = controller.ReadDataFromCom(1);
                    if (response != "E5")
                    {
                        MessageBox.Show($"{x.指令名称}:bad response {response}");
                        return;
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show($"{x.指令名称} can't connect,please check:{e1.Message}");
                    return;
                }
            }
            else
            {
                MessageBox.Show($"P{x.指令名称} send faild");
                return;
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            autoSize.ControlAutoSize(this);
        }

        private void PulseRadiobtn_CheckedChanged(object sender, EventArgs e)
        {
            if (PulseRadiobtn.Checked)
            {
                PulseInputForm inputForm = new PulseInputForm(this);
                inputForm.StartPosition = FormStartPosition.Manual;
                inputForm.Location = new Point(mainForm.Location.X + mainForm.Width / 2 - inputForm.Width/2, mainForm.Location.Y + mainForm.Height / 2 - inputForm.Height / 2);
                inputForm.TransfEvent += InputForm_TransfEvent;
                inputForm.ShowDialog();        
            }
        }

        private void InputForm_TransfEvent(string value)
        {
            sendtbx.Text = value;
        }
    }
    public class Instruction
    {
        public string 指令内容 { get; set; }
        public string 指令名称 { get; set; }
    }

    public class AtSet
    {
        public string ATSet { get; set; }
    }
    public struct UnitRatio
    {
        public string UnitName { get; set; }
        public decimal Ratio { get; set; }
    }
}
