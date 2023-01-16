using System;
using System.Diagnostics;

namespace Draco.RedGreenTree.Attributes;

[Conditional("DRACO_SOURCEGENERATOR_ATTRIBUTE")]
[AttributeUsage(AttributeTargets.Class)]
public sealed class GreenTreeAttribute : Attribute
{
}
