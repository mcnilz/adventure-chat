namespace AdventureChat.Server.Models;

public sealed record DialogueCondition(string Object, string State)
{
    public DialogueCondition() : this("", "") { }
}

public sealed record ConditionalReply(DialogueCondition When, string Reply)
{
    public ConditionalReply() : this(new DialogueCondition(), "") { }
}

public sealed record DialogueOption(string Id, string Text, string Reply,
    DialogueCondition? When = null, List<ConditionalReply>? Replies = null)
{
    public DialogueOption() : this("", "", "") { }
}

public sealed record NpcDefinition(string Id, string Name, string CharacterId, Point2D Position,
    Point2D TalkPoint, List<DialogueOption> Options)
{
    public NpcDefinition() : this("", "", "", new(double.NaN, double.NaN), new(double.NaN, double.NaN), []) { }
}

public sealed record NpcPresence(string Id, string Name, string CharacterId, Point2D Position, Point2D TalkPoint);
public sealed record AvailableDialogueOption(string Id, string Text, string Reply);
public sealed record NpcDialogueRequest(string RoomId, string NpcId, AvailableDialogueOption? Choice = null);
public sealed record NpcDialogueResult(string? Error, IReadOnlyList<AvailableDialogueOption>? Options = null,
    bool TooFar = false, AvailableDialogueOption? Selected = null);
