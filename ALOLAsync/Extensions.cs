using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALOLAsync
{
    static class Extensions
    {
        /// <summary>
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
    }
}
