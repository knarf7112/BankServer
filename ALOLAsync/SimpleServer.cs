using Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace ALOLAsync
{
    /// <summary>
    /// 簡易的Single Thread Server
    /// </summary>
    public class SimpleServer : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SimpleServer));
        //public ALOLAsync ALOLComponent { get; set; }

        //public ALOLAsync ALOLComponent2 { get; set; }

        //public ALOLAsync ALOLComponent3 { get; set; }

        public IDictionary<string,ALOLAsync> AlolList { get; set; }

        public Socket mainSocket { get; set; }

        public int SendTimeout { get; set; }

        public int ReceiveTimeout { get; set; }

        public ManualResetEvent connectDone = new ManualResetEvent(false);

        public readonly int port;
        public int MaxAccept { get; set; }
        public bool keepService { get; set; }
        public SimpleServer(int port, int maxAccept, int sendTimeout = 0, int receiveTimeout = 0)
        {
            this.port = port;
            this.MaxAccept = maxAccept;
            this.SendTimeout = sendTimeout;
            this.ReceiveTimeout = receiveTimeout;
            this.AlolList = new Dictionary<string, ALOLAsync>();
            //銀行連接者物件 "000":BankCode  "10.27.68.161":BankIP  58002:BankPort
            //this.ALOLComponent = new ALOLAsync("000", "10.27.68.161", 58002, 10000, Encoding.ASCII);
            //玉山
            //this.ALOLComponent = new ALOLAsync("0808", "0100+0420", "60.199.64.90", 9310, 5000, Encoding.ASCII, 6000, 180000);
            //this.ALOLComponent2 = new ALOLAsync("0808", "0120", "60.199.64.90", 9311, 5000, Encoding.ASCII, 6000, 180000);
            //this.ALOLComponent3 = new ALOLAsync("0808", "0302", "60.199.64.90", 9312, 5000, Encoding.ASCII, 6000, 180000);
        }

        /// <summary>
        /// Async Accept
        /// </summary>
        public void Start2()
        {
            try
            {
                log.Debug("開始啟動Server");
                this.keepService = true;

                //開始載入Xml連線元件設定
                this.LoadALOLAsyncConfig();
                //開始連線銀行端
                this.StartConnections();
                
                this.mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = this.SendTimeout,
                    ReceiveTimeout = this.ReceiveTimeout,
                };
                //綁定關連
                this.mainSocket.Bind(new IPEndPoint(IPAddress.Any, this.port));
                //排隊人數
                this.mainSocket.Listen(this.MaxAccept);
                //log.Debug("Bigen Listen...");
                do
                {
                    log.Debug("wait connection...");
                    connectDone.Reset();
                    this.mainSocket.BeginAccept(ConnectCakllback, this.mainSocket);
                    connectDone.WaitOne();
                        //try
                        //{
                        //    log.Debug("Client from " + this.mainSocket.RemoteEndPoint.ToString());
                            
                            //if (!this.AlolList.ContainsKey("08080100+0420"))
                            //{
                            //    throw new Exception("送0100+0420電文用的Connection不存在");
                            //}
                            //string key = this.AlolList["08080100+0420"].Send(client);
                            ////string key = this.ALOLComponent.Send(client);
                            ////string key = this.ALOLComponent.Send("Qoo1", "Task..........................1", true);

                            //int timeStop = 0;
                            //while (this.mainSocket.ReceiveTimeout >= timeStop)
                            //{
                            //    log.Debug("ReceiveTimeout:" + this.mainSocket.ReceiveTimeout + "  timeStop:" + timeStop);
                            //    if (this.AlolList["08080100+0420"].IsSuccess(key))
                            //        break;
                            //    Thread.Sleep(1000);
                            //    timeStop += 1000;
                            //}
                            //log.Debug("此client結束");
                            //this.AlolList["08080100+0420"].RevomeKey(key);
                        //}
                        //catch (Exception ex)
                        //{
                        //    log.Error("Ex:" + ex.StackTrace);
                        //}
                    
                    log.Debug("結束上個client後,開始一個新的等待");
                }
                while (this.keepService);
            }
            catch (Exception ex)
            {
                log.Debug("[SimpleServer][Start] Error:" + ex.ToString());
            }
        }

        private void ConnectCakllback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    Socket mainSck = (Socket)ar.AsyncState;
                    using (Socket client = mainSck.EndAccept(ar))
                    {
                        connectDone.Set();
                        if (!this.AlolList.ContainsKey("08080120"))
                        {
                            throw new Exception("送08080120電文用的Connection不存在");
                        }
                        string key = this.AlolList["08080120"].Send(client);
                        //string key = this.ALOLComponent.Send(client);
                        //string key = this.AlolList["08080100+0420"].Send("Qoo1", "Task2..........................1", true);

                        int timeStop = 0;
                        while (this.mainSocket.ReceiveTimeout >= timeStop)
                        {
                            log.Debug("ReceiveTimeout:" + this.mainSocket.ReceiveTimeout + "  timeStop:" + timeStop);
                            if (this.AlolList["08080120"].IsSuccess(key))
                            {
                                break;
                            }
                            Thread.Sleep(500);
                            timeStop += 500;
                        }
                        log.Debug("此client結束");
                        this.AlolList["08080120"].RevomeKey(key);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("[SimpleServer][ConnectCakllback] Error:" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Sync Accept
        /// </summary>
        public void Start()
        {
            try
            {
                log.Debug("開始啟動Server");
                this.keepService = true;

                //開始載入Xml連線元件設定
                this.LoadALOLAsyncConfig();
                //開始連線銀行端
                this.StartConnections();
                //this.ALOLComponent.Start();
                //this.ALOLComponent2.Start();
                //this.ALOLComponent3.Start();
                
                this.mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = this.SendTimeout,
                    ReceiveTimeout = this.ReceiveTimeout,
                };
                //綁定關連
                this.mainSocket.Bind(new IPEndPoint(IPAddress.Any, this.port));
                //排隊人數
                this.mainSocket.Listen(this.MaxAccept);
                //log.Debug("Bigen Listen...");
                do
                {
                    log.Debug("wait connection...");
                    
                    using (Socket client = this.mainSocket.Accept())
                    {
                        try
                        {
                            log.Debug("Client from " + client.RemoteEndPoint.ToString());

                            if (!this.AlolList.ContainsKey("08080100+0420"))
                            {
                                throw new Exception("Key:08080100+0420");
                            }
                            string key = this.AlolList["08080100+0420"].Send(client);
                            //string key = this.ALOLComponent.Send(client);
                            //string key = this.ALOLComponent.Send("Qoo1", "Task..........................1", true);

                            int timeStop = 0;
                            while (this.mainSocket.ReceiveTimeout >= timeStop)
                            {
                                log.Debug("ReceiveTimeout:" + this.mainSocket.ReceiveTimeout + "  timeStop:" + timeStop);
                                if (this.AlolList["08080100+0420"].IsSuccess(key))
                                    break;
                                Thread.Sleep(1000);
                                timeStop += 1000;
                            }
                            log.Debug("此client結束");
                            this.AlolList["08080100+0420"].RevomeKey(key);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Ex:" + ex.StackTrace);
                        }
                    }
                    log.Debug("結束上個client後,開始一個新的等待");
                }
                while (this.keepService);
            }
            catch (Exception ex)
            {
                log.Debug("[SimpleServer][Start] Error:" + ex.ToString());
            }
        }
        /// <summary>
        /// 從Xml資源檔載入設定元件資料
        /// </summary>
        public void LoadALOLAsyncConfig()
        {
            if (this.AlolList.Count == 0)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                log.Debug("Path:" + path);
                XmlDocument xml = new XmlDocument();
                try
                {
                    xml.Load(path + "Config\\BankConfig.xml");
                    XmlNodeList nodes = xml.GetElementsByTagName("Connection");
                    foreach (XmlNode node in nodes)
                    {
                        XmlAttributeCollection attrCollection = node.Attributes;
                        //Attributes
                        string bankCode = attrCollection.GetNamedItem("BankCode").Value;
                        string messageType = attrCollection.GetNamedItem("MessageType").Value;

                        if (this.AlolList.ContainsKey((bankCode + messageType)))
                        {
                            log.Error("此Key:" + (bankCode + messageType) + "已存在於字典檔");
                            continue;
                        }
                        //var s2 = node.Attributes.Item(1).Value;

                        //InnerXml Data
                        string[] Config = node.InnerXml.Split(':');//取得設定資料127.0.0.1:999:10000:ASCII:6000:180000
                        string ip = Config[0];
                        int port = Convert.ToInt32(Config[1]);
                        int sendTimeout = Convert.ToInt32(Config[2]);
                        Encoding encode = GetEncoding(Config[3]);
                        int connectTimeout = Convert.ToInt32(Config[4]);
                        int echoTimeout = Convert.ToInt32(Config[5]);
                        log.Debug("IP:" + ip + " port:" + port + " sendTimeout:" + sendTimeout + " Encode:" + Config[3] + " connectTimeout:" + connectTimeout + " echoTimeout:" + echoTimeout);
                        ALOLAsync ALOLComponent = new ALOLAsync(bankCode, messageType, ip, port, sendTimeout, encode, connectTimeout, echoTimeout);
                        //add 
                        this.AlolList.Add((bankCode + messageType), ALOLComponent);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("讀取XML異常:" + ex.ToString());
                }
            }
        }

        /// <summary>
        /// 開始用字典檔內Component連線銀行
        /// </summary>
        public void StartConnections()
        {
            if (this.AlolList.Count == 0)
            {
                throw new Exception("字典檔內無連線元件");
            }
            foreach (ALOLAsync bankType in this.AlolList.Values)
            {
                bankType.Start();
            }
        }

       /// <summary>
       /// 選擇編碼設定
       /// </summary>
       /// <param name="encodeName"></param>
       /// <returns></returns>
        private Encoding GetEncoding(string encodeName)
        {
            switch (encodeName)
            {
                case "ASCII":
                    return Encoding.ASCII;
                case "UTF8":
                    return Encoding.UTF8;
                case "Unicode":
                    return Encoding.Unicode;
                case "BIG5":
                    return Encoding.GetEncoding("big5");
                case "":
                    return Encoding.Default;
                default:
                    throw new Exception("Encoding not setting");
            }
        }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            try
            {
                this.keepService = false;
                if (this.mainSocket != null)
                {
                    this.mainSocket.Shutdown(SocketShutdown.Both);
                    this.mainSocket.Close();
                }
                //釋放元件資源
                foreach (ALOLAsync bankType in this.AlolList.Values)
                {
                    bankType.Stop();
                }
                this.AlolList.Clear();
                //this.ALOLComponent.Stop();
                //this.ALOLComponent2.Stop();
                //this.ALOLComponent3.Stop();
            }
            catch (Exception ex)
            {
                log.Debug("[SimpleServer][Dispose] Error:" + ex.ToString());
            }
            finally
            {
                //this.ALOLComponent = null;
                //this.ALOLComponent2 = null;
                //this.ALOLComponent3 = null;
                this.mainSocket = null;
            }
        }
    }

}
