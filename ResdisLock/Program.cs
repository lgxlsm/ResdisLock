using ResdisLock.Lock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResdisLock
{
    class Program
    {
        static int i = 1;
        static void Main(string[] args)
        {
            for (int i = 0; i < 1000; i++)
            {
                ThreadStart threadStart = new ThreadStart(GetLock);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            Thread.Sleep(2000);
            Console.ReadLine();
        }

        public static void GetLock()
        {
            try
            {
                using (var Lock = RedisLockExtension.CreateLock("luckLock", TimeSpan.FromMilliseconds(1000), 6))
                {
                    Console.WriteLine(++i);
                }
            }
            catch (RedisLockCreateException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
