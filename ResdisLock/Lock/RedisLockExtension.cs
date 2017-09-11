using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResdisLock.Lock
{
    public class RedisLockExtension
    {
        private static StackExchange.Redis.IDatabase db;

        static RedisLockExtension()
        {
            //ConnectionMultiplexer默认会为sub/pub创建单独的连接，如果需要使用sub/pub功能则去掉下面的commands设置  -- by hoho
            //参考：https://stackoverflow.com/questions/28145865/stackexchange-redis-why-does-connectionmultiplexer-connect-establishes-two-clien
            var commands = new Dictionary<string, string>
            {
                {"SUBSCRIBE", null}, // disabled
            };
            ConfigurationOptions config = new ConfigurationOptions()
            {
                AbortOnConnectFail = false,
                ConnectRetry = 10,
                ConnectTimeout = 5000,
                CommandMap = CommandMap.Create(commands),
                ResolveDns = true,
                SyncTimeout = 5000,
                EndPoints = { { "127.0.0.1:6379" } },
                Password = "111111",
                AllowAdmin = true,
                KeepAlive = 180
            };

            var conn = ConnectionMultiplexer.Connect(config);
            if (conn != null && conn.IsConnected)
            {
                db = conn.GetDatabase();
            }
            else
            {
                throw new RedisLockException("Redis is not Connected");
            }

        }

        public static object ConfigurationManager { get; }

        public static LockResult CreateLock(string lockKey, TimeSpan expiry)
        {
            return CreateLock(lockKey, expiry, 3);
        }

        public static LockResult CreateLock(string lockKey, TimeSpan expiry, int retryTimes)
        {
            if (string.IsNullOrEmpty(lockKey))
            {
                throw new RedisLockException(nameof(lockKey) + "is null");
            }
            var lk = new RedisLock(db);
            if (!lk.Lock(lockKey, expiry, retryTimes))
            {
                throw new RedisLockCreateException("create lock failed");
            }
            return new LockResult(lk);
        }
    }

    public class LockResult : IDisposable
    {
        private readonly ILock _lock;

        public LockResult(ILock lockObj)
        {
            _lock = lockObj;
        }

        public void Dispose()
        {
            _lock.Unlock();
        }
    }

    public class RedisLockException : Exception
    {
        public RedisLockException(string message) : base(message)
        {

        }
    }

    /// <summary>
    /// 创建锁失败异常
    /// </summary>
    public class RedisLockCreateException : Exception
    {
        public RedisLockCreateException(string message) : base(message)
        {

        }
    }
}
