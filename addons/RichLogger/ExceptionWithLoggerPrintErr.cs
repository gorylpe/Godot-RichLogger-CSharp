using System;
using System.Runtime.CompilerServices;
// ReSharper disable ExplicitCallerInfoArgument

public class ExceptionWithLoggerPrintErr : Exception
{
	public ExceptionWithLoggerPrintErr(string message,
		[CallerMemberName] string             memberName = "",
		[CallerFilePath]   string             filePath   = "",
		[CallerLineNumber] int                lineNumber = 0) : base(message) => Logger.Error(message, memberName, filePath, lineNumber, skipFrames: 1);

	public static void ThrowIfNull(object?             argument,
		string?                                        context      = null,
		[CallerArgumentExpression("argument")] string? argumentName = null,
		[CallerMemberName]                     string  memberName   = "",
		[CallerFilePath]                       string  filePath     = "",
		[CallerLineNumber]                     int     lineNumber   = 0)
	{
		if (argument != null) return;
		throw new ExceptionWithLoggerPrintErr($"Argument {argumentName} is null" + (context is null ? "" : $"; {context}"), memberName, filePath, lineNumber);
	}
}
