#if TOOLS
using Godot;

[Tool]
public partial class LoggerToolbarPlugin : EditorPlugin
{
	private LoggerToolbar? _toolbar;

	public override void _EnterTree()
	{
		_toolbar = new LoggerToolbar();

#if GODOT4_6_OR_GREATER
// ┠╴Output
// ┃  ┠╴@Timer@7423
// ┃  ┖╴@HBoxContainer@7424
// ┃     ┠╴@VBoxContainer@7425
// ┃     ┃  ┠╴@RichTextLabel@7428
// ┃     ┃  ┃  ┠╴@VScrollBar@7426
// ┃     ┃  ┃  ┖╴@Timer@7427
// ┃     ┃  ┖╴@LineEdit@7429
		var outputHBoxContainer = GetParent().FindChild("Output", owned: false).GetChildren()[1];
#else
// ┖╴@EditorLog@7343
//     ┠╴@Timer@7325
//     ┠╴@VBoxContainer@7326
//     ┃  ┠╴@RichTextLabel@7328
//     ┃  ┃  ┖╴@VScrollBar@7327
//     ┃  ┖╴@LineEdit@7329
		var outputHBoxContainer = GetParent().FindChild("*EditorLog*", owned: false);
#endif
		if (outputHBoxContainer == null)
		{
			GD.Print("[CSharpRichLogger] Could not find Output window");
			return;
		}
		
		var vbLeft = FindOutputPanelVBoxLeft(outputHBoxContainer);
		if (vbLeft == null)
		{
			GD.PrintErr("[CSharpRichLogger] Could not find vb_left container in EditorLog!");
			return;
		}

		_toolbar.Name = "RichLoggerToolbar";
		_toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		vbLeft.AddChild(_toolbar);
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
