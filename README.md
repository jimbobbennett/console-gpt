# ConsoleGPT

ChatGPT for your console using Semantic Kernel!

This is a demo application to show how to use Semantic Kernel in your dotnet applications to interact with Large Language Models like GPT-3.5 to build a chat tool. You can interact with the LLM using your console, or using speech.

## Get started

* Clone this repo

* Navigate to the `src` folder

* Get your API keys:

    * You will need an OpenAI key. Head to [platform.openai.com/signup](https://platform.openai.com/signup) to sign up and get an API key.
    * If you want to use speech to interact with ConsoleGPT, you will need to create an [Azure Cognitive Services Speech resource](https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices).

* Set your OpenAI API key as a user secret:

    ```bash
    dotnet user-secrets set "OpenAI:Key" "<your key>"
    ```

* If you want to use speech to interact with ConsoleGPT, set your Azure Cognitive services key and region:

    ```bash
    dotnet user-secrets set "AzureCognitiveServices:Key" "<your key>"
    dotnet user-secrets set "AzureCognitiveServices:Region" "<your region>"
    ```

* Run the code with `dotnet run`

* Ask away! To end the chat say goodbye.

    ```output
    ➜  src git:(main) ✗ dotnet run
    info: Microsoft.Hosting.Lifetime[0]
          Application started. Press Ctrl+C to shut down.
    info: Microsoft.Hosting.Lifetime[0]
          Hosting environment: Production
    info: Microsoft.Hosting.Lifetime[0]
          Content root path: /Users/jimbennett/GitHub/console-gpt/src
    Hello. Ask me a question or say goodbye to exit.
    What is the most recent star wars movie?
    The most recent Star Wars movie is "Star Wars: The Rise of Skywalker," which was released in December 2019. It is the ninth and final installment in the Skywalker saga.
    Who is the main character?
    The main character in "Star Wars: The Rise of Skywalker" is Rey, a powerful Force user who is trying to find her place in the galaxy and confront her past. She is played by actress Daisy Ridley.
    Goodbye
    Goodbye! Don't hesitate to ask if you have any more questions.
    
    Chat history:
    
    System:    You are a friendly, intelligent, and curious assistant who is good at conversation. Your name is Orko.
    User:      What is the most recent star wars movie?
    Assistant: The most recent Star Wars movie is "Star Wars: The Rise of Skywalker," which was released in December 2019. It is the ninth and final installment in the Skywalker saga.
    User:      Who is the main character?
    Assistant: The main character in "Star Wars: The Rise of Skywalker" is Rey, a powerful Force user who is trying to find her place in the galaxy and confront her past. She is played by actress Daisy Ridley.
    User:      Goodbye
    Assistant: Goodbye! Don't hesitate to ask if you have any more questions.
    info: Microsoft.Hosting.Lifetime[0]
          Application is shutting down...
    ```

## How does this app work?

This app uses [Semantic Kernel](https://github.com/microsoft/semantic-kernel), an open source .NET library from Microsoft that aims to simplify building apps that use LLMs. You can read more about Semantic Kernel in the following locations:

* [The Semantic Kernel documentation on Microsoft Learn](https://learn.microsoft.com/semantic-kernel)
* [The Semantic Kernel repo on GitHub](https://github.com/microsoft/semantic-kernel)
* [The Semantic Kernel Discord](https://aka.ms/sk/discord)
* [The Semantic Kernel blog](https://devblogs.microsoft.com/semantic-kernel/)

Semantic Kernel provides services to interact with both text and chat focused LLMs either using OpenAI directly, or via the Azure Open AI service.

The app runs as a hosted application using the Microsoft hosting extension library.

### Services

The chat service has a chat history to implement a memory so that conversations can flow naturally, and earlier prompts and responses can be chained to build later prompts. For example, the prompt:

```output
What is the most recent star wars movie?
```

Will give a response such as:

```output
The most recent Star Wars movie is "Star Wars: The Rise of Skywalker," which was released in December 2019. It is the ninth and final installment in the Skywalker saga.
```

This prompt and response can be chained with the next prompt:

```output
Who is the main character?
```

to give a response that 'knows' that the main character question refers to the "Star Wars: The Rise of Skywalker" movie:

```output
The main character in "Star Wars: The Rise of Skywalker" is Rey, a powerful Force user who is trying to find her place in the galaxy and confront her past. She is played by actress Daisy Ridley.
```

### Skills

Semantic Kernel uses *skills* that define named *functions*. These functions communicate using text - the basic building block of large language models. Functions can take a `string` as a parameter, return a `string` or both. You can then chain these functions to build a pipeline.

In this app, the `ConsoleSkill` implements 3 semantic kernel functions:

```csharp
 [SKFunction("Get console input.")]
[SKFunctionName("Listen")]
public Task<string> Listen(SKContext context);

[SKFunction("Write a response to the console.")]
[SKFunctionName("Respond")]
public Task<string> Respond(string message, SKContext context);

[SKFunction("Did the user say goodbye.")]
[SKFunctionName("IsGoodbye")]
public Task<string> IsGoodbye(SKContext context);
```

The `Listen` function takes input from the console, the `Respond` function writes a response to the console (and returns it), and the `IsGoodbye` function returns if the `Listen` function received "goodbye" as it's input.

The `ChatSkill` implements 2 functions:

```csharp
[SKFunction("Send a prompt to the LLM.")]
[SKFunctionName("Prompt")]
public async Task<string> Prompt(string prompt);

[SKFunction("Log the history of the chat with the LLM.")]
[SKFunctionName("LogChatHistory")]
public Task LogChatHistory();
```

The `Prompt` function sends the given prompt to the LLM and returns the response. This uses the `IChatCompletion` service from semantic kernel. This is a service that has methods to create and manage chats. You can create a chat with a system prompt that gives the LLM background information to use when crafting responses. For example, the default system prompt for this app is:

_You are a friendly, intelligent, and curious assistant who is good at conversation. Your name is [Orko](https://en.wikipedia.org/wiki/Orko_(character))._

This prompt is set in the `configuration.json` file, so can be changed to suit your needs. This prompt sets up all chats, so if you were to ask:

```output
What is your name?
```

You would get the response:

```output
My name is Orko. How can I assist you today?
```

This service also manages chat history through an instance of `OpenAIChatHistory`. This tracks history of the chat, tagging messages as `System` for the system prompt, `User` for prompts from the user and `Assistant` for the responses. This chat history is passed with every prompt so that the LLM can use the chat history to guide the response. There is a limit to the size that the LLM can handle for the prompt, so only the size specified in the `MaxTokens` field in the `configuration.json` file is sent. You can read more about tokens and their size in the [OpenAI documentation](https://help.openai.com/articles/4936856-what-are-tokens-and-how-to-count-them).

The `LogChatHistory` function logs the chat history, including all the system, user and assistant messages. This is called at the end of the session to show the user what was sent and received. For example:

```output
System:    You are a friendly, intelligent, and curious assistant who is good at conversation. Your name is Orko.
User:      What is the most recent star wars movie?
Assistant: The most recent Star Wars movie is "Star Wars: The Rise of Skywalker," which was released in December 2019. It is the ninth and final installment in the Skywalker saga.
User:      Who is the main character?
Assistant: The main character in "Star Wars: The Rise of Skywalker" is Rey, a powerful Force user who is trying to find her place in the galaxy and confront her past. She is played by actress Daisy Ridley.
User:      Goodbye
Assistant: Goodbye! Don't hesitate to ask if you have any more questions.
```

Semantic Kernel also has some out of the box skills to do things like interact with HTTP APIs, work with files or manipulate text.

The advantage of using skills is that you can easily swap out skills as long as they have the same name. For example, in this app there is an alternative to the `ConsoleSkill` that uses speech to text and text to speech to interact with the user. It has the same functions on it marked with the same attributes, so can be swapped in.

### Creating functions from prompts

This app also had (and is commented out to start with), and example function that converts text to poetry. Functions do not need to be built in code, but can be created using a text prompt. The poetry function is created with the following code:

```csharp
string poemPrompt = """
  Take this "{{$INPUT}}" and convert it to a poem in iambic pentameter.
  """;

_poemFunction = _semanticKernel.CreateSemanticFunction(poemPrompt, maxTokens: openAIOptions.Value.MaxTokens,
    temperature: openAIOptions.Value.Temperature, frequencyPenalty: openAIOptions.Value.FrequencyPenalty,
    presencePenalty: openAIOptions.Value.PresencePenalty, topP: openAIOptions.Value.TopP);
```

This code creates the function using the prompt _Take this "{{$INPUT}}" and convert it to a poem in iambic pentameter._. The function that is created with the call to `CreateSemanticFunction` is a function that takes a string as input, replaces the `{{$INPUT}}` field in the text with that string, sends it to the text completion service, and returns the result. This allows you to quickly create libraries of standard prompts in text that can be used in pipelines to process data.

### Pipelines

Semantic Kernel functions take and return text, so you can chain them together into pipelines. For example, chaining the `Listen`, `Prompt` and `Respond` functions to create a chatbot.

```csharp
await _semanticKernel.RunAsync(_speechSkill["Listen"], _chatSkill["Prompt"], _speechSkill["Respond"]);
```

This works as long as any function returns the right input for the next function in the pipeline. For example, the `Listen` function returns a `string`, which is the input for the `Prompt` function, which in turn returns a `string`, which is the input for the `Respond` function.

If you wanted to add more functions to the pipeline you can, for example inserting the `_poemFunction` mentioned above before the call to the `Respond` function to get the results in poetry.

These pipelines can be defined in code, so can be constructed on the fly if needed.

## Customizing the app

### Changing the model

By default this app uses the 

### Speech

By default this app runs on the command line and you type your questions, getting the response out on the command line. You can also enable speech mode to be able to ask your questions with your voice and receive a spoken answer. To do this:

* Make sure you have the relevant cognitive service resource configured and the key and region set as described above.

* In `Program.cs` comment out the line that adds the `ConsoleSkill` singleton, and uncomment the line that adds the `AzCognitiveServicesSpeechSkill` singleton.

    ```csharp
    // services.AddSingleton<ISpeechSkill, ConsoleSkill>();
    services.AddSingleton<ISpeechSkill, AzCognitiveServicesSpeechSkill>();
    ```

* Run the app

The app will use your default microphone and speaker to interact with you. Say the word "goodbye" to end the conversation.

### Poetry

This app also includes some example code to show how to create a semantic function using a prompt, in this case to convert the response to poetry. To enable this:

* In the `ConsoleGPTService`, uncomment the `_poemFunction`

    ```csharp
    private readonly ISKFunction _poemFunction;
    ```

* Uncomment where this function is created in the `ConsoleGPTService` constructor:

    ```csharp
    _semanticKernel.Config.AddOpenAITextCompletionService("text", openAIOptions.Value.TextModel, 
        openAIOptions.Value.Key);

    string poemPrompt = """
      Take this "{{$INPUT}}" and convert it to a poem in iambic pentameter.
      """;

    _poemFunction = _semanticKernel.CreateSemanticFunction(poemPrompt, maxTokens: openAIOptions.Value.MaxTokens,
        temperature: openAIOptions.Value.Temperature, frequencyPenalty: openAIOptions.Value.FrequencyPenalty,
        presencePenalty: openAIOptions.Value.PresencePenalty, topP: openAIOptions.Value.TopP);
    ```

* Uncomment the line in `ExecuteAsync` that appends the `_poemFunction` to the pipeline:

    ```csharp
    pipeline.Append(_poemFunction).Append(_speechSkill["Respond"]);
    ```

* Run the app. This works with both the console and speech output

When the app runs it will output the standard response, then convert it to poetry and output it again.