using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnEventTest
{
    class Program
    {
        static void Main()
        {
            Holder h = new Holder();
            
            h.OnWriteConsole += (name) => { Console.WriteLine(name + ": this is out Side Message"); return true; };
            h.OnWriteConsole += (name) => { Console.WriteLine(name + ": this is out Side Message2"); return true; };
            WriteConsole e2 = (WriteConsole)h.GetType()//1.取得物件型別
                .GetField("OnWriteConsole", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance)//2.取得FieldInfo資訊
                .GetValue(h);//3.取得值後轉換回宣告的delegate型別
            int eventCount = e2.GetInvocationList().Length;//4.執行delegate的GetInvocationList()方法取得註冊事件的數量長度
            WriteConsole e3 = GetDelegate<Holder, WriteConsole>(h, "OnWriteConsole");
            var s = e3.Method.Name;//.Invoke("KKKKKKKKKKKK");
            
            Console.WriteLine("註冊事件的數量:" + e3.GetInvocationList().Length);
            Console.WriteLine("註冊事件的數量:" + eventCount);
            

            h.console("Qoo");
            Console.ReadKey();
        }
        //取得註冊物件內event的delegate[]
        public static DeleType GetDelegate<T, DeleType>(T obj, string eventName)
        {
            Type objType = obj.GetType();

            FieldInfo fieldInfo = objType.GetField(eventName, System.Reflection.BindingFlags.GetField
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance);
            DeleType objdele = (DeleType)fieldInfo.GetValue(obj);
            //int eventCount = ((T2)objdele).GetInvocationList().Length;
            return objdele;
        }
        public delegate bool WriteConsole(string name);
        public class Holder
        {
            
            public event WriteConsole OnWriteConsole;
            public int count;

            public void console(string name)
            {
                Console.WriteLine("Console say : hi i am beman!!! {0}",count);
                if (OnWriteConsole != null && count < 10)
                {
                    Thread.Sleep(500);
                    bool callback = OnWriteConsole.Invoke(name);
                    if (callback)
                    {
                        count++;
                        console(name + count);
                    }
                }
                
            }
        }
    }
}
