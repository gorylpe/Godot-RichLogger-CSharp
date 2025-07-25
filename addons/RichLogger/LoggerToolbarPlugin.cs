#if TOOLS
using System.Linq;
using Godot;

[Tool]
public partial class LoggerToolbarPlugin : EditorPlugin
{
	private LoggerToolbar? _toolbar;

	public override void _EnterTree()
	{
		_toolbar = new LoggerToolbar();

		var tempControl = new Control { Name = "Temp" };
		var tabButton = AddControlToBottomPanel(tempControl, "Temp");

#if GODOT4_4_OR_GREATER
		var hBoxParent = tabButton.GetParent().GetParent().GetParent();
#elif GODOT4_3_OR_GREATER
		var hBoxParent = tabButton.GetParent().GetParent();
#else
		GD.PrintErr($"[{nameof(LoggerToolbarPlugin)}] Unsupported Godot version!");
		var hBoxParent = tabButton.GetParent().GetParent().GetParent();
#endif
		RemoveControlFromBottomPanel(tempControl);
		tempControl.QueueFree();

		var editorToaster = hBoxParent.GetChildren().FirstOrDefault(x => x.Name.ToString().Contains("EditorToaster"));
		if (editorToaster == null)
		{
			GD.PrintErr($"[{nameof(LoggerToolbarPlugin)}] Failed to find EditorToaster!");
			return;
		}

		if (editorToaster.GetChild(0) is not VBoxContainer outputVBoxContainer)
		{
			GD.PrintErr($"[{nameof(LoggerToolbarPlugin)}] Failed to find output VBoxContainer!");
			return;
		}

		outputVBoxContainer.AddChild(_toolbar);
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
