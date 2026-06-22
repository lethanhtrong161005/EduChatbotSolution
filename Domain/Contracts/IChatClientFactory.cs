using Microsoft.Extensions.AI;

namespace Domain.Contracts;

public interface IChatClientFactory
{
    IChatClient GetChatClient(string modelName);
}
