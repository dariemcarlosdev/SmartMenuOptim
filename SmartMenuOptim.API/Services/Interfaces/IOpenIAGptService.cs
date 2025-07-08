namespace SmartMenuOptim.API.Services.Interfaces
{
    public interface IOpenIAGptService
    {
        /// <summary>
        /// Generates a response from the OpenAI GPT model based on the provided user prompt and optional parameters.
        /// </summary>
        /// <param name="userPrompt"></param>
        /// <param name="systemPrompt"></param>
        /// <param name="temperature"></param>
        /// <param name="maxTokens"></param>
        /// <param name="deploymentName"></param>
        /// <returns></returns>
        Task<string> GenerateAsync(string userPrompt, string? systemPrompt = null, float? temperature = null, int? maxTokens = null, string? deploymentName = null);
    }
}
