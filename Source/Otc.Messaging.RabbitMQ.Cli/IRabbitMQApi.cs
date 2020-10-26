using Refit;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli
{
    internal interface IRabbitMQApi
    {
        [Get("/overview")]
        Task<ApiResponse<string>> GetOverview();

        [Get("/vhosts/{name}")]
        Task<ApiResponse<string>> GetVHost(string name);

        [Put("/vhosts/{name}")]
        Task<ApiResponse<string>> PutVHost(string name);

        [Put("/policies/{vhost}/{name}")]
        Task<ApiResponse<string>> PutPolicy(string vhost, string name, [Body] string body);
    }
}