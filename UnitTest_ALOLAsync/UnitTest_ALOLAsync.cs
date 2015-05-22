using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//
using ALOLAsync;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
namespace UnitTest_ALOLAsync
{
    [TestClass]
    public class UnitTest_ALOLAsync
    {
        ALOLAsync.ALOLAsync connection1 { get; set; }

        string ip = "127.0.0.1";

        int port = 9999;

        Encoding encode = Encoding.ASCII;

        int sendTimeout = 5000;

        [TestInitialize]
        public void init()
        {
            this.connection1 = new ALOLAsync.ALOLAsync("test", "0100", this.ip, this.port, this.sendTimeout, this.encode);
        }

        [TestMethod]
        public void TestMethod1()
        {
            Trace.Write("QOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");
        }

        private Socket ServerSetting()
        {
            //test server
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Any, this.port));
            server.Listen(10);
            return server;
        }

        delegate void DeleTimer(int max);

        DeleTimer go = new DeleTimer((int max) => {
            int i = 0;
            while (i < max)
            {
                i += 100;
                Thread.Sleep(100);
            }
            throw new Exception("等待超時");
        });
        [TestMethod]
        public void Test_ALOLAsyncConnect()
        {            
            //connection
            this.connection1.Start();
            
            //test server
            using (Socket server = this.ServerSetting())
            {
                //IAsyncResult ar = go.BeginInvoke(1000, null, null);

                using (Socket client = server.Accept())
                {
                    //try
                    //{
                    //    go.EndInvoke(ar);

                    //}
                    //catch (Exception ex)
                    //{
                    //    throw ex;
                    //}
                    Assert.IsNotNull(client, "Client Socket = null");
                }

            }
        }

        [TestMethod]
        public void Test_ALOLAsyncSend()
        {
            string actualRequestMsg = "08088888" + "0800" + "82200000000000000400000000000000" + "0507195232" + "195232" + "301";
            string expectRequestMsg = "08088888" + "0800" + "82200000000000000400000000000000" + "0507195232" + "195232" + "301";

            //connection
            this.connection1.Start();

            //test server
            using (Socket server = this.ServerSetting())
            {
                ThreadPool.QueueUserWorkItem(
                    (object obj)=>
                    {
                        //casting
                        string sendString = obj as string;
                        //串資料
                        byte[] definedSize = new byte[2] { 0, (byte)sendString.Length };
                        byte[] msgBytes = this.encode.GetBytes(sendString);
                        byte[] allMsg = new byte[definedSize.Length + msgBytes.Length];
                        definedSize.CopyTo(allMsg, 0);
                        msgBytes.CopyTo(allMsg, definedSize.Length);
                        //等3秒再送
                        Thread.Sleep(3000);
                        this.connection1.Send("0800", allMsg);//資料送出

                    },actualRequestMsg
                );
                //模擬的Server開始接收資料
                using (Socket client = server.Accept())
                {
                    byte[] buffer = new byte[0x1000];
                    int length = client.Receive(buffer);
                    Array.Resize(ref buffer, length);

                    byte[] receiveMsg = buffer.Skip(2).Take(buffer.Length - 2).ToArray();//略過前面2byte的header再帶出所有後面的資料

                    string actualReceiveStr = this.encode.GetString(receiveMsg);
                    Debug.WriteLine("預計送出的資料:" + expectRequestMsg);
                    Debug.WriteLine("實際送出的資料:" + actualReceiveStr);
                    Assert.AreEqual(expectRequestMsg, actualReceiveStr);
                }
            }
        }

        [TestMethod]
        public void Test_ALOLAsyncReceive()
        {
            string actualSendStr = "88880808" + "0810" + "82200000020000000400000000000000" + "0507195232" + "195232" + "00" + "301";
            string expectResponseMsg = "08088888" + "0810" + "82200000020000000400000000000000" + "0507195232" + "195232" + "00" + "301";//response

            //connection
            this.connection1.Start();
            
            

            //test server
            Socket server = this.ServerSetting();
            using (Socket client = server.Accept())
            {
                //this.connection1.Send(,)
                byte[] buffer = new byte[0x1000];
                int length = client.Receive(buffer);
                Array.Resize(ref buffer, length);
            }
        }

        [TestCleanup]
        public void Clean()
        {
            this.connection1.Stop();

        }
    }
}
