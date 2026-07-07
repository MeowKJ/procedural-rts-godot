using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public static class BattleCursorGodotShapes
{
    public static Input.CursorShape ToInputShape(BattleCursorShape shape)
    {
        return shape switch
        {
            BattleCursorShape.PointingHand => Input.CursorShape.PointingHand,
            BattleCursorShape.Cross => Input.CursorShape.Cross,
            BattleCursorShape.Move => Input.CursorShape.Move,
            BattleCursorShape.CanDrop => Input.CursorShape.CanDrop,
            BattleCursorShape.Forbidden => Input.CursorShape.Forbidden,
            BattleCursorShape.Drag => Input.CursorShape.Drag,
            BattleCursorShape.Help => Input.CursorShape.Help,
            _ => Input.CursorShape.Arrow,
        };
    }

    public static Control.CursorShape ToControlShape(BattleCursorShape shape)
    {
        return shape switch
        {
            BattleCursorShape.PointingHand => Control.CursorShape.PointingHand,
            BattleCursorShape.Cross => Control.CursorShape.Cross,
            BattleCursorShape.Move => Control.CursorShape.Move,
            BattleCursorShape.CanDrop => Control.CursorShape.CanDrop,
            BattleCursorShape.Forbidden => Control.CursorShape.Forbidden,
            BattleCursorShape.Drag => Control.CursorShape.Drag,
            BattleCursorShape.Help => Control.CursorShape.Help,
            _ => Control.CursorShape.Arrow,
        };
    }
}
