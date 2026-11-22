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
    Point2D TalkPoint, List<DialogueOption> Options, List<Point2D>? Waypoints = null, List<NpcTourStep>? Tour = null,
    double? FollowDistance = null)
{
    public NpcDefinition() : this("", "", "", new(double.NaN, double.NaN), new(double.NaN, double.NaN), []) { }
}

public sealed record NpcTourStep(Point2D? Position = null, string? Text = null, string? Exit = null)
{
    public NpcTourStep() : this(null, null, null) { }
}

public sealed record NpcPresence(string Id, string Name, string CharacterId, Point2D Position, Point2D TalkPoint,
    NpcMotion? Motion = null);
public sealed record NpcMotion(string Direction, IReadOnlyList<Point2D> Route);
public sealed record NpcMovementDto(string RoomId, long Sample, IReadOnlyList<NpcPresence> Npcs);
public sealed record NpcPatrolCheckpoint(string Id, Point2D Position, int NextWaypoint, double WaitMs,
    bool Blocked, string Direction, IReadOnlyList<Point2D> Route, Point2D? ReactionTarget = null);
public sealed record PreviewNpcPatrols(IReadOnlyList<NpcPatrolCheckpoint> Checkpoints, int ElapsedMs = 0,
    string? PausedNpc = null, int? ResumeAfterMs = null);
public sealed record AvailableDialogueOption(string Id, string Text, string Reply);
public sealed record NpcDialogueRequest(string RoomId, string NpcId, AvailableDialogueOption? Choice = null,
    string? ApproachId = null, bool Prepare = false);
public sealed record NpcDialogueResult(string? Error, IReadOnlyList<AvailableDialogueOption>? Options = null,
    bool TooFar = false, AvailableDialogueOption? Selected = null, Point2D? TalkPoint = null, bool TourStarted = false);
