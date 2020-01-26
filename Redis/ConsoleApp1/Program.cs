using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //LibRedis.RedisClient.test();
            LibRedis.RedisClient c = new LibRedis.RedisClient();
            c.setValueByKey("Key1", "value");
            string s = c.getValueByKey("Key1");
            //c.test();
            //c.setCallbackForIncomingMessage("test", (msg) => 
            //{
            //    System.Console.WriteLine(msg);
            //});
            Task t1 = Task.Run(() => 
            {
                c.setCallbackForIncomingMessage("test", (s1) => 
                {
                    System.Console.WriteLine($"Task1: {s1}");
                });
            });
            Task t2 = Task.Run(() =>
            {
                c.setCallbackForIncomingMessage("test", (s2) =>
                {
                    System.Console.WriteLine($"Task2: {s2}");
                });
            });
            System.Console.ReadKey();
        }
    }
}
