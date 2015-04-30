using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncConnection_Test
{
    public interface IAsyncConnect<T>
    {
        /// <summary>
        /// 啟動非同步連線
        /// </summary>
        void Start(int retryCount);
        /// <summary>
        /// 停止非同步連線
        /// </summary>
        void Stop();
        /// <summary>
        /// 非同步送資料
        /// </summary>
        void Send<T>(T obj);
        /// <summary>
        /// 非同步收資料
        /// </summary>
        void Recieve<T>(T obj);
    }
}
