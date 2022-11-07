using InFlux.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ConsoleApp;

partial class Program
{
    static void Main(string[] args)
    {
        HelloFrom("Generated Code");


        var inst = new testClass();
        
    }

    static partial void HelloFrom(string name);
}

[AutoWireup]
public partial class testClass
{
    int id; // ** all generated code seeded from THIS field, once project builds. ** 
}

