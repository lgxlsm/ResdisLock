using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResdisLock.Lock
{
    public interface ILock
    {
        bool Lock(string lockKey);
        bool Lock(string lockKey, TimeSpan expiry);
        bool Lock(string lockKey, TimeSpan expiry, int retryTimes);
        void Unlock();
    }
}
