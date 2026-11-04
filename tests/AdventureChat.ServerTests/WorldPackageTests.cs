using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class WorldPackageTests
{
    [Test]
    public void Load_UnsupportedFormat_RejectsBeforeReadingRooms()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "world.yaml"), "formatVersion: 9.0.0\n");
            var error = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(root));
            Assert.That(error!.Message, Does.Contain("9.0.0").And.Contain("formatVersion").And.Contain("world.yaml"));
        }
        finally { Directory.Delete(root, true); }
    }
    [Test]
    public void Load_MissingRequiredManifestFields_RejectsPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "world.yaml"), "formatVersion: 1.0.0\n");
            Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(root));
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void Load_ReferenceWorld_UsesDeclaredStartAndLegacyFormat()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        var world = WorldPackage.Load(root);
        Assert.Multiple(() =>
        {
            Assert.That(world.Id, Is.EqualTo("reference"));
            Assert.That(world.StartRoom, Is.EqualTo("lounge"));
            Assert.That(world.StartSpawn, Is.EqualTo("entry"));
        });
    }

    [Test]
    public void LoadRooms_ReferenceWorld_LoadsValidLegacyRooms()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        var world = WorldPackage.Load(root);
        var rooms = world.LoadRooms();
        Assert.That(rooms.Select(r => r.Id), Is.EquivalentTo(new[] { "lounge", "garden" }));
    }

    [Test]
    public async Task Server_ConfiguredWorld_ListsPublicMetadata()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var server = Path.Combine(solution, "src", "AdventureChat.Server");
        var root = Path.Combine(solution, "worlds", "reference");
        var app = AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = server,
            WebRootPath = Path.Combine(server, "wwwroot"),
            Args = ["--urls", "http://127.0.0.1:0", "--World:Root", root]
        });
        try
        {
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var response = await client.GetAsync("/api/worlds");
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(body, Does.Contain("reference").And.Not.Contain(root));
        }
        finally { await app.DisposeAsync(); }
    }

    [Test]
    public async Task Server_WorldAssets_SeparateIdenticalRelativePaths()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var server = Path.Combine(solution, "src", "AdventureChat.Server");
        var original = Path.Combine(solution, "worlds", "reference");
        var copy = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        foreach (var file in Directory.GetFiles(original, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(copy, Path.GetRelativePath(original, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        var manifest = Path.Combine(copy, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("id: reference", "id: other"));
        File.Copy(Path.Combine(copy, "assets", "images", "garden.png"), Path.Combine(copy, "assets", "images", "lounge.png"), true);
        var app = AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = server, WebRootPath = Path.Combine(server, "wwwroot"),
            Args = ["--urls", "http://127.0.0.1:0", "--World:Roots:0", original, "--World:Roots:1", copy]
        });
        try
        {
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var first = await client.GetByteArrayAsync("/world-assets/reference/images/lounge.png");
            var second = await client.GetByteArrayAsync("/world-assets/other/images/lounge.png");
            Assert.That(first, Is.EqualTo(File.ReadAllBytes(Path.Combine(original, "assets", "images", "lounge.png"))));
            Assert.That(second, Is.EqualTo(File.ReadAllBytes(Path.Combine(copy, "assets", "images", "lounge.png"))));
            Assert.That(second, Is.Not.EqualTo(first));
            var firstRoom = await client.GetAsync("/api/worlds/reference/rooms/lounge");
            var otherRoom = await client.GetAsync("/api/worlds/other/rooms/lounge");
            Assert.That(firstRoom.IsSuccessStatusCode, Is.True);
            Assert.That(otherRoom.IsSuccessStatusCode, Is.True);
            Assert.That(await firstRoom.Content.ReadAsStringAsync(), Does.Contain("/world-assets/reference/images/lounge.png"));
            Assert.That(await otherRoom.Content.ReadAsStringAsync(), Does.Contain("/world-assets/other/images/lounge.png"));
            Assert.That((await client.GetAsync("/api/worlds/missing/rooms/lounge")).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
            Assert.That((await client.GetAsync("/api/worlds/other/rooms/missing")).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        }
        finally { await app.DisposeAsync(); Directory.Delete(copy, true); }
    }

    [Test]
    public async Task Server_WorldAssets_ResolveByConfiguredWorldId()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var server = Path.Combine(solution, "src", "AdventureChat.Server");
        var root = Path.Combine(solution, "worlds", "reference");
        var app = AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = server,
            WebRootPath = Path.Combine(server, "wwwroot"),
            Args = ["--urls", "http://127.0.0.1:0", "--World:Root", root]
        });
        try
        {
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var image = await client.GetAsync("/world-assets/reference/images/lounge.png");
            Assert.That(image.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(await image.Content.ReadAsByteArrayAsync(), Is.EqualTo(File.ReadAllBytes(Path.Combine(root, "assets", "images", "lounge.png"))));
            Assert.That((await client.GetAsync("/world-assets/missing/images/lounge.png")).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        }
        finally { await app.DisposeAsync(); }
    }

    [Test]
    public async Task Server_WorldStart_RequiresKnownWorldId()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var server = Path.Combine(solution, "src", "AdventureChat.Server");
        var root = Path.Combine(solution, "worlds", "reference");
        var app = AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = server,
            WebRootPath = Path.Combine(server, "wwwroot"),
            Args = ["--urls", "http://127.0.0.1:0", "--World:Root", root]
        });
        try
        {
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var known = await client.GetAsync("/api/worlds/reference/start");
            Assert.That(known.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(await known.Content.ReadAsStringAsync(), Does.Contain("lounge").And.Not.Contain(root));
            var unknown = await client.GetAsync("/api/worlds/missing/start");
            Assert.That(unknown.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        }
        finally { await app.DisposeAsync(); }
    }

    [Test]
    public async Task Server_ExternalWorld_ExposesDeclaredStartRoom()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var server = Path.Combine(solution, "src", "AdventureChat.Server");
        var root = Path.Combine(solution, "worlds", "reference");
        var app = AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = server,
            WebRootPath = Path.Combine(server, "wwwroot"),
            Args = ["--urls", "http://127.0.0.1:0", "--World:Root", root]
        });
        try
        {
            await app.StartAsync();
            var serverAddresses = app.Urls.Single();
            using var client = new HttpClient { BaseAddress = new Uri(serverAddresses) };
            var response = await client.GetAsync("/api/world/start");
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("lounge"));
        }
        finally { await app.DisposeAsync(); }
    }

    [TestCase("schemas/room.schema.json", "1967F9E5D2DDF5F3F2EE0B0A26C240A35969B250036B385836974AE5B8C99898")]
    [TestCase("schemas/world/1.0.0/world.schema.json", "EE546F3E592CE735EF7A8C5EB242462F3A8AEB34625A40A7E5ECE4300828E37B")]
    [TestCase("schemas/world/1.0.0/room.schema.json", "F781D028F4BC8DB01A47B42A6180BCB45BB4B229BD8429E05B603136244A6D8E")]
    public void PublishedSchemas_RemainUnchanged(string relative, string expectedHash)
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var hash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(root, relative)));
        Assert.That(Convert.ToHexString(hash), Is.EqualTo(expectedHash));
    }

    [Test]
    public void Server_DuplicateWorldIds_RejectsConfiguration()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var server = Path.Combine(solution, "src", "AdventureChat.Server");
        var root = Path.Combine(solution, "worlds", "reference");
        var ex = Assert.Throws<InvalidOperationException>(() => AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = server,
            WebRootPath = Path.Combine(server, "wwwroot"),
            Args = ["--World:Roots:0", root, "--World:Roots:1", root]
        }));
        Assert.That(ex!.Message, Does.Contain("Duplicate world IDs"));
    }

}
