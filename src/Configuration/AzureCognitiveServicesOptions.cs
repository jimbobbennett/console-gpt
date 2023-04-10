#pragma warning disable 8618
namespace ConsoleGPT
{
    /// <summary>
    /// Configuration options class for interacting with Azure Cognitive Services.
    /// </summary>
    public class AzureCognitiveServicesOptions
    {
        /// <summary>
        /// Location/region (e.g. EastUS)
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Access Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Default language for speech recognition (speech-to-text).
        /// </summary>
        public string SpeechRecognitionLanguage { get; set; }

        /// <summary>
        /// Name of the voice to use for speaking (text-to-speech).
        /// </summary>
        /// <remarks>
        /// https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=stt-tts#text-to-speech
        /// </remarks>
        public string SpeechSynthesisVoiceName { get; set; }
        
        /// <summary>
        /// True to enable style cues when speaking.
        /// </summary>
        public bool EnableSpeechStyle { get; set; }

        /// <summary>
        /// Indicates the speaking rate of the text.
        /// https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup-voice#adjust-prosody
        /// </summary>
        public string Rate { get; set; }
    }
}