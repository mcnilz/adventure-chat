using AdventureChat.Server.Models;
using AdventureChat.Server.Services;

namespace AdventureChat.Server;

internal static class EditorPreviewEndpoints
{
    public static void MapPreviewPath(this RouteGroupBuilder group)
    {
        group.MapPost("/preview-path", (PreviewPathRequest request, IEditorService editor, IPathfindingService paths) =>
        {
            var validation = editor.ValidateRoom(request.Room, request.TargetFormat);
            if (!validation.IsValid) return Results.BadRequest(new { errors = validation.Errors });
            var definition = request.Room.ToRoomDefinition();
            var navigation = new ObjectNavigation(new PreviewRoomService([definition]), _ => request.States ?? []);
            if (!ValidStates(definition, request.States ?? [])) return Results.BadRequest(new { errors = new[] { "Unknown preview state." } });
            var result = paths.CalculatePath(navigation.Geometry(definition),
                new Point2D(request.Start.X, request.Start.Y), new Point2D(request.Target.X, request.Target.Y));
            return Results.Ok(new { success = result.Success, waypoints = result.Waypoints });
        });
        group.MapPost("/preview-action", (PreviewActionRequest request, IEditorService editor, IPathfindingService paths) =>
        {
            var command = new PreviewCommand(request.Room, request.Position, request.States, request.RoomId, request.TargetFormat,
                request.CharacterId);
            return Run(editor, paths, command, (manager, activeRoom) =>
            {
                var action = new ObjectActionRequestDto(activeRoom, request.ObjectId, request.ActionId, 0);
                var result = request.CompleteGesture ? manager.CompletePreviewObjectAction("preview", action) :
                    manager.RequestObjectAction("preview", action);
                return (result.Success, result.Error, result.ErrorCode);
            });
        });
        group.MapPost("/preview-exit", (PreviewExitRequest request, IEditorService editor, IPathfindingService paths) =>
        {
            var command = new PreviewCommand(request.Room, request.Position, request.States, request.RoomId, request.TargetFormat,
                request.CharacterId);
            return Run(editor, paths, command, (manager, _) =>
            {
                var result = manager.ChangeRoom("preview", request.ExitId);
                return (result.Success, result.Error, null);
            });
        });
        group.MapPost("/preview-snapshot", (PreviewCommand command, IEditorService editor, IPathfindingService paths) =>
            Run(editor, paths, command, (_, _) => (true, null, null)));
    }

    private static IResult Run(IEditorService editor, IPathfindingService paths, PreviewCommand command,
        Func<RoomSessionManager, string, (bool Success, string? Error, string? ErrorCode)> run)
    {
        var drafts = editor.WorldWithDraft(command.Room);
        var validation = editor.ValidateWorld(drafts, command.TargetFormat);
        if (!validation.IsValid) return Results.BadRequest(new { errors = validation.Errors });
        var definitions = drafts.Select(r => r.ToRoomDefinition()).ToList();
        var roomId = command.RoomId ?? command.Room.Id;
        var position = new Point2D(command.Position.X, command.Position.Y);
        if (definitions.Find(room => room.Id == roomId)?.Geometry.IsWalkable(position) != true)
            return Results.BadRequest(new { errors = new[] { "Invalid preview footpoint." } });
        var manager = new RoomSessionManager(new PreviewRoomService(definitions), paths);
        try { manager.RestorePreview(command.States ?? []); }
        catch (ArgumentException ex) { return Results.BadRequest(new { errors = new[] { ex.Message } }); }
        var joined = manager.JoinRoom("preview", roomId, "Tester", position, command.CharacterId);
        if (!joined.Success) return Results.BadRequest(new { errors = new[] { joined.Error } });
        var result = run(manager, roomId);
        var states = definitions.Select(room => new PreviewObjectStates(room.Id, manager.PreviewStates(room))).ToList();
        return Results.Ok(new { success = result.Success, error = result.Error, errorCode = result.ErrorCode,
            snapshot = manager.GetSnapshot("preview"), states });
    }

    private static bool ValidStates(RoomDefinition room, IReadOnlyList<ObjectStateDto> states) =>
        states.Select(s => s.ObjectId).Distinct().Count() == states.Count && states.All(s =>
            room.Objects?.Any(o => o.Id == s.ObjectId && o.Interaction?.States.Any(v => v.Id == s.StateId) == true) == true);

    internal sealed record PreviewPathRequest(RoomYamlDto Room, PointDto Start, PointDto Target,
        IReadOnlyList<ObjectStateDto>? States = null, string? TargetFormat = null);
    internal sealed record PreviewActionRequest(RoomYamlDto Room, PointDto Position, string ObjectId, string ActionId,
        IReadOnlyList<PreviewObjectStates>? States = null, string? RoomId = null, string? TargetFormat = null,
        string? CharacterId = null, bool CompleteGesture = false);
    internal sealed record PreviewExitRequest(RoomYamlDto Room, PointDto Position, string ExitId,
        IReadOnlyList<PreviewObjectStates>? States = null, string? RoomId = null, string? TargetFormat = null, string? CharacterId = null);
    private sealed record PreviewCommand(RoomYamlDto Room, PointDto Position,
        IReadOnlyList<PreviewObjectStates>? States, string? RoomId, string? TargetFormat, string? CharacterId = null);
}
