using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibRedis
{
    public class RedisClient
    {
        ConnectionMultiplexer redis = null;
        IDatabase db = null;
        public RedisClient(int database=0)
        {
            redis = ConnectionMultiplexer.Connect("localhost");
            db = redis.GetDatabase(database);
        }
        ~RedisClient() 
        {
            if (redis != null)
                redis.Close();
        }
        public bool setValueByKey(string key, string value)
        {
            return db.StringSet(key, value);
        }
        public string getValueByKey(string key)
        {
            string ret = null;
            RedisValue v = db.StringGet(key);
            if (!v.IsNullOrEmpty)
                ret = v;
            return ret;
        }
        public bool setCallbackForIncomingMessage(string channel, Action<string> cb)
        {
            bool ret = false;
            if (cb != null)
            {
                var sub = redis.GetSubscriber();
                sub.Subscribe(channel, (c, m) => 
                {
                    cb(m);
                });
                ret = true;
            }
            return ret;
        }
        public void test()
        {
            var sub = redis.GetSubscriber();
            // publish test "message"
            sub.Subscribe("test", (channel, message) =>
            {
                System.Console.WriteLine($"channel: {channel}, message={message}");
            });

        }
    }
}
