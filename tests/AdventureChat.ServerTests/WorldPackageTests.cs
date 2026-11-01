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
    [TestCase("schemas/world/1.0.0/world.schema.json", "408E4EDD9609C09986EDC8CD5789ACE9E360FEB46C968D627A86CDFFCD827645")]
    [TestCase("schemas/world/1.0.0/room.schema.json", "D19D4BD19137D70AD9EF9F2FFED47EC07BC1610CC96AC7502F4C5FF770C48399")]
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
