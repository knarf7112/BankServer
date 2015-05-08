using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
//
using AsyncConnection;
using Newtonsoft.Json;
using OL_Autoload_Lib;
using System.IO;
using System.Xml;
using System.Timers;
using System.Threading;
using Common.Logging;
using System.Linq;

namespace ALOLAsync
{
    /// <summary>
    /// 簡易接收的緩存物件
    /// </summary>
    public class ReceiveState : IReceive
    {
        public const int ReceiveSize = 0x1000;
        private byte[] _ReceiveBuffer = new byte[ReceiveSize];
        public byte[] ReceiveBuffer
        {
            get
            {
                return _ReceiveBuffer;
            }
            set
            {
                this._ReceiveBuffer = value;
            }
        }

        public Socket workSocket { get; set; }
    }

    /// <summary>
    /// 接收到資料委派的處理方法類型(測試用)
    /// </summary>
    /// <param name="value">字串</param>
    /// <returns>處理成功/處理失敗</returns>
    public delegate bool ReceiveAction2(string value);

    /// <summary>
    /// 接收到資料後委派的處理方法類型
    /// </summary>
    /// <param name="value">注入的Socket物件</param>
    /// <param name="apSocket">注入的物件</param>
    /// <returns></returns>
    public delegate bool ReceiveAction(object value,Socket apSocket);

    /// <summary>
    /// Msg Context(任務緩存狀態物件)
    /// </summary>
    public class MsgHandle : IState
    {
        #region Field
        private string _id = null;
        private string _sendString = null;
        public const int ReceiveSize = 0x1000;
        private string _receivestring = null;
        private byte[] _sendBuffer = null;
        private byte[] _receiveBuffer = new byte[ReceiveSize];
        private object _receiveObject = null;
        #endregion

        #region Property
        /// <summary>
        /// 需確定編碼來轉換陣列
        /// </summary>
        public Encoding Encoding { get; set; }
        /// <summary>
        /// 此記錄物件的ID
        /// </summary>
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                this._id = value;
            }
        }
        /// <summary>
        /// 送出的字串(自動用編碼轉成Byte[])
        /// </summary>
        public string SendString
        {
            get
            {
                return _sendString;
            }
            set
            {
                try
                {
                    this.SendBuffer = this.Encoding.GetBytes(value);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                this._sendString = value;
            }
        }
        /// <summary>
        /// 接收資料後是否完成任務
        /// </summary>
        public bool isSuccess { get; set; }
        /// <summary>
        /// 接收到的字串(加入event調用外部註冊方法,並回傳true/false到isSuccess屬性上)
        /// </summary>
        public string ReceiveString
        {
            get
            {
                return _receivestring;
            }
            set
            {
                if (OnReceiveAction2 != null)
                {
                    //測試用
                    this.isSuccess = OnReceiveAction2.Invoke(value);
                }
                this._receivestring = value;
            }
        }

        /// <summary>
        /// 接收到的資料物件(加入event調用外部註冊方法,並回傳true/false到isSuccess屬性上)
        /// </summary>
        public object ReceiveObject 
        {
            get
            {
                return _receiveObject;
            }
            set
            {
                if (OnReceiveAction != null && this.APSocket != null)
                {
                    //接收到的物件調用委派方法並回傳任務成功或失敗給此物件屬性
                    this.isSuccess = OnReceiveAction.Invoke(value, this.APSocket);
                }
                this._receiveObject = value;
            } 
        }

        /// <summary>
        /// 送出的資料陣列
        /// </summary>
        public byte[] SendBuffer
        {
            get
            {
                return _sendBuffer;
            }
            set
            {
                this._sendBuffer = value;
            }
        }

        /// <summary>
        /// 接收的緩存
        /// </summary>
        public byte[] ReceiveBuffer
        {
            get
            {
                return _receiveBuffer;
            }
            set
            {
                this._receiveBuffer = value;
            }
        }

        /// <summary>
        /// 跟銀行連線的Socket
        /// </summary>
        public Socket workSocket { get; set; }

        /// <summary>
        /// AP來的Socket
        /// </summary>
        public Socket APSocket { get; set; }
        #endregion

        #region Delegate 
        /// <summary>
        /// 收到資料的處理事件(string)
        /// </summary>
        public event ReceiveAction2 OnReceiveAction2;
        /// <summary>
        /// 收到資料的處理事件(string + apSocket)或(object + apSocket)~自己轉型吧
        /// </summary>
        public event ReceiveAction OnReceiveAction;
        #endregion
    }

    /// <summary>
    /// 銀行端連接者(Connecter)
    /// </summary>
    public class ALOLAsync
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ALOLAsync));

        #region Field
        /// <summary>
        /// connect component
        /// </summary>
        public AsyncConnect asyncConnect = null;
        /// <summary>
        /// ISO8583 Parser for 聯名卡專用
        /// </summary>
        private ISO8583Parser ALOLISO8583Parser = null;
        /// <summary>
        /// lock object
        /// </summary>
        private object lockObj = new object();
        #endregion

        #region Property
        private QueueStorange ReceiveBufferQ { get; set; }

        public Encoding Encode { get; set; }

        private StringBuilder ReceiveBufferString { get; set; }
 
        private System.Timers.Timer timer { get; set; }

        /// <summary>
        /// Echo test Timer
        /// </summary>
        private System.Timers.Timer EchoTimer { get; set; }

        //是否重新初始化物件並啟動連線
        private bool IsReInitialObject { get; set; }

        //是否已開始連線
        private bool Started { get; set; }

        public string BankCode { get; set; }

        public string MessageType { get; set; }

        private ManualResetEvent WaitStartdone { get; set; }

        public IDictionary<string, MsgHandle> dicMsghandles { get; set; }
        private IDictionary<string, string> dicReqToRspMsgType { get; set; }

        private IDictionary<string, string> dicServerConfig { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 自動加值連線銀行端元件
        /// </summary>
        /// <param name="bankCode">銀行代碼-組Echo時會用到</param>
        /// <param name="messageType">電文MessageType(暫時沒用)</param>
        /// <param name="ip">遠端IP</param>
        /// <param name="port">遠端Port</param>
        /// <param name="sendTimeout">送出資料的逾時</param>
        /// <param name="connectTimeout">連線逾時(暫時沒用-非同步用)</param>
        /// <param name="encoding">編碼格式</param>
        /// <param name="connectTimerInterval">啟動連線間隔時間(預設:6秒)</param>
        /// <param name="echoTimerInterval">發Echo間隔時間(預設:180秒)</param>
        public ALOLAsync(string bankCode,string messageType,string ip, int port,int sendTimeout, Encoding encoding,double connectTimerInterval = 6000,double echoTimerInterval = 180000)
        {
            this.BankCode = bankCode;//
            this.MessageType = messageType;
            this.Encode = encoding;
            //Receive Buffer 用來接收Socket receive到的資料,存放接收到資料的物件
            this.ReceiveBufferString = new StringBuilder();
            this.ReceiveBufferQ = new QueueStorange();
            //
            this.asyncConnect = new AsyncConnect(ip, port, sendTimeout);
            //init timer
            //啟動連線的太碼
            this.timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = connectTimerInterval
            };
            //啟動Echo的太碼
            this.EchoTimer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = echoTimerInterval
            };
            //是否重新初始化AsyncConnect元件
            this.IsReInitialObject = false;
            this.Started = false;
            //等待啟動連線用的鎖
            this.WaitStartdone = new ManualResetEvent(false);

            //註冊計時器
            this.timer.Elapsed += timer_Elapsed;
            this.EchoTimer.Elapsed += EchoTimer_Elapsed;
            //註冊非同步送出後的Callback事件要呼叫的方法(委派的任務)
            this.asyncConnect.OnSendAsyncCallback += SendCallback;
            //註冊非同步接收後的Callback事件要呼叫的方法(委派的任務)
            this.asyncConnect.OnReceiveAsyncCallback += ReceiveCallback;

            //init ISO8583 Parser
            this.ALOLISO8583Parser = new ISO8583Parser();

            //init Msg handler dic
            this.dicMsghandles = new Dictionary<string, MsgHandle>();
            //init Server Config storage dic(ex: 0800 => 10.27.68.155:6105:5000:5000)
            this.dicServerConfig = new Dictionary<string, string>();
            //init request pair response dictionary(用來對應回來時候的Message Type)
            this.dicReqToRspMsgType = new Dictionary<string, string>() 
            {
                {"0100","0110"},//自動加值(正常) Message Type 的 request 和response 對應狀況
                {"0120","0130"},//自動加值(代行) Message Type 的 request 和response 對應狀況
                {"0121","0130"},//自動加值(重代行) Message Type 的 request 和response 對應狀況
                {"0420","0430"},//自動加值(沖正) Message Type 的 request 和response 對應狀況
                {"0421","0430"},//自動加值(重沖正) Message Type 的 request 和response 對應狀況
                //{"0800","0810"},//Sign On/Off/Echo Message Type 的 request 和response 對應狀況
                //{"0302","0312"},//連線掛失/掛失取消/增加拒絕代行授權名單 Message Type 的 request 和response 對應狀況
            };
        }

        #endregion

        /// <summary>
        /// 開始連線作業
        /// </summary>
        /// <param name="obj">暫時沒用-沒傳資料</param>
        private void Init(object obj)
        {
            try
            {
                do
                {
                    this.EchoTimer.Stop();
                    this.Started = true;
                    this.WaitStartdone.Reset();
                    //如果連線成功
                    if (this.asyncConnect.Start())
                    {
                        //停止連線用記時器
                        this.timer.Stop();
                        //開始Echo Test 計時器
                        this.EchoTimer.Start();
                        //開始接收
                        this.asyncConnect.StartAsyncReceive(new ReceiveState());
                        //是否重新初始化連線物件過
                        this.IsReInitialObject = false;
                        //是否已執行起動
                        this.Started = false;
                    }
                    else
                    {
                        //blocking wait timer set it
                        this.timer.Start();
                        this.WaitStartdone.WaitOne();
                        this.timer.Stop();
                        //只重做一次初始化asyncConnect連線物件
                        if (!this.IsReInitialObject)
                        {
                            this.ReInitialObject();
                        }
                        //this.Init(obj);
                    }
                }
                while (this.asyncConnect.Status != ConnectStatus.Connected);
                
            }
            catch (Exception ex)
            {
                log.Debug("[ALOLAsync][Init] Error:" + ex.ToString());
            }
        }

        /// <summary>
        /// 開始連線
        /// </summary>
        public void Start()
        {
            //啟動連線
            //this.timer.Start();
            try
            {
                //如果執行過啟動
                if (this.Started)
                { return; }
                ThreadPool.QueueUserWorkItem(Init);
            }
            catch (Exception ex)
            {
                //應該是非SocketException的錯誤,因為SocketException已在this.asyncConnect.Start()時攔截回傳false
                log.Debug("[ALOLAsync][Start] Error:" + ex.Message);//.ToString());
            }
        }

        /// <summary>
        /// 重新初始化連線物件
        /// </summary>
        protected void ReInitialObject()
        {
            //如果沒有重新初始化過
            if (!this.IsReInitialObject)
            {
                //停止並釋放資源
                this.asyncConnect.Stop();
                //重新初始化連線物件
                this.asyncConnect.Init();
                //狀態改成已手動重新初始化,即直接執行Connect
                this.asyncConnect.ManualInited = true;
                //已重新初始化
                this.IsReInitialObject = true;
            }
        }

        /// <summary>
        /// 停止ALOL元件並釋放所有資源與取消事件
        /// </summary>
        public void Stop()
        {
            //取消註冊非同步送出後的Callback事件要呼叫的方法
            this.asyncConnect.OnSendAsyncCallback -= SendCallback;
            //取消註冊非同步接收後的Callback事件要呼叫的方法
            this.asyncConnect.OnReceiveAsyncCallback -= ReceiveCallback;
            //取消註冊計時器
            this.timer.Elapsed -= timer_Elapsed;
            this.timer.Close();
            this.EchoTimer.Elapsed -= EchoTimer_Elapsed;
            this.EchoTimer.Close();

            //關閉Socket Component並釋放資源
            this.asyncConnect.Stop();

            //clean StringBuilder
            this.ReceiveBufferString.Clear();
            
            //clean dictionary
            this.dicMsghandles.Clear();
            this.dicReqToRspMsgType.Clear();
            this.dicServerConfig.Clear();

            this.WaitStartdone.Close();
        }

        #region Send Method

        /// <summary>
        /// 接收AP來的Socket連線接收資料後非同步送出並回傳一個辨識ID
        /// </summary>
        /// <param name="apSck">AP連過來的Socket Clinet</param>
        /// <returns>辨識ID</returns>
        public string Send(Socket apSck)
        {
            try
            {
                if (this.asyncConnect.Status != ConnectStatus.Connected)
                {
                    log.Debug("連線狀態:" + this.asyncConnect.Status.ToString() + " 開始重新連線...");
                    //重新初始化連線物件並連線
                    this.Start();
                }
                byte[] receiveData = new byte[0x1000];
                SocketError socketErr;
                log.Debug("開始接收資料");
                int receiveLength = apSck.Receive(receiveData, 0, receiveData.Length, SocketFlags.None, out socketErr);

                #region 接收AP端異常時
                if (receiveLength == 0)
                {
                    log.Debug("接收AP的Receive資料為0");
                    return String.Empty;
                }
                if (socketErr != SocketError.Success)
                {
                    log.Debug("接收AP的Receive時Socket異常:" + socketErr.ToString());
                    return String.Empty;
                    //throw new SocketException((int)socketErr);
                }
                #endregion

                else
                {
                    log.Debug("開始接收AP資料");
                    Array.Resize<byte>(ref receiveData, receiveLength);
                    string JsonString = Encoding.UTF8.GetString(receiveData, 0, receiveLength);
                    log.Debug("AP傳來的物件:" + JsonString);
                    AutoloadRqt_2Bank autoloadRqt_2Bank = JsonConvert.DeserializeObject<AutoloadRqt_2Bank>(JsonString);

                    //1. Parse 成傳送字串
                    //string msgString = this.ALOLISO8583Parser.BuildMsg(autoloadRqt_2Bank.MESSAGE_TYPE, autoloadRqt_2Bank);
                    byte[] msgBytes = this.ALOLISO8583Parser.BuildMsg(autoloadRqt_2Bank.MESSAGE_TYPE, AddDefineSize, autoloadRqt_2Bank);//委派AddDefineSize方法加料header
                    //log.Debug("轉ISO8583後的電文(Type:" + autoloadRqt_2Bank.MESSAGE_TYPE + ") :" + msgString);
                    //初始化訊息物件
                    MsgHandle msgHandle = new MsgHandle();
                    //(Resposne) Message Type + STAN + TRANS_DATETIME //這三樣資料在Request和Response時都有資料且不變,故當作唯一值 ,即辨識ID
                    msgHandle.ID = this.dicReqToRspMsgType[autoloadRqt_2Bank.MESSAGE_TYPE] + autoloadRqt_2Bank.STAN + autoloadRqt_2Bank.TRANS_DATETIME;//抓response的Message Type
                    msgHandle.Encoding = this.Encode;
                    msgHandle.APSocket = apSck;
                    msgHandle.workSocket = this.asyncConnect.MainSck;
                    //msgHandle.SendString = msgString;//Msg//舊的  傳3碼 + 純電文
                    msgHandle.SendBuffer = msgBytes;//新的  2碼自定義的資料長度(byte Array) + 純電文(轉byte Array)
                    if (this.dicMsghandles.ContainsKey(msgHandle.ID))
                    {
                        throw new Exception("此資料已存在於字典檔 \nID:" + msgHandle.ID + " \nSendString:" + msgHandle.SendString);
                    }
                    //Context Object加入委派任務-送出時就決定接收到回應後要做啥
                    msgHandle.OnReceiveAction += ALOLResponse;//DelegateFactory(autoloadRqt_2Bank.MESSAGE_TYPE);
                        
                    //2. Async 送出到銀行並註冊回來的東西檢測
                    this.asyncConnect.Send(msgHandle);
                    return msgHandle.ID;
                }
            }
            catch (Exception ex)
            {
                log.Debug("[ALOLAsync][Send]" + ex.ToString());
                return String.Empty;
            }
        }

        /// <summary>
        /// 測試用的Send方法,單收字串
        /// </summary>
        /// <param name="msgContext"></param>
        /// <returns></returns>
        public string Send(string id, string msgContext,bool hasDele,ReceiveAction2 testDele = null)
        {
            try
            {
                if (this.dicMsghandles.ContainsKey(id))
                {
                    throw new ArgumentException("Key : " + id + " is reapet");
                }
                MsgHandle context = new MsgHandle()
                {
                    ID = id,
                    Encoding = this.Encode,
                    SendString = msgContext//Msg Length(3 bytes) + Msg
                };
                //註冊一個接收到資料後要執行的匿名方法
                if (hasDele)
                {
                    if (testDele == null)
                    {
                        testDele = new ReceiveAction2((string value) =>
                        {
                            log.Debug("開始處理委派的任務..." + value);
                            if (value.Length > 10)
                            {
                                log.Debug("資料長度大於10 : 符合規定");
                                return true;
                            }
                            else
                            {
                                log.Debug("資料長度小於等於10 : 不符合規定");
                                return false;
                            }
                        });
                    }
                    context.OnReceiveAction2 += testDele;
                }
                log.Debug("開始非同步送出訊息:" + context.SendString);
                this.asyncConnect.Send(context);
                return context.ID;
            }
            catch (Exception ex)
            {
                log.Debug("[ALOLAsync][Sned]" + ex.ToString());
                return string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="responseMsg"></param>
        public void Send(string messageType,string responseMsg)
        {
            try
            {
                if (this.asyncConnect.Status != ConnectStatus.Connected)
                {
                    log.Debug("連線狀態:" + this.asyncConnect.Status.ToString() + " 開始重新連線...");
                    //重新初始化連線物件並連線
                    this.ReInitialObject();
                    //開始連線
                    this.Start();
                    log.Debug("因連線異常已重新開始連線,故無法送出資料");
                    return;
                }
                MsgHandle context = new MsgHandle()
                {
                    ID = messageType,
                    Encoding = this.Encode,
                    SendString = responseMsg//Msg
                };
                
                log.Debug("開始非同步送出訊息:" + context.SendString);

                this.asyncConnect.Send(context);
            }
            catch (Exception ex)
            {
                log.Debug("[Sned]" + ex.ToString());
            }
        }

        /// <summary>
        /// 將要送出的資料(byte[])加入(包含加料部分)
        /// </summary>
        /// <param name="messageType">送出的格式</param>
        /// <param name="responseMsg">回送給銀行端的資料(含加料)</param>
        public void Send(string messageType, byte[] responseMsg)
        {
            try
            {
                if (this.asyncConnect.Status != ConnectStatus.Connected)
                {
                    log.Debug("連線狀態:" + this.asyncConnect.Status.ToString() + " 開始重新連線...");
                    //重新初始化連線物件並連線
                    this.ReInitialObject();
                    //開始連線
                    this.Start();
                    log.Debug("因連線異常已重新開始連線,故無法送出資料");
                    return;
                }
                MsgHandle context = new MsgHandle()
                {
                    ID = messageType,
                    Encoding = this.Encode,
                    SendBuffer = responseMsg//SendString = responseMsg//Msg
                };

                log.Debug("開始非同步送出訊息:" + context.SendString);

                this.asyncConnect.Send(context);
            }
            catch (Exception ex)
            {
                log.Debug("[Sned]" + ex.ToString());
            }
        }

        #endregion

        #region Delegate Method
        /// <summary>
        /// 自定義的資料長度(2bytes) + 純電文(轉byte array)
        /// </summary>
        /// <param name="msg">純電文字串</param>
        /// <returns>加料的電文(byte[])</returns>
        public virtual byte[] AddDefineSize(string msg)
        {
            //取得純電文長度
            int msgLength = msg.Length;
            //自定義的資料長度
            byte[] dataSize = this.ReceiveBufferQ.IntegerToByteAry(msgLength, 2);
            log.Debug("自定義的資料長度(byte[]):{ " + dataSize.ByteToString(0, 2) + " } MsgData:" + msg);
            //純電文資料
            byte[] data = this.Encode.GetBytes(msg);
            //合併陣列
            //byte[] result = dataSize.Concat(data).ToArray();
            byte[] result = new byte[dataSize.Length + data.Length];
            Buffer.BlockCopy(dataSize, 0, result, 0, dataSize.Length);
            Buffer.BlockCopy(data, 0, result, dataSize.Length, data.Length);
            log.Debug("合併後的byte[](length:" + result.Length + "):" + result.ByteToString(0, result.Length));
            return result;
        }

        /// <summary>
        /// 計時器的委派任務
        /// </summary>
        /// <param name="sender">計時器物件</param>
        /// <param name="e">沒用到</param>
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                log.Debug("連線啟動時間:" + e.SignalTime.ToString("HH:mm:ss"));
                int workthread,completeThread;
                ThreadPool.GetAvailableThreads(out workthread, out completeThread);
                log.Debug("workthread:" + workthread + "  completeThread" + completeThread);
                this.WaitStartdone.Set();
            }
            catch (Exception ex)
            {
                log.Debug("[timer_Elapsed] Error:" + ex.ToString());
            }
        }

        /// <summary>
        /// 送Echo的計時器方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EchoTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            //送出Echo Test電文
            this.SendEchoRequest(e.SignalTime);
        }

        /// <summary>
        /// 非同步送出後的callback委派方法
        /// </summary>
        /// <param name="ar">非同步作業的狀態緩存</param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    SocketError sendErr;
                    MsgHandle msgHandle = (MsgHandle)ar.AsyncState;

                    int length = msgHandle.workSocket.EndSend(ar, out sendErr);
                    if (this.asyncConnect.IsStop) return;
                    if (sendErr == SocketError.Success && length == msgHandle.SendBuffer.Length)
                    {
                        //接收到的MsgContext有任務要委派執行的則加入字典檔內(SignOn/Off和掛失類{Request類}需要自己自動連到AP Server)
                        //只增加加值類和沖正類到任務集合內等著結果回送
                        if ((msgHandle.ID.IndexOf("0100") > -1) ||
                            (msgHandle.ID.IndexOf("0120") > -1) || 
                            (msgHandle.ID.IndexOf("0121") > -1) || 
                            (msgHandle.ID.IndexOf("0420") > -1) ||
                            (msgHandle.ID.IndexOf("0421") > -1))
                        {
                            if (!this.dicMsghandles.ContainsKey(msgHandle.ID))
                            {
                                dicMsghandles.Add(msgHandle.ID, msgHandle);
                                log.Debug(msgHandle.ID + "新增至集合內 => 送出長度:" + length + " \n => 送出資料:" + msgHandle.SendBuffer.ByteToString(0, msgHandle.SendBuffer.Length));
                            }
                            else
                            {
                                log.Debug("Key:" + msgHandle.ID + " 已存在於字典檔");
                            }
                        }
                        else
                        {
                            log.Debug("MsgID:" + msgHandle.ID + " => 送出長度:" + length + " \n => 送出資料:" + msgHandle.SendBuffer.ByteToString(0, msgHandle.SendBuffer.Length));
                        }
                    }
                    else
                    {
                        log.Debug("資料送出有問題:" + sendErr.ToString());
                        this.asyncConnect.Status = ConnectStatus.ConnectError;
                        log.Debug("對方可能斷線... 所以開始重新連線 ...");
                        this.Start();
                        //this.WaitStartdone.WaitOne();
                    }
                }
            }
            catch (SocketException sckEx)
            {
                //若若被對方斷線則重新啟動
                //if (sckEx.SocketErrorCode == SocketError.ConnectionRefused)
                //{
                log.Debug("[ALOLAsync][SendCallback] SocketException:" + sckEx.Message);
                    
                    log.Debug("對方可能斷線... 所以開始重新連線 ...");
                    this.asyncConnect.Status = ConnectStatus.ConnectError;
                    this.Start();
                    //this.WaitStartdone.WaitOne();
                //}
            }
            catch (Exception ex)
            {
                log.Debug("[ALOLAsync][SendCallback]" + ex.ToString());
            }
            finally
            {
                this.asyncConnect.SendDone.Set();
            }
        }

        /// <summary>
        /// 非同步接收後的callback委派方法
        /// </summary>
        /// <param name="ar">非同步作業的狀態緩存</param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try 
            {
                if (ar.IsCompleted)
                {
                    SocketError receiveErr;
                    ReceiveState receiveState = (ReceiveState)ar.AsyncState;

                    int receiveLength = receiveState.workSocket.EndReceive(ar, out receiveErr);
                    if (receiveErr == SocketError.Success)
                    {
                        byte[] recieveData = new byte[receiveLength];
                        Buffer.BlockCopy(receiveState.ReceiveBuffer, 0, recieveData, 0, receiveLength);

                        //--------------------Old Version--------------------------------------------------------
                        string receiveString = this.Encode.GetString(recieveData);
                        log.Debug("[接收端]收到的總資料長度:" + receiveLength + " 內容:" + receiveString);
                        
                        //----------------------2015-05-06 變成接收byte array的前兩byte當作資料長度----------------
                        //接收到的資料塞入Queue<byte>物件
                        log.Debug("[接收端]加入資料前Queue內的資料(byte[]):" + this.ReceiveBufferQ.ToString());
                        this.ReceiveBufferQ.InsertData(recieveData);
                        log.Debug("[接收端]加入接收資料後Queue內的資料(byte[]):" + this.ReceiveBufferQ.ToString());
                        //迴圈處理StringBuilder字串,自定義的byte[]大小=>2
                        HandleRecieveString(this.ReceiveBufferQ, 2);
                        //log.Debug("[ReceiveCallback]處理後的字串:" + ReceiveBufferString.ToString());
                        //----------------------2015-05-04後測試的版本(使用StringBuilder)-------------------------
                        //改用StringBiulder方式處理串接的字串
                        //將接收到的字串插入StringBuilder
                        //log.Debug("插入接收字串前的資料:" + ReceiveBufferString.ToString());
                        //ReceiveBufferString.Append(receiveString);
                        //log.Debug("插入接收字串後的資料:" + ReceiveBufferString.ToString());
                        ////迴圈處理StringBuilder字串
                        //HandleRecieveString(ReceiveBufferString);
                        //log.Debug("[ReceiveCallback]處理後的字串:" + ReceiveBufferString.ToString());
                        //-----------------------2015-05-04前的版本----------------------------------------------
                        //if (receiveString.Length < 13)
                        //{
                        //    log.Debug("接收資料長度不符:" + receiveString.Length);
                        //    return;
                        //}
                        //int count = 1;
                        //若一次來多段電文,依頭3碼分段處理電文
                        //for (int i = 0; i < (receiveString.Length - 1); )
                        //{
                        //    //切頭3碼轉數字
                        //    int stringLength = Convert.ToInt32(receiveString.Substring(i, 3)) + 3;
                        //    //依長度取資料字串
                        //    string msgString = (receiveString.Length - i >= stringLength) ? receiveString.Substring(i, stringLength) : receiveString.Substring(i, (receiveString.Length - i));// + 3, stringLength) : receiveString.Substring((i + 3), (receiveString.Length - (i + 3)));
                        //    //i += 3;
                        //    log.Debug("第" + count + "段電文:" + " Length:" + stringLength + " \nMsgData:" + msgString);
                        //    if (stringLength == msgString.Length)
                        //    {
                        //        string messageType = msgString.Substring((8 + 3), 4);//3碼:字串長度 + 找Message Type當parse依據
                        //        TryParseMsg(messageType, msgString);
                        //        i += msgString.Length;
                        //        count++;
                        //    }
                        //    else
                        //    {
                        //        log.Error("長度異常: 定義長度=>" + stringLength + "  實際長度=>" + msgString.Length + "  實際=>" + msgString);
                        //        //後面都不比對了(應該也沒了)
                        //        break;
                        //    }
                        //}
                        //----------------------------------------------------------------------------
                        // 測試用
                        //若接收到資料的Key與字典檔內的Key相符,表示為此字典檔內某個msgHandle所需的接收資料
                        //string comparekey = receiveString.Substring(0, 4);
                        //if (this.dicMsghandles.ContainsKey(comparekey))
                        //{
                        //    this.dicMsghandles[comparekey].ReceiveString = receiveString;
                        //}
                        //----------------old-2015-04-21--------------------
                        //string messageType = receiveString.Substring(8, 4);//找Message Type當parse依據
                        //TryParseMsg(messageType, receiveString);
                        //--------------------------------------------------
                    }
                    else
                    {
                        log.Debug("非同步接收失敗:" + receiveErr.ToString());//用nmap當server測試斷線都就會產生ConnectReset的socket錯誤
                    }
                }
            }
            catch (SocketException sckEx)
            {
                log.Error("[ALOLAsync][ReceiveCallback]SocketException:" + sckEx.ToString());
                //若若被對方斷線則重新啟動
                //if (sckEx.SocketErrorCode == SocketError.ConnectionRefused)
                //{
                    //this.WaitStartdone.Reset();
                    log.Error("對方可能斷線... 所以開始重新連線 ...");
                    this.Start();
                    //this.WaitStartdone.WaitOne();
                //}
            }
            catch (ObjectDisposedException objEx)
            {
                //Socket物件若被dispose,會有一個最後的Async receive會因被強制停止而產生excpetion
                log.Error("[ReceiveCallback]ObjectDisposedException:" + objEx.ToString());
            }
            catch (Exception ex)
            {
                log.Error("[ALOLAsync][ReceiveCallback]" + ex.ToString());
            }
            finally
            {
                this.asyncConnect.ReceiveDone.Set();
            }
        }

        /// <summary>
        /// [Old Version]截取接收到的字串作處理
        /// </summary>
        /// <param name="ReceiveStringQueue">接收到的字串</param>
        private void HandleRecieveString(StringBuilder ReceiveStringQueue)
        {
            int count = 1;
            int stringLength = 0;
            //bool dataDeficiency = false;
            while(ReceiveStringQueue.Length > 3)
            {
                //切頭3碼
                string definedLength = ReceiveStringQueue.ToString(0, 3);
                //資料定義長度+資料長度
                stringLength = Convert.ToInt32(definedLength) + definedLength.Length;
                if (ReceiveStringQueue.Length < stringLength)
                {
                    log.Debug(@"目前定義長度:" + stringLength + " < 現有字串長度:" + ReceiveStringQueue.Length);
                    //dataDeficiency = true;
                    break;
                }
                //依長度取資料字串
                string msgString = ReceiveStringQueue.ToString(0, stringLength);
                //移除
                ReceiveStringQueue.Remove(0, stringLength);
                log.Debug("第" + count + "段電文:" + " Length:" + stringLength + " \nMsgData:" + msgString);
                string messageType = msgString.Substring((8 + 3), 4);//3碼:字串長度 + 找Message Type當parse依據
                //電文字串 => POCO資料物件
                object obj = this.ALOLISO8583Parser.ParseMsg(messageType, msgString);
                TryParseMsg(obj);
                count++;
            };
            //log.Debug("(方法內部)處理後的字串:" + ReceiveStringQueue.ToString());
        }

        /// <summary>
        /// 截取接收到的字串作處理
        /// </summary>
        /// <param name="bufferQ">接收到的字串</param>
        /// <param name="defineSize">自定義的資料長度(2個byte)</param>
        private void HandleRecieveString(QueueStorange bufferQ,int defineSize)
        {
            int count = 1;
            bool isDataEnough = true;
            while (isDataEnough)
            {
                //資料不足(連取自定義的值都不夠)
                if (bufferQ.GetLength() <= defineSize)
                {
                    isDataEnough = false;
                    break;
                }
                else 
                {
                    //讀取自定義長度位元(2 bytes)
                    byte[] dataSizeBytes = bufferQ.GetQueueDefineSizeBytes(defineSize);
                    log.Debug("[接收端]取得自定義byte[]:" + dataSizeBytes.ByteToString(0, dataSizeBytes.Length));
                    //計算資料長度
                    int dataSize = bufferQ.ByteAryToInteger(dataSizeBytes);
                    log.Debug("[接收端]計算後所需資料的長度:" + dataSize);
                    //若緩存內資料長度 >= 2bytes + 純電文大小
                    if (bufferQ.GetLength() >= (dataSize + dataSizeBytes.Length))
                    {
                        log.Debug("[接收端]Queue資料足夠,開始取得Queue內的資料...");
                        //byte[] GetdataSizeBytes = bufferQ.GetData(defineSize);
                        //用定義長度取得所有資料(包含2碼自定義byte[])
                        byte[] allDataBytes = bufferQ.GetData(dataSize + dataSizeBytes.Length);
                        byte[] justISO8583Data = new byte[dataSize];
                        Buffer.BlockCopy(allDataBytes, dataSizeBytes.Length, justISO8583Data, 0, dataSize);
                        //電文陣列轉換成電文字串
                        string msgString = this.Encode.GetString(justISO8583Data);
                        log.Debug("[接收端]第" + count + "段電文:" + " Length:" + dataSize + " \nMsgData:" + msgString);
                        string messageType = msgString.Substring((8 + 3), 4);//3碼:字串長度 + 找Message Type當parse依據
                        //電文字串 => POCO資料物件
                        object obj = this.ALOLISO8583Parser.ParseMsg(messageType, msgString);
                        TryParseMsg(obj);
                        count++;
                    }
                    else
                    {
                        isDataEnough = false;
                        break;
                    }
                }
            }
        }

        //暫不用分類
        private ReceiveAction DelegateFactory(string messageType)
        {
            switch (messageType)
            {
                case "0100":
                case "0120":
                case "0121":
                    return new ReceiveAction(ALOLResponse);
                case "0420":
                case "0421":
                    //return new ReceiveAction(RALOLResponse);
                case "0810":
                //return new ReceiveAction(Sign)
                default:
                    return null;
            }
        }

        /// <summary>
        /// 將授權資料送回AP端且成功則回傳true,失敗回傳false
        /// </summary>
        /// <param name="value">授權物件</param>
        /// <param name="apSocket">AP的Socket</param>
        /// <returns>是否完成</returns>
        private bool ALOLResponse(object value, Socket apSocket)
        {
            bool returnSuccess = false;
            try
            {
                AutoloadRqt_2Bank bankRsp = value as AutoloadRqt_2Bank;
                log.Debug("開始執行自動加值授權回應:" + bankRsp.MESSAGE_TYPE);
                if (bankRsp == null)
                {
                    throw new InvalidCastException("object => AutoloadRqt_2Bank : Failed");
                }
                string bankRspString = JsonConvert.SerializeObject(bankRsp);
                log.Debug("銀行回傳物件:" + bankRspString);
                byte[] sendDataToAp = Encoding.UTF8.GetBytes(bankRspString);
                if (apSocket.Connected)// || (!(apSocket.Poll(1000, SelectMode.SelectRead) && (apSocket.Available == 0))))
                {
                    SocketError sendErr;
                    int sendlength = apSocket.Send(sendDataToAp, 0, sendDataToAp.Length, SocketFlags.None, out sendErr);
                    if (sendErr == SocketError.Success && sendlength == sendDataToAp.Length)
                    {
                        log.Debug(bankRsp.MESSAGE_TYPE + "資料送到AP端:成功");
                        returnSuccess = true;
                        return true;
                    }
                }
                if (!returnSuccess)
                {
                    log.Debug(bankRsp.MESSAGE_TYPE + "資料送到AP端:失敗");
                    //TODO..........................還要組沖正.........................
                }
                
                return false;
            }
            catch (Exception ex)
            {
                log.Debug("[ALOLAsync][ALOLResponse] Error:" + ex.ToString());
                if (!returnSuccess)
                {
                    log.Debug("授權資料送到AP端:失敗");
                    //TODO..........................還要組沖正.........................
                }
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 電文字串轉型成物件...做物件類型該做的事
        /// </summary>
        /// <param name="messageType">格式(0110/0130/0430 | 0302 | 0800)</param>
        /// <param name="msg">電文字串(ASCII)</param>
        private void TryParseMsg(object obj)
        {
            try
            {
                //電文字串 => POCO資料物件
                //object obj = this.ALOLISO8583Parser.ParseMsg(messageType, msg);
                if (obj is AutoloadRqt_2Bank)
                {
                    //收到的資料類型是0110/0130/0430(屬於Response類)
                    AutoloadRqt_2Bank bankRsp = (AutoloadRqt_2Bank)obj;
                    log.Debug("收到的資料類型:" + bankRsp.MESSAGE_TYPE + "(屬於Response類)");
                    string key = bankRsp.MESSAGE_TYPE + bankRsp.STAN + bankRsp.TRANS_DATETIME;//辨識ID
                    log.Debug("Recieve Response: " + "\n" + 
                                      "Key:" + key + "\n" + 
                                      "BankCode:" + bankRsp.BANK_CODE + "\n" +
                                      "MESSAGE_TYPE:" + bankRsp.MESSAGE_TYPE + "\n" + 
                                      "STAN:" + bankRsp.STAN + "\n" + 
                                      "TRANS_DATETIME:" + bankRsp.TRANS_DATETIME + "\n" + 
                                      "ReturnCode:" + bankRsp.RC);
                    //若字典檔內有此(0110/0130/0430)交易紀錄
                    if (this.dicMsghandles.ContainsKey(key))
                    {
                        log.Debug("丟入此紀錄的receive欄位產生event自動回送訊息到AP端");
                        //丟入此紀錄的receive欄位產生event自動回送訊息到AP端
                        this.dicMsghandles[key].ReceiveObject = bankRsp;
                    }
                    else
                    {
                        //若字典檔內無此(0110/0130/0430)交易紀錄........TODO..........
                        log.Debug("字典檔無此Key:" + key + "可存放回傳的交易紀錄(0110/0130/0430).....");
                    }
                }
                else if (obj is AutoloadRqt_FBank)
                {
                    //收到的資料類型是0302(屬於Request類)
                    AutoloadRqt_FBank bankRequest = (AutoloadRqt_FBank)obj;
                    log.Debug("收到的資料類型:" + bankRequest.MESSAGE_TYPE + "(屬於Request類)");
                    //執行連線掛失/掛失取消/增加拒絕授權流程--丟回AP並取得回應再還Response給銀行
                    this.DoLossReportOrAddRejectList(bankRequest);
                    
                }
                else if (obj is Sign_Domain)
                {
                    //收到的資料類型是0800(屬於Request類)
                    Sign_Domain sign_Domain = (Sign_Domain)obj;
                    log.Debug("收到的資料類型:" + sign_Domain.COM_Type + "(屬於Request類)");
                    //執行Sign On/Off/Echo 流程
                    this.DoSignOn_Off_Echo(sign_Domain);
                }
                else
                {
                    log.Debug("無定義的型別");
                }
            }
            catch (Exception ex)
            {
                log.Debug("[TryParseMsg] Error:" + ex.ToString());
            }
        } 

        /// <summary>
        /// 是否完成Send交付的工作
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>T(完成)/F(未完成)</returns>
        public bool IsSuccess(string key)
        {
            if (this.dicMsghandles.ContainsKey(key))
            {
                log.Debug(key + "的委託是否完成 :" + this.dicMsghandles[key].isSuccess);
                return this.dicMsghandles[key].isSuccess;
            }
            else
            {
                return false;
                //throw new Exception("Key:" + key + " not exist");
            }
        }

        /// <summary>
        /// 移除字典檔內存放的Handle
        /// </summary>
        /// <param name="key">鍵值</param>
        public void RevomeKey(string key)
        {
            if (this.dicMsghandles.ContainsKey(key))
            {
                //取消此物件註冊的事件
                this.dicMsghandles[key].OnReceiveAction -= ALOLResponse;
                this.dicMsghandles.Remove(key);
                log.Debug("remove :" + key + "   dicMsghandles Count:" + dicMsghandles.Count);
            }
            else
            {
                log.Debug("Key:" + key + " not exist");
            }
        }

        /// <summary>
        /// 取得Xml資源檔內的服務設定IP和Port,若無則載入字典檔
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        private string GetServiceConfig(string messageType)
        {
            try
            {
                if (!this.dicServerConfig.ContainsKey(messageType))
                {
                    string IpAndPort = string.Empty;
                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    XmlDocument xml = new XmlDocument();

                    using (StreamReader sr = new StreamReader(path + "Config\\ServiceConfig.xml"))
                    {
                        xml.Load(sr);
                        XmlNodeList xNodeList = xml.DocumentElement.GetElementsByTagName("Service");
                        foreach (XmlNode node in xNodeList)
                        {
                            if (node.Attributes.GetNamedItem("name").Value == messageType)
                            {
                                IpAndPort = node.InnerXml;
                                break;
                            }
                        }
                    }
                    this.dicServerConfig.Add(messageType, IpAndPort);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return this.dicServerConfig[messageType];
        }

        /// <summary>
        /// 送出Echo電文
        /// </summary>
        /// <returns></returns>
        private void SendEchoRequest(DateTime sendTime)
        {
            //if(this.asyncConnect.Status)
            DateTime now = sendTime;
            string requestString = 
                "8888" + this.BankCode.PadLeft(4,'0') +                         //Message header(8 bytes ASCII)
                "0800" +                                                        //Message Type  (4 bytes ASCII)
                "8220000000000000" + "0400000000000000" +                       //Bit Map (Primary + Secondary)(32 bytes ASCII)
                now.ToString("MMddHHmmss") + now.ToString("HHmmss") + "301";    //Field 07 + Field 11 + Field 70(10 + 6 + 3 bytes ASCII)
            //requestString = requestString.Length.ToString("D3") + requestString;// +3 bytes OldVersion
            byte[] requestBytes = this.AddDefineSize(requestString);
            log.Debug("時間:" + now.ToString("HH:mm:ss") + " 開始送出Echo電文:" + requestString);
            this.Send("0800", requestBytes);
        }

        /// <summary>
        /// 處理連線掛失/掛失取消/增加拒絕代行授權名單Request
        /// </summary>
        /// <param name="bankRequest">銀行端發送的Request</param>
        private void DoSignOn_Off_Echo(Sign_Domain bankRequest)
        {
            try
            {
                log.Debug("開始處理 Sign On/Off/Echo");
                //取Xml資源設定檔內的IP和Port(只有第一次會去讀取檔案)
                string[] serverConfig = GetServiceConfig(bankRequest.COM_Type).Split(':');
                string ip = serverConfig[0];
                int port = Convert.ToInt32(serverConfig[1]);
                int sendTimeout = Convert.ToInt32(serverConfig[2]);
                int receivetimeout = Convert.ToInt32(serverConfig[3]);
                log.Debug("取得XML資源檔設定: \nIP:" + ip + " \nPort:" + port + " \nSendTimeout:" + sendTimeout + "\nReceiveTimeout:" + receivetimeout);
                //送出的資料: Object => json string => byte[]
                string sendJsonString = JsonConvert.SerializeObject(bankRequest);
                log.Debug("送出的資料(JSON):" + sendJsonString);
                byte[] dataBytes = Encoding.UTF8.GetBytes(sendJsonString);//AP端編碼為UTF8
                byte[] receiveBytes = null;

                using (SocketClient.Domain.SocketClient toAP = new SocketClient.Domain.SocketClient(ip, port, sendTimeout, receivetimeout))
                {
                    if (toAP.ConnectToServer())
                    {
                        receiveBytes = toAP.SendAndReceive(dataBytes);
                    }
                }
                //

                if (receiveBytes == null)
                {
                    log.Debug("與AP交訊失敗");
                }
                else
                {
                    string receiveJsonString = Encoding.UTF8.GetString(receiveBytes);
                    log.Debug("收到AP給的資料(JSON): " + receiveJsonString);
                    Sign_Domain responseToBank = JsonConvert.DeserializeObject<Sign_Domain>(receiveJsonString);
                    //物件 => 電文字串(ASCII)
                    //string responseMSG = this.ALOLISO8583Parser.BuildMsg(responseToBank.COM_Type, responseSign: responseToBank);
                    //送出的全部資料(陣列)
                    byte[] responseMSG = this.ALOLISO8583Parser.BuildMsg(responseToBank.COM_Type, AddDefineSize, responseSign: responseToBank);
                    log.Debug("Send msg Context(Response) back Bank[" + responseToBank.BankCode + "]:" + responseMSG);
                    //Response送回銀行端
                    this.Send(responseToBank.COM_Type, responseMSG);
                }
            }
            catch (Exception ex)
            {
                log.Debug("[DoSignOn_Off_Echo] Error: " + ex.ToString());
            }
        }

        /// <summary>
        /// 處理連線掛失/掛失取消/增加拒絕代行授權名單Request
        /// </summary>
        /// <param name="bankRequest">銀行端發送的Request</param>
        private void DoLossReportOrAddRejectList(AutoloadRqt_FBank bankRequest)
        {
            try
            {
                log.Debug("開始處理 連線掛失/掛失取消/增加拒絕代行授權名單");
                //取Xml資源設定檔內的IP和Port
                string[] serverConfig = GetServiceConfig(bankRequest.MESSAGE_TYPE).Split(':');
                string ip = serverConfig[0];
                int port = Convert.ToInt32(serverConfig[1]);
                int sendTimeout = Convert.ToInt32(serverConfig[2]);
                int receivetimeout = Convert.ToInt32(serverConfig[3]);
                log.Debug("取得XML資源檔設定: \nIP:" + ip + " \nPort:" + port + " \nSendTimeout:" + sendTimeout + "\nReceiveTimeout:" + receivetimeout);
                //送出的資料: Object => json string => byte[]
                string sendJsonString = JsonConvert.SerializeObject(bankRequest);
                log.Debug("送出的資料(JSON):" + sendJsonString);
                byte[] dataBytes = Encoding.UTF8.GetBytes(sendJsonString);
                byte[] receiveBytes = null;

                using (SocketClient.Domain.SocketClient toAP = new SocketClient.Domain.SocketClient(ip, port, sendTimeout, receivetimeout))
                {
                    if (toAP.ConnectToServer())
                    {
                        receiveBytes = toAP.SendAndReceive(dataBytes);
                    }
                }
                //
                
                if (receiveBytes == null)
                {
                    log.Debug("與AP交訊失敗");
                }
                else
                {
                    string receiveJsonString = Encoding.UTF8.GetString(receiveBytes);
                    log.Debug("收到AP給的資料(JSON): " + receiveJsonString);
                    AutoloadRqt_FBank responseToBank = JsonConvert.DeserializeObject<AutoloadRqt_FBank>(receiveJsonString);

                    //string responseMSG = this.ALOLISO8583Parser.BuildMsg(responseToBank.MESSAGE_TYPE, responseFromBank: responseToBank);
                    //送出的全部資料(陣列)
                    byte[] responseMSG = this.ALOLISO8583Parser.BuildMsg(responseToBank.MESSAGE_TYPE, AddDefineSize, responseFromBank: responseToBank);
                    log.Debug("Send msg Context(Response) back Bank[" + responseToBank.BANK_CODE + "]:" + responseMSG);
                    this.Send(responseToBank.MESSAGE_TYPE, responseMSG);
                    //-------------TODO..........................................
                }
            }
            catch (Exception ex)
            {
                log.Debug("[DoLossReportOrAddRejectList] Error: " + ex.ToString());
            }
        }
    }
}
