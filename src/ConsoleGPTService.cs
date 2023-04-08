using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI.ChatCompletion;

namespace ConsoleGPT
{
    /// <summary>
    /// This is the main application service.
    /// This takes console input, then sends it to the configured AI service, and then prints the response.
    /// All conversation history is maintained in the chat history.
    /// </summary>
    internal class ConsoleGPTService : IHostedService
    {
        private readonly IKernel _semanticKernel;
        private readonly IChatCompletion _chatCompletion;
        private readonly OpenAIChatHistory _chatHistory;
        private readonly ChatRequestSettings _chatRequestSettings;

        public ConsoleGPTService(IKernel semanticKernel,
                                 IOptions<OpenAiServiceOptions> openAIOptions)
        {
            _semanticKernel = semanticKernel;

            // Set up the chat request settings
            _chatRequestSettings = new ChatRequestSettings()
            {
                MaxTokens = openAIOptions.Value.MaxTokens,
                Temperature = openAIOptions.Value.Temperature,
                FrequencyPenalty = openAIOptions.Value.FrequencyPenalty,
                PresencePenalty = openAIOptions.Value.PresencePenalty,
                TopP = openAIOptions.Value.TopP
            };

            // Configure the semantic kernel
            _semanticKernel.Config.AddOpenAIChatCompletionService("chat", openAIOptions.Value.Model,
                openAIOptions.Value.Key, openAIOptions.Value.OrganizationId);

            // Set up the chat completion and history - the history is used to keep track of the conversation
            // and is part of the prompt sent to ChatGPT to allow a continuous conversation
            _chatCompletion = _semanticKernel.GetService<IChatCompletion>();
            _chatHistory = (OpenAIChatHistory)_chatCompletion.CreateNewChat(openAIOptions.Value.SystemPrompt);
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);

        /// <summary>
        /// Stop a running service.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Write to the console that the conversation is beginning
            WriteAIResponse("Hello. Ask me a question or say goodbye to exit.");

            // Loop till we are cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                // Get the users input
                var line = Console.ReadLine();

                // If there was no input, wait again
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var reply = string.Empty;
                try
                {
                    // Add the question as a user message to the chat history, then send everything to OpenAI.
                    // The chat history is used as context for the prompt
                    _chatHistory.AddUserMessage(line);
                    reply = await _chatCompletion.GenerateMessageAsync(_chatHistory, _chatRequestSettings);

                    // Add the interaction to the chat history.
                    _chatHistory.AddAssistantMessage(reply);
                }
                catch (AIException aiex)
                {
                    reply = $"OpenAI returned an error ({aiex.Message}). Please try again.";
                }

                // Write the response to the console
                WriteAIResponse(reply);

                // If the user says goodbye, end the chat
                if (line.ToLower() == "goodbye")
                {
                    // Log the history so we can see the prompts used
                    LogChatHistory();
                    break;
                }
            }

            // Kill the app
            System.Environment.Exit(0);
        }

        private void LogChatHistory()
        {
            Console.WriteLine();
            Console.WriteLine("Chat history:");
            Console.WriteLine();

            // Log the chat history including system, user and assistant (AI) messages
            foreach (var message in _chatHistory.Messages)
            {
                var role = message.AuthorRole;
                switch (role)
                {
                    case "system":
                        role = "System:    ";
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case "user":
                        role = "User:      ";
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case "assistant":
                        role = "Assistant: ";
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }

                Console.WriteLine($"{role}{message.Content}");
            }
        }

        private void WriteAIResponse(string response)
        {
            // Write the response in Green, then revert the console color
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(response);
            Console.ForegroundColor = oldColor;
        }
    }
}