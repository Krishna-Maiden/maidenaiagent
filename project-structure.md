# MaidenAI Agent - Project Structure and Build Instructions

## Project Structure

Here is the full project structure with all files:

```
MaidenAIAgent/
│
├── MaidenAIAgent.sln
│
├── README.md
│
├── MaidenAIAgent.API/
│   ├── MaidenAIAgent.API.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Controllers/
│   │   └── AgentController.cs
│   └── Extensions/
│       └── ServiceExtensions.cs
│
├── MaidenAIAgent.Core/
│   ├── MaidenAIAgent.Core.csproj
│   ├── GlobalUsings.cs
│   ├── Models/
│   │   └── AgentModels.cs
│   ├── Services/
│   │   ├── IAgentService.cs
│   │   └── AgentService.cs
│   └── Tools/
│       ├── ITool.cs
│       ├── IToolRegistry.cs
│       ├── ToolRegistry.cs
│       ├── ToolResult.cs
│       ├── ToolInfo.cs
│       ├── SearchTool.cs
│       ├── CalculatorTool.cs
│       ├── WeatherTool.cs
│       └── ChatTool.cs
│
├── MaidenAIAgent.Infrastructure/
│   ├── MaidenAIAgent.Infrastructure.csproj
│   └── Services/
│       └── ExternalAPIService.cs
│
└── MaidenAIAgent.Shared/
    ├── MaidenAIAgent.Shared.csproj
    └── Constants/
        └── AgentConstants.cs
```

## Build and Run Instructions

### Option 1: Using Visual Studio

1. Open the `MaidenAIAgent.sln` file in Visual Studio
2. Restore NuGet packages (right-click on the solution and select "Restore NuGet Packages")
3. Build the solution (Ctrl+Shift+B or Build → Build Solution)
4. Set `MaidenAIAgent.API` as the startup project (right-click on the project and select "Set as Startup Project")
5. Run the application (F5 or Debug → Start Debugging)

### Option 2: Using Command Line

1. Navigate to the solution directory in a terminal/command prompt
2. Restore packages:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```
4. Run the API project:
   ```bash
   cd MaidenAIAgent.API
   dotnet run
   ```

### Access the API

Once running, the API will be available at:
- https://localhost:7173 (HTTPS)
- http://localhost:5217 (HTTP)

Swagger UI is available at:
- https://localhost:7173/swagger

## Testing the API

You can test the API using tools like Postman, curl, or the built-in Swagger UI.

### Example API Calls

#### Get Available Tools
```http
GET https://localhost:7173/api/agent/tools
```

#### Process a Query with the Weather Tool
```http
POST https://localhost:7173/api/agent/process
Content-Type: application/json

{
  "query": "What's the weather in Seattle?",
  "parameters": {
    "location": "Seattle"
  },
  "useAllTools": true
}
```

#### Process a Query with the Calculator Tool
```http
POST https://localhost:7173/api/agent/process
Content-Type: application/json

{
  "query": "Calculate 25 * 4",
  "useAllTools": true
}
```

#### Process a Query with the Search Tool
```http
POST https://localhost:7173/api/agent/process
Content-Type: application/json

{
  "query": "Search for information about AI agents",
  "useAllTools": true
}
```

#### Process a Query with the Chat Tool
```http
POST https://localhost:7173/api/agent/process
Content-Type: application/json

{
  "query": "Hello, what can you help me with?",
  "useAllTools": true
}
```

## Project Configuration

The AI agent can be configured through the `appsettings.json` file. The `AgentSettings` section allows you to:

- Enable/disable the use of all tools
- Specify which tools are enabled by default
- Set a maximum number of concurrent tools
- Configure default timeout values

## Development Notes

- This is a placeholder implementation with simulated tool responses
- For a production system, you'd integrate with real APIs for search, weather, etc.
- The tool selection logic is simple and could be enhanced with NLP or ML techniques