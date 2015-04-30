using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;
using System.Net;
using System.Threading;
namespace AsyncConnection_Test
{
    public class AsyncConnectTest<T> : IAsyncConnect<T>
    {
        public readonly string BankCode;
        /// <summary>
        /// 和銀行保持Connect的ClientSocket
        /// </summary>
        private Socket mainSck { get; set; }
        /// <summary>
        /// Socket Send時用的block event
        /// </summary>
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        public StateObject<String> BankState;//存放銀行的State(交訊資訊與buffer)
        private bool IsStop { get; set; }
        private string ip;
        private int port;
        private int maxRetry;
        private int sendTimeout;
        private int receiveTimeout;
        private static object lockObj = new object();

        //初始設訂某銀行的連線資料
        public AsyncConnectTest(string bankCode,string ip,int port,int maxRetry,int sendTimeout,int receiveTimeout)
        {
            this.ip = ip;
            this.port = port;
            this.BankCode = bankCode;
            this.maxRetry = maxRetry;
            this.sendTimeout = sendTimeout;
            this.receiveTimeout = receiveTimeout;
            this.mainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        //啟動連線(同步)
        public void Start(int retryCount = 0)
        {
            try
            {
                this.IsStop = false;
                Console.WriteLine("開始連線...");
                this.mainSck.SendTimeout = this.sendTimeout;
                this.mainSck.ReceiveTimeout = this.receiveTimeout;
                this.mainSck.Connect(IPAddress.Parse(this.ip), this.port);
                BankState = new StateObject<string>() 
                {
                    workSocket = this.mainSck
                };

                Console.WriteLine("連線中");
            }
            catch (Exception ex)
            {
                if (ex is SocketException && ((SocketException)ex).ErrorCode == 10061)
                {
                    retryCount++;
                    if (retryCount < this.maxRetry)
                    {
                        this.Start(retryCount);
                    }
                    else
                    {
                        throw new Exception("已重新連線超過" + retryCount + "次");
                    }
                }
                Console.WriteLine(ex.ToString());
                //throw ex;
            }
        }
        //Socket連線檢測
        private bool SocketConnect(Socket s)
        {
            try
            {
                bool part1 = s.Poll(-1, SelectMode.SelectRead);//可讀狀態(如果對方中斷也回跳到這裡,但等待時間要加長才看得出來)
                bool part2 = (s.Available == 0);//但讀的資料又沒有
                if (part1 && part2)
                {
                    return false;
                }
                return true;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("[SocketConnect]" + ex.ToString());
                return false;
            }
        }
        //停止Socket連線
        public void Stop()
        {
            try
            {
                if (this.mainSck != null && (this.mainSck.Connected || SocketConnect(this.mainSck)))
                {
                    Console.WriteLine("關閉連線中...");
                    this.mainSck.Shutdown(SocketShutdown.Both);
                    this.mainSck.Close();
                    this.IsStop = true;
                    Console.WriteLine("連線關閉");
                }
            }
            catch (SocketException ex)
            {
                try
                {
                    Console.WriteLine("關閉連線時產生錯誤:" + ex.ToString());
                    this.mainSck.Dispose();
                }
                catch (SocketException ex2)
                {
                    Console.WriteLine("Dispose failed:" + ex2.ToString());
                    throw ex2;
                }
            }
            finally
            {
                this.mainSck = null;
            }
        }
        public void SendSynchronous(string Msg)
        {
            try
            {
                byte[] b = Encoding.ASCII.GetBytes(Msg);
                SocketError sendErr;
                int i = this.mainSck.Send(b, 0, b.Length, SocketFlags.None, out sendErr);
                if (i == b.Length && sendErr == SocketError.Success)
                {
                    Console.WriteLine("送出成功:" + Msg);
                }
                else
                {
                    Console.WriteLine("送出有問題:" + sendErr.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Error:" + ex.ToString());
            }
        }
        //非同步送出附上來源AP的socket-自己回應-先傳字串進去
        public void Send<T>(T obj)//Socket APSocket,T obj)
        {
            try
            {
                lock (lockObj)
                {
                    //if (obj is OL_Autoload_Lib.AutoloadRqt_2Bank)
                    //{
                        //Parsing....先用Console送字串
                    //byte[] sendMsg = Encoding.ASCII.GetBytes((string)(obj.ToString()));//new byte[123];
                        //
                if (this.IsStop) return;
                if (obj is string)
                    this.BankState.SendString = obj.ToString();
                this.sendDone.Reset();
                this.BankState.workSocket.BeginSend(this.BankState.SendBytes, 0, this.BankState.SendBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback<T>), this.BankState);
                this.sendDone.WaitOne(new TimeSpan(0, 0, 5));
                //----------------------------------------------------------
                //StateObject<T> tmpObj = new StateObject<T>();
                //tmpObj.SendString = obj.ToString();
                ////tmpObj.APSocket = APSocket;
                //tmpObj.sendObj = obj;
                //tmpObj.workSocket = this.mainSck;

                //this.sendDone.Reset();
                //this.mainSck.BeginSend(tmpObj.SendBytes, 0, tmpObj.SendBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback<T>), tmpObj);
                //this.sendDone.WaitOne();    
                    //Console.WriteLine("開始非同步送出資料:" + tmpObj.SendString);
                        //Thread.Sleep(500);    
                        //Console.WriteLine("i.IsCompleted:" + i.IsCompleted);
                    
                        //Console.WriteLine("i.CompletedSynchronously:" + i.CompletedSynchronously);
                    //}
                    //else if (obj is OL_Autoload_Lib.AutoloadRqt_FBank)
                    //{

                    //}
                    //else if (obj is OL_Autoload_Lib.AutoloadRqt_NetMsg)
                    //{

                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Send]" + ex.ToString());
            }
        
        }

        public void SendCallback<T>(IAsyncResult ar)
        {
            try
            {
                if(ar.IsCompleted)//lock (lockObj)
                {
                    if (this.IsStop) return;
                    //Console.WriteLine("ar.IsCompleted:" + ar.IsCompleted);
                    //Console.WriteLine("ar.CompletedSynchronously:" + ar.CompletedSynchronously);
                    StateObject<T> tmpObj = (StateObject<T>)ar.AsyncState;
                    SocketError sendErr = 0;
                    
                    int i = tmpObj.workSocket.EndSend(ar, out sendErr);
                    
                    //Console.WriteLine("完成非同步傳送");
                    if (sendErr == SocketError.Success)
                    {
                        tmpObj.RequestNo += 1;
                        tmpObj.dicRequest.Add(tmpObj.RequestNo, tmpObj.SendString);
                        //Console.WriteLine("CurrentThreadId:" + Thread.CurrentThread.ManagedThreadId);
                        Console.WriteLine("資料送出成功: 長度:" + i + "=>" + tmpObj.dicRequest[tmpObj.RequestNo]);
                        this.Recieve<T>(tmpObj);
                    }
                    else
                    {
                        Console.WriteLine("資料送出有問題:" + sendErr.ToString());
                    }
                    this.sendDone.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SendCallback<T>]" + ex.ToString());
            }
        }

        public void Recieve<T>(StateObject<T> client)
        {
            //while (true)
            //{
                this.receiveDone.Reset();
            
                this.mainSck.BeginReceive(client.Receivebuffer, 0, client.Receivebuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                this.receiveDone.WaitOne();
            //}
        }

        public void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    StateObject<T> tmp = (StateObject<T>)ar.AsyncState;
                    SocketError receiveErr;
                    int i = tmp.workSocket.EndReceive(ar,out receiveErr);
                    
                    //this.receiveDone.Set();
                    if (receiveErr == SocketError.Success)
                    {
                        string receiveString = Encoding.ASCII.GetString(tmp.Receivebuffer, 0, i);
                        tmp.ResponseNo += 1;
                        tmp.dicResponse.Add(tmp.ResponseNo, receiveString);
                        
                        Console.WriteLine("Server回應: 長度:" + i + " => " + receiveString);
                    }
                    else
                    {
                        Console.WriteLine("非同步接收失敗:" + receiveErr.ToString());
                    }
                    this.receiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveCallback:" + ex.ToString());
            }
        }


        public void Recieve<T>(T obj)
        {
            throw new NotImplementedException();
        }
    }
}
