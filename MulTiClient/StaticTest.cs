using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MulTiClient
{
    public class StaticTest
    {
        static bool Status;
        static int count;
        object lc = new object();
        public void ChangeStatus()
        {
            lock (lc)
            {
                Status = !Status;
                //count++;
            }
        }

        public void GetStatus()
        {
            lock (lc)
            {
                count++;

                Console.WriteLine("第" + count.ToString("D4") + "次狀態:" + Status.ToString());
            }
        }
    }
}
