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
            // Register Memory Cache for rate limiting
            services.AddMemoryCache();

            // Register Rate Limiter
            services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();

            // Register Claude LLM Service with streaming support
            services.AddHttpClient<StreamingClaudeService>();
            services.AddScoped<IStreamingLLMService, StreamingClaudeService>();
            services.AddScoped<ILLMService>(sp => sp.GetRequiredService<IStreamingLLMService>());

            // Register NLP Service
            services.AddScoped<INLPService, ClaudeNLPService>();

            // Register Tool Services
            services.AddScoped<ITool, SearchTool>();
            services.AddScoped<ITool, CalculatorTool>();
            services.AddScoped<ITool, WeatherTool>();
            services.AddScoped<ITool, ChatTool>();
            services.AddScoped<ITool, StreamingChatTool>();

            // Register Enhanced Tool Registry
            services.AddScoped<EnhancedToolRegistry>();
            services.AddScoped<IToolRegistry>(sp => sp.GetRequiredService<EnhancedToolRegistry>());

            // Register Tool Orchestrator Service
            services.AddScoped<IToolOrchestratorService, ToolOrchestratorService>();

            // Register Augmented Chat Tool (after other tools so it can use them)
            services.AddScoped<ITool, AugmentedChatTool>();

            // Register AI Agent core services
            services.AddScoped<IAgentService, EnhancedAgentService>();

            // Register configuration
            services.Configure<AgentSettings>(configuration.GetSection("AgentSettings"));
            services.Configure<ClaudeSettings>(configuration.GetSection("ClaudeSettings"));
            services.Configure<ChatToolSettings>(configuration.GetSection("ChatToolSettings"));
            services.Configure<RateLimiterSettings>(configuration.GetSection("RateLimiterSettings"));

            return services;
        }
    }
}