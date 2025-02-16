using OpenAI.Chat;

namespace ProjectAssistant.IoC;

public static class RegisterLlmClients
{
    public static IServiceCollection RegisterOpenAiClient(this IServiceCollection serviceCollection, IConfiguration configuration) =>
        serviceCollection.AddScoped<ChatClient>(_ =>
        {
            const string llmModel = "gpt-4o";
            var apiKey = configuration.GetValue<string>("OpenAiApiKey");

            return new ChatClient(llmModel, apiKey);
        });
}