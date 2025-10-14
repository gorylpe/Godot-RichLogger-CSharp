using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

public enum LogLevel
{
	Error   = 0,
	Warning = 1,
	Info    = 2,
	Debug   = 3,
	Verbose = 4
}

public static partial class Logger
{
	[GeneratedRegex(@"\[/?(?:color|hint)(?:=[^\]]+)?\]")]
	private static partial Regex BbCodeRegex();

	private const  string         PluginSettingsPath = "user://logger_settings.cfg";
	private static LogFileWriter? _fileWriter;
	private static DateTime       _lastSettingsCheck = DateTime.MinValue;
	private static DateTime       _lastSettingsWrite = DateTime.MinValue;
	private static bool           _checkInProgress;
	private static readonly long  _processId = OS.GetProcessId();

	static Logger()
	{
		LoadSettings();
		SaveSettings();
		InitializeFileWriter();
		_lastSettingsWrite = GetSettingsFileTime();
	}

	public static LogLevel CurrentLevel       { get; set; } = LogLevel.Info;
	public static bool     IncludeStackTraces { get; set; }
	public static int      StackTraceDepth    { get; set; } = 3;
	public static bool     LogToFile          { get; set; } = true;
	public static long     ProcessId          => _processId;

	private static void InitializeFileWriter()
	{
		try
		{
			_fileWriter = new LogFileWriter(_processId);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to initialize file writer: {ex.Message}");
		}
	}

	private static DateTime GetSettingsFileTime()
	{
		try
		{
			var filePath = ProjectSettings.GlobalizePath(PluginSettingsPath);
			if (File.Exists(filePath))
				return File.GetLastWriteTimeUtc(filePath);
		}
		catch
		{
		}

		return DateTime.MinValue;
	}

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

		var fullMessage = coloredMessage + stackTrace;
		GD.PrintRich(fullMessage);

		if (LogToFile)
			_fileWriter?.Write(StripBbCode(fullMessage));
	}

	private static string StripBbCode(string text) => BbCodeRegex().Replace(text, "");

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

		var instancePrefix = $"[color=#CCCCCC][PID:{_processId}][/color] ";

		return $"{instancePrefix}[color=#AAAAAA][{timestamp}][/color] {levelColor}{hoverTooltip}[{levelName}]{hoverClose}{resetColor} {message}";
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

	private static void CheckAndReloadSettings()
	{
		var now = DateTime.UtcNow;
		if ((now - _lastSettingsCheck).TotalSeconds < 1.0)
			return;

		_lastSettingsCheck = now;
		var currentWriteTime = GetSettingsFileTime();

		if (currentWriteTime > _lastSettingsWrite)
		{
			_lastSettingsWrite = currentWriteTime;
			LoadSettings();
		}
	}

	public static void SaveSettings()
	{
		var config = new ConfigFile();
		config.SetValue("Logger", "LogLevel", (int)CurrentLevel);
		config.SetValue("Logger", "IncludeStackTraces", IncludeStackTraces);
		config.SetValue("Logger", "StackTraceDepth", StackTraceDepth);
		config.SetValue("Logger", "LogToFile", LogToFile);

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

		if (config.HasSectionKey("Logger", "LogToFile"))
		{
			var logToFile = (bool)config.GetValue("Logger", "LogToFile");
			LogToFile = logToFile;
		}
	}
}
