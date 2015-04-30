﻿using System;
//using Common.Logging;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;

namespace MqTest.Consumer
{
    public class QueueReceiver : IMessageReceiver
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(QueueReceiver));
        //
        private IConnection conn;
        private ISession session;
        private IQueue queue;
        private bool disposed = false;
        private IMessageConsumer Consumer;
        //
        public string Destination { get; set; }
        public string BrokerURL { get; set; }
        public string ConsumerId { get; set; }
        //
        public event MessageReceivedDelegate<string> OnMessageReceived; 
        //
        public QueueReceiver()
        {
            this.disposed = true;
        }

        public QueueReceiver( string brokerURL, string destination )
        {
            this.BrokerURL = brokerURL;
            this.Destination = destination;
            this.disposed = true;
        }      

        public void Start()
        {
            if (null == this.BrokerURL || null == this.Destination)
            {
                throw new Exception("Parameter Error...");
            }
            if (this.disposed)
            {
                IConnectionFactory connectionFactory = new ConnectionFactory(this.BrokerURL);
                this.conn = connectionFactory.CreateConnection();
                this.conn.Start();
                this.session = conn.CreateSession();
                //
                this.queue = new ActiveMQQueue(this.Destination);
                //
                this.Consumer = this.session.CreateConsumer(this.queue);
                this.Consumer.Listener +=
                (
                    message =>
                    {
                        ITextMessage textMessage = message as ITextMessage;
                        if (textMessage == null)
                        {
                            throw new InvalidCastException();
                        }
                        if (OnMessageReceived != null)
                        {
                            OnMessageReceived(textMessage.Text);
                        }
                    }
                );
                this.disposed = false;
            }
        }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Consumer.Close();
                this.Consumer.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        } 
    }
}
