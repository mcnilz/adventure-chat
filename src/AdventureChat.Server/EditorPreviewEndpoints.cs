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
            var result = paths.CalculatePath(request.Room.ToRoomDefinition().Geometry,
                new Point2D(request.Start.X, request.Start.Y), new Point2D(request.Target.X, request.Target.Y));
            return Results.Ok(new { success = result.Success, waypoints = result.Waypoints });
        });
    }

    internal sealed record PreviewPathRequest(RoomYamlDto Room, PointDto Start, PointDto Target);
}
