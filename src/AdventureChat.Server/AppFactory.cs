using System.Text.Json.Serialization;
using AdventureChat.Server.Hubs;
using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Antiforgery;

namespace AdventureChat.Server;

public static class AppFactory
{
    public static WebApplication CreateApp(string[] args, WebApplicationOptions? options = null)
    {
        var suppliedArgs = options?.Args ?? args;
        var authoring = suppliedArgs.Contains("--authoring", StringComparer.Ordinal);
        var filteredArgs = suppliedArgs.Where(arg => arg != "--authoring").ToArray();
        var builder = options is not null 
            ? WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = filteredArgs,
                ApplicationName = options.ApplicationName,
                EnvironmentName = options.EnvironmentName,
                ContentRootPath = options.ContentRootPath,
                WebRootPath = options.WebRootPath
            })
            : WebApplication.CreateBuilder(filteredArgs);

        builder.Services.ConfigureHttpJsonOptions(opt =>
        {
            opt.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
        });

        builder.Services.AddSignalR();
        builder.Services.AddAntiforgery(o => o.HeaderName = "X-Author-Token");
        builder.Services.AddSingleton<IContentPathProvider>(_ => new ContentPathProvider(
            builder.Configuration["Content:Root"], builder.Configuration["Content:Schema"], builder.Configuration["Content:WebRoot"]));
        builder.Services.AddSingleton<IRoomYamlSerializer, RoomYamlSerializer>();
        builder.Services.AddSingleton<IRoomValidator>(sp =>
        {
            var pathProvider = sp.GetRequiredService<IContentPathProvider>();
            if (!File.Exists(pathProvider.GetSchemaFilePath()))
                throw new InvalidOperationException($"Required room schema not found: {pathProvider.GetSchemaFilePath()}");
            return new RoomValidator(pathProvider.GetSchemaFilePath());
        });
        builder.Services.AddSingleton<IPathfindingService, PathfindingService>();
        builder.Services.AddSingleton<IRoomService, RoomService>();
        builder.Services.AddHostedService<RoomStartupValidation>();
        builder.Services.AddSingleton<IRoomSessionManager, RoomSessionManager>();
        builder.Services.AddSingleton<IEditorService>(sp =>
        {
            var pathProvider = sp.GetRequiredService<IContentPathProvider>();
            var yamlSerializer = sp.GetRequiredService<IRoomYamlSerializer>();
            var validator = sp.GetRequiredService<IRoomValidator>();
            var roomService = sp.GetRequiredService<IRoomService>();
            return new EditorService(pathProvider, yamlSerializer, validator, roomService, authoring);
        });

        var app = builder.Build();

        app.UseAuthoringAccess(authoring);

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                ctx.Context.Response.Headers.Pragma = "no-cache";
                ctx.Context.Response.Headers.Expires = "0";
            }
        });

        // Serve schemas folder statically if available
        var contentPathProvider = app.Services.GetRequiredService<IContentPathProvider>();
        var schemaDir = Path.GetDirectoryName(contentPathProvider.GetSchemaFilePath());
        if (!string.IsNullOrEmpty(schemaDir) && Directory.Exists(schemaDir))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(schemaDir),
                RequestPath = "/schemas"
            });
        }

        // Serve content assets if available
        var contentRoot = contentPathProvider.GetContentRoot();
        if (Directory.Exists(contentRoot))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(contentRoot),
                RequestPath = "/content"
            });
        }

        app.MapHub<RoomHub>("/hub/room");

        app.MapGet("/api/rooms/{id}", (string id, IRoomService roomService) =>
        {
            var room = roomService.GetRoom(id);
            if (room is null)
            {
                return Results.NotFound(new { error = $"Room '{id}' not found." });
            }

            return Results.Ok(new
            {
                id = room.Id,
                name = room.Name,
                width = room.Width,
                height = room.Height,
                backgroundUrl = room.BackgroundUrl,
                spawnPoint = new { x = room.SpawnPoint.X, y = room.SpawnPoint.Y },
                geometry = new
                {
                    outerBoundary = room.Geometry.OuterBoundary.Vertices.Select(v => new { x = v.X, y = v.Y }),
                    obstacles = room.Geometry.Obstacles.Select(obs => obs.Vertices.Select(v => new { x = v.X, y = v.Y }))
                },
                spawns = room.Spawns.Select(s => new { id = s.Id, position = new { x = s.Position.X, y = s.Position.Y } }),
                exits = room.Exits.Select(e => new
                {
                    id = e.Id,
                    position = new { x = e.Position.X, y = e.Position.Y },
                    radius = e.Radius,
                    bounds = e.Bounds != null ? new { x = e.Bounds.X, y = e.Bounds.Y, width = e.Bounds.Width, height = e.Bounds.Height } : null,
                    label = e.Label,
                    target = new { room = e.TargetRoomId, spawn = e.TargetSpawnId }
                }),
                notes = room.Notes
            });
        });

        app.MapPost("/api/rooms/{id}/path", (string id, PathRequestDto request, IRoomService roomService, IPathfindingService pathfinder) =>
        {
            var room = roomService.GetRoom(id);
            if (room is null)
            {
                return Results.NotFound(new { success = false, error = $"Room '{id}' not found." });
            }

            var start = new Point2D(request.Start.X, request.Start.Y);
            var target = new Point2D(request.Target.X, request.Target.Y);

            var result = pathfinder.CalculatePath(room.Geometry, start, target);

            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    error = result.Error ?? "Path calculation failed."
                });
            }

            return Results.Ok(new
            {
                success = true,
                waypoints = result.Waypoints!.Select(w => new { x = w.X, y = w.Y })
            });
        });

        var editorGroup = app.MapGroup("/api/editor");

        editorGroup.MapGet("/status", (HttpContext context, IEditorService editorService, IAntiforgery antiforgery) =>
        {
            return Results.Ok(new
            {
                enabled = editorService.IsEditorEnabled,
                schemaUrl = "/schemas/room.schema.json",
                token = antiforgery.GetAndStoreTokens(context).RequestToken
            });
        });

        editorGroup.MapGet("/rooms", (IEditorService editorService) =>
        {
            return Results.Ok(editorService.ListRooms());
        });

        editorGroup.MapGet("/rooms/{id}", (string id, IEditorService editorService) =>
        {
            try
            {
                var room = editorService.GetRoom(id);
                if (room is null)
                {
                    return Results.NotFound(new { error = $"Room '{id}' not found." });
                }
                return Results.Ok(room);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        editorGroup.MapGet("/rooms/{id}/yaml", (string id, IEditorService editorService) =>
        {
            try
            {
                var yaml = editorService.GetRoomRawYaml(id);
                if (yaml is null)
                {
                    return Results.NotFound(new { error = $"Room '{id}' not found." });
                }
                return Results.Content(yaml, "text/yaml; charset=utf-8");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        editorGroup.MapPost("/validate", (RoomYamlDto dto, IEditorService editorService) =>
        {
            var result = editorService.ValidateRoom(dto);
            return Results.Ok(new
            {
                valid = result.IsValid,
                errors = result.Errors
            });
        });

        editorGroup.MapGet("/validate-world", (IEditorService editorService) =>
        {
            var result = editorService.ValidateWorld();
            return Results.Ok(new
            {
                valid = result.IsValid,
                errors = result.Errors
            });
        });

        editorGroup.MapPost("/validate-world", (IReadOnlyList<RoomYamlDto> rooms, IEditorService editorService) =>
        {
            var result = editorService.ValidateWorld(rooms);
            return Results.Ok(new
            {
                valid = result.IsValid,
                errors = result.Errors
            });
        });

        editorGroup.MapGet("/backgrounds", (IEditorService editorService) =>
        {
            return Results.Ok(editorService.ListBackgrounds());
        });

        editorGroup.MapPost("/backgrounds", async (HttpRequest request, IEditorService editorService) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Erwartet multipart/form-data mit Bilddatei." });
            }

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "Keine Datei im Upload übermittelt." });
            }

            // Limit file size to 10MB
            if (file.Length > 10 * 1024 * 1024)
            {
                return Results.BadRequest(new { error = "Die Datei überschreitet die Maximalgröße von 10 MB." });
            }

            using var stream = file.OpenReadStream();
            var result = editorService.SaveBackground(file.FileName, stream);
            if (!result.Success)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new
            {
                success = true,
                name = result.Name,
                path = result.Path
            });
        });

        editorGroup.MapPost("/rooms/{id}", (string id, RoomYamlDto dto, IEditorService editorService) =>
        {
            if (!string.Equals(dto.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    errors = new[] { $"Room ID in path ('{id}') does not match ID in payload ('{dto.Id}')." }
                });
            }

            try
            {
                var result = editorService.SaveRoom(dto);
                if (!result.Success)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        errors = result.Errors
                    });
                }

                return Results.Ok(new
                {
                    success = true,
                    roomId = result.RoomId
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    errors = new[] { ex.Message }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    errors = new[] { ex.Message }
                });
            }
        });

        return app;
    }

}
