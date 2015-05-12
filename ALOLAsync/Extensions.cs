using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALOLAsync
{
    static class Extensions
    {
        /// <summary>
        /// bytes內的資料轉成字串
        /// byte: {1,255} => string: "1,255"
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="startIndex">起始位置</param>
        /// <param name="size">大小</param>
        /// <returns>Byte(hex string)</returns>
        public static string ByteToString(this byte[] data,int startIndex,int size)
        {
            if (startIndex + size > data.Length)
            {
                throw new ArgumentException("StartIndex + size(" + (startIndex + size) + ") > (" + data.Length + ")data Length");
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                sb.Append(data[startIndex + i].ToString() + ",");//.ToString("X2") + ",");//轉hex用
            }
            sb.Remove((sb.Length - 1), 1);
            return sb.ToString();
        }
        /// <summary>
        /// 依定界符號(複數)切割可列舉的列舉物件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">可列舉的來源</param>
        /// <param name="delimiter">定界符號</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, IEnumerable<T> delimiter)
        {
            //定界符號轉集合
            IList<T> delimiterList = delimiter.ToList();
            //output buffer
            List<T> outputBuffer = new List<T>();
            //counter
            int i = 0;

            //列舉來源物件去比對
            foreach (T item in source)
            {
                //若來源物件與定界符號相同
                if(item.Equals(delimiterList[i]))
                {
                    i++;
                    if (i == delimiterList.Count)
                    {
                        i = 0;
                        if (outputBuffer.Count > 0)
                        {
                            yield return outputBuffer;
                            outputBuffer = new List<T>();
                        }

                    }
                }
                else
                {
                    outputBuffer.AddRange(delimiterList.Take(i));
                    if (item.Equals(delimiterList[0]))
                    {
                        i = 1;
                    }
                    else
                    {
                        i = 0;
                        outputBuffer.Add(item);
                    }
                }
            }

            outputBuffer.AddRange(delimiterList.Take(i));

            if (outputBuffer.Count > 0)
            {
                yield return outputBuffer;
            }
        }
    }
}
