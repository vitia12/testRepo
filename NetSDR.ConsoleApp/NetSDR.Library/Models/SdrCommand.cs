namespace NetSDR.Library.Models;

public class SdrCommand(int identifier, string description)
{
    public int Identifier { get; } = identifier;

    public string Description { get; } = description;
}