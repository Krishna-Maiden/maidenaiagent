# MaidenAI Agent

An AI agent built with C# Web API that provides processing capabilities through specialized tools and LLM integration.

## Features

- RESTful API for interacting with the AI agent
- 6 specialized tools:
  - **Search Tool**: Searches for information (simulated)
  - **Calculator Tool**: Performs mathematical calculations
  - **Weather Tool**: Retrieves weather information for a location (simulated)
  - **Chat Tool**: Provides intelligent responses using Claude 3.7 Sonnet
  - **StreamingChat**: Supports streaming responses for long-form content
  - **AugmentedChat**: Uses Claude with the ability to call other tools as needed
- Advanced capabilities:
  - **Tool Orchestration**: Allows LLM to use specialized tools as needed
  - **NLP Processing**: Intent recognition, entity extraction, and sentiment analysis
  - **Streaming Responses**: Progressive delivery of long-form content
  - **Rate Limiting**: Prevents excessive API usage
- Extensible architecture to easily add more tools

## Project Structure

The solution follows clean architecture principles with these components:

- **MaidenAIAgent.API**: Web API endpoints and controllers
- **MaidenAIAgent.Core**: Business logic, tools, and interfaces
- **MaidenAIAgent.Infrastructure**: External service integrations
- **MaidenAIAgent.Shared**: Common/shared components
- **MaidenAIAgent.Tests**: Unit tests for vali