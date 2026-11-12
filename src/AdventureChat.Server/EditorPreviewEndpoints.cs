using AdventureChat.Server.Models;
using AdventureChat.Server.Services;

namespace AdventureChat.Server;

internal static class EditorPreviewEndpoints
{
    public static void MapPreviewPath(this RouteGroupBuilder group)
    {
        group.MapPost("/preview-path", (PreviewPathRequest request, IEditorService editor, IPathfindingService paths) =>
        {
            var validation = editor.ValidateRoom(request.Room);
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
            var drafts = editor.ListRooms().Where(r => !string.Equals(r.Id, request.Room.Id, StringComparison.OrdinalIgnoreCase))
                .Select(r => editor.GetRoom(r.Id)!).Append(request.Room).ToList();
            var validation = editor.ValidateWorld(drafts);
            if (!validation.IsValid) return Results.BadRequest(new { errors = validation.Errors });
            var definitions = drafts.Select(r => r.ToRoomDefinition()).ToList();
            var position = new Point2D(request.Position.X, request.Position.Y);
            if (!request.Room.ToRoomDefinition().Geometry.IsWalkable(position))
                return Results.BadRequest(new { errors = new[] { "Invalid preview footpoint." } });
            var manager = new RoomSessionManager(new PreviewRoomService(definitions), paths);
            try { manager.RestorePreview(request.States ?? []); }
            catch (ArgumentException ex) { return Results.BadRequest(new { errors = new[] { ex.Message } }); }
            var joined = manager.JoinRoom("preview", request.Room.Id, "Tester", new(request.Position.X, request.Position.Y));
            if (!joined.Success) return Results.BadRequest(new { errors = new[] { joined.Error } });
            var result = manager.RequestObjectAction("preview", new(request.Room.Id, request.ObjectId, request.ActionId, 0));
            var states = definitions.Select(r => new PreviewObjectStates(r.Id, manager.PreviewStates(r))).ToList();
            return Results.Ok(new { success = result.Success, error = result.Error, snapshot = manager.GetSnapshot("preview"), states });
        });
    }

    private static bool ValidStates(RoomDefinition room, IReadOnlyList<ObjectStateDto> states) =>
        states.Select(s => s.ObjectId).Distinct().Count() == states.Count && states.All(s =>
            room.Objects?.Any(o => o.Id == s.ObjectId && o.Interaction?.States.Any(v => v.Id == s.StateId) == true) == true);

    internal sealed record PreviewPathRequest(RoomYamlDto Room, PointDto Start, PointDto Target, IReadOnlyList<ObjectStateDto>? States = null);
    internal sealed record PreviewActionRequest(RoomYamlDto Room, PointDto Position, string ObjectId, string ActionId,
        IReadOnlyList<PreviewObjectStates>? States = null);
}
