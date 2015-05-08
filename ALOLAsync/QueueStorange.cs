using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALOLAsync
{
    /// <summary>
    /// byte array 串接用
    /// Performance ref: http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
    /// </summary>
    public class QueueStorange
    {
        #region Field
        private Queue<byte> bufferQ;

        #endregion

        #region Constructor
        public QueueStorange()
        {
            this.bufferQ = new Queue<byte>();
        }
        #endregion

        #region public Method

        /// <summary>
        /// 取得目前Queue存放的資料大小
        /// </summary>
        /// <returns>目前資料大小</returns>
        public int GetLength()
        {
            return this.bufferQ.Count;
        }

        /// <summary>
        /// 將資料寫入Queue
        /// </summary>
        /// <param name="data"></param>
        public void InsertData(byte[] data)
        {
            lock (this.bufferQ)
            {
                foreach (byte b in data.AsEnumerable())
                {
                    this.bufferQ.Enqueue(b);
                }
            }
        }

        /// <summary>
        /// 取得先進Queue的資料
        /// </summary>
        /// <param name="length">out出來的的陣列長度</param>
        /// <returns></returns>
        public virtual byte[] GetData(int length)
        {
            if (length > this.bufferQ.Count)
                throw new Exception("需求長度(" + length + ")大於Queue內資料長度(" + this.bufferQ.Count + ")");
            
            byte[] tmpAry = new byte[length];
            lock (this.bufferQ)
            {
                for (int i = 0; i < length; i++)
                {
                    tmpAry[i] = this.bufferQ.Dequeue();//將資料吐出來
                }
            }
            
            return tmpAry;
            
        }

        /// <summary>
        /// 讀取定義的資料大小
        /// </summary>
        /// <param name="defineSize">自定義Byte數量</param>
        /// <returns>byte[]</returns>
        public byte[] GetQueueDefineSizeBytes(int defineSize)
        {
            IEnumerable<byte> QbufferEnumerator = this.bufferQ.AsEnumerable();
            byte[] defineBytes = new byte[defineSize];
            int count = 0;
            foreach (byte b in QbufferEnumerator)
            {
                if (count >= defineSize)
                {
                    break;
                }
                defineBytes[count] = b;
                count++;
            }

            return defineBytes;
        }

        /// <summary>
        /// 轉換自定義資料長度 
        /// byte[]{ 1, 2 } => 258:(int)
        /// </summary>
        /// <param name="count">定義碼數</param>
        /// <returns></returns>
        public virtual int ByteAryToInteger(byte[] defineSize)
        {
            int definelength = 0;

            for (int i = 0; i < defineSize.Length; i++)
            {
                byte b = defineSize[i];
                definelength += (b << (8 * (defineSize.Length - (i + 1))));
            }

            return definelength;
        }

        /// <summary>
        /// 轉換自定義資料長度
        /// 258(int) => (byte[]){ 1, 2 }
        /// </summary>
        /// <param name="dataSize">自定義的資料大小</param>
        /// <returns></returns>
        public virtual byte[] IntegerToByteAry(int dataSize)
        {
            Stack<byte> defineBytes = new Stack<byte>();
            do
            {
                byte b = (byte)(dataSize & 0xff);//取最後8bits的值(轉byte)
                defineBytes.Push(b);
                dataSize = dataSize >> 8;//右移8bits(移掉剛取到值的8bits)
            }
            while (dataSize > 0);
            return defineBytes.ToArray();//Pop出來
        }

        /// <summary>
        /// 轉換自定義資料長度
        /// 258(int) => (byte[]){ 1, 2 }
        /// byteCount:3 => 63(int) => (byte[]){ 0, 0, 63 }
        /// </summary>
        /// <param name="dataSize">自定義的資料大小</param>
        /// <param name="byteCount">佔幾個byte</param>
        /// <returns></returns>
        public virtual byte[] IntegerToByteAry(int dataSize, int byteCount)
        {
            byte[] result = this.IntegerToByteAry(dataSize);
            //byte[] resultAry = new byte[byteCount

            //Pop出來的byte陣列數量 < 定義的大小
            if (result.Length < byteCount)
            {
                byte[] resultAry = new byte[byteCount];
                int j, i;
                for (j = result.Length - 1, i = 1; j >= 0; j--, i++)
                {
                    resultAry[byteCount - i] = result[j];
                }
                //for (int i = byteCount - result.Length; i < byteCount; )
                //{
                //    i++;
                //    result[i] = 0x00;
                //}
                return resultAry;
            }
            return result;
        }

        /// <summary>
        /// Queue內存放的暫存資料(hex string)
        /// display=> FF,01,1A,F3,AA,.......
        /// </summary>
        /// <returns>hex string</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in this.bufferQ.AsEnumerable())
            {
                sb.Append(b.ToString("X2") + ",");//將Queue存放的每個byte轉成hex字串顯示用
            }
            if (this.bufferQ.Count >= 1)
                sb.Remove((sb.Length - 1), 1);
            return sb.ToString();
        }
        #endregion

    }
}
