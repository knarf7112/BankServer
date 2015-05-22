using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BankServer
{

    public class TestAsyncAccept
    {
        private int Count = 0;

        private object lockObj = new object();

        private Socket MainSocket { get; set; }
        
        public ManualResetEvent allDone { get; set; }

        private int port;
        public TestAsyncAccept(int port)
        {
            this.port = port;
            this.allDone = new ManualResetEvent(false);
        }

        public void Start()
        {
            if (this.MainSocket == null)
            {
                this.MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            while (true)
            {
                Console.WriteLine("開始一個等待...");
                this.MainSocket.BeginAccept(AcceptCallback, this.MainSocket);
                this.allDone.WaitOne();
                Debug.WriteLine(Console.Out);
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    Socket client = ar.AsyncState as Socket;
                    lock(lockObj)
                    {
                        Count++;
                    }
                    StateObject clientState = new StateObject()
                    {
                        ClinetNO = Count,
                        workSocket = client,
                    };
                    //client.BeginSend()
                    //client.BeginReceive(clientState.buffer,0,clientState.buffer.Length,SocketFlags.None,)
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AcceptCallback] Error:" + ex.StackTrace);
            }
        }
    }
}
