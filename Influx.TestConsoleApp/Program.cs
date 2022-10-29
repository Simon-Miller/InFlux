using InFlux.Attributes;
using System.ComponentModel.DataAnnotations;

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
internal class testa
{
    [Range(1, 16)]
    [RegularExpression("^Berty$")]
    [Required]
    private int id;

    [AutoWireupIgnore]
    public bool IgnoreMe;

    public int ShouldSkipMe { get; set; } = 0;
}