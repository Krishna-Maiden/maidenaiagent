# MaidenAI Agent

An AI agent built with C# Web API that provides processing capabilities through 3 specialized tools.

## Features

- RESTful API for interacting with the AI agent
- 4 specialized tools:
  - **Search Tool**: Searches for information (simulated)
  - **Calculator Tool**: Performs mathematical calculations
  - **Weather Tool**: Retrieves weather information for a location (simulated)
  - **Chat Tool**: Engages in conversation and provides general assistance
- Extensible architecture to easily add more tools

## Project Structure

The solution follows clean architecture principles with these components:

- **MaidenAIAgent.API**: Web API endpoints and controllers
- **MaidenAIAgent.Core**: Business logic, tools, and interfaces
- **MaidenAIAgent.Infrastructure**: External service integrations
- **MaidenAIAgent.Shared**: Common/shared components

## Getting Started

### Prerequisites

- .NET 7.0 SDK or later
- An IDE (Visual Studio, VS Code, JetBrains Rider, etc.)

### Running the Application

1. Clone the repository
2. Navigate to the solution directory
3. Run the application:

```bash
dotnet restore
dotnet build
cd MaidenAIAgent.API
dotnet run
```

The API will be available at https://localhost:7001 (or a similar port)  
Swagger UI will be available at https://localhost:7001/swagger

## API Usage

### Process a Query

```http
POST /api/agent/process
Content-Type: application/json

{
  "query": "What's the weather in New York?",
  "parameters": {
    "location": "New York"
  },
  "useAllTools": true,
  "specificTools": []
}
```

### Get Available Tools

```http
GET /api/agent/tools
```

## Extending the Agent

To add a new tool:

1. Create a class that implements the `ITool` interface in the Core project
2. Register the tool in the DI container in `ServiceExtensions.cs`

## License

MIT