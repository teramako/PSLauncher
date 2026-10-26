using System;
using System.Collections.Generic;
using System.Text;

namespace PSLauncher.Models;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
internal class IconAttribute(string resourceName) : Attribute
{
    public string ResourceName { get; } = resourceName;
}
