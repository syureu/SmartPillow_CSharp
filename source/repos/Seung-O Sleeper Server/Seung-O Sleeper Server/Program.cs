using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Seung_O_Sleeper_Server
{
    class ServerThread
    {
        int portNumber = 6528;
        int maxConnectionNumber = 100;
        Socket socket;
        byte[] buffer;
        IPAddress addr;
        SeungOProtocol sop;

        public ServerThread()
        {
            addr = IPAddress.Parse(new WebClient().DownloadString("https://api.ipify.org/"));
        }

        public void run()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(addr, portNumber));
            socket.Listen(maxConnectionNumber);

            Console.WriteLine("Network part of Seung-O Sleeper is now running!");
            Console.WriteLine("IPAddress = " + addr);
            Console.WriteLine("Port = " + portNumber);

            while (true)
            {
                Socket accepted = socket.Accept();
                Console.WriteLine("소켓하나가 생성되었습니다.");
                buffer = new byte[4096];
                int bytesRead = accepted.Receive(buffer);
                Console.WriteLine("서버 : 클라이언트가 요구사항을 보냈습니다.");
                byte[] formatted = new byte[bytesRead];
                for (int i = 0; i < bytesRead; i++)
                {
                    formatted[i] = buffer[i];
                }
                string json_to_class = Encoding.Default.GetString(formatted);
                JObject jobj = JObject.Parse(json_to_class);

                if (jobj["isSleep"].Value<bool>())
                {
                    Console.WriteLine("서버 : 클라이언트가 수면을 요청했습니다.");
                    SeungOProtocol sop2 = new SeungOProtocol();
                    sop2.okSign = true;
                    byte[] sendbytes = Encoding.Default.GetBytes(sop2.ToJson() + "<EOF>\n");
                    Console.WriteLine("서버 : 클라이언트에게 가능하다는 답을 보냅니다.");
                    accepted.Send(sendbytes);
                    byte[] decode = Convert.FromBase64String(jobj["base64data"].Value<String>());
                    run2(decode[0], decode[1]);
                    Console.WriteLine("서버 : 클라이언트가 요구사항을 보냈습니다.");
                    Console.WriteLine("서버 : 클라이언트가 일어나겠다고 합니다.");
                    Console.WriteLine("서버 : 클라이언트야 일어나라!!");

                }
                else if (jobj["isGetData"].Value<bool>())
                {
                    Console.WriteLine("서버 : 클라이언트가 데이터을 요청했습니다.");
                    SeungOProtocol sop2 = new SeungOProtocol();
                    sop2.okSign = true;
                    string sendData = "";
                    string FolderName = "D:\\time";
                    DirectoryInfo di = new DirectoryInfo(FolderName);
                    foreach (FileInfo File in di.GetFiles())
                    {
                        if (File.Extension.ToLower().CompareTo(".txt") == 0)
                        {
                            string FileNameOnly = File.Name.Substring(0, File.Name.Length - 4);
                            sendData += FileNameOnly;
                        }
                    }
                    sop2.dataID = sendData;
                    byte[] sendbytes = Encoding.Default.GetBytes(sop2.ToJson() + "<EOF>\n");
                    Console.WriteLine("서버 : 클라이언트에게 어느데이터를 받을지 물어봅니다.");
                    accepted.Send(sendbytes);
                    while (true)
                    {
                        bytesRead = accepted.Receive(buffer);
                        Console.WriteLine("서버 : 클라이언트가 뭘 달라고 했는지 얘기했습니다.");
                        for(int i=0; i<bytesRead; i++)
                        {
                            formatted[i] = buffer[i];
                        }
                        json_to_class = Encoding.Default.GetString(formatted);
                        jobj = JObject.Parse(json_to_class);
                        if(jobj["isGetData"].Value<bool>())
                        {
                            SeungOProtocol sop3 = new SeungOProtocol();
                            string filename = jobj["dataID"].Value<string>();
                            // filename open and get string data
                            try
                            {
                                FileStream fs = new FileStream(filename + ".txt", FileMode.Open);
                                StreamReader r = new StreamReader(fs);
                                String sends = "";
                                String buffers;
                                while ((buffers=r.ReadLine())!=null)
                                {
                                    sends += buffers;
                                }
                                r.Close();
                                fs.Close();
                                sop3.base64data = sends;
                            }
                            catch (FileNotFoundException e)
                            {
                                sop3.base64data = "Fail";
                            }
                            sendbytes = Encoding.Default.GetBytes(sop3.ToJson() + "<EOF>");
                            accepted.Send(sendbytes);
                        }
                        else if(jobj["okSign"].Value<bool>())
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void run2(int ahour, int amin)
        {
            SerialPort _serialPort;
            _serialPort = new SerialPort();
            _serialPort.PortName = "COM4";
            _serialPort.BaudRate = 9600;
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            int nowhour = DateTime.Now.Hour;
            int nowminute = DateTime.Now.Minute;
            int nowsecond = DateTime.Now.Second;
            int aday = 0;
            int count = 0;
            int temp = 0;

            string fileName = DateTime.Now.ToString();
            fileName = fileName.Replace(':', '-');
            string X, Y, Z, volts;
            double dx, dy, dz, dvolts;
            byte[] time = new byte[5];
            time[0] = (byte)nowhour;
            time[1] = (byte)nowminute;
            time[2] = (byte)nowsecond;
            time[3] = (byte)ahour;
            time[4] = (byte)amin;
            if (nowhour >= ahour && nowminute > amin)
            {
                aday = DateTime.Now.Day + 1;
            }
            else
                aday = DateTime.Now.Day;

            System.DateTime date1 = new System.DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, nowhour, nowminute, nowsecond);
            System.DateTime date2 = new System.DateTime(DateTime.Now.Year, DateTime.Now.Month, aday, ahour, amin, 0);
            System.TimeSpan diff1 = date2.Subtract(date1);
            double diff2 = 0;
            diff2 = diff1.TotalSeconds;
            Console.WriteLine(diff2);

            int[] count_data = new int[(int)diff2];
            int[] maxValue = new int[(int)diff2];
            double[] volts_data = new double[(int)diff2];
            maxValue[0] = 99;
            Console.WriteLine("시간 : " + nowhour + " 분 : " + nowminute + " 초 : " + nowsecond);
            for (int i = 0; i < 5; i++)
            {
                _serialPort.Write(time, i, 1);
                Console.WriteLine("보낸 값 : " + time[i]);
            }
            Console.WriteLine("아두이노에서 반환된 시간 :" + _serialPort.ReadLine());
            Console.WriteLine("아두이노에서 반환된 부운 :" + _serialPort.ReadLine());
            Console.WriteLine("아두이노에서 반환된 초오 :" + _serialPort.ReadLine());

            string start = _serialPort.ReadLine();
            Console.WriteLine(start);
            string start2 = _serialPort.ReadLine();
            Console.WriteLine(start2);

            for (int i = 0; i < (int)diff2; i++)
            {
                X = _serialPort.ReadLine();
                Console.WriteLine("X = " + X);
                Y = _serialPort.ReadLine();
                Console.WriteLine("Y = " + Y);
                Z = _serialPort.ReadLine();
                Console.WriteLine("Z = " + Z);
                volts = _serialPort.ReadLine();
                Console.WriteLine("volts = " + volts);

                dx = double.Parse(X);
                dy = double.Parse(Y);
                dz = double.Parse(Z);
                dvolts = double.Parse(volts);
                volts_data[i] = dvolts;
                if (i == 0)
                {
                    maxValue[i] = 99;
                    temp++;
                }
                else if (dx >= 1 || dx <= -1 || dy >= 1 && dy <= -1)
                {
                    count++;
                    if (maxValue[i - 1] <= 94)
                    {
                        maxValue[i] = maxValue[i - 1] + 5;
                    }
                    else if (maxValue[i] == 95)
                    {
                        maxValue[i - 1] = maxValue[i - 1] + 4;
                    }
                    else if (maxValue[i - 1] == 96)
                    {
                        maxValue[i] = maxValue[i - 1] + 3;
                    }
                    else if (maxValue[i - 1] == 97)
                    {
                        maxValue[i] = maxValue[i - 1] + 2;
                    }
                    else if (maxValue[i - 1] == 98)
                    {
                        maxValue[i] = maxValue[i - 1] + 1;
                    }
                    else
                    {
                        maxValue[i] = 99;
                    }
                    temp = 0;
                }
                else
                {
                    if (temp >= 5)
                    {//temp = 13; maxValue[i-1] = 21
                        if (maxValue[i - 1] >= 1)
                        {
                            if (maxValue[i - 1] > (temp - 4))
                                maxValue[i] = maxValue[i - 1] - (temp - 4);
                            else
                                maxValue[i] = 0;
                        }
                        else if (maxValue[i - 1] == 0)
                        {
                            maxValue[i] = 0;
                        }
                        temp++;
                    }
                    else
                    {
                        maxValue[i] = maxValue[i - 1];
                        temp++;
                    }
                }
                count_data[i] = count;
                Console.WriteLine("뒤척임 횟수 = " + count);
                Console.WriteLine("카운트 = " + i);
                Console.WriteLine("");

                Thread.Sleep(1000);
            }
            string path = "D:\\time\\" + fileName + ".txt";
            StreamWriter sw = File.CreateText(path);
            sw.WriteLine(date2.ToString());
            for (int i = 0; i < (int)diff2; i++)
            {
                sw.WriteLine(maxValue[i] + " " + volts_data[i]);
            }
            sw.Close();

        }
    }

    class SeungOProtocol
    {
        public bool isSleep { get; set; }
        public bool isWakeup { get; set; }
        public bool isGetData { get; set; }
        public bool isReturnData { get; set; }
        public bool okSign { get; set; }
        public string dataID { get; set; }
        public byte[] data { get; set; }
        public string base64data { get; set; }

        public SeungOProtocol()
        {
            dataID = "";
            base64data = "";
        }

        public SeungOProtocol(string jsonString)
        {
            JObject obj = JObject.Parse(jsonString);
            isSleep = obj["isSleep"].Value<bool>();
            isWakeup = obj["isWakeup"].Value<bool>();
            isGetData = obj["isGetData"].Value<bool>();
            isReturnData = obj["isReturnData"].Value<bool>();
            okSign = obj["okSign"].Value<bool>();
            dataID = obj["dataID"].Value<string>();
            base64data = obj["base64data"].Value<string>();

            data = Convert.FromBase64String(base64data);
        }

        public string ToJson()
        {
            JObject obj = new JObject();
            obj["isSleep"] = isSleep;
            obj["isWakeup"] = isWakeup;
            obj["isGetData"] = isGetData;
            obj["isReturnData"] = isReturnData;
            obj["okSign"] = okSign;
            obj["dataId"] = dataID;
            obj["data"] = data;
            obj["base64data"] = base64data;
            return obj.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ServerThread st = new ServerThread();
            ThreadStart ts = new ThreadStart(st.run);
            Thread t = new Thread(ts);
            t.Start();
            t.Join();
        }
    }
}