using MaidenAIAgent.Core.Models;
using MaidenAIAgent.Core.Services;
using MaidenAIAgent.Core.Tools;
using MaidenAIAgent.Infrastructure.Services;
using MaidenAIAgent.Shared.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            services.AddScoped<ITool, ChatTool>();

            // Register Memory Cache for rate limiting
            services.AddMemoryCache();

            // Register Rate Limiter
            services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();

            // Register Claude LLM Service
            // Register the base service first
            services.AddHttpClient<ClaudeService>();
            services.AddScoped<ILLMService, RateLimitedClaudeService>();

            // Register configuration
            services.Configure<AgentSettings>(configuration.GetSection("AgentSettings"));
            services.Configure<ClaudeSettings>(configuration.GetSection("ClaudeSettings"));
            services.Configure<ChatToolSettings>(configuration.GetSection("ChatToolSettings"));
            services.Configure<RateLimiterSettings>(configuration.GetSection("RateLimiterSettings"));

            return services;
        }
    }
}