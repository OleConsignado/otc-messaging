using Refit;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli
{
    internal interface IRabbitMQApi
    {
        [Get("/vhosts/{name}")]
        Task<ApiResponse<string>> GetVHost(string name);

        [Put("/vhosts/{name}")]
        Task<ApiResponse<string>> PutVHost(string name);
    }
}