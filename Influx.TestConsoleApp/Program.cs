using InFlux.Attributes;

namespace ConsoleApp;

partial class Program
{
    static void Main(string[] args)
    {
        HelloFrom("Generated Code");
    }

    static partial void HelloFrom(string name);
}

[AutoWireup]
class testa
{
    private int id;

    [AutoWireupIgnore]
    public bool IgnoreMe;

    public int ShouldSkipMe { get; set; } = 0;
}