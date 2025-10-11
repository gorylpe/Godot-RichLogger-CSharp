using Godot;
using System;
using System.IO;
using System.Linq;

public class LogFileWriter : IDisposable
{
	private readonly string       _logFilePath;
	private readonly string       _logDirectory;
	private          StreamWriter? _writer;
	private          int          _messagesSinceFlush;
	private          bool         _flushInProgress;

	private const int FlushThreshold = 10;

	public int MaxLogFiles { get; set; } = 10;

	public LogFileWriter()
	{
		_logDirectory = ProjectSettings.GlobalizePath("user://logs/");
		Directory.CreateDirectory(_logDirectory);

		CleanupOldLogs();

		var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
		var context = Engine.IsEditorHint() ? "editor" : "game";
		_logFilePath = Path.Combine(_logDirectory, $"logger_{timestamp}_{context}.log");

		_writer = new StreamWriter(_logFilePath, true) { AutoFlush = false };
	}

	public void Write(string message)
	{
		if (_writer == null) return;

		try
		{
			_writer.WriteLine(message);
			_messagesSinceFlush++;

			if (_messagesSinceFlush >= FlushThreshold && !_flushInProgress)
			{
				_messagesSinceFlush = 0;
				_flushInProgress = true;

				var writer = _writer;
				WorkerThreadPool.AddTask(Callable.From(() =>
				{
					try
					{
						writer?.Flush();
					}
					catch (Exception ex)
					{
						GD.PrintErr($"LogFileWriter flush error: {ex.Message}");
					}
					finally
					{
						_flushInProgress = false;
					}
				}));
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"LogFileWriter error: {ex.Message}");
		}
	}

	private void CleanupOldLogs()
	{
		try
		{
			var logFiles = Directory.GetFiles(_logDirectory, "logger_*.log")
				.OrderByDescending(f => f)
				.Skip(MaxLogFiles - 1)
				.ToList();

			foreach (var file in logFiles)
				File.Delete(file);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to cleanup old logs: {ex.Message}");
		}
	}

	public void Dispose()
	{
		try
		{
			_writer?.Flush();
			_writer?.Dispose();
			_writer = null;
		}
		catch (Exception ex)
		{
			GD.PrintErr($"LogFileWriter dispose error: {ex.Message}");
		}
	}
}
