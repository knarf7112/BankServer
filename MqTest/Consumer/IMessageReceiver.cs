using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MqTest.Consumer
{
    public delegate void MessageReceivedDelegate<T>(T message);
    interface IMessageReceiver : IDisposable
    {
        void Start();
        void Stop();
    }
}
