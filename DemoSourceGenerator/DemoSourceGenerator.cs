using Microsoft.CodeAnalysis;
using System;

namespace DemoSourceGenerator;

[Generator]
public class DemoSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
    }
}

