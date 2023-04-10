using System.Text.RegularExpressions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace ConsoleGPT.Skills
{
    /// <summary>
    /// A speech skill using Azure Cognitive Services speech recognition and synthesis.
    /// </summary>
    public class AzCognitiveServicesSpeechSkill : IDisposable, ISpeechSkill
    {
        private readonly ILogger _logger;
        private readonly AzureCognitiveServicesOptions _options;
        private readonly AudioConfig _audioConfig;
        private readonly SpeechRecognizer _speechRecognizer;
        private readonly SpeechSynthesizer _speechSynthesizer;
        private bool _isGoodbye = false;


        public AzCognitiveServicesSpeechSkill(
            IOptions<AzureCognitiveServicesOptions> options,
            ILogger<AzCognitiveServicesSpeechSkill> logger)
        {
            _logger = logger;
            _options = options.Value;

            // Build an audio config from the default microphone - this needs to be configured correctly
            _audioConfig = AudioConfig.FromDefaultMicrophoneInput();

            // Build a speech configuration from our settings
            var speechConfig = SpeechConfig.FromSubscription(_options.Key, _options.Region);
            speechConfig.SpeechRecognitionLanguage = _options.SpeechRecognitionLanguage;
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");
            speechConfig.SpeechSynthesisVoiceName = _options.SpeechSynthesisVoiceName;

            // Create the speech synthesizer and recognizer
            _speechRecognizer = new SpeechRecognizer(speechConfig, _audioConfig);           
            _speechSynthesizer = new SpeechSynthesizer(speechConfig);
        }

        /// <summary>
        /// Listens to the microphone and performs speech-to-text, returning what the user said.
        /// </summary>
        [SKFunction("Listen to the microphone and perform speech-to-text.")]
        [SKFunctionName("Listen")]
        public async Task<string> Listen(SKContext context)
        {
            _logger.LogInformation("Listening...");

            // Listen till a natural break in the speech is detected
            var result = await _speechRecognizer.RecognizeOnceAsync();

            // Check the result and see if we got text
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                // If we got speech, log it
                _logger.LogInformation($"Recognized: {result.Text}");

                // Check if the user said goodbye - the application will use this after processing the speech
                // to terminate the app
                if (result.Text.ToLower().StartsWith("goodbye"))
                    _isGoodbye = true;

                // Return the speech
                return result.Text;
            }

            // If we didn't get speech, return an empty string
            return string.Empty;
        }
        
        /// <summary>
        /// Speaks the given message using text-to-speech. This returns the message so it can be used
        /// in the next function in the pipeline
        /// </summary>
        [SKFunction("Speak the current context (text-to-speech).")]
        [SKFunctionName("Respond")]
        public async Task<string> Respond(string message, SKContext context)
        {
            // Check if we have a message to speak
            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.LogInformation($"Speaking: {message}");

                // Build some SSML with the text to speak
                string ssml = GenerateSsml(message, _options.SpeechSynthesisVoiceName);

                _logger.LogDebug(ssml);

                // Speak the SSML
                await _speechSynthesizer.SpeakSsmlAsync(ssml);
            }

            // Return the message so the next function in the pipeline can use it
            return message;
        }

        /// <summary>
        /// Checks if the user said goodbye
        /// </summary>
        [SKFunction("Did the user say goodbye.")]
        [SKFunctionName("IsGoodbye")]
        public Task<string> IsGoodbye(SKContext context)
        {
            return Task.FromResult(_isGoodbye ? "true" : "false");
        }

        /// <summary>
        /// Generate speech synthesis markup language (SSML) from a message for the given voice.
        /// </summary>
        private string GenerateSsml(string message, string voiceName)
            => "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">" +
                $"<voice name=\"{voiceName}\">" +
                    $"<prosody rate=\"{_options.Rate}\">" +
                        $"{message}" +
                    "</prosody>" +
                    "</voice>" +
                "</speak>";

        /// <summary>
        /// Dispose of the speech synthesizer and recognizer
        /// </summary>
        public void Dispose()
        {
            _speechRecognizer.Dispose();
            _audioConfig.Dispose();
        }
    }
}