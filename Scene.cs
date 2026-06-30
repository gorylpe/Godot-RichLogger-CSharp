using Godot;

namespace RichLogger;

[Tool]
public partial class Scene : Node2D
{
	public override async void _Ready()
	{
		Logger.Info("Test");

		if (!Engine.IsEditorHint())
		{
			GetTree().Quit();
			return;
		}

		if (DisplayServer.GetName() != "headless") // only self-test in CI, not while editing
			return;

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var ok = EditorInterface.Singleton.GetBaseControl()
			.FindChild("RichLoggerToolbar", recursive: true, owned: false) != null;
		GD.Print(ok ? "[SelfTest] PASS" : "[SelfTest] FAIL");
		GetTree().Quit(ok ? 0 : 1);
	}
}
