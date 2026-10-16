using System.Collections.Generic;
using System.Linq;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Selector item pairing a value with its label.</summary>
internal sealed class Choice<T>
{
    public Choice(T value, string label)
    {
        Value = value;
        Label = label;
    }

    public T Value { get; }

    public string Label { get; }

    public static object? Find(DropDownSelector selector, T value) =>
        selector.Items.OfType<Choice<T>>().FirstOrDefault(c => EqualityComparer<T>.Default.Equals(c.Value, value));

    public override string ToString() => Label;
}
