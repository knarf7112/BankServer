using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AsyncConnection
{
    class test1
    {
        public AsyncConnect a1;

        public void a1_OnReceiveAsyncCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {

                StateObject state = (StateObject)ar.AsyncState;
                SocketError receiveErr;
                int receivelength = state.workSocket.EndReceive(ar, out receiveErr);

                string str = Encoding.ASCII.GetString(state.ReceiveBuffer, 0, receivelength);

                Console.WriteLine("這是事件外掛的方法:" + str);
            }
            a1.ReceiveDone.Set();
        }
    }
    class Program
    {
        
        static void Main(string[] args)
        {
            test1 t1 = new test1();
            AsyncConnect a1 = new AsyncConnect("127.0.0.1", 999, 10);
            t1.a1 = a1;
            StateObject state = new StateObject(Encoding.ASCII);
            a1.OnReceiveAsyncCallback += t1.a1_OnReceiveAsyncCallback;
            a1.Start();
            a1.StartAsyncReceive(state);
            object olock = new object();
            //a1.Send(a1.mainSck);
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread((object o) =>
                {
                    
                    string s = o as string;

                    //Console.WriteLine("s=" + s);
                    lock (state)
                    {
                        state.SendString = s;
                        
                        a1.Send(state);
                    }
                });
                t.Start("Qoo" + i);
                //t.Join();
            }
            //Thread.Sleep(3000);
            //--------停止接收-------
            //char key = Console.ReadKey().KeyChar;
            //if (key == 'y')
            //{
            //    a1.KeepReceive = false;
            //}
            //else
            //{
            //    a1.KeepReceive = true;
            //}
            //----------------------
            Thread.Sleep(5000);
            var s1 = a1;
            Console.ReadKey();
            a1.Stop();
            //Console.ReadKey();
        }
    }
}
