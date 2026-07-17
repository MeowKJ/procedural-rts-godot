using Godot;
using ProceduralRts.MapAuthoring.Editor;

namespace ProceduralRts.MapAuthoring.Qa;

static class MapAuthoringCreateDialogSmoke
{
    private const string Title = "Map Authoring Create Node QA";

    public static async Task Run(Func<Task> nextFrame)
    {
        EditorInterface.Singleton.PopupCreateDialog(
            Callable.From<string>(_ => { }),
            MapAuthoringTypeRegistry.BaseType,
            MapAuthoringTypeRegistry.Types[0].Name,
            Title,
            new Godot.Collections.Array<StringName>());
        await nextFrame();

        var sceneRoot = EditorInterface.Singleton.GetBaseControl().GetTree().Root;
        var dialog = Descendants<ConfirmationDialog>(sceneRoot).Single(window => window.Title == Title);
        var searchFields = Descendants<LineEdit>(dialog).Where(line => line.IsVisibleInTree()).ToArray();
        var search = searchFields.FirstOrDefault(line => line.PlaceholderText.Contains("Search", StringComparison.OrdinalIgnoreCase))
            ?? searchFields.SingleOrDefault()
            ?? throw new InvalidOperationException("Create Node search field was not found.");
        foreach (var descriptor in MapAuthoringTypeRegistry.Types)
        {
            search.Text = descriptor.Name;
            search.EmitSignal(LineEdit.SignalName.TextChanged, descriptor.Name);
            await nextFrame();
            Require(Descendants<Tree>(dialog).Any(tree => Contains(tree.GetRoot(), descriptor.Name)),
                $"Create Node search must show custom type {descriptor.Name}.");
        }

        dialog.GetCancelButton().EmitSignal(BaseButton.SignalName.Pressed);
        await nextFrame();
        Require(!dialog.Visible, "Create Node dialog must close through its Cancel action.");
    }

    private static bool Contains(TreeItem? item, string text)
    {
        for (var current = item; current is not null; current = current.GetNext())
        {
            if (current.GetText(0) == text || Contains(current.GetFirstChild(), text))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
            {
                yield return match;
            }
            foreach (var nested in Descendants<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
