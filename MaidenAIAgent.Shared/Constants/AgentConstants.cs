namespace MaidenAIAgent.Shared.Constants
{
    public static class AgentConstants
    {
        public static class ToolNames
        {
            public const string Search = "Search";
            public const string Calculator = "Calculator";
            public const string Weather = "Weather";
            public const string Chat = "Chat";
        }

        public static class ErrorMessages
        {
            public const string NoQueryProvided = "No query provided";
            public const string NoToolFound = "No suitable tool found for the query";
            public const string InternalError = "An internal error occurred while processing your request";
        }
    }
}