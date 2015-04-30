using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ImplementMQ
{
    interface IQQ
    {
        string s2 { get; set; }
    }
    class QQ : ICloneable, IQQ
    {
        public string s1;
        public int i1;
        public bool b1;
        public QQ q1;

        public string s2 { get; set; }
        public int i2 { get; set; }
        public bool b2 { get; set; }
        public QQ q2 { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    class QQ2 : ICloneable, IQQ
    {
        public string s2 { get; set; }
        public string s3 { get; set; }
        public int i3 { get; set; }
        public bool b3 { get; set; }
        public QQ2 q3 { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    class testPropertyuseRef
    {
        public class poco1
        {
            public string str1 { get; set; }

            public string str2 { get; set; }

            public int i1 { get; set; }

            public int i2 { set; get; }

            public bool b1 { get; set; }

            public string[] strAry { get; set; }

            public int[] iAry { get; set; }

            public override string ToString()
            {
                return string.Format("str1:{0}\nstr2:{1}\ni1:{2}\ni2:{3}\nb1:{4}\nstrAry:{5}\niAry:{6}", str1, str2, i1, i2, b1, string.Join(",", strAry), string.Join(",", iAry.Select(x => x.ToString()).ToArray()));
            }
        }

        public poco1 p { get; set; }

        public void pocoIn(poco1 p)
        {
            Console.WriteLine("改變一些值");
            p.i1 = 999;
            p.str1 = "changed";
            p.strAry = new string[] { "12", "34", "56" };
            Console.WriteLine("(內部)改變後的結果:\n" + p.ToString());
        }

        public void callpocoIn()
        {
            Console.WriteLine("第一次定義物件屬性值");
            this.p = new poco1()
            {
                i1 = 123,
                str1 = "str1",
                strAry = new string[] { "ab", "cd", "ef" },
                str2 = "str2",
                iAry = new int[] { 9,8,7,6,5,4,3,2,1},
            };
            Console.WriteLine("(外部)第一次結果:\n" + this.p.ToString());

            this.pocoIn(this.p);

            Console.WriteLine("(外部)最後結果:\n" + this.p.ToString());
        }

    }
    class Program
    {
        static void Main()
        {
            testPropertyuseRef test1 = new testPropertyuseRef();
            
            test1.callpocoIn();
            Console.ReadKey();
        }

        static void Main1(string[] args)
        {
            Dictionary<string, string> dic1 = new Dictionary<string, string>();
            dic1.Add("a1", "abc123deftest1");
            dic1.Add("b1", "bbc123deftest1");
            dic1.Add("c1", "cbc123deftest1");
            bool has = "abcd".Contains("bc");
            bool include = dic1["a1"].IndexOf("123") == -1;

            //string result = dic1.Values.SingleOrDefault<string>((string n) => n.IndexOf("cbc") == -1);
            Console.ReadKey();
        }

        static void OutTest(out SocketError err)
        {
            try
            {
                err = SocketError.SocketError;
                {
                    err = SocketError.Success;
                }
            }
            finally
            {
                err = SocketError.TimedOut;
            }
        }

        static void TryCasting()
        {
            QQ obj1 = new QQ
            {
                s1 = "s1",
                i1 = 999,
                b1 = true,
                q1 = new QQ
                {
                    s1 = "s1-1",
                    i1 = 9999,
                    b1 = false,
                }
                ,
                s2 = "s2",
                i2 = 99,
                b2 = true,
                q2 = new QQ()
            };
            object qq1 = obj1 as QQ;
            QQ2 obj2 = new QQ2() { s2 = "qq2" };

            bool isTheSame = qq1 is QQ2;
            var cast1 = (QQ2)qq1;//as QQ2;
        }
    }
}
