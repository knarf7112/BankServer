using System;
//
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;

namespace MqTest.Producer
{
    public class TopicPublisher :IMessageSender
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(TopicPublisher));
        private bool disposed = false;
        private ITopic topic;
        private ISession session;
        private IConnection conn;
        //
        public string BrokerURL { get; set; }
        public string Destination { get; set; }
        public IMessageProducer Producer { get; set; }
        #region Constructor
        public TopicPublisher()
        {
            this.disposed = true;
        }
        public TopicPublisher(string brokerURL, string destination)
        {
            this.BrokerURL = brokerURL;
            this.Destination = destination;
            this.disposed = true;
        }
        #endregion
        public void SendMessage<T>(T message)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
            ITextMessage textMessage = this.Producer.CreateTextMessage(message as string);
            this.Producer.Send(textMessage);
        }

        public void Start()
        {
            if (null == this.BrokerURL || null == this.Destination)
            {
                throw new Exception("Parameter Error....");
            }
            if (this.disposed)
            {
                IConnectionFactory connectionFactory = new ConnectionFactory(this.BrokerURL);
                this.conn = connectionFactory.CreateConnection();
                this.conn.Start();
                this.session = conn.CreateSession();
                this.topic = new ActiveMQTopic(this.Destination);
                this.Producer = this.session.CreateProducer(this.topic);
                this.disposed = false;
            }
        }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            Console.WriteLine("Run Dispose...");
            if (!this.disposed)
            {
                this.Producer.Close();
                this.Producer.Dispose();
                this.topic.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        }
    }
}
