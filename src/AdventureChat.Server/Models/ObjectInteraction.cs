namespace AdventureChat.Server.Models;

public sealed class ObjectInteraction
{
    public string Name { get; set; } = "";
    public string InitialState { get; set; } = "";
    public RoomPointYamlDto? UsePoint { get; set; }
    public double UseRadius { get; set; }
    public List<ObjectState> States { get; set; } = [];
    public List<ObjectAction> Actions { get; set; } = [];
}

public sealed class ObjectState
{
    public List<RoomPolygonYamlDto>? Obstacles { get; set; }
    public List<string>? BlockedExits { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public RoomPointYamlDto Position { get; set; } = new();
    public RoomSizeYamlDto Size { get; set; } = new();
    public double DepthY { get; set; }
}

public sealed class ObjectAction
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public List<ObjectEffect>? Effects { get; set; }
}

public sealed class ObjectEffect
{
    public string Room { get; set; } = "";
    public string Object { get; set; } = "";
    public string State { get; set; } = "";
}
