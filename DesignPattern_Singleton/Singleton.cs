using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern_Singleton
{
    class Singleton
    {
        private static Singleton _instance;

        //Lock synchronization object
        private static object syncLock = new object();
        /// <summary>
        /// Constructor is private
        /// </summary>
        private Singleton()
        {

        }

        public static Singleton Instance()
        {
            //雙重檢查是避免multi threaded每次呼叫此方法都被lock住
            // Support multithreaded applications through

            // 'Double checked locking' pattern which (once

            // the instance exists) avoids locking each

            // time the method is invoked
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Singleton();
                    }
                }
            }
            return _instance;
        }
    }

    class TypeSelect
    {
        public Type getType()
        {//int a = 10, int b = 5){
            int a = 1, b = 2;
            switch (1)
            {
                case 1:
                    while (true) { Console.WriteLine("Loop..."); }
                    return typeof(a1);
            }
        }
        public bool count(int x ,int y)
        {
            return x > y;
        }
    }
    interface Ia{}
    class a3:Ia{}
    class a4:Ia{}
    class a5:Ia{}
}
