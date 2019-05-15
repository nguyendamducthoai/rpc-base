using System.Collections.Generic;
using System.Threading.Tasks;

namespace rpc_base.demo
{
    public class RPCTest : RpcServer
    {
        public RPCTest(Dictionary<string, string> config) : base(config)
        {
            
        }

        public override Task<string> onMessage(string message)
        {
            return Task.Run(() => { return $"{message} processed"; });
        }
    }
}