using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace DesignPattern_Singleton
{
    class test
    {
        public string name { get; set; }
        public bool check { get; set; }
    }
    abstract class a1
    {
        public static int i;
        public a1()
        {
            i++;
            if (dic == null || dic.Count == 0)
            {
                dic= new Dictionary<string, test>();
                test same = new test { name = "第一銀", check = false };
                dic.Add("0001", same);
                dic.Add("0002", same);
                dic.Add("0003", new test { name = "中信銀", check = false });
                dic.Add("0004", new test { name = "台新銀", check = true });
                dic.Add("0005", new test { name = "台灣銀", check = true });
            }
        }

        public static IDictionary<string, test> dic;
    }
    class a2 : a1
    {

        public a2()
        {
            geti();
            foreach (var item in dic.Keys)
            {
                Console.WriteLine("Key:" + item + " => " + dic[item].name + "銀行狀態:" + dic[item].check);
            }
        }
        public void add()
        {
            dic.Add(new KeyValuePair<string, test>());
        }
        public void geti()
        {
            var diic2 = dic;
            Console.WriteLine("目前的i=" + i);
        }
    }
    class Program
    {
          
        static void Main(string[] args)
        {
            
            //
            ManualResetEvent test = new ManualResetEvent(false);
            bool isReset = test.Reset();
            
            if(isReset)
            {
                Console.WriteLine("waitHandle set?..Y/N");
                ConsoleKeyInfo readKey = Console.ReadKey();
                if (readKey.Key == ConsoleKey.Y)
                {
                    test.Set();
                }
                bool signal = test.WaitOne(new TimeSpan(0, 0, 5));
                Console.WriteLine("Signal:" + signal.ToString());
            }
            Console.WriteLine("passed the wait..");
            Console.ReadKey();
            //
            a2 q1;
            for (int i = 0; i < 10; i++)
            {
                if (i == 5)
                {
                    a2.dic["0002"].check = true;
                }
                q1 = new a2();
            }
            q1 = new a2();
            Console.ReadKey();
            // Constructor is protected -- cannot use new

            Singleton s1 = Singleton.Instance();

            Singleton s2 = Singleton.Instance();

            

            // Test for same instance

            if (s1 == s2)
            {

                Console.WriteLine("Objects are the same instance");

            }



            // Wait for user

            Console.ReadKey();
        }
    }
}
