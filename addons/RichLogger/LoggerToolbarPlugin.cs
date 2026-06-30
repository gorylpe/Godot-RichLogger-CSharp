#if TOOLS
using System;
using Godot;
namespace RichLogger;

[Tool]
public partial class LoggerToolbarPlugin : EditorPlugin
{
	private LoggerToolbar? _toolbar;

	private string PluginPath => GetScript().As<CSharpScript>().ResourcePath.GetBaseDir();
    
	public override void _EnablePlugin()
	{
		AddAutoloadSingleton("RichLogger", $"{PluginPath}/LoggerAutoload.cs");
		base._EnablePlugin();
	}
    
	public override void _DisablePlugin()
	{
		RemoveAutoloadSingleton("RichLogger");
		base._DisablePlugin();
	}
	
	public override void _EnterTree()
	{
		_toolbar = new LoggerToolbar();

		var outputPanel = FindOutputPanelRoot(out bool foundOutputPanelRoot);
		var vbLeft = FindOutputPanelVBoxLeft(outputPanel, requireLineEdit: !foundOutputPanelRoot);
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
	private VBoxContainer? FindOutputPanelVBoxLeft(Node editorLog, bool requireLineEdit)
	{
		if (editorLog is VBoxContainer vbox && IsOutputPanelVBoxLeft(vbox, requireLineEdit))
			return vbox;

		foreach (Node child in editorLog.GetChildren())
		{
			var found = FindOutputPanelVBoxLeft(child, requireLineEdit);
			if (found != null)
				return found;
		}

		return null;
	}

	private Node FindOutputPanelRoot(out bool foundOutputPanelRoot)
	{
		var parent = GetParent();
		var output = parent.FindChild("Output", recursive: true, owned: false);
		if (output != null)
		{
			foundOutputPanelRoot = true;
			return output;
		}

		var editorLog = parent.FindChild("*EditorLog*", recursive: true, owned: false);
		if (editorLog != null)
		{
			foundOutputPanelRoot = true;
			return editorLog;
		}

		foundOutputPanelRoot = false;
		return parent;
	}

	private bool IsOutputPanelVBoxLeft(VBoxContainer vbox, bool requireLineEdit)
	{
		if (!HasDescendant<RichTextLabel>(vbox))
			return false;

		if (!requireLineEdit)
			return true;

		var filterLineEdit = FindDescendant<LineEdit>(vbox, lineEdit =>
			lineEdit.PlaceholderText.Contains("Filter", StringComparison.OrdinalIgnoreCase) ||
			lineEdit.Name.ToString().Contains("Filter", StringComparison.OrdinalIgnoreCase));
		if (filterLineEdit != null)
			return true;

		// Godot editor internals move between minor versions. The Output panel's
		// log VBox is still the one containing the RichTextLabel even if the
		// filter LineEdit moves into a nested toolbar/control.
		return FindDescendant<LineEdit>(vbox) != null;
	}

	private bool HasDescendant<T>(Node node) where T : Node
	{
		return FindDescendant<T>(node) != null;
	}

	private T? FindDescendant<T>(Node node, Func<T, bool>? predicate = null) where T : Node
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is T typedChild && (predicate == null || predicate(typedChild)))
				return typedChild;

			var found = FindDescendant(child, predicate);
			if (found != null)
				return found;
		}

		return null;
	}

	public override void _ExitTree()
	{
		if (_toolbar == null)
			return;

		var parent = _toolbar.GetParent() as VBoxContainer;
		parent?.RemoveChild(_toolbar);

		_toolbar.QueueFree();
		_toolbar = null;
	}
}
#endif
