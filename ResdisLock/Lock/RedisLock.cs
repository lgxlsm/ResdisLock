using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace ResdisLock.Lock
{
    sealed class RedisLock : ILock
    {
        //单实例，未实现多实例 如果是集群需要最少 实例数量/2+1个通过才算锁
        bool _isLock = false;
        IDatabase _db;

        string _lockKey = string.Empty;
        TimeSpan _expiry = TimeSpan.FromMilliseconds(100);//redis值过期时间
        TimeSpan _retryDelay = TimeSpan.FromMilliseconds(10);//线程休眠时间
        int _retryTimes = 3;//重新获取lock次数
        int _ttlFix = 1;
        string _keyPrefix = "LOCK_KEY:{0}";
        long _lockValue;
        const String UnlockScript = @"
            if redis.call(""get"",KEYS[1]) == ARGV[1] then
                return redis.call(""del"",KEYS[1])
            else
                return 0
            end";
        public RedisLock(IDatabase db)
        {
            _db = db;
        }

        public bool Lock(string lockKey)
        {
            return Lock(lockKey, _expiry);
        }

        public bool Lock(string lockKey, TimeSpan expiry)
        {
            return Lock(lockKey, expiry, _retryTimes);
        }

        public bool Lock(string lockKey, TimeSpan expiry, int retryTimes)
        {
            if (string.IsNullOrWhiteSpace(lockKey)) throw new KeyNotFoundException(nameof(lockKey));
            if (expiry == TimeSpan.Zero) throw new NotSupportedException(nameof(expiry));
            if (retryTimes <= 0) throw new NotSupportedException(nameof(retryTimes));

            _lockKey = string.Format(_keyPrefix, lockKey);
            _expiry = expiry;
            _retryTimes = retryTimes;
            for (int i = 0; i < retryTimes; i++)
            {
                _isLock = GetLock();
                if (_isLock) break;
                System.Threading.Thread.Sleep(_retryDelay);
            }
            return _isLock;
        }

        private bool GetLock()
        {
            bool block = false;
            var startTime = DateTime.Now;
            _lockValue = GetLockValue(startTime, _expiry, _ttlFix);
            block = _db.StringSet(_lockKey, _lockValue, _expiry, When.NotExists);
            if (block) return block;
            long keyVal = -1;
            var key = _db.StringGet(_lockKey);
            if (key != RedisValue.Null && key.TryParse(out keyVal))
            {//如果并发没有找到key，进下一个循环，放弃再尝试
                var time = DateTime.FromBinary(keyVal);
                if (time < DateTime.Now)
                {
                    _lockValue = GetLockValue(startTime, _expiry, _ttlFix);
                    var nkey = _db.StringGetSet(_lockKey, _lockValue);
                    if (nkey != RedisValue.Null && key.Equals(nkey))
                    {
                        block = true;
                    }
                }
            }
            return block;
        }

        private long GetLockValue(DateTime startTime, TimeSpan expiry, int ttlFix)
        {
            return startTime.Add(expiry).AddMilliseconds(ttlFix).ToBinary();
        }

        public void Unlock()
        {
            if (!_isLock) return;
            RedisKey[] key = { _lockKey };
            RedisValue[] values = { _lockValue };

            var redis = _db.ScriptEvaluate(
                UnlockScript,
                key,
                values
                );
        }
    }
}
