using Godot;

namespace ProceduralRts;

public partial class StyleCandidateDeckRoot
{
    private void HandleNavigationKey(Key keycode)
    {
        var families = Families();
        if (keycode == Key.Escape)
        {
            GetTree().Quit();
        }
        else if (keycode == Key.Left)
        {
            _selected = (_selected + families.Length - 1) % families.Length;
            QueueRedraw();
        }
        else if (keycode == Key.Right)
        {
            _selected = (_selected + 1) % families.Length;
            QueueRedraw();
        }
        else if (keycode >= Key.Key1 && keycode <= Key.Key6)
        {
            _selected = Mathf.Clamp((int)(keycode - Key.Key1), 0, families.Length - 1);
            QueueRedraw();
        }
        else if (keycode >= Key.Kp1 && keycode <= Key.Kp6)
        {
            _selected = Mathf.Clamp((int)(keycode - Key.Kp1), 0, families.Length - 1);
            QueueRedraw();
        }
    }
}
