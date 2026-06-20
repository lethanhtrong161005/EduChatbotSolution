using Domain.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.AI.Chat;

public class ChatClientFactory(IServiceProvider provider) : IChatClientFactory
{
    private readonly IServiceProvider _provider = provider;

    public IChatClient GetChatClient(string modelName)
    {
        return _provider.GetRequiredKeyedService<IChatClient>(modelName);
    }
}
