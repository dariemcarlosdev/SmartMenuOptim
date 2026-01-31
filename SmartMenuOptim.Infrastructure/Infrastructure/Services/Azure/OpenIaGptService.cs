using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SmartMenuOptim.Application.Interfaces;

namespace SmartMenuOptim.Infrastructure.Infrastructure.Services.Azure
{
    /// <summary>
    /// Provides methods to interact with the OpenAI GPT service using Azure's OpenAI client.
    /// </summary>
    /// <remarks>This service is configured using the provided <see cref="IConfiguration"/> instance, which
    /// must contain the necessary Azure OpenAI endpoint, key, and deployment name. It logs operations and errors using
    /// the provided <see cref="ILogger{OpenIaGptService}"/>.
    /// With OpenAiGptService, you can now reuse this for:
    /// Chat-like user flows
    /// Dynamic report generation
    /// Strategy suggestions
    /// Social media content writing
    /// Automatic customer feedback summaries
    ///</remarks>
    public class OpenIaGptService : IOpenIAGptService
    {
        private readonly AzureOpenAIClient _openAIClient;
        private readonly string _deploymentName;
        private readonly ILogger<OpenIaGptService> _logger;

        public OpenIaGptService(IConfiguration config, ILogger<OpenIaGptService> logger)
        {
            // config in Deployment should contain Azure:OpenAI:Endpoint, Azure:OpenAI:Key, and Azure:OpenAI:Deployment
            //Theses keys in Azure App Service can be set in the Application Settings section or using Azure Key Vault(IConfiguration config  will get the value from Key Vault via App Settings.)

            var endpoint = config["Azure:OpenAI:Endpoint"];
            var key = config["Azure:OpenAI:Key"];
            _deploymentName = config["Azure:OpenAI:Deployment"]; // Default deployment name, can be overridden by configuration
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(_deploymentName))
            {
                throw new ArgumentException("OpenAI configuration is not properly set.");
            }
            _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs a chat completion request to the OpenAI GPT model using the provided user prompt and optional parameters.
        /// </summary>
        /// <param name="userPrompt"></param>
        /// <param name="systemPrompt"></param>
        /// <param name="temperature"></param>
        /// <param name="maxTokens"></param>
        /// <param name="deploymentName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> GenerateAsync(string userPrompt, string? systemPrompt = null, float? temperature = null, int? maxTokens = null, string? deploymentName = null)
        {
            try
            {
                ChatClient chatClient = _openAIClient.GetChatClient(_deploymentName);
                var messages = new List<ChatMessage>();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new SystemChatMessage(systemPrompt));
                }
                messages.Add(new UserChatMessage(userPrompt));

                ChatClient chatCompletionClient = _openAIClient.GetChatClient(_deploymentName);
                ChatCompletion chatCompletion = await chatCompletionClient.CompleteChatAsync(
                    messages
                );

                var responseMessage = chatCompletion.Content[0].Text;
                if (string.IsNullOrWhiteSpace(responseMessage))
                {
                    throw new InvalidOperationException("The AI response was empty or null.");
                }
                return responseMessage.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GenerateAsync.");
                return "An error occurred while generating a response from the AI service. Please try again later.";
            }
        }
    }
}
