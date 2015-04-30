using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//
using MqTest.Producer;
namespace BankServer
{
    public class BankServer
    {
        private Socket BankSocket { get; set; }
        
        public ManualResetEvent allDone { get; set; }

        private int port;
        public BankServer(int port)
        {
            this.port = port;
            this.allDone = new ManualResetEvent(false);
        }

        public void Start()
        {
            if (this.BankSocket == null)
            {
                //create Tcp/Ip Socket
                this.BankSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            try
            {
                //Bind Socket to the IPEndPoint
                this.BankSocket.Bind(new IPEndPoint(IPAddress.Any, this.port));
                this.BankSocket.Listen(100);
                int count = 1;
                while (true)
                {
                    allDone.Reset();
                    Console.WriteLine("Waiting for a connection...{0}",count++);
                    this.BankSocket.BeginAccept(new AsyncCallback(CallBack), this.BankSocket);//開始非同步監聽,並把自己(socket)傳入當參數給方法
                    
                    allDone.WaitOne();//封鎖執行緒,為了保持有一個監聽者持續接受連線
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">> Socket BeginAccept Error:" + ex.ToString());
            }
            //Console.WriteLine("\nPress ENTER to continue...");
            //Console.Read();
        }


        public void CallBack(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            this.allDone.Set();

            // Get the socket that handles the client request.
            Socket BKSocket = ar.AsyncState as Socket;
            Socket handler = BKSocket.EndAccept(ar);

            // Create the state object.
            StateObject obj = new StateObject();
            obj.workSocket = handler;
            NetworkStream ns = new NetworkStream(handler);
            handler.BeginReceive(obj.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), obj);
        }
        
        public void ReadCallback(IAsyncResult ar)
        {
            string content = String.Empty;
            
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = ar.AsyncState as StateObject;
            Socket handler = state.workSocket;
            SocketError sckErr;
            // Read data from the client socket. 
            int bytesRaed = handler.EndReceive(ar,out sckErr);
            if (sckErr != SocketError.Success) return;
            if (bytesRaed > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRaed));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();

                if (content.IndexOf("true") > -1)
                {
                    string brokerURL = "tcp://10.27.68.155:61616";//localhost:61616";
                    string destination = "BankAlive";
                    IMessageSender publisher = new TopicPublisher(brokerURL, destination);
                    publisher.Start();
                    publisher.SendMessage<string>("true");
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);

                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else if (content.IndexOf("false") > -1)
                {
                    string brokerURL = "tcp://10.27.68.155:61616";//"tcp://localhost:61616";
                    string destination = "BankAlive";
                    IMessageSender publisher = new TopicPublisher(brokerURL, destination);
                    publisher.Start();
                    publisher.SendMessage<string>("false");
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);
                    
                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private void Send(Socket handler,string data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = ar.AsyncState as Socket;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void Stop()
        {
            if (this.BankSocket != null)
            {

            }
        }
    }
}
