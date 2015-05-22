using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Diagnostics;
namespace MulTiClient
{
    class Program
    {
        private static int newTask(int ms)
        {
            Console.WriteLine("任務開始");
            Thread.Sleep(ms);
            Random random = new Random();
            int n = random.Next(10000);
            Console.WriteLine("任務完成");
            return n;
        }

        private delegate int NewTaskDelegate(int ms);


        static void Main(string[] args)
        {
            Debug.WriteLine("Qoo1");
            Debug.Print("TEst123");
            Console.WriteLine("Qoo2");
            string dateTime = "20150312122430";
            DateTime d = DateTime.ParseExact(dateTime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);

            NewTaskDelegate task = newTask;
            IAsyncResult asyncResult = task.BeginInvoke(2000, null, null);

            // EndInvoke方法将被阻塞2秒
            int result = task.EndInvoke(asyncResult);
            Console.WriteLine(result);
            Console.ReadKey();
        }
        //--------------------------------------------------------------------------
        static void Main2(string[] args)
        {
            Task<int> t = AccessTheWebAsync();
            Console.WriteLine((int)t.Result);
            for (int i = 0; i < 20; i++)
            {
                object obj = new object();
                Task.Factory.StartNew(
                    (o) =>
                    {
                        lock (obj)
                        {
                            int tmp = (Int32)o;
                            StaticTest t1 = new StaticTest();
                            //t1.GetStatus();
                            if (tmp == 10)
                            {
                                t1.ChangeStatus();
                            }

                            if (tmp == 15)
                            {
                                t1.ChangeStatus();
                            }

                            t1.GetStatus();
                        }
                    },i);//.Start();
            }
            Console.ReadKey();
        }

        async static Task<int> AccessTheWebAsync()
        {
            //Console.ReadKey();

            //int i = await GetInt();
            //return i;
            // You need to add a reference to System.Net.Http to declare client.

            HttpClient client = new HttpClient();

            // GetStringAsync returns a Task<string>. That means that when you await the
            // task you'll get a string (urlContents).
            Task<string> getStringTask = client.GetStringAsync("http://msdn.microsoft.com");

            // You can do work here that doesn't rely on the string from GetStringAsync.
            //DoIndependentWork();

            // The await operator suspends AccessTheWebAsync.
            //  - AccessTheWebAsync can't continue until getStringTask is complete.
            //  - Meanwhile, control returns to the caller of AccessTheWebAsync.
            //  - Control resumes here when getStringTask is complete. 
            //  - The await operator then retrieves the string result from getStringTask.
            string urlContents = await getStringTask;

            // The return statement specifies an integer result.
            // Any methods that are awaiting AccessTheWebAsync retrieve the length value.
            return urlContents.Length;
        }

        private static Task<int> GetInt()
        {
            //throw new NotImplementedException();
            Task<int> i = Task.Factory.StartNew(() => {
                int o = 10; return o + 10;
            });
            return i;
        }
    }
}
