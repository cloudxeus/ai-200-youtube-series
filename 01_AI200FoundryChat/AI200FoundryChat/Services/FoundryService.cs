using Azure.AI.Projects;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using OpenAI.Responses;

namespace AI200FoundryChat.Services;

public class FoundryService
{
    private readonly ProjectResponsesClient _responsesClient;

    public FoundryService(IConfiguration configuration)
    {
        string endpoint =
            configuration["Foundry:ProjectEndpoint"]
            ?? throw new InvalidOperationException(
                "Foundry project endpoint is missing.");

        string modelDeployment =
            configuration["Foundry:ModelDeployment"]
            ?? throw new InvalidOperationException(
                "Foundry model deployment is missing.");

        // Authenticate to the Microsoft Foundry project
        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Create a client for our deployed AI model
        _responsesClient =
            projectClient.ProjectOpenAIClient
                .GetProjectResponsesClientForModel(
                    modelDeployment);
    }

    public async Task<string> GetResponseAsync(
        string prompt)
    {
        var result  =
            await _responsesClient.CreateResponseAsync(
                prompt);

        var response = result.Value;

        string output = response.GetOutputText();

        if (string.IsNullOrWhiteSpace(output))
        {
            return "The model returned an empty response.";
        }

        return output;
    }
}