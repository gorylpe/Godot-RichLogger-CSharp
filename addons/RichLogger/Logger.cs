using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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
	private const  string             PluginSettingsPath = "user://logger_settings.cfg";
	private static FileSystemWatcher? _fileWatcher;
	private static bool               _settingsChanged;

	static Logger()
	{
		LoadSettings();
		SaveSettings();
		SetupFileWatcher();
	}

	public static LogLevel CurrentLevel       { get; set; } = LogLevel.Info;
	public static bool     IncludeStackTraces { get; set; }
	public static int      StackTraceDepth    { get; set; } = 3;

	public static void Error(string message,
		[CallerMemberName] string   memberName = "",
		[CallerFilePath]   string   filePath   = "",
		[CallerLineNumber] int      lineNumber = 0,
		int                         skipFrames = 0)
	{
		CheckAndReloadSettings();
		if (CurrentLevel >= LogLevel.Error)
			Log(LogLevel.Error, message, memberName, filePath, lineNumber, skipFrames);
	}

	public static void Warning(string message,
		[CallerMemberName] string     memberName = "",
		[CallerFilePath]   string     filePath   = "",
		[CallerLineNumber] int        lineNumber = 0,
		int                           skipFrames = 0)
	{
		CheckAndReloadSettings();
		if (CurrentLevel >= LogLevel.Warning)
			Log(LogLevel.Warning, message, memberName, filePath, lineNumber, skipFrames);
	}

	public static void Info(string message,
		[CallerMemberName] string  memberName = "",
		[CallerFilePath]   string  filePath   = "",
		[CallerLineNumber] int     lineNumber = 0,
		int                        skipFrames = 0)
	{
		CheckAndReloadSettings();
		if (CurrentLevel >= LogLevel.Info)
			Log(LogLevel.Info, message, memberName, filePath, lineNumber, skipFrames);
	}

	public static void Debug(string message,
		[CallerMemberName] string   memberName = "",
		[CallerFilePath]   string   filePath   = "",
		[CallerLineNumber] int      lineNumber = 0,
		int                         skipFrames = 0)
	{
		CheckAndReloadSettings();
		if (CurrentLevel >= LogLevel.Debug)
			Log(LogLevel.Debug, message, memberName, filePath, lineNumber, skipFrames);
	}

	public static void Verbose(string message,
		[CallerMemberName] string     memberName = "",
		[CallerFilePath]   string     filePath   = "",
		[CallerLineNumber] int        lineNumber = 0,
		int                           skipFrames = 0)
	{
		CheckAndReloadSettings();
		if (CurrentLevel >= LogLevel.Verbose)
			Log(LogLevel.Verbose, message, memberName, filePath, lineNumber, skipFrames);
	}

	public static void InternalInfo(string message) => Log(LogLevel.Info, message, "", "", 0, 0);

	private static void Log(LogLevel level, string message, string memberName, string filePath, int lineNumber, int skipFrames = 0)
	{
		var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

		var callerInfo = GetCallerInfo(memberName, filePath, lineNumber);
		var coloredMessage = GetColoredMessage(level, timestamp, message, callerInfo);

		var stackTrace = "";
		if (IncludeStackTraces)
			stackTrace = GetStackTrace(skipFrames);

		GD.PrintRich(coloredMessage + stackTrace);
	}

	private static string GetCallerInfo(string memberName, string filePath, int lineNumber)
	{
		var fileName = Path.GetFileName(filePath);
		var className = string.IsNullOrEmpty(fileName) ? "Unknown" : Path.GetFileNameWithoutExtension(fileName);
		return $"Class: {className} Method: {memberName} File: {fileName} Line: {lineNumber}";
	}

	private static string GetColoredMessage(LogLevel level, string timestamp, string message, string callerInfo)
	{
		var levelName = level.ToString().ToUpper();
		var levelColor = GetColorForLevel(level);
		var resetColor = "[color=#FFFFFF]";

		var hoverTooltip = $"[hint={callerInfo}]";
		var hoverClose = "[/hint]";

		return $"[color=#AAAAAA][{timestamp}][/color] {levelColor}{hoverTooltip}[{levelName}]{hoverClose}{resetColor} {message}";
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

	private static string GetStackTrace(int skipAdditionalFrames = 0)
	{
		var sb = new StringBuilder();
		sb.AppendLine("\n[color=#888888]Stack trace:[/color]");

		var stackTrace = new StackTrace(true);
		var stackFrames = stackTrace.GetFrames();

		// Skip first frames which are the logger methods themselves + any additional frames requested
		var startFrame = 3 + skipAdditionalFrames; // Skip Logger.GetStackTrace, Logger.Log, Logger.Error/Debug/etc. + skipAdditionalFrames
		var endFrame = Math.Min(startFrame + StackTraceDepth, stackFrames.Length);

		for (var i = startFrame; i < endFrame; i++)
		{
			var frame = stackFrames[i];
			var fileName = Path.GetFileName(frame.GetFileName() ?? "Unknown");
			var methodName = frame.GetMethod()?.Name ?? "Unknown";
			var lineNumber = frame.GetFileLineNumber();

			sb.AppendLine($"[color=#888888]  at {methodName} in {fileName}:line {lineNumber}[/color]");
		}

		return sb.ToString();
	}

	public static void LogObject<T>(LogLevel level, string context, T obj,
		[CallerMemberName] string            memberName = "",
		[CallerFilePath]   string            filePath   = "",
		[CallerLineNumber] int               lineNumber = 0,
		int                                  skipFrames = 0)
	{
		if (CurrentLevel < level) return;

		var objString = obj is GodotObject gdObj && GodotObject.IsInstanceValid(gdObj) ? gdObj.ToString() : obj?.ToString() ?? "null";

		Log(level, $"{context}: {objString}", memberName, filePath, lineNumber, skipFrames);
	}

	private static void SetupFileWatcher()
	{
		try
		{
			var filePath = ProjectSettings.GlobalizePath(PluginSettingsPath);
			if (!File.Exists(filePath))
				throw new FileNotFoundException($"Settings file not found: {filePath}");

			var directory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Invalid directory path");
			var fileName = Path.GetFileName(filePath);

			_fileWatcher = new FileSystemWatcher(directory)
			{
				Filter = fileName,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
				IncludeSubdirectories = false,
				EnableRaisingEvents = true
			};
			_fileWatcher.Changed += OnFileChanged;
			_fileWatcher.Created += OnFileChanged;
			_fileWatcher.Renamed += OnFileChanged;
		}
		catch (Exception ex)
		{
			InternalInfo($"File watcher setup failed: {ex.Message}");
		}
	}

	private static void OnFileChanged(object sender, FileSystemEventArgs e) => _settingsChanged = true;

	private static void CheckAndReloadSettings()
	{
		if (!_settingsChanged) return;
		_settingsChanged = false;
		LoadSettings();
	}

	public static void SaveSettings()
	{
		var config = new ConfigFile();
		config.SetValue("Logger", "LogLevel", (int)CurrentLevel);
		config.SetValue("Logger", "IncludeStackTraces", IncludeStackTraces);
		config.SetValue("Logger", "StackTraceDepth", StackTraceDepth);

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
