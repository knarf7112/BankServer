
using System.Net.Sockets;
namespace AsyncConnection
{
    public interface IAsyncConnect
    {
        /// <summary>
        /// 啟動連線(成功True/False失敗)
        /// </summary>
        bool Start();
        /// <summary>
        /// 停止連線
        /// </summary>
        void Stop();
        /// <summary>
        /// 處理Context
        /// </summary>
        void Send(IState state);

        /// <summary>
        /// 檢查Sokcet連線狀態
        /// </summary>
        /// <param name="sck">Sokcet物件</param>
        /// <returns>連線/斷線</returns>
        bool CheckConnect(Socket sck);
    }
}
