using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Core.Tools;

namespace MaidenAIAgent.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAIAgentServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register AI Agent core services
            services.AddScoped<IAgentService, AgentService>();

            // Register Tool Services
            services.AddScoped<IToolRegistry, ToolRegistry>();
            services.AddScoped<ITool, SearchTool>();
            services.AddScoped<ITool, CalculatorTool>();
            services.AddScoped<ITool, WeatherTool>();

            // Register configuration
            services.Configure<AgentSettings>(configuration.GetSection("AgentSettings"));

            return services;
        }
    }
}
