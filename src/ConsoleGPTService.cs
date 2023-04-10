using ConsoleGPT.Skills;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

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
        private readonly IDictionary<string, ISKFunction> _speechSkill;
        private readonly IDictionary<string, ISKFunction> _chatSkill;
        private readonly IHostApplicationLifetime _lifeTime;

        // Uncomment this to create a function that converts text to a poem
        // private readonly ISKFunction _poemFunction;

        public ConsoleGPTService(IKernel semanticKernel,
                                 ISpeechSkill speechSkill,
                                 ChatSkill chatSkill,
                                 IOptions<OpenAiServiceOptions> openAIOptions,
                                 IHostApplicationLifetime lifeTime)
        {
            _semanticKernel = semanticKernel;
            _lifeTime = lifeTime;

            // Import the skills to load the semantic kernel functions
            _speechSkill = _semanticKernel.ImportSkill(speechSkill);
            _chatSkill = _semanticKernel.ImportSkill(chatSkill);

            // Uncomment this to create a function that converts text to a poem
            // _semanticKernel.Config.AddOpenAITextCompletionService("text", openAIOptions.Value.TextModel, 
            //     openAIOptions.Value.Key);

            // string poemPrompt = """
            // Take this "{{$INPUT}}" and convert it to a poem in iambic pentameter.
            // """;

            // _poemFunction = _semanticKernel.CreateSemanticFunction(poemPrompt, maxTokens: openAIOptions.Value.MaxTokens,
            //     temperature: openAIOptions.Value.Temperature, frequencyPenalty: openAIOptions.Value.FrequencyPenalty,
            //     presencePenalty: openAIOptions.Value.PresencePenalty, topP: openAIOptions.Value.TopP);
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop a running service.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// The main execution loop. This awaits input and responds to it using semantic kernel functions.
        /// </summary>
        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Write to the console that the conversation is beginning
            await _semanticKernel.RunAsync("Hello. Ask me a question or say goodbye to exit.", _speechSkill["Respond"]);

            // Loop till we are cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                // Run the pipeline
                // Comment this line out and uncomment the one below to get the response as a poem
                await _semanticKernel.RunAsync(_speechSkill["Listen"], _chatSkill["Prompt"], _speechSkill["Respond"]);
                // await _semanticKernel.RunAsync(_speechSkill["Listen"], _chatSkill["Prompt"], _speechSkill["Respond"], _poemFunction, _speechSkill["Respond"]);

                // Did we say goodbye? If so, exit
                var goodbyeContext = await _semanticKernel.RunAsync(_speechSkill["IsGoodbye"]);
                var isGoodbye = bool.Parse(goodbyeContext.Result);

                // If the user says goodbye, end the chat
                if (isGoodbye)
                {
                    // Log the history so we can see the prompts used
                    await _semanticKernel.RunAsync(_chatSkill["LogChatHistory"]);

                    // Stop the application
                    _lifeTime.StopApplication();
                }
            }
        }
    }
}