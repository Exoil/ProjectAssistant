using System;
using MediatR;

namespace ProjectAssistant.Features.AgentFeatures.SearchInCompanyStorage;

public class SearchInCompanyStorageCommandHandler
{

}

public record SearchInCompanyStorageCommand(string Request) : IRequest<string>;
