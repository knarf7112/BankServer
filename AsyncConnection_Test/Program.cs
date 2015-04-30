using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MicrosoftAsync;
namespace AsyncConnection_Test
{
    class Program
    {
        public int i =200;
        static void Main(string[] args)
        {
            AsynchronousServer.StartListening();
            //AsynchronousClient tt = new AsynchronousClient();
            //AsynchronousClient2 tt = new AsynchronousClient2();
            //tt.StartClient();
            //AsyncConnectTest<string> a1 = new AsyncConnectTest<string>("0822", "127.0.0.1", 999, 10, 1000, 1000);
            //a1.Start();

            //for (int i = 0; i < 120; i++)
            //{
            //    //string s2 = "Qoo" + i.ToString();
            //    //a1.Send<string>(s2);
            //    //Thread.Yield();
            //    //a1.SendSynchronous(s2);
            //    Thread t = new Thread((object o) =>
            //    {
            //        string s = o as string;
            //        a1.Send<string>(s);
            //        //a1.SendSynchronous(s);
            //    });
            //    t.Start("Qoo" + i);
            //    //t.Join();
            //}
                //do
                //{
                //    string sendStr = Console.ReadLine();
                //    a1.Send<string>(sendStr);
                //}
                //while (consoleStr != "exit");
                //Thread.Sleep(15000);
            Console.ReadKey();
                //a1.Stop();
            //Console.WriteLine("Start Async Connect...");
            

            
            
            //Console.ReadKey();
        }
    }
}
