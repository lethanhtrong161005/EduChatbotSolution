using DataAccess.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

public class ChatVariantModelTests
{
    [Test]
    public void ChatMessage_ExposesStableTurnAndVariantIdentity()
    {
        var message = new ChatMessage();

        Assert.Multiple(() =>
        {
            Assert.That(message.MessageIndex, Is.Null);
            Assert.That(message.InReplyToMessageId, Is.Null);
            Assert.That(message.VariantIndex, Is.Null);
            Assert.That(message.IsSelectedVariant, Is.False);
            Assert.That(message.AssistantVariants, Is.Empty);
        });
    }

    [Test]
    public void ChatMessageModel_UsesCompositeReplyKeyAndFilteredUniqueIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ChatMessage))!;

        var alternateKey = entity.GetKeys().Single(key =>
            key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ChatMessage.Id), nameof(ChatMessage.ChatSessionId)]));
        var replyForeignKey = entity.GetForeignKeys().Single(foreignKey =>
            foreignKey.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ChatMessage.InReplyToMessageId), nameof(ChatMessage.ChatSessionId)]));

        Assert.Multiple(() =>
        {
            Assert.That(alternateKey, Is.Not.Null);
            Assert.That(replyForeignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.NoAction));
            Assert.That(entity.GetIndexes().Count(index => index.IsUnique && index.GetFilter() is not null), Is.EqualTo(3));
        });
    }

    private static EduChatAiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=chat_model_only;Username=unused;Password=unused",
                npgsql => npgsql.UseVector())
            .Options;

        return new EduChatAiDbContext(options);
    }
}
