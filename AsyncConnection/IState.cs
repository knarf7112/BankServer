using System.Net.Sockets;

namespace AsyncConnection
{
    /// <summary>
    /// 此次交訊的紀錄
    /// </summary>
    public interface IState : IReceive
    {
        /// <summary>
        /// 暫定MessageType + STAN + 交易時間
        /// </summary>
        string ID { get; set; }

        string SendString { get; set; }

        string ReceiveString { get; set; }

        byte[] SendBuffer { get; set; }

        Socket APSocket { get; set; }

    }

    public interface IReceive
    {
        byte[] ReceiveBuffer { get; set; }

        Socket workSocket { get; set; }
    }
}
