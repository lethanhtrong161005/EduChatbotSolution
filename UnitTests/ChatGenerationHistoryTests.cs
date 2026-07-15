using Domain.Contracts.DTOs;
using Domain.Entities;
using Presentation.Realtime;

namespace UnitTests;

public class ChatGenerationHistoryTests
{
    [Test]
    public void SelectHistory_UsesLogicalOrderAndOnlySelectedAssistantVariants()
    {
        var firstUser = Message(ChatRole.User, 1, MessageStatus.Completed);
        var selectedFirstAnswer = Message(ChatRole.Assistant, 2, MessageStatus.Completed, selected: true);
        var unselectedFirstAnswer = Message(ChatRole.Assistant, 2, MessageStatus.Completed, selected: false);
        var targetUser = Message(ChatRole.User, 3, MessageStatus.Completed);
        var targetAssistant = Message(ChatRole.Assistant, 4, MessageStatus.Pending, selected: true);
        var laterUser = Message(ChatRole.User, 5, MessageStatus.Completed);

        var history = ChatGenerationCoordinator.SelectHistoryMessages(
            [laterUser, unselectedFirstAnswer, targetAssistant, targetUser, selectedFirstAnswer, firstUser],
            targetAssistant,
            maximumMessageCount: 12);

        Assert.That(history.Select(message => message.Id), Is.EqualTo(new[]
        {
            firstUser.Id,
            selectedFirstAnswer.Id,
            targetUser.Id,
        }));
    }

    [Test]
    public void SelectHistory_AppliesLimitFromTheEndAndKeepsTargetUser()
    {
        var firstUser = Message(ChatRole.User, 1, MessageStatus.Completed);
        var firstAnswer = Message(ChatRole.Assistant, 2, MessageStatus.Completed, selected: true);
        var targetUser = Message(ChatRole.User, 3, MessageStatus.Completed);
        var targetAssistant = Message(ChatRole.Assistant, 4, MessageStatus.Pending, selected: true);

        var history = ChatGenerationCoordinator.SelectHistoryMessages(
            [firstUser, firstAnswer, targetUser, targetAssistant],
            targetAssistant,
            maximumMessageCount: 1);

        Assert.That(history.Single().Id, Is.EqualTo(targetUser.Id));
    }

    [Test]
    public void AttachTargetVariant_AddsExplicitUnselectedTargetToSelectedSessionView()
    {
        var session = new ChatSession { Id = Guid.NewGuid() };
        var targetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resolved = new ResolvedChatMessage
        {
            Id = targetId,
            ChatRole = ChatRole.Assistant,
            Content = string.Empty,
            SentAt = DateTime.UtcNow,
            Status = MessageStatus.Pending,
            MessageIndex = 2,
            InReplyToMessageId = userId,
            VariantIndex = 2,
            IsSelectedVariant = false,
        };

        var target = ChatGenerationCoordinator.AttachTargetVariant(session, resolved);

        Assert.Multiple(() =>
        {
            Assert.That(target.Id, Is.EqualTo(targetId));
            Assert.That(target.MessageIndex, Is.EqualTo(2));
            Assert.That(target.IsSelectedVariant, Is.False);
            Assert.That(session.Messages.Single().Id, Is.EqualTo(targetId));
        });
    }

    private static ChatMessage Message(
        ChatRole role,
        int messageIndex,
        MessageStatus status,
        bool selected = false)
        => new()
        {
            Id = Guid.NewGuid(),
            ChatRole = role,
            MessageIndex = messageIndex,
            Status = status,
            IsSelectedVariant = selected,
            SentAt = DateTime.UtcNow.AddMinutes(messageIndex),
        };
}
