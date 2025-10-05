using Godot;

[Tool]
public partial class LoggerToolbar : HBoxContainer
{
    private OptionButton _logLevelDropdown = null!;
    private CheckBox _stackTraceToggle = null!;
    private SpinBox _stackDepthSpinner = null!;
    private Button _testLogButton = null!;

    private string _pluginSettingsPath = "user://logger_settings.cfg";

    public override void _EnterTree()
    {
        if (GetChildCount() == 0)
            SetupUi();

        LoadSettingsFromLogger();
        Logger.InternalInfo($"Logger initialized with settings Level: {Logger.CurrentLevel}, Stack Trace: {Logger.IncludeStackTraces} at {Logger.StackTraceDepth} depth");
    }

    private void SetupUi()
    {
        AddThemeConstantOverride("separation", 8);

        var label = new Label { Text = "Logger:" };
        AddChild(label);

        _logLevelDropdown = new OptionButton();
        _logLevelDropdown.AddItem("Error", (int)LogLevel.Error);
        _logLevelDropdown.AddItem("Warning", (int)LogLevel.Warning);
        _logLevelDropdown.AddItem("Info", (int)LogLevel.Info);
        _logLevelDropdown.AddItem("Debug", (int)LogLevel.Debug);
        _logLevelDropdown.AddItem("Verbose", (int)LogLevel.Verbose);
        _logLevelDropdown.CustomMinimumSize = new Vector2(100, 0);
        _logLevelDropdown.TooltipText = "Set the global logging level";
        _logLevelDropdown.ItemSelected += OnLogLevelSelected;
        AddChild(_logLevelDropdown);

        var separator1 = new VSeparator();
        AddChild(separator1);

        _stackTraceToggle = new CheckBox { Text = "Stack Traces" };
        _stackTraceToggle.TooltipText = "Include stack traces in log output";
        _stackTraceToggle.Toggled += OnStackTraceToggled;
        AddChild(_stackTraceToggle);

        var depthLabel = new Label { Text = "Depth:" };
        AddChild(depthLabel);

        _stackDepthSpinner = new SpinBox();
        _stackDepthSpinner.MinValue = 1;
        _stackDepthSpinner.MaxValue = 20;
        _stackDepthSpinner.Value = 3;
        _stackDepthSpinner.CustomMinimumSize = new Vector2(70, 0);
        _stackDepthSpinner.TooltipText = "Number of stack frames to display";
        _stackDepthSpinner.ValueChanged += OnStackDepthChanged;
        AddChild(_stackDepthSpinner);

        var separator2 = new VSeparator();
        AddChild(separator2);

        _testLogButton = new Button();
        _testLogButton.Text = "Test Log";
        _testLogButton.TooltipText = "Generate test logs at all levels";
        _testLogButton.Pressed += OnTestLogPressed;
        AddChild(_testLogButton);
    }

    private static void OnLogLevelSelected(long index)
    {
        Logger.CurrentLevel = (LogLevel)index;
        Logger.InternalInfo("Log level changed to {0}", Logger.CurrentLevel);
        Logger.SaveSettings();
    }

    private static void OnStackTraceToggled(bool toggled)
    {
        Logger.IncludeStackTraces = toggled;
        Logger.InternalInfo("Stack traces {0}", toggled ? "enabled" : "disabled");
        Logger.SaveSettings();
    }

    private static void OnStackDepthChanged(double value)
    {
        Logger.StackTraceDepth = (int)value;
        Logger.InternalInfo("Stack trace depth set to {0}", Logger.StackTraceDepth);
        Logger.SaveSettings();
    }

    private static void OnTestLogPressed()
    {
        Logger.Error("Test ERROR message");
        Logger.Warning("Test WARNING message");
        Logger.Info("Test INFO message");
        Logger.Debug("Test DEBUG message");
        Logger.Verbose("Test VERBOSE message");

        Logger.InternalInfo("Current settings: Level={0}, StackTraces={1}, Depth={2}",
            Logger.CurrentLevel,
            Logger.IncludeStackTraces,
            Logger.StackTraceDepth);
    }

    private void LoadSettingsFromLogger()
    {
        _logLevelDropdown.Selected = (int)Logger.CurrentLevel;
        _stackTraceToggle.ButtonPressed = Logger.IncludeStackTraces;
        _stackDepthSpinner.Value = Logger.StackTraceDepth;
    }
}