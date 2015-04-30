using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace AsyncConnection
{
    /// <summary>
    /// 此銀行的記錄物件
    /// </summary>
    /// <typeparam name="T">來源物件</typeparam>
    public class StateObject : IState
    {

        public StateObject(Encoding encoding)
        {
            this.Encode = encoding;
        }
        public Encoding Encode { get; set; }

        /// <summary>
        /// 和銀行保持連線的Socket
        /// </summary>
        public Socket workSocket { get; set; }

        /// <summary>
        /// 從AP端Connect過來的Socket.
        /// </summary>
        public Socket APSocket = null;

        #region Request and Response Collection

        private int _requestNo = 0;

        /// <summary>
        /// Request id(發出訊息的紀錄ID)
        /// </summary>
        public int RequestNo 
        {
            get
            {
                return this._requestNo;
            }
            set
            {
                if (value == int.MaxValue)
                {
                    this._requestNo = 0;
                }
                else
                {
                    this._requestNo = value;
                }
                
            }
        }

        /// <summary>
        /// response id
        /// </summary>
        private int _responseNo = 0;

        /// <summary>
        /// Response id(回傳訊息的紀錄ID)
        /// </summary>
        public int ResponseNo
        {
            get
            {
                return this._responseNo;
            }
            set
            {
                if (value == int.MaxValue)
                {
                    this._responseNo = 0;
                }
                else
                {
                    this._responseNo = value;
                }
            }
        }

        /// <summary>
        /// 發出訊息的紀錄集合
        /// </summary>
        public IDictionary<int, string> dicRequest = new Dictionary<int, string>();

        /// <summary>
        /// 接收訊息的紀錄集合
        /// </summary>
        public IDictionary<int, string> dicResponse = new Dictionary<int, string>();

        #endregion
        private object lockObj = new object();

        //private T _SendObj;
        /// <summary>
        /// 來源物件(先暫時當作送出字串用)
        /// </summary>
        //public T SendObj
        //{
        //    get
        //    {
        //        return _SendObj;
        //    }
        //    set
        //    {
        //        //TODO...暫時先當進入的是字串
        //        this.SendString = value.ToString();
        //        //
        //        this._SendObj = value;
        //    }
        //}

        #region Send and Receive data

        private string _sendString = null;

        /// <summary>
        /// Size of receive buffer.
        /// </summary>
        public const int BufferSize = 4096;

        private byte[] _ReceiveBuffer = new byte[BufferSize];
        /// <summary>
        /// Receive buffer.
        /// </summary>
        public byte[] ReceiveBuffer 
        {
            get { return _ReceiveBuffer; }
            set { this._ReceiveBuffer = value; }
        }

        /// <summary>
        /// send data(string),自動會編碼成ASCII格式的byte array並存放在SendBytes屬性
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
                        this.SendBuffer = Encode.GetBytes(value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[SendString] convert to byte failed:" + ex.ToString());
                    }
                    this._sendString = value;
                
            } 
        }

        /// <summary>
        /// send byte array(byte array)
        /// </summary>
        public byte[] SendBuffer { get; set; }

        #endregion

        public string ID
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string ReceiveString
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        Socket IState.APSocket
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }

    #region 暫不使用
    //暫不使用  因為無法和SendObject掛勾
    public class ReceiveObject
    {
        public Socket workSocket = null;

        public const int receiveSize = 4096;

        public byte[] receiveBuffer = new byte[receiveSize];
    }
    //暫不使用  因為無法和ReceiveObject掛勾
    public class SendObject
    {
        private string _SendString = null;
        /// <summary>
        /// 工作的Socket
        /// </summary>
        public Socket workSocket = null;

        /// <summary>
        /// 指定的編碼
        /// </summary>
        public Encoding encode { get; set; }
        /// <summary>
        /// 載入送出字串後自動寫入SendBuffer屬性
        /// </summary>
        public string SendString 
        {
            get
            {
                return _SendString;
            }
            set
            {
                if(encode != null)
                {
                    try
                    {
                        SendBuffer = encode.GetBytes(value);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                this._SendString = value;
            }
        }
        /// <summary>
        /// 送出的byte array
        /// </summary>
        public byte[] SendBuffer { get; private set; }
    }
    #endregion

}
