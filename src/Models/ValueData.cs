namespace PSLauncher.Models;

/// <summary>
/// Container of Table view's row for string or primitive value.
/// This represents a value and its runtime type.
/// </summary>
/// <param name="Value"></param>
/// <see cref="TableList"/>
public readonly record struct ValueData(object? Value)
{
    public readonly Type Type => Value?.GetType() ?? typeof(void);
};
