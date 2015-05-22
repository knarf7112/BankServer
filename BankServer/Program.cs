#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;

namespace BankServer
{
    class Program
    {
        static List<a> list = new List<a>(); 
    
        //[Conditional("DEBUG")]
        static void Main(string[] args)
        {

        //    list.Add(new a { a1 = "str", a2 = 10 });
        //    list.Add(new a { a1 = "str2", a2 = 20 });
        //    list.Add(new a { a1 = "str3", a2 = 30 });
        //    var s = list;
        //    clearList(list);
        //    list.Add(new a { a1 = "str4", a2 = 40 });
        //    list[0].a1.ToLower();
        //    var s2 = list;
        //    Console.ReadKey();
            //BankServer b1 = new BankServer(999);
            //b1.Start();
            
            Console.ReadKey();
        }


        public static void Main2()
        {
            //Debug在組態設定為Release時會被忽略,在Debug時會被執行
            //Trace則是在兩種都會出現在輸出
            //或上面的#define註解取消 則Debug的類別都會被執行
            Debug.WriteLine("Test Debug");
            Trace.WriteLine("Test Trace");
            Console.WriteLine("Test Console");
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));//將Debug的輸出複製一份到Console
            Debug.AutoFlush = true;
            Debug.Indent();
            Debug.WriteLine("Entering Main");
            Console.WriteLine("Hello World.");
            Debug.WriteLine("Exiting Main");
            Debug.Unindent();
            Console.WriteLine("Test Console");
            //Debug.WriteLine(Console.Out);
        } 
        private static void clearList(List<a> list)
        {
            //list = new List<a>();
            //list.Clear();
            //list = null;
            list[0].a1 = null;//"Qoo";
        }
    }
    class a
    {
        public string a1 { get; set; }
        public int a2 { get; set; }
    }
    class TestStatic
    {
        public static ArrayList a = new ArrayList();

        public void GetData()
        {
            
        }
    }
    class a1 { public string str { get; set; } }
    class b1 { public int i { get; set; } }
    class c1
    {
        public bool s;
    }
}
