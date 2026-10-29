namespace AdventureChat.Server.Models;

public record PointDto(double X, double Y);
public record PathRequestDto(PointDto Start, PointDto Target);
