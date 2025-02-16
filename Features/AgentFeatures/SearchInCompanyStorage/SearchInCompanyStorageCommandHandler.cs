using MediatR;
using OpenAI.Chat;
using ProjectAssistant.Features.Exceptions;
using ProjectAssistant.Features.Models;
using ProjectAssistant.Features.Utilities;
using Serilog;
using Microsoft.Data.SqlClient;
using Dapper;


namespace ProjectAssistant.Features.AgentFeatures.SearchInCompanyStorage;

public class SearchInCompanyStorageCommandHandler : IRequestHandler<SearchInCompanyStorageCommand, Result<string, Exception>>
{
    private readonly ChatClient _chatClient;
    private readonly string _connectionString;

    public SearchInCompanyStorageCommandHandler(
        ChatClient chatClient,
        IConfiguration configuration)
    {
        _connectionString = configuration["DbConnectionString"]!;
        _chatClient = chatClient;
    }

    public async Task<Result<string, Exception>> Handle(
        SearchInCompanyStorageCommand request,
        CancellationToken cancellationToken)
    {
        request.ChatMessages.Add(request.Request);
        const string systemPrompt = @"
        <context>
        You are a helpful assistant that can search for information in the company storage to create a scrum team for a project or provide information about data in the company storage.
        You know T-SQL and can use it to search for information in the company storage.
        You need to return the information in the company storage that matches the request.
        </context>

        <rules>
        1. Use <think> </think> tags in your answer to include descriptions of your deductions. For each new thought, use the think tags.
        2. Use <query> </query> tags in your answer to include T-SQL queries.
        3. Use <result> </result> tags in your answer to include the result of your search.
        4. If the input is not clear, return in <result> </result> tags that you can't help with that.
        5. If the input is not related to the company storage, such as explaining how to use T-SQL or finding information about a city, return in <result> </result> tags that you can't help with that.
        6. You can use the T-SQL language to search for information in the company storage.
        7. Available tables in storage are: [dbo].[Employees], [dbo].[Departments], [dbo].[Projects], [dbo].[Roles], [dbo].[EmployeesProjects].
        8. You can retrieve data about how tables are built in storage.
        9. You can retrieve data about how relations between tables are built in storage.
        10. You can retrieve data using SELECT statements.
        11. You can retrieve data using JOIN statements.
        12. You can retrieve data using WHERE statements.
        13. You can retrieve data using GROUP BY statements.
        14. You can retrieve data using HAVING statements.
        15. You can retrieve data using ORDER BY statements.
        16. You can retrieve data using LIMIT statements.
        17. It is forbidden to use: DELETE, UPDATE, INSERT, ALTER, DROP, CREATE, RENAME, TRUNCATE.
        18. User can input in English or Polish. If the input is in Polish, the decision to translate and the translation itself should be included in the <think> section.
        </rules>
        ";
        var chatMessages = new List<ChatMessage>()
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(request.Request)
                };
        var breakLoop = false;
        var counter = 0;
        var finalResult = "";
        Log.Information("Start searching in company storage");
        const int delay = 5;
        const int retryLimit = 3;
        var retryCount = 0;
        while (!breakLoop)
        {
            var chatCompletion = await _chatClient.CompleteChatAsync(
                chatMessages,
                cancellationToken: cancellationToken);

            var assistantAnswerFromChat = chatCompletion
                .Value
                .Content
                .Select(x => x.Text);

            var assistantAnswer = string.Join(" ", assistantAnswerFromChat); 
            request.ChatMessages.Add(assistantAnswer);
            chatMessages.Add(assistantAnswer);

            if (assistantAnswer.Contains("</result>"))
            {
                Log.Information("End searching in company storage");
                finalResult = assistantAnswer;
                break;
            }
            else if (assistantAnswer.Contains("</think>"))
            {
                Log.Information("Thinking about the request");
            }
            else if (assistantAnswer.Contains("</query>"))
            {
                Log.Information("Using query to search in company storage");
                try
                {
                    var queryResult  = await ExecuteRequestAsync(assistantAnswer);
                    request.ChatMessages.Add(queryResult);
                    chatMessages.Add(new UserChatMessage(queryResult));
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= retryLimit)
                    {
                        Log.Error("Retry limit reached");
                        request.ChatMessages.Add(ex.Message);
                        break;
                    }
                    request.ChatMessages.Add(ex.Message);
                    chatMessages.Add(new UserChatMessage(ex.Message));
                }

            }
            else
            {
                Log.Information("Assistant error");
                return ExceptionUtility
                    .ResolveExceptionToReturn(
                        new DomainException("Assistant error", "500", "Assistant error"));
            }
            request.OnUpdate?.Invoke(); // N
            counter++;
            breakLoop = counter == request.IterationLimit;
            Thread.Sleep(TimeSpan.FromSeconds(delay));
        }

        return finalResult;
    }
    
private async Task<string> ExecuteRequestAsync(string query)
{
    var queryWithoutTags = query
        .Replace("</query>", string.Empty)
        .Replace("<query>", string.Empty);

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    var result = await connection.QueryAsync<string>(queryWithoutTags);

    return string.Join(" ", result); // Concatenate results into a single string
}
}



public record SearchInCompanyStorageCommand(
    string Request, 
    int IterationLimit, 
    List<string> ChatMessages, 
    Action OnUpdate) : IRequest<Result<string, Exception>>;
