﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace rpc_base.demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            var rpcConfig = new Dictionary<string, string>();
            rpcConfig.Add("ConsumerEnpoint", "rpc_queue");
            rpcConfig.Add("HostName", "localhost");

            var rpcServer = new RPCTest(rpcConfig);


            var thread = new Thread(() => { rpcServer.startServer(); });
            
            thread.Start();

            Thread.Sleep(2000);
            
            var rpcClientConfig = new Dictionary<string, string>();
            rpcClientConfig.Add("ProducerEnpoint", "rpc_queue");
            rpcClientConfig.Add("HostName", "localhost");

            using (var rpcClient = new RpcClient(rpcClientConfig))
            {
                Console.WriteLine(" [x] Requesting fib(30)");
                //var response = rpcClient.Call("30");
                var response = await rpcClient.CallAsync("30");
            
                Console.WriteLine(" [.] Got '{0}'", response);

                Console.WriteLine("client finished");
                thread.Interrupt();
            }
        }
    }
}