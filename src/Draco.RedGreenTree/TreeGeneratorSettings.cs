using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

public sealed class TreeGeneratorSettings
{
    public string GreenNamespace { get; set; } = "GreenTree";
    public string RedNamespace { get; set; } = "RedTree";
    public Func<string, string> GreenToRedName { get; set; } = n => n;
    public string ParentName { get; set; } = "Parent";
    public string GreenName { get; set; } = "green";
    public string ToRedMethodName { get; set; } = "ToRed";
    public Accessibility RedAccessibility { get; set; } = Accessibility.Internal;
}
