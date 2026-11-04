using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class CharacterCatalogTests
{
    private string _root = null!;
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "character-world-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "assets/images"));
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        File.Copy(Path.Combine(solution, "src/AdventureChat.Server/wwwroot/assets/images/character.png"), Path.Combine(_root, "assets/images/actor.png"));
    }
    [TearDown] public void TearDown() => Directory.Delete(_root, true);
    private void Manifest(string catalog, string version = "1.3.0") => File.WriteAllText(Path.Combine(_root, "world.yaml"), $"""
        formatVersion: {version}
        id: test
        name: Test
        contentVersion: 1.0.0
        roomFormat: legacy-1
        startRoom: lounge
        startSpawn: entry
        {catalog}
        """);
    private const string Catalog = """
        defaultCharacterId: hero
        characters:
          - { id: hero, name: Hero, image: assets/images/actor.png, frameWidth: 48, frameHeight: 64 }
        """;
    [Test]
    public void ValidCatalog_LoadsAndKeepsManifestStable()
    {
        Manifest(Catalog);
        var world = WorldPackage.Load(_root);
        Assert.DoesNotThrow(world.EnsureManifestUnchanged);
    }
    [TestCase("1.0.0")][TestCase("1.1.0")][TestCase("1.2.0")]
    public void OlderFormat_RejectsCatalog(string version)
    {
        Manifest(Catalog, version);
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root));
    }

    [Test]
    public void DuplicateIds_AreRejected()
    {
        Manifest(Catalog + "\n  - { id: hero, name: Another, image: assets/images/actor.png, frameWidth: 48, frameHeight: 64 }");
        Assert.That(Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root))!.Message, Does.Contain("Duplicate").IgnoreCase);
    }

    [TestCase("defaultCharacterId: hero", "defaultCharacterId: missing")]
    [TestCase("assets/images/actor.png", "assets/images/absent.png")]
    [TestCase("assets/images/actor.png", "assets/images/../actor.png")]
    [TestCase("frameWidth: 48", "frameWidth: 64")]
    [TestCase("frameHeight: 64", "frameHeight: 0")]
    [TestCase("frameHeight: 64", "frameHeight: 64.5")]
    [TestCase("frameHeight: 64", "frameHeight: 257")]
    public void InvalidCatalog_IsRejected(string oldText, string newText)
    {
        Manifest(Catalog.Replace(oldText, newText));
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root));
    }

    [Test]
    public void TruncatedCharacterImage_IsRejectedWithCharacterDiagnostic()
    {
        Manifest(Catalog);
        File.WriteAllBytes(Path.Combine(_root, "assets/images/actor.png"), [137, 80, 78, 71]);
        Assert.That(Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root))!.Message, Does.Contain("hero"));
    }

    [Test]
    public void CatalogChange_RefusesEditorOverwrite()
    {
        Manifest(Catalog);
        var world = WorldPackage.Load(_root);
        Manifest(Catalog.Replace("name: Hero", "name: Changed"));
        Assert.Throws<InvalidOperationException>(world.EnsureManifestUnchanged);
    }
}
