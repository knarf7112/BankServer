using System;
//
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;

namespace MqTest.Consumer
{
    public class TopicSubscriber : IMessageReceiver
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(TopicSubscriber));
        //
        private IConnection conn;
        private ISession session;
        private ITopic topic;
        private bool disposed = false;
        private IMessageConsumer Consumer;
        //
        public string BrokerURL { get; set; }
        public string Destination { get; set; }
        public string ConsumerId { get; set; }

        public event MessageReceivedDelegate<string> OnMessageReceived;
        //
        public TopicSubscriber()
        {
            this.disposed = true;
        }

        public TopicSubscriber(string brokerURL, string destination)
        {
            this.BrokerURL = brokerURL;
            this.Destination = destination;
            this.disposed = true;
        }
        //
        public void Start()  
        {
            if (null == this.BrokerURL || null == this.Destination)
            {
                throw new Exception("Parameter Error...");
            }
            if (this.disposed)
            {
                //MQ內的固定流程
                //產生連結工廠
                IConnectionFactory connectionFactory = new ConnectionFactory(this.BrokerURL);
                //建立聯結
                this.conn = connectionFactory.CreateConnection();
                //啟動連結
                this.conn.Start();
                //(重點)建立Session
                this.session = conn.CreateSession();
                //建立發佈者(依賴Destination)
                this.topic = new ActiveMQTopic(this.Destination);
                //this.Consumer = this.session.CreateDurableConsumer(this.topic, this.ConsumerId, null, false);
                //建立訂閱者(依賴Destination)
                this.Consumer = this.session.CreateConsumer(this.topic);
                //註冊訂閱者監聽事件
                this.Consumer.Listener +=
                (
                    message =>
                    {
                        ITextMessage textMessage = message as ITextMessage;
                        if (textMessage == null)
                        {
                            throw new InvalidCastException();
                        }
                        //Fire Event
                        if (OnMessageReceived != null)
                        {
                            //log.Debug(this.ConsumerId + " Run OnMessageReceived: " + textMessage);
                            OnMessageReceived(textMessage.Text);
                        }
                    }
                );
                this.disposed = false;
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Consumer.Close();
                this.Consumer.Dispose();
                this.topic.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        }
        public void Stop()
        {
            this.Dispose();
        }
    }
}
