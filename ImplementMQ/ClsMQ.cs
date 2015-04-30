using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ImplementMQ
{
    class ClsMQ<T>
    {

        private Queue<T> queue;

        private Timer timer;

        public ClsMQ()
        {
            timer.Elapsed += timer_Elapsed;
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (queue.Count() > 0)
            {

            }
        }

        public void SetTimer(double interval = 1000, bool start = false)
        {
            this.timer = new Timer(interval);
            this.timer.AutoReset = true;//每次Reset
            this.timer.Enabled = start;
        }
        public void EnQueue(T obj)
        {
            queue.Enqueue(obj);
        }

        public void DeQueue()
        {
            
            T send = queue.Dequeue();
        }
    }
}
