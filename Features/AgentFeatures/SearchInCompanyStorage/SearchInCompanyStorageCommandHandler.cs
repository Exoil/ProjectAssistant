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
        ChatClient chatClient)
    {
        _chatClient = chatClient;
        _connectionString = "";
    }

    public async Task<Result<string, Exception>> Handle(
        SearchInCompanyStorageCommand request,
        CancellationToken cancellationToken)
    {
        request.ChatMessages.Add(request.Request);
        const string systemPrompt = @"
        <context>
        You are a helpful assistant that can search for information in the company storage to create scrum team for project or give information about data in the company storage.
        You know T-Sql and you can use it to search for information in the company storage.
        You need to return the information in the company storage that matches the request.
        </context>

        <rules>
        1. Use <think> </think> tags in your answer to include descriptions of your deductions. For each new thought, use the think tags.
        2. Use <query> </query> tags in your answer to use t-sql query.
        3. Use <result> </result> tags in your answer to include the result of your search.
        4. If Input is not clear. Return in <result> </result> tags that you can't help with that.
        5. If Input is not related to the company storage like explain how to use T-Sql or find say about some city, etc. Return in <result> </result> tags that you can't help with that.
        6. You can use the T-Sql language to search for information in the company storage.
        7. Avabile tables in storage are: [dbo].[Employees], [dbo].[Departments], [dbo].[Projects], [dbo].[Roles], [dbo].[EmployeesProjects]
        8. You can retrieve Data about how built are tables in storage.
        9. You can retrieve Data about how built are relations between tables in storage.
        10. You can retrieve using select statement.
        11. You can retrieve using join statements.
        12. You can retrieve using where statements.
        13. You can retrieve using group by statements.
        14. You can retrieve using having statements.
        15. You can retrieve using order by statements.
        16. You can retrieve using limit statements.
        17. Is forbidden to use: DELETE, UPDATE, INSERT, ALTER, DROP, CREATE, RENAME, TRUNCATE
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
                var queryResult  = await ExecuteRequestAsync(assistantAnswer);
                request.ChatMessages.Add(queryResult);
                chatMessages.Add(new UserChatMessage(queryResult));
            }
            else
            {
                Log.Information("Assistant error");
                return ExceptionUtility
                    .ResolveExceptionToReturn(
                        new DomainException("Assistant error", "500", "Assistant error"));
            }
            counter++;
            breakLoop = counter == request.IterationLimit;
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



public record SearchInCompanyStorageCommand(string Request, int IterationLimit, List<string> ChatMessages) : IRequest<Result<string, Exception>>;
