global using Logger = RichLogger.Logger;
using Godot;


namespace RichLogger;

public partial class LoggerAutoload : Node
{
	public LogLevel CurrentLevel
	{
		get => Logger.CurrentLevel;
		set
		{
			Logger.CurrentLevel = value;
			Logger.SaveSettings();
		}
	}

	public bool IncludeStackTraces
	{
		get => Logger.IncludeStackTraces;
		set
		{
			Logger.IncludeStackTraces = value;
			Logger.SaveSettings();
		}
	}

	public int StackTraceDepth
	{
		get => Logger.StackTraceDepth;
		set
		{
			Logger.StackTraceDepth = value;
			Logger.SaveSettings();
		}
	}

	public bool LogToFile
	{
		get => Logger.LogToFile;
		set
		{
			Logger.LogToFile = value;
			Logger.SaveSettings();
		}
	}

	public void Error(string message) => Logger.Error(message, skipFrames: 1);

	public void Warning(string message) => Logger.Warning(message, skipFrames: 1);

	public void Info(string message) => Logger.Info(message, skipFrames: 1);

	public void Debug(string message) => Logger.Debug(message, skipFrames: 1);

	public void Verbose(string message) => Logger.Verbose(message, skipFrames: 1);
}
