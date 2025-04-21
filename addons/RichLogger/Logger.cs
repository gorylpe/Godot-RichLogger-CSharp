using Godot;
using System;
using System.Diagnostics;
using System.Text;

public enum LogLevel
{
    Error   = 0,
    Warning = 1,
    Info    = 2,
    Debug   = 3,
    Verbose = 4
}

public static class Logger
{
    private const string PluginSettingsPath = "user://logger_settings.cfg";

    static Logger() => LoadSettings();

    public static LogLevel CurrentLevel       { get; set; } = LogLevel.Info;
    public static bool     IncludeStackTraces { get; set; }
    public static int      StackTraceDepth    { get; set; } = 3;

    public static void Error(string message, params object[] args)
    {
        if (CurrentLevel >= LogLevel.Error)
            Log(LogLevel.Error, message, args);
    }

    public static void Warning(string message, params object[] args)
    {
        if (CurrentLevel >= LogLevel.Warning)
            Log(LogLevel.Warning, message, args);
    }

    public static void Info(string message, params object[] args)
    {
        if (CurrentLevel >= LogLevel.Info)
            Log(LogLevel.Info, message, args);
    }

    public static void Debug(string message, params object[] args)
    {
        if (CurrentLevel >= LogLevel.Debug)
            Log(LogLevel.Debug, message, args);
    }

    public static void Verbose(string message, params object[] args)
    {
        if (CurrentLevel >= LogLevel.Verbose)
            Log(LogLevel.Verbose, message, args);
    }
    
    public static void InternalInfo(string message, params object[] args) => Log(LogLevel.Info, message, args);

    private static void Log(LogLevel level, string message, params object[] args)
    {
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        var coloredMessage = GetColoredMessage(level, timestamp, formattedMessage);

        var stackTrace = "";
        if (IncludeStackTraces)
            stackTrace = GetStackTrace();

        GD.PrintRich(coloredMessage + stackTrace);
    }

    private static string GetColoredMessage(LogLevel level, string timestamp, string message)
    {
        var levelName = level.ToString().ToUpper();
        var levelColor = GetColorForLevel(level);
        var resetColor = "[color=#FFFFFF]";

        return $"[color=#AAAAAA][{timestamp}][/color] {levelColor}[{levelName}]{resetColor} {message}";
    }

    private static string GetColorForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Error   => "[color=#FF5555]",
            LogLevel.Warning => "[color=#FFAA55]",
            LogLevel.Info    => "[color=#55AAFF]",
            LogLevel.Debug   => "[color=#55FF55]",
            LogLevel.Verbose => "[color=#AAAAAA]",
            _                => "[color=#FFFFFF]"
        };
    }

    private static string GetStackTrace()
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n[color=#888888]Stack trace:[/color]");

        var stackTrace = new StackTrace(true);
        StackFrame[] stackFrames = stackTrace.GetFrames();

        // Skip first frames which are the logger methods themselves
        var startFrame = 3; // Skip Logger.Log, Logger.Error/Debug/etc., and the calling method
        var endFrame = Math.Min(startFrame + StackTraceDepth, stackFrames.Length);

        for (var i = startFrame; i < endFrame; i++)
        {
            var frame = stackFrames[i];
            var fileName = System.IO.Path.GetFileName(frame.GetFileName() ?? "Unknown");
            var methodName = frame.GetMethod()?.Name ?? "Unknown";
            var lineNumber = frame.GetFileLineNumber();

            sb.AppendLine($"[color=#888888]  at {methodName} in {fileName}:line {lineNumber}[/color]");
        }

        return sb.ToString();
    }

    public static void LogObject<T>(LogLevel level, string context, T obj)
    {
        if (CurrentLevel < level) return;

        var objString = GodotObject.IsInstanceValid(obj as GodotObject) ? (obj as GodotObject).ToString() : obj?.ToString() ?? "null";

        Log(level, $"{context}: {objString}");
    }

    public static void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("Logger", "LogLevel",           (int)CurrentLevel);
        config.SetValue("Logger", "IncludeStackTraces", IncludeStackTraces);
        config.SetValue("Logger", "StackTraceDepth",    StackTraceDepth);

        var error = config.Save(PluginSettingsPath);
        if (error != Godot.Error.Ok)
        {
            GD.PrintErr($"Failed to save logger settings: {error}");
        }
    }

    public static void LoadSettings()
    {
        var config = new ConfigFile();
        var error = config.Load(PluginSettingsPath);

        if (error != Godot.Error.Ok)
            return;

        if (config.HasSectionKey("Logger", "LogLevel"))
        {
            var logLevel = (int)config.GetValue("Logger", "LogLevel");
            CurrentLevel = (LogLevel)logLevel;
        }

        if (config.HasSectionKey("Logger", "IncludeStackTraces"))
        {
            var includeStackTraces = (bool)config.GetValue("Logger", "IncludeStackTraces");
            IncludeStackTraces = includeStackTraces;
        }

        if (config.HasSectionKey("Logger", "StackTraceDepth"))
        {
            var stackTraceDepth = (int)config.GetValue("Logger", "StackTraceDepth");
            StackTraceDepth = stackTraceDepth;
        }
    }
}