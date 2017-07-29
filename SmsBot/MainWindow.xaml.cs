using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmsBot.Data.Concrete;
using SmsBot.Data.Entities;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;

namespace SmsBot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static int deleteCount = 9;
        static string messageFromClient;
        static string numPhoneClient;
        static string phoneForSending;
        static string message = null;
        static bool flag = true;
        public int LabelSmsInQueueCounter = 0;
        public int LabelSmsSendedCounter = 0;
        public string LabelPercentCounter = "0%";

        static Queue<Phone> queueList = null;

        static SerialPort port;
        Thread threadGetSendSms;
        Thread threadHandleSms;
        DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();
            comPort.Text = "0";
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 2);
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            port = new SerialPort();

            int p = Int32.Parse(comPort.Text);
            if (p > 0)
            {
                InitializePort(comPort.Text);
                comPort.IsEnabled = false;
                threadGetSendSms = new Thread(GetSendSms);
                threadHandleSms = new Thread(HandleSms);
                threadGetSendSms.Start();
                threadHandleSms.Start();
                StartBtn.IsEnabled = false;
                dispatcherTimer.Start();
            }
            else
            {

            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            LabelSmsInQueue.Content = LabelSmsInQueueCounter;
            LabelPercent.Content = LabelPercentCounter;
            LabelSmsSended.Content = LabelSmsSendedCounter;
        }

        private static void InitializePort(string comPort)
        {
            port.BaudRate = 9600;
            port.DataBits = 8;

            port.StopBits = StopBits.One;
            port.Parity = Parity.None;

            port.Encoding = Encoding.GetEncoding("windows-1251");
            port.PortName = "COM" + comPort;

            try
            {
                port.Open();
                port.WriteLine("AT\r\n"); // означает "Внимание!" для модема 
                System.Threading.Thread.Sleep(500);
            }
            catch (Exception e) { }
        }

        private void GetSendSms()
        {
            queueList = new Queue<Phone>();

            try
            {
                port.Write("AT+CMGF=1 \r\n"); // устанавливается текстовый режим для отправки сообщений
                System.Threading.Thread.Sleep(100);

                while (true)
                {
                    port.Write("AT+CMGL=\"REC UNREAD\" \r\n"); // read unread messages
                    Thread.Sleep(100);

                    string data = port.ReadExisting();



                    // recieve phone number from data

                    string searchPhone = "+7";
                    int textBeforePhone = data.IndexOf(searchPhone); //text before +7

                    if (textBeforePhone != -1)
                    {
                        string dataPhoneEdited = data.Substring(textBeforePhone + searchPhone.Length); //delete the text before +7

                        int textAfterPhone = dataPhoneEdited.IndexOf("\"" + ",,\""); //text after \",,\"
                        if (textAfterPhone != -1)
                        {
                            numPhoneClient = "8" + dataPhoneEdited.Substring(0, textAfterPhone); //delete the text after \",,\" and get phone number
                        }
                    }

                    // recieve text message

                    string searchMsg = "\"" + "\r\n";
                    int textBeforeMsg = data.IndexOf(searchMsg); //text before \"\r\n      \r\n\r\n  

                    if (textBeforeMsg != -1)
                    {
                        string dataMsgEdited = data.Substring(textBeforeMsg + searchMsg.Length); //delete the text before \"\r\n

                        int textAfterMsg = dataMsgEdited.IndexOf("\r\n\r\n"); //text after \r\n\r\n 
                        if (textAfterMsg != -1)
                        {
                            messageFromClient = dataMsgEdited.Substring(0, textAfterMsg); //delete the text after \r\n\r\n  and get message
                        }
                    }

                    if (numPhoneClient != null)
                    {
                        queueList.Enqueue(new Phone() { Date = DateTime.Now.ToString(), NumPhone = numPhoneClient, Message = messageFromClient });

                        LabelPercentCounter = "10%";
                        numPhoneClient = null;
                        messageFromClient = null;
                        deleteCount++; //count messages in memory of sim-card
                    }

                    if (deleteCount == 10)
                    {
                        port.Write("AT+CMGD=1,1 \r\n"); // delete unread messages
                        deleteCount = 0;
                    }

                    if (message != null)
                    {
                        string telNumber = phoneForSending;
                        string textSms = message;

                        port.Write("AT+CMGF=0\r\n"); // устанавливается цифровой режим PDU для отправки сообщений
                        System.Threading.Thread.Sleep(500);
                        LabelPercentCounter = "80%";
                        // encoding phone number to PDU format
                        string result = "";
                        if ((telNumber.Length % 2) > 0) telNumber += "F";

                        int s = 0;
                        while (s < telNumber.Length)
                        {
                            result += telNumber[s + 1].ToString() + telNumber[s].ToString();
                            s += 2;
                        }

                        telNumber = "01" + "00" + telNumber.Length.ToString("X2") + "91" + result.Trim();

                        // encoding sms text to UCS2
                        UnicodeEncoding ue = new UnicodeEncoding();
                        byte[] ucs2 = ue.GetBytes(textSms);

                        int a = 0;
                        while (a < ucs2.Length)
                        {
                            byte b = ucs2[a + 1];
                            ucs2[a + 1] = ucs2[a];
                            ucs2[a] = b;
                            a += 2;
                        }
                        textSms = BitConverter.ToString(ucs2).Replace("-", "");

                        string leninByte = (textSms.Length / 2).ToString("X2");
                        textSms = telNumber + "00" + "0" + "8" + leninByte + textSms;

                        double lenMes = textSms.Length / 2; // получаем количество октет в десятичной системе

                        port.Write("AT+CMGS=" + (Math.Ceiling(lenMes)).ToString() + "\r\n");
                        System.Threading.Thread.Sleep(500);

                        textSms = "00" + textSms;
                        LabelPercentCounter = "90%";

                        port.Write(textSms + char.ConvertFromUtf32(26) + "\r\n"); // опять же с комбинацией CTRL-Z на конце
                        System.Threading.Thread.Sleep(5000);
                        LabelPercentCounter = "100%";

                        port.Write("AT+CMGF=1 \r\n"); // устанавливается текстовый режим для отправки сообщений
                        System.Threading.Thread.Sleep(500);

                        flag = true;
                        message = null;
                        phoneForSending = null;
                        LabelSmsSendedCounter++;
                        LabelPercentCounter = "0%";
                    }
                }
            }
            catch (Exception e) { }
        }

        private void HandleSms()
        {
            EFContext context = new EFContext();
            while (true)
            {

                if (queueList.Count() > 0 && flag)
                {
                    flag = false;
                    LabelSmsInQueueCounter++;
                    LabelPercentCounter = "20%";
                    Phone phone = queueList.Dequeue();
                    LabelPercentCounter = "30%";
                    phoneForSending = phone.NumPhone;
                    context.Phones.Add(phone);//10c  
                    context.SaveChangesAsync();
                    LabelPercentCounter = "40%";

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://rzdkaz.ru/smshandler");
                        request.Method = "POST";
                        byte[] byteArray = Encoding.UTF8.GetBytes("queryFromWPFApp_SmsBot=" + phone.Message);
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = byteArray.Length;
                        Stream dataStream = request.GetRequestStream();
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        dataStream.Close();
                        LabelPercentCounter = "50%";
                        var response = request.GetResponse();
                        var stream = response.GetResponseStream();
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd(); //"{\"train\":\"97\",\"car\":\"2\",\"way\":\"2\",\"section\":\"B\"}";
                        response.Close();
                        LabelPercentCounter = "60%";
                        dynamic json = JValue.Parse(content);
                        string train = json.Train;
                        string car = json.Car;
                        string way = json.Way;
                        string section = json.Section;
                        LabelPercentCounter = "70%";
                        message = "Ваш поезд - " + train + ", Вагон - " + car + ", Путь - " + way + ", Секция - " + section;
                    }
                    catch (Exception e) { }
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
