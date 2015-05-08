using Common.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

namespace ALOLAsync
{

    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            //QueueStorange bufferQ = new QueueStorange();
            //byte[] size = bufferQ.IntegerToByteAry(62);
            //int iii = bufferQ.ByteAryToInteger(size);
            
            //Console.WriteLine("Byte:" + size.ByteToString(0, 2));
            //string echostr = "我888";// "08080800822000000000000004000000000000000420175111175111301";
            //byte[] utf8 = Encoding.UTF8.GetBytes(echostr);
            //byte[] ascii = Encoding.ASCII.GetBytes(echostr);
            //string ascii2 = Encoding.ASCII.GetString(ascii);
            //char ch = echostr[0];
            //int iii = Convert.ToInt32(ch);
            //int l = echostr.Length;
            //byte[] echo = Encoding.ASCII.GetBytes(echostr);
            //string echo1 = echo[0].ToString();
            //byte[] ss = new byte[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16};
            //byte[] s2 = new byte[]{10,20,30,40,50,60,70,80,90,100};
            
            //byte[] tmpb = bufferQ.GetData(15);
            //string oo = "ff";
            
            //string data = tmpb.ByteToString(3, 5);
            //Console.WriteLine(data);
            //Console.WriteLine(bufferQ.ToString());
            //----------------------------------------------------------
            //Queue<byte> bufferQ = new Queue<byte>(ss.AsEnumerable());
            //new Thread(new ParameterizedThreadStart((object o) => {
            //    Queue<byte> buffer = o as Queue<byte>;
            //    lock (buffer)
            //    {
            //        var s3 = ((ss[0] << 2 * 4) | ss[1]);
            //        int count = 3;
            //        int definelength = 0;
            //        List<byte> bList = new List<byte>(5);
            //        for (int i = 0; i < count; i++)
            //        {
            //            byte tmpB = bufferQ.Dequeue();
            //            bList.Add(tmpB);
            //            int shiftR = tmpB << (8 * (count - (i + 1)));
            //            definelength |= shiftR;
            //            //definelength |= (bufferQ.Peek() << (8 * (count - (i + 1))));
            //        }
            //        Console.WriteLine(definelength);
            //    }
            //})).Start(bufferQ);
            //Thread.Sleep(4);
            //Console.WriteLine("de:" + bufferQ.Dequeue());
            //Console.WriteLine(bufferQ.Count);
            //Console.ReadKey();
            IList<SimpleServer> listSimpleServer = new List<SimpleServer>();
            //Dictionary<string, int> dic = new Dictionary<string, int>()
            //{
            //  { "999", 123 },
            //  { "888", 456 },
            //  { "78877", 789}
            //};
            //if(dic.Keys.Any(str=>str.IndexOf("88") > -1)){
            //    Console.WriteLine("has 88");
            //}
            //var ans = dic.Where(k => k.Key.IndexOf("88") > -1).FirstOrDefault().Value;//.Select(v => v.Value);
            
            //StringBuilder ReceiveStringQueue = new StringBuilder();
            //ReceiveStringQueue.Append("0011");
            //ReceiveStringQueue.Append("00222");
            //ReceiveStringQueue.Append("003333");
            //ReceiveStringQueue.Append("00444");
            //HandleRecieveString(ReceiveStringQueue);
            //ReceiveStringQueue.Append("44005");
            //HandleRecieveString(ReceiveStringQueue);
            //Console.ReadKey();
            //--------------------------------------------------------
            NameValueCollection appSettingKeys = System.Configuration.ConfigurationManager.AppSettings;
            IEnumerable<string> keys = appSettingKeys.AllKeys.Where(k => k.IndexOf("SimpleServer") > -1);//.First();
            foreach (string str in keys)
            {
                string[] setting = appSettingKeys[str].Split(':');
                if (setting.Length != 5) 
                {
                    log.Debug("此key:" + str + "的appSettings(5個)設定錯誤 => parameters:" + setting.Length);
                    continue;
                };
                int listenPort = Convert.ToInt32(setting[0]);
                int maxBacklog = Convert.ToInt32(setting[1]);
                string xmlNodeName = setting[2];
                int sendTimeout = Convert.ToInt32(setting[3]);
                int receiveTimeout = Convert.ToInt32(setting[4]);
                SimpleServer s1 = new SimpleServer(listenPort, maxBacklog, xmlNodeName, sendTimeout, receiveTimeout);
                ThreadPool.QueueUserWorkItem((object o)=>{
                    SimpleServer server = o as SimpleServer;
                    server.Start();},s1);
                listSimpleServer.Add(s1);
            }
            Console.ReadKey();
            //--------------------Old Version-------------------------------------------------
            //SimpleServer s1 = null;
            //ThreadPool.QueueUserWorkItem((object o) => {
            //    s1 = new SimpleServer(6108, 200,"", 6000, 6000);
            //    s1.Start2(); 
            //});
            
            //-------------------------------------------------------------------------------
            //-----------------Component Test------------------------------------------------
            //ALOLAsync a1 = new ALOLAsync("000", "127.0.0.1", 999,  3000, Encoding.ASCII);
            //a1.Start();

            //Console.WriteLine("按任意鍵後開始送出資料...");
            //Console.ReadKey();
            //string key = a1.Send("Qoo1", "hiIambeman", true);
            //a1.Send("Qoo2", "Hello!!!");
            ////Thread.Sleep(15000);
            //Console.WriteLine("按任意鍵後開始委派任務是否完成...");
            //Console.ReadKey();
            //if (a1.IsSuccess(key))
            //{
            //    a1.RevomeKey(key);
            //};
            
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine("確定要結束程序?...y/n");
            }
            while (key.Key != ConsoleKey.Y);
            Console.WriteLine("開始結束程序.......");
            foreach(SimpleServer s in listSimpleServer)
            {
                s.Stop();
            }
        }

        static void HandleRecieveString(StringBuilder ReceiveStringQueue)
        {
            int count = 1;
            int stringLength = 0;
            bool dataDeficiency = false;
            do
            {
                //切頭3碼
                string definedLength = ReceiveStringQueue.ToString(0, 3);
                //轉字串長度
                stringLength = Convert.ToInt32(definedLength) + definedLength.Length;
                if (stringLength > ReceiveStringQueue.Length)
                {
                    Console.WriteLine(@"目前定義長度:" + stringLength + " < 現有字串長度:" + ReceiveStringQueue.Length);
                    dataDeficiency = true;
                    continue;
                }
                //依長度取資料字串
                string msgString = ReceiveStringQueue.ToString(0, stringLength);
                //移除
                ReceiveStringQueue.Remove(0, stringLength);
                Console.WriteLine("第" + count + "段電文:" + " Length:" + stringLength + " \nMsgData:" + msgString);
                //string messageType = msgString.Substring((8 + 3), 4);//3碼:字串長度 + 找Message Type當parse依據
                //TryParseMsg(messageType, msgString);
                count++;
            }
            while (!dataDeficiency);
        }
        static int ch(int i)
        {
            int j = i + 1 ;
            j = j * 2;
            return j;
        }
    }
}
