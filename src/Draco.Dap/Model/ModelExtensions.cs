using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Dap.Model;

/// <summary>
/// Extension functionality for models.
/// </summary>
public static class ModelExtensions
{
    public static string? GetMessage(this ErrorResponse error)
    {
        if (error.Error is null) return null;

        var result = new StringBuilder(error.Error.Format);
        if (error.Error.Variables is not null)
        {
            foreach (var (name, value) in error.Error.Variables)
            {
                result.Replace($"{{{name}}}", value);
            }
        }
        return result.ToString();
    }
}
