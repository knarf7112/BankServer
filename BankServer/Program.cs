using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace BankServer
{
    class Program
    {
        static List<a> list = new List<a>();     
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
            BankServer b1 = new BankServer(999);
            b1.Start();
            Console.ReadKey();
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
