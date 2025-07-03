#if TOOLS
using Godot;

[Tool]
public partial class LoggerToolbarPlugin : EditorPlugin
{
    private LoggerToolbar? _toolbar;

    public override void _EnterTree()
    {
        _toolbar = new LoggerToolbar();

        var tempControl = new Control();
        var tabButton = AddControlToBottomPanel(tempControl, "Temp");

        var tabContainer = tabButton.GetParent();
        var panelContainer = tabContainer.GetParent();

        Control? contentContainer = null;
        for (var i = 0; i < panelContainer.GetChildCount(); i++)
        {
            var child = panelContainer.GetChild(i);
            if (child == tabContainer || child is not Control control)
                continue;

            contentContainer = control;
            break;
        }

        RemoveControlFromBottomPanel(tempControl);
        tempControl.QueueFree();

        var outputPanel = contentContainer!.GetChild(0) as VBoxContainer;

        outputPanel!.AddChild(_toolbar);
        _toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    }

    public override void _ExitTree()
    {
        if (_toolbar == null)
            return;

        var parent = _toolbar.GetParent() as VBoxContainer;
        parent!.RemoveChild(_toolbar);

        _toolbar.QueueFree();
        _toolbar = null;
    }
}
#endif
