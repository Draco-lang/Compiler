using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// A simple addon to display the current random seed in the footer.
/// </summary>
public sealed class SeedFooterAddon : FuzzerAddon
{
    public override StatusItem CreateStatusItem() =>
        new(Key.Null, $"Seed: {this.Fuzzer.Settings.Seed}", () => { });
}
