using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//

namespace OnEventTest
{
    class Class1
    {
        //取得註冊事件的數量總數
        static void Main1(string[] args)
        {
            DataTable dt = new DataTable();
            
            string typeName = "OnEventTest.SomeClass, OnEventTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            string eventName = "SomeEvent";

            Type declaringType = Type.GetType(typeName);
            object target = Activator.CreateInstance(declaringType);

            EventHandler eventDelegate;
            eventDelegate = GetEventHandler(target, eventName);
            if (eventDelegate == null) { Console.WriteLine("No listeners"); }

            // attach a listener
            SomeClass bleh = (SomeClass)target;
            bleh.SomeEvent += delegate { };
            bleh.SomeEvent += bleh_SomeEvent;
            bleh.SomeEvent += bleh_SomeEvent;
            bleh.SomeEvent -= bleh_SomeEvent;
            //

            eventDelegate = GetEventHandler(target, eventName);
            if (eventDelegate == null)
            { 
                Console.WriteLine("No listeners"); 
            }
            else
            { 
                Console.WriteLine("Listeners: " + eventDelegate.GetInvocationList().Length); 
            }

            Console.ReadKey();

        }

        private static void bleh_SomeEvent(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        static EventHandler GetEventHandler(object classInstance, string eventName)
        {
            Type classType = classInstance.GetType();
            FieldInfo eventField = classType.GetField(eventName, BindingFlags.GetField
                                                               | BindingFlags.NonPublic
                                                               | BindingFlags.Instance);

            EventHandler eventDelegate = (EventHandler)eventField.GetValue(classInstance);

            // eventDelegate will be null if no listeners are attached to the event
            if (eventDelegate == null)
            {
                return null;
            }

            return eventDelegate;
        }
    }

    class SomeClass
    {
        public event EventHandler SomeEvent;
    }

}
