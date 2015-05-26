using Common.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

namespace ALOLAsync
{
    /// <summary>
    /// 簡易的Single Thread Server
    /// </summary>
    public class SimpleServer : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SimpleServer));

        /// <summary>
        /// 集合存放所有對銀行連結的元件
        /// </summary>
        public IDictionary<string,ALOLAsync> DicALOL { get; set; }

        /// <summary>
        /// 負責接收AP來的socket並取得自動加值(或沖正)請求轉成電文與銀行端交訊
        /// </summary>
        private ALOLAsync SendALOL { get; set; }

        public Socket mainSocket { get; set; }

        public int SendTimeout { get; set; }

        public int ReceiveTimeout { get; set; }

        public string ConnectionsId { get; set; }

        public ManualResetEvent connectDone = new ManualResetEvent(false);

        public readonly int port;
        public int MaxAccept { get; set; }
        public bool keepService { get; set; }
        public SimpleServer(int port, int maxAccept,string id, int sendTimeout = 0, int receiveTimeout = 0)
        {
            this.port = port;
            this.MaxAccept = maxAccept;
            this.ConnectionsId = id;
            this.SendTimeout = sendTimeout;
            this.ReceiveTimeout = receiveTimeout;
            this.DicALOL = new Dictionary<string, ALOLAsync>();
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
        public void Start()
        {
            try
            {
                log.Debug("開始啟動Server");
                this.keepService = true;

                //開始載入Xml連線元件設定
                this.LoadBankConnectionConfig(this.ConnectionsId);
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

                    log.Debug("continue a new BeginAccept");
                }
                while (this.keepService);
                log.Debug("End Start...");
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
                        if (this.SendALOL == null)
                        {
                            throw new Exception("送電文用的Connection不存在");
                        }
                        // AP(Client)   ==socket==>> (Server)this(Client)   ==socket==>> (Server)Bank
                        //            <<==socket==                        <<==socket==
                        string key = this.SendALOL.Send(client);

                        int timeSpend = 0;
                        while (this.mainSocket.ReceiveTimeout >= timeSpend)
                        {
                            log.Debug("ReceiveTimeout(ms):" + this.mainSocket.ReceiveTimeout + "  TimeSpend(ms):" + timeSpend);
                            if (this.SendALOL.IsSuccess(key))
                            {
                                break;
                            }
                            Thread.Sleep(500);
                            timeSpend += 500;
                        }
                        log.Debug("此client結束");
                        this.SendALOL.RevomeKey(key);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("[SimpleServer][ConnectCakllback] Error:" + ex.StackTrace);
            }
        }

        #region Start Method(old Version)
        /// <summary>
        /// (Old Version)Sync Accept
        /// </summary>
        public void Start2()
        {
            try
            {
                log.Debug("開始啟動Server");
                this.keepService = true;

                //開始載入Xml連線元件設定
                this.LoadBankConnectionConfig(this.ConnectionsId);
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
                    
                    using (Socket client = this.mainSocket.Accept())
                    {
                        try
                        {
                            log.Debug("Client from " + client.RemoteEndPoint.ToString());

                            if (this.SendALOL == null)
                            {
                                throw new Exception("Key:Send Connection not exist");
                            }
                            string key = this.SendALOL.Send(client);

                            int timeStop = 0;
                            while (this.mainSocket.ReceiveTimeout >= timeStop)
                            {
                                log.Debug("ReceiveTimeout(ms):" + this.mainSocket.ReceiveTimeout + "  TimeSpend(ms):" + timeStop);
                                if (this.SendALOL.IsSuccess(key))
                                    break;
                                Thread.Sleep(1000);
                                timeStop += 1000;
                            }
                            log.Debug("此client結束");
                            this.SendALOL.RevomeKey(key);
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
        /// (Old Version)從Xml資源檔載入設定元件資料
        /// </summary>
        public void LoadALOLAsyncConfig(string xmlTagName)
        {
            if (this.DicALOL.Count == 0)
            {
                log.Debug("開始讀取Xml設定檔");
                string path = AppDomain.CurrentDomain.BaseDirectory;
                XmlDocument xml = new XmlDocument();
                
                try
                {
                    string fullPath = path + "Config\\BankConfig.xml";
                    log.Debug("Full Path:" + fullPath);
                    xml.Load(fullPath);
                    XmlNodeList nodes = xml.GetElementsByTagName(xmlTagName);
                    foreach (XmlNode node in nodes)
                    {
                        XmlElement xElement = node as XmlElement;
                        XmlAttributeCollection attrCollection = node.Attributes;
                        //Attributes
                        string bankCode = attrCollection.GetNamedItem("BankCode").Value;
                        string messageType = attrCollection.GetNamedItem("MessageType").Value;
                        string sendSocket = attrCollection.GetNamedItem("SendSocket").Value;
                        xElement.HasAttribute("SendSocket");
                        if (this.DicALOL.ContainsKey(messageType))
                        {
                            log.Error("此Key:" + messageType + "已存在於字典檔");
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
                        //this.AlolList.Add((bankCode + messageType), ALOLComponent);
                        log.Debug("AlolList字典集合加入一組連線元件 => key:" + messageType);
                        this.DicALOL.Add(messageType, ALOLComponent);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("讀取XML異常:" + ex.ToString());
                }
            }
        }
        #endregion

        /// <summary>
        /// 從\Config\BankConfig.xml載入連線銀行元件設定資料
        /// </summary>
        /// <param name="id">群組的id</param>
        public void LoadBankConnectionConfig(string id)
        {
            if (this.DicALOL.Count == 0)
            {
                log.Debug("開始讀取Xml設定檔");
                string path = AppDomain.CurrentDomain.BaseDirectory;

                try
                {
                    string fullPath = path + "Config\\BankConfig.xml";
                    log.Debug("Full Path:" + fullPath);
                    XElement xRoot = XElement.Load(fullPath);//load all elements
                    //Enumerable all XElement by the condition
                    IEnumerable<XElement> xElements = xRoot.XPathSelectElements("//Connections[@id='" + id + "']//Connection");
                    foreach (XElement xe in xElements)
                    {
                        string bankCode = xe.Attribute("BankCode") != null ? xe.Attribute("BankCode").Value : "None";
                        string messageType = xe.Attribute("MessageType") != null ? xe.Attribute("MessageType").Value : "None";
                        bool isSendALOL = (xe.Attribute("SendSocket") != null && xe.Attribute("SendSocket").Value.ToUpper() == "YES") ? true : false;
                        string[] Config = xe.Value.Split(':');//取得設定資料127.0.0.1:999:10000:ASCII:6000:180000
                        if (Config.Length != 6)
                        {
                            log.Error("BankCode:" + bankCode + "的Xml設定檔未滿6項");
                            continue;
                        }
                        string ip = Config[0];
                        int port = Convert.ToInt32(Config[1]);
                        int sendTimeout = Convert.ToInt32(Config[2]);
                        Encoding encode = GetEncoding(Config[3]);
                        int connectTimeout = Convert.ToInt32(Config[4]);
                        int echoTimeout = Convert.ToInt32(Config[5]);
                        log.Debug("BankCode:" + bankCode + " MessageType:" + messageType + " IP:" + ip + " port:" + port + " sendTimeout:" + sendTimeout + " Encode:" + Config[3] + " connectTimeout:" + connectTimeout + " echoTimeout:" + echoTimeout);
                        ALOLAsync aLOLComponent = new ALOLAsync(bankCode, messageType, ip, port, sendTimeout, encode, connectTimeout, echoTimeout);

                        log.Debug("DicALOL(Count:" + this.DicALOL.Count +")字典集合加入一組連線元件 => key:" + (bankCode + messageType));
                        if (isSendALOL)
                        {
                            // AP(Client)   ==socket==>> (Server)this(Client)   ==socket==>> (Server)Bank
                            //            <<==socket==                        <<==socket==
                            this.SendALOL = aLOLComponent;
                        }
                        //add
                        this.DicALOL.Add((bankCode + messageType), aLOLComponent);
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
            if (this.DicALOL.Count == 0)
            {
                throw new Exception("字典檔內無連線元件");
            }
            foreach (ALOLAsync bankType in this.DicALOL.Values)
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
                this.SendALOL = null;
                foreach (ALOLAsync bankType in this.DicALOL.Values)
                {
                    bankType.Stop();
                }
                this.DicALOL.Clear();
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
