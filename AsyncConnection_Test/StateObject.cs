using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AsyncConnection_Test
{
    /// <summary>
    /// 此次電文的狀態
    /// </summary>
    /// <typeparam name="T">來源物件</typeparam>
    public class StateObject<T>
    {

        public StateObject()
        {
            this.OnGetIt = new GetIt(SendtoAP);
        }
        //private delegate 
        /// <summary>
        /// main socket.
        /// </summary>
        public Socket workSocket = null;
        /// <summary>
        /// AP's socket.
        /// </summary>
        public Socket APSocket = null;
        /// <summary>
        /// Size of receive buffer.
        /// </summary>
        public const int BufferSize = 4096;
        /// <summary>
        /// Receive buffer.
        /// </summary>
        public byte[] Receivebuffer = new byte[BufferSize];

        public int RequestNo = 0;
        public int ResponseNo = 0;
        public IDictionary<int, string> dicRequest = new Dictionary<int, string>();
        public IDictionary<int, string> dicResponse = new Dictionary<int, string>();
        private static object lockObj = new object();

        private string _receivedString = null;
        /// <summary>
        /// Received data string.
        /// </summary>
        public string ReceivedString {
            get
            {
                return _receivedString;
            }
            set 
            {
                if (OnGetIt != null)
                {
                    OnGetIt.Invoke(value);
                }
                this._receivedString = value;
            }
        }
        /// <summary>
        /// 來源物件
        /// </summary>
        public T sendObj { get; set; }

        private delegate void GetIt(string msg);

        private GetIt OnGetIt;

        private string _sendString = null;
        public string SendString 
        { 
            get { return _sendString; } 
            set 
            {
                //lock (lockObj)
                //{
                    this.SendBytes = Encoding.ASCII.GetBytes(value);
                    this._sendString = value;
                //}
            } 
        }
        public byte[] SendBytes { get;private set; }
        private void SendtoAP(string receiveMsg)
        {

        }
    }
}
