using Refit;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli
{
    internal interface IRabbitMQApi
    {
        [Get("/overview")]
        Task<ApiResponse<string>> GetOverviewAsync();

        [Get("/vhosts/{name}")]
        Task<ApiResponse<string>> GetVHostAsync(string name);

        [Put("/vhosts/{name}")]
        Task<ApiResponse<string>> PutVHostAsync(string name);

        [Put("/policies/{vhost}/{name}")]
        Task<ApiResponse<string>> PutPolicyAsync(string vhost, string name, [Body] string body);
    }
}