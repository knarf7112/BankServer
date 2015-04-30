using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ALOLAsync
{

    class Program
    {

        static void Main(string[] args)
        {
            SimpleServer s1 = null;
            ThreadPool.QueueUserWorkItem((object o) => {
                s1 = new SimpleServer(6108, 20, 6000, 6000);
                s1.Start2(); 
            });
            
            //-------------------------------------------------------------------------------
            //-----------------Component Test------------------------------------------------
            //ALOLAsync a1 = new ALOLAsync("000", "127.0.0.1", 999,  3000, Encoding.ASCII);
            //a1.Start();

            //Console.WriteLine("按任意鍵後開始送出資料...");
            //Console.ReadKey();
            //string key = a1.Send("Qoo1", "hiIambeman", true);
            //a1.Send("Qoo2", "Hello!!!");
            ////Thread.Sleep(15000);
            //Console.WriteLine("按任意鍵後開始委派任務是否完成...");
            //Console.ReadKey();
            //if (a1.IsSuccess(key))
            //{
            //    a1.RevomeKey(key);
            //};
            
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine("確定要結束程序?...y/n");
            }
            while (key.Key != ConsoleKey.Y);
            Console.WriteLine("開始結束程序.......");
            s1.Stop();
        
        }
        static int ch(int i)
        {
            int j = i + 1 ;
            j = j * 2;
            return j;
        }
    }
}
