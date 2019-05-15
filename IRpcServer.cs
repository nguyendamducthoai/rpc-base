using System.Threading.Tasks;

namespace rpc_base
{
    public interface IRpcServer<T>
    {
        Task<T> onMessage(string message);
    }
}