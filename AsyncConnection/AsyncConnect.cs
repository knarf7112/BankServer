using System;
using System.Text;
//
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Common.Logging;
//using OL_Autoload_Lib.Model;
namespace AsyncConnection
{
    /// <summary>
    /// 非同步交訊
    /// </summary>
    public class AsyncConnect : IAsyncConnect,IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AsyncConnect));

        //public readonly string BankCode;
        /// <summary>
        /// 和銀行連線的Socket
        /// </summary>
        public Socket MainSck { get; set; }

        /// <summary>
        /// 目前socket的連線狀態(false:斷線/true:連線)
        /// </summary>
        public bool CurrentConnectStatus { get; set; }

        public ManualResetEvent ConnectDone { get; private set; }

        /// <summary>
        /// 控制非同步送出Block的event
        /// </summary>
        public ManualResetEvent SendDone { get; private set; }

        /// <summary>
        /// 控制非同步接收Block的event
        /// </summary>
        public ManualResetEvent ReceiveDone { get;private set; }

        /// <summary>
        /// 非同步送出完成後的執行動作
        /// </summary>
        public event AsyncCallback OnSendAsyncCallback;

        /// <summary>
        /// 非同步接收完成後的執行動作
        /// </summary>
        public event AsyncCallback OnReceiveAsyncCallback;

        /// <summary>
        /// SOcket Connect Status
        /// </summary>
        public ConnectStatus Status { get; set; }

        /// <summary>
        /// 已手動初始化
        /// </summary>
        public bool ManualInited { get; set; }
        //public int CurrentRetryCount { get; set; }
        public bool IsStop { get; private set; }
        private string ip;
        private int port;
        private int maxRetry;
        private int sendTimeout;
        //------非同步用beginConnect----
        //public int connectTimeout;
        //private bool isConnect;
        //-----------------------------
        /// <summary>
        /// 鎖定執行緒的私有物件
        /// </summary>
        private object lockObj = new object();

        #region 內部解除接收的blocking
        private delegate bool UnBlock(bool b);

        private UnBlock OnReleaseBlock;
        private bool _keepReceive;
        public bool KeepReceive 
        {
            get
            {
                return _keepReceive;
            }
            set
            {
                if (OnReleaseBlock != null)
                {
                    this._keepReceive = OnReleaseBlock.Invoke(value);
                }
                else
                {
                    this._keepReceive = value;
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 初始化銀行的連線資料
        /// </summary>
        /// <param name="ip">銀行的IP</param>
        /// <param name="port">銀行的Port</param>
        /// <param name="maxRetry">重試上限(-1:無上限)</param>
        /// <param name="sendTimeout">非同步送出的Timeout(second)</param>
        public AsyncConnect(string ip,int port,int sendTimeout,int maxRetry = -1)
        {
            this.ip = ip;
            this.port = port;
            //this.connectTimeout = connectTimeout;
            this.Status = ConnectStatus.None;
            this.ManualInited = false;
            //this.isConnect = false;
            //this.BankCode = bankCode;
            this.maxRetry = maxRetry;
            this.sendTimeout = sendTimeout;
            //this.CurrentConnectStatus = false;
            this.ConnectDone = new ManualResetEvent(false);
            //this.CurrentRetryCount = 0;
            //若要取消Receive,設定KeepReceive屬性 => false,內部的委派會Invoke這個暱名方法 
            this.OnReleaseBlock = (bool keep) =>
            {
                if (!keep)
                {
                    log.Debug("取消接收");
                    if (this.ReceiveDone != null)
                        this.ReceiveDone.Set();//解除接收的blocking
                }
                else
                {
                    log.Debug("保持接收");
                }
                return keep;
            };
        }
        #endregion

        #region old 使用設定來retry
        /// <summary>
        /// 啟動連線(同步)
        /// 要先註冊好非同步傳送和非同步接收的,
        /// 否則就會使用內部預設的StateObject物件來接收
        /// </summary>
        //public void Start()
        //{
        //    try
        //    {
        //        this.IsStop = false;
        //        this.KeepReceive = true;
        //        log.Debug("開始連線...");
        //        //this.mainSck.SendTimeout = this.sendTimeout;
        //        //this.mainSck.ReceiveTimeout = this.receiveTimeout;//Async 沒有辦法設定接收逾時,只能用KeepReceive屬性來中斷
        //        if (this.mainSck != null) this.Stop();
        //        this.mainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //        this.mainSck.Connect(IPAddress.Parse(this.ip), this.port);
        //        log.Debug("連線中...");
        //        //確定有無Event,若無則給執行內定預設方法
        //        //if (OnSendAsyncCallback == null)
        //        //{
        //        //    this.OnSendAsyncCallback = new AsyncCallback(SendCallback);
        //        //}
        //        //if (OnReceiveAsyncCallback == null)
        //        //{
        //        //    this.OnReceiveAsyncCallback = new AsyncCallback(ReceiveCallBack);
        //        //}
        //        //start async receive
        //        //this.StartReceive(this.BankState);
        //        //log.Debug("開始非同步接收");
        //    }
        //    catch (Exception ex)
        //    {
        //        //若連線被中斷
        //        if (ex is SocketException && ((SocketException)ex).ErrorCode == 10061)
        //        {
        //            if (this.CurrentRetryCount >= int.MaxValue)
        //            {
        //                this.CurrentRetryCount = 0;
        //            }
        //            this.CurrentRetryCount++;
        //            //永遠重連線
        //            if (this.maxRetry == -1)
        //            {
        //                //Thread.Sleep(6000);//每六秒重連一次
        //                this.Start();
        //            }
        //            //小於最大重新連線數
        //            else if (this.CurrentRetryCount < this.maxRetry)
        //            {
        //                //Thread.Sleep(6000);//每六秒重連一次
        //                this.Start();
        //            }
        //            else
        //            {
        //                throw new Exception("已重新連線超過" + this.CurrentRetryCount + "次");
        //            }
        //        }
        //        log.Debug(ex.ToString());
        //    }
        //}
        #endregion

        //初始化連線物件
        public void Init()
        {
            //若手動執行初始化
            if (this.ManualInited)
                return;
            //連線間錯誤
            if (this.Status == ConnectStatus.ConnectError)
            {
                this.Stop();
                this.Status = ConnectStatus.None;
            }
            //未初始化
            if (this.Status == ConnectStatus.None)
            {
                log.Debug("初始化連線物件...");
                this.IsStop = false;
                this.KeepReceive = true;
                this.SendDone = new ManualResetEvent(false);
                this.ReceiveDone = new ManualResetEvent(false);
                

                //this.mainSck.ReceiveTimeout = this.receiveTimeout;//Async 沒有辦法設定接收逾時,只能用KeepReceive屬性來中斷
                this.MainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //已初始化
                this.Status = ConnectStatus.NotConnect;
            }
        }

        /// <summary>
        /// 啟動連線(同步)
        /// 要先註冊好非同步傳送和非同步接收的,
        /// 否則就會使用內部預設的StateObject物件來接收
        /// </summary>
        public bool Start()
        {
            try
            {
                    this.Init();
                //-----------------------------------------------------------
                    this.MainSck.Connect(IPAddress.Parse(this.ip), this.port);
                    this.Status = ConnectStatus.Connected;
                    this.ManualInited = false;
                    log.Debug("連線中...");
                    return true;
                //-----------------------------------------------------------
                    //lock (lockObj)
                    //{
                    //    this.ConnectDone.Reset();
                    //    this.MainSck.BeginConnect(IPAddress.Parse(this.ip), this.port, new AsyncCallback(ConnectCallback), this.MainSck);
                    //    if (!this.ConnectDone.WaitOne(this.connectTimeout))
                    //    {
                    //        return false;
                    //    };
                    //    return this.isConnect;
                    //}
            }
            catch (Exception ex)
            {
                //若拋出的異常為SocketException
                if (ex is SocketException)// && ((SocketException)ex).ErrorCode == 10061)
                {
                    log.Debug("[AsyncConnect][Start] Error:" + ex.Message);
                    //try
                    //{
                    //    this.MainSck.Disconnect(true);

                    //}
                    //catch (Exception ex2)
                    //{
                    //    log.Error(ex2.StackTrace);
                    //}
                    this.Status = ConnectStatus.ConnectError;
                    //this.isConnect = true;
                    return false;
                }
                log.Debug("[AsyncConnect][Start] Error:" + ex.ToString());
                throw ex;
            }
        }

        public void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    Socket tempSck = null;
                    try
                    {
                        tempSck = (Socket)ar.AsyncState;
                        tempSck.EndConnect(ar);
                    }
                    catch (Exception ex)
                    {
                        log.Debug("[Qoo]" + ex.ToString());
                        this.Status = ConnectStatus.ConnectError;
                        tempSck.Close();
                        return;
                    }
                    this.MainSck = tempSck;
                    this.Status = ConnectStatus.Connected;
                    this.ManualInited = false;
                    log.Debug("連線中...");
                }
            }
            catch (Exception ex)
            {
                log.Debug("[AsyncConnect][ConnectCallback] Error:" + ex.ToString());
                this.Status = ConnectStatus.ConnectError;
            }
            finally
            {
                this.ConnectDone.Set();
            }
        }

        /// <summary>
        /// 終止Socket連線
        /// </summary>
        public void Stop()
        {
            try
            {
                this.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// 非同步送出
        /// </summary>
        /// <param name="state">記錄交訊的參考物件</param>
        public void Send(IState state)
        {
            try
            {
                bool isTimeout = false;
                if (this.IsStop) 
                {
                    log.Debug("此Send命令停止送出");
                    return; 
                }//若已停止則略過
                //if (obj is string)
                //this.BankState.SendObj = data;//把參考物件丟到StateObject的屬性,讓其處理
                lock (lockObj)
                {
                    if (this.MainSck.Connected || this.CheckConnect(this.MainSck))
                    {
                        this.SendDone.Reset();
                        if (state.workSocket == null)
                            state.workSocket = this.MainSck;
                        //若外部沒註冊此OnSendAsyncCallback事件則拋出異常
                        if (OnSendAsyncCallback == null || OnSendAsyncCallback.GetInvocationList().Length <= 0)
                        {
                            throw new Exception("OnSendAsyncCallback event is null or 0");
                        }
                        this.MainSck.BeginSend(state.SendBuffer, 0, state.SendBuffer.Length, SocketFlags.None, OnSendAsyncCallback, state);
                        isTimeout = !this.SendDone.WaitOne(new TimeSpan(0, 0, this.sendTimeout));//Blocking and timeout is 5s
                        //Timeout
                        if (isTimeout)
                        {
                            //do nonthing ...
                            log.Debug("此次送出逾時");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Debug("[AsyncConnect][Send] Error:" + ex.ToString());
                //socket exception maybe network error
                if (ex is SocketException)
                {
                    this.Status = ConnectStatus.ConnectError;
                    return;
                }
                throw ex;
            }
        
        }

        /// <summary>
        /// 用ThreadPool開始非同步接收
        /// </summary>
        /// <param name="state">接收用的緩存</param>
        public void StartAsyncReceive(IReceive state)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(Receive), state);
            }
            catch (Exception ex)
            {
                log.Debug("[AsyncConnect][StartAsyncReceive] Error:" + ex.ToString());
            }
        }

        /// <summary>
        /// 停止非同步接收
        /// </summary>
        public void StopAsyncReceive()
        {
            this.KeepReceive = false;
        }

        /// <summary>
        /// 非同步接收
        /// </summary>
        /// <param name="para">交訊物件</param>
        private void Receive(object para)
        {
            try
            {
                log.Debug("開始接收資料");
                IReceive state = para as IReceive;
                if (state != null)
                {
                    do
                    {
                        //清除Buffer Array資料
                        Array.Clear(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length);

                        this.ReceiveDone.Reset();
                        if (state.workSocket == null)
                            state.workSocket = this.MainSck;
                        if (OnReceiveAsyncCallback == null || OnReceiveAsyncCallback.GetInvocationList().Length <= 0)
                        {
                            throw new Exception("OnReceiveAsyncCallback event is null or 0");
                        }
                        this.MainSck.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length, SocketFlags.None, OnReceiveAsyncCallback, state);
                        this.ReceiveDone.WaitOne();//new TimeSpan(0, 0, 10));
                    }
                    while (this.KeepReceive);
                }
                else
                {
                    throw new Exception("cast to IState failed!");
                }
            }
            catch (SocketException sckEx)
            {
                log.Debug("[AsyncConnect][Recieve] Error:" + sckEx.ToString());
                this.Status = ConnectStatus.ConnectError;
                //throw sckEx;
            }
            catch (Exception ex)
            {
                log.Debug("[AsyncConnect][Recieve]" + ex.ToString());
            }
        }

        /// <summary>
        /// Socket連線檢測
        /// </summary>
        /// <param name="sck">Socket object</param>
        /// <returns>(連線)True/False(斷線)</returns>
        public bool CheckConnect(Socket sck)
        {
            try
            {
                bool part1 = sck.Poll(50000, SelectMode.SelectRead);//等50ms,可讀狀態(如果對方中斷也回跳到這裡,但等待時間要加長才看得出來)              
                bool part2 = (sck.Available == 0);//但讀的資料又沒有
                if (part1 && part2)
                {
                    return false;
                }
                return true;
            }
            catch (SocketException ex)
            {
                log.Debug("[SocketConnect]" + ex.ToString());
                return false;
            }

        }

        /// <summary>
        /// 用Poll方法檢查此物件的Sokcet連線狀態
        /// </summary>
        /// <returns>連線/斷線</returns>
        public bool CheckConnect()
        {
            //this.CurrentConnectStatus = this.CheckConnect(this.MainSck);
            return this.CheckConnect(this.MainSck);
        }

        #region 若外部沒有註冊(非同步)送出或接收要執行的動作 則執行預設的Callback方法
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)//lock (lockObj)
                {
                    StateObject tmpObj = (StateObject)ar.AsyncState;
                    SocketError sendErr = 0;
                    int i = tmpObj.workSocket.EndSend(ar, out sendErr);
                    if (this.IsStop) return;
                    //this.sendDone.Set();//這裡解除blocking的話,有可能會跟下幾次非同步的訊息加在一起(即 電文1+電文2...之類)

                    if (sendErr == SocketError.Success)
                    {
                        tmpObj.RequestNo += 1;
                        tmpObj.dicRequest.Add(tmpObj.RequestNo, tmpObj.SendString);
                        log.Debug("資料送出成功: 長度:" + i + "=>" + tmpObj.dicRequest[tmpObj.RequestNo]);
                        //this.Recieve<T>(tmpObj);
                    }
                    else
                    {
                        log.Debug("資料送出有問題:" + sendErr.ToString());
                    }
                }
            }
            catch (SocketException sckEx)
            {
                log.Debug("[SendCallback]SocketException:" + sckEx.ToString());
            }
            catch (Exception ex)
            {
                log.Debug("[SendCallback]" + ex.ToString());
            }
            finally
            {
                this.SendDone.Set();//解除blocking
            }
        }

        //(非同步)完成接收後的動作
        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    StateObject state = (StateObject)ar.AsyncState;
                    SocketError receiveErr;
                    int receiveLength = state.workSocket.EndReceive(ar,out receiveErr);
                    
                    //this.receiveDone.Set();
                    if (receiveErr == SocketError.Success)
                    {
                        //將接收的資料轉成字串
                        string receiveString = Encoding.ASCII.GetString(state.ReceiveBuffer, 0, receiveLength);
                        //加上編號
                        state.ResponseNo += 1;
                        //新增至參考物件response集合內
                        state.dicResponse.Add(state.ResponseNo, receiveString);

                        log.Debug("Server回應: 長度:" + receiveLength + " => " + state.dicResponse[state.ResponseNo]);
                    }
                    else
                    {
                        log.Debug("非同步接收失敗:" + receiveErr.ToString());
                    }
                }
                else
                {
                    log.Debug("非同步接收未完成");
                }
            }
            catch (SocketException sckEx)
            {
                log.Debug("[ReceiveCallback]SocketException:" + sckEx.ToString());
            }
            catch (ObjectDisposedException objEx)
            {
                //Socket物件若被dispose,會有一個最後的Async receive會因被強制停止而產生excpetion
                log.Debug("[ReceiveCallback]ObjectDisposedException:" + objEx.ToString());
            }
            catch (Exception ex)
            {
                log.Debug("[ReceiveCallback]" + ex.ToString());
            }
            finally
            {
                this.ReceiveDone.Set();
            }
        }
        #endregion

        /// <summary>
        /// 釋放並關閉Socket物件資源並清除參考
        /// </summary>
        public void Dispose()
        {
            try
            {
                
                this.KeepReceive = false;
                if (this.MainSck != null)
                {
                    log.Debug("關閉連線中...");
                    if (this.MainSck.Connected)// || CheckConnect(this.MainSck))
                        this.MainSck.Shutdown(SocketShutdown.Both);
                    this.MainSck.Close();
                    log.Debug("連線關閉");
                }
                this.ManualInited = false;
                this.IsStop = true;
                if (this.SendDone != null)
                    this.SendDone.Close();
                if (this.ReceiveDone != null)
                    this.ReceiveDone.Close();
                this.SendDone = null;
                this.ReceiveDone = null;
            }
            catch (SocketException ex)
            {
                try
                {
                    if (ex.SocketErrorCode == SocketError.NotConnected)
                        log.Debug("關閉連線時產生錯誤:" + ex.SocketErrorCode.ToString());//ex.ToString());
                    this.MainSck.Dispose();
                }
                catch (SocketException ex2)
                {
                    log.Debug("[AsyncConnect][Dispose] failed:" + ex2.ToString());
                    throw ex2;
                }
            }
            finally
            {
                this.MainSck = null;
                this.Status = ConnectStatus.None;
            }
        }
    }
}
