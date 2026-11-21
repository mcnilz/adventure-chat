namespace AdventureChat.Server.Models;

public sealed record DialogueOption(string Id, string Text, string Reply)
{
    public DialogueOption() : this("", "", "") { }
}

public sealed record NpcDefinition(string Id, string Name, string CharacterId, Point2D Position,
    Point2D TalkPoint, List<DialogueOption> Options)
{
    public NpcDefinition() : this("", "", "", new(double.NaN, double.NaN), new(double.NaN, double.NaN), []) { }
}

public sealed record NpcPresence(string Id, string Name, string CharacterId, Point2D Position, Point2D TalkPoint);
public sealed record NpcDialogueRequest(string RoomId, string NpcId);
public sealed record NpcDialogueResult(string? Error, IReadOnlyList<DialogueOption>? Options = null, bool TooFar = false);
