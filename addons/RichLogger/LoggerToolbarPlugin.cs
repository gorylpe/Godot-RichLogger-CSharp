#if TOOLS
using System;
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

		// Get the BottomPanel parent container of all buttons
		foreach (var item in tempControl.GetParent().GetChildren())
		{
			// Find the button for the 'output' tab / panel
			if (item.Name.ToString().Contains("EditorLog"))
			{
				// Find the left VBoxContainer (vb_left in the C++ code)
				var vbLeft = FindOutputPanelVBoxLeft(item);
				if (vbLeft != null)
				{
					vbLeft.AddChild(_toolbar);
				}
				else
				{
					GD.PrintErr("[CSharpRichLogger] Could not find vb_left container in EditorLog!");
				}
			}
		}
		_toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		RemoveControlFromBottomPanel(tempControl);
	}

	/// <summary>
	/// Finds the left VBoxContainer in the EditorLog node.
	/// See: https://github.com/godotengine/godot/blob/master/editor/editor_log.cpp for how the output panel
	/// is built interally in godot
	/// </summary>
	/// <param name="editorLog"></param>
	/// <returns></returns>
	private VBoxContainer? FindOutputPanelVBoxLeft(Node editorLog)
	{
		// The structure is: EditorLog (HBoxContainer) -> VBoxContainer (vb_left)
		foreach (Node child in editorLog.GetChildren())
		{
			if (child is VBoxContainer vbox)
			{
				// Check if this VBoxContainer has the log and search box
				bool hasRichTextLabel = false;
				bool hasLineEdit = false;
				
				foreach (Node grandchild in vbox.GetChildren())
				{
					if (grandchild is RichTextLabel) hasRichTextLabel = true;
					if (grandchild is LineEdit lineEdit && lineEdit.PlaceholderText.Contains("Filter")) hasLineEdit = true;
				}
				
				if (hasRichTextLabel && hasLineEdit)
				{
					return vbox;
				}
			}
		}
		return null;
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
