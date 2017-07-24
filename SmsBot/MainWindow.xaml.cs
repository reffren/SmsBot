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

namespace SmsBot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static SerialPort port;
        static int deleteCount = 9;
        static string messageFromClient;
        static string numPhoneClient;
        Repository repository;


        static Queue<Phone> queueList = null;
        public MainWindow()
        {
            InitializeComponent();
        }


        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            // port = new SerialPort();
            //InitializePort();
            //SendSms();
            LabelSmsInQueue.Content = "12";
            Thread threadRead = new Thread(GetSms);
            threadRead.Start();
            SaveData();
        }



        private void SaveData()
        {
            repository = new Repository();
            while (true)
            {
                Thread.Sleep(1000);
                if (queueList.Count() > 0)
                {
                    Phone phone = queueList.Dequeue();
                    repository.SavePhone(phone);
                    string result = JsonData(phone.Message);
                    SendSms(result);
                    Thread.Sleep(500);
                }
            }
        }

        private string JsonData(string query)
        {
            string dataToServer = "queryFromWPFApp_SmsBot=" + query;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://rzdkaz.ru/smshandler");
                request.Method = "POST";
                byte[] byteArray = Encoding.UTF8.GetBytes(dataToServer);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                var sr = new StreamReader(stream);
                var content = sr.ReadToEnd(); //"{\"train\":\"97\",\"car\":\"2\",\"way\":\"2\",\"section\":\"B\"}";
                response.Close();

                dynamic json = JValue.Parse(content);
                string train = json.Train;
                string car = json.Car;
                string way = json.Way;
                string section = json.Section;

                string message = "Ваш поезд - " + train + ", Вагон - " + car + ", Путь - " + way + ", Секция - " + section;

                return message;
            }
            catch (Exception e)
            {
                return null;
            }

        }
    
        public static void InitializePort()
        {
            port.BaudRate = 9600; // 2400 еще варианты 4800, 9600, 28800 или 56000
            port.DataBits = 8; //7 еще варианты 8, 9

            port.StopBits = StopBits.One; // еще варианты StopBits.Two StopBits.None или StopBits.OnePointFive         
            port.Parity = Parity.None; // еще варианты Parity.Even Parity.Mark Parity.None или Parity.Space

            //   port.ReadTimeout = 500; // самый оптимальный промежуток времени
            //   port.WriteTimeout = 500; // самый оптимальный промежуток времени

            port.Encoding = Encoding.GetEncoding("windows-1251");
            port.PortName = "COM13";

            // незамысловатая конструкция для открытия порта
            //  if (port.IsOpen)
            //      port.Close(); // он мог быть открыт с другими параметрами
        }

        private static void SendSms(string message)
        {
            string telnumber = "89510654443";
            string textSms = message;
            try
            {
                //  port.Open();

                port.WriteLine("AT\r\n"); // означает "Внимание!" для модема 
                System.Threading.Thread.Sleep(500);

                port.Write("AT+CMGF=0\r\n"); // устанавливается цифровой режим PDU для отправки сообщений
                System.Threading.Thread.Sleep(500);

                telnumber = "01" + "00" + telnumber.Length.ToString("X2") + "91" + EncodePhoneNumber(telnumber);
                textSms = StringToUCS2(textSms);
                string leninByte = (textSms.Length / 2).ToString("X2");
                textSms = telnumber + "00" + "0" + "8" + leninByte + textSms;

                double lenMes = textSms.Length / 2; // получаем количество октет в десятичной системе
                port.Write("AT+CMGS=" + (Math.Ceiling(lenMes)).ToString() + "\r\n");
                System.Threading.Thread.Sleep(500);

                textSms = "00" + textSms;

                port.Write(textSms + char.ConvertFromUtf32(26) + "\r\n"); // опять же с комбинацией CTRL-Z на конце
                System.Threading.Thread.Sleep(500);

                port.Write("AT+CMGD=1 \r\n"); // delete first message

                port.Close();
            }
            catch (Exception e) { }
        }


        private void GetSms()
        {
           // MainWindow ew = new MainWindow();
            queueList = new Queue<Phone>();
            int i = 5;
            //Thread threadRead = new Thread(ew.SaveDataDb);
            //threadRead.Start();
            try
            {
                //port.Open();
                //port.WriteLine("AT\r\n"); // означает "Внимание!" для модема 
                //System.Threading.Thread.Sleep(500);

                //port.Write("AT+CMGF=1 \r\n"); // устанавливается текстовый режим для отправки сообщений
                //System.Threading.Thread.Sleep(500);
                int s=0;
                while (true)
                {
                    //port.Write("AT+CMGL=\"REC UNREAD\" \r\n"); // read unread messages
                    Thread.Sleep(100);
                    
                    //ParseData(port.ReadExisting());
                    System.Threading.Thread.Sleep(1000); //TODO after you need delete it
                    ParseData("AT+CMGL=\"REC UNREAD\" \r\r\nOK\r\n");
                    if (i == 5)
                    {
                        ParseData("AT+CMGL=\"REC UNREAD\" \r\r\n+CMGL: 0,\"REC UNREAD\",\"+79510654443-" + s + "\",,\"17/07/15,10:41:22+12\"\r\n97-4\r\n\r\nOK\r\n");
                        i = 0;
                        s++;
                    }
                    i++;


                    if (numPhoneClient != null)
                    {
                        queueList.Enqueue(new Phone() { Date = DateTime.Now.ToString(), NumPhone = numPhoneClient, Message = messageFromClient});

                        numPhoneClient = null;
                        messageFromClient = null;
                        deleteCount++; //count messages in memory of sim-card
                    }

                    
                    if (deleteCount == 10)
                    {
                        port.Write("AT+CMGD=1,1 \r\n"); // delete unread messages
                        deleteCount = 0;
                    }
                }

            }
            catch (Exception e) { }
        }
       

        // encoding sms text to UCS2
        public static string StringToUCS2(string str)
        {
            UnicodeEncoding ue = new UnicodeEncoding();
            byte[] ucs2 = ue.GetBytes(str);

            int i = 0;
            while (i < ucs2.Length)
            {
                byte b = ucs2[i + 1];
                ucs2[i + 1] = ucs2[i];
                ucs2[i] = b;
                i += 2;
            }
            return BitConverter.ToString(ucs2).Replace("-", "");
        }

        // encoding phone number to PDU format
        public static string EncodePhoneNumber(string PhoneNumber)
        {
            string result = "";
            if ((PhoneNumber.Length % 2) > 0) PhoneNumber += "F";

            int i = 0;
            while (i < PhoneNumber.Length)
            {
                result += PhoneNumber[i + 1].ToString() + PhoneNumber[i].ToString();
                i += 2;
            }
            return result.Trim();
        }

        //Parse data from modem
        private static void ParseData(string data)
        {
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
        }
    }
}
