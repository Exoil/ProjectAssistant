using OpenAI.Chat;

namespace ProjectAssistant.IoC;

public static class RegisterLlmClients
{
    public static IServiceCollection RegisterOpenAiClient(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ChatClient>(_ =>
        {
            const string llmModel = "gpt-4o";
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_APIKEY")!;

            return new ChatClient(llmModel, apiKey);
        });
}