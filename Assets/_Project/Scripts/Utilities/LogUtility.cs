using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public static class LogUtility
{
    public enum LogLevel
    {
        Chat,
        Info,
        Warning,
        Error,
        None
    }

    public enum LogColor
    {
        Default,
        Warning,
        WarningDark,
        Error,
        ErrorDark,
        File,
        Function,
        Line,
        Inverted
    }

    public static readonly LogLevel verbosity = LogLevel.Warning;
    public static readonly LogLevel batchModeConsoleVerbosity = LogLevel.Info;

    private static int _horizontalLineWidth = 0;
    private static string _horizontalLine = string.Empty;
    private static string HorizontalLine
    {
        get
        {
            if (_horizontalLineWidth != Console.WindowWidth)
            {
                StringBuilder builder = new StringBuilder(Console.WindowWidth);
                for (int i = 0; i < builder.Capacity; i++)
                {
                    builder.Append("#");
                }
                _horizontalLine = builder.ToString();
                _horizontalLineWidth = Console.WindowWidth;
            }
            return _horizontalLine;
        }
    }
    private static int _isBatchMode = 0;
    private static bool IsBatchMode
    {
        get
        {
            if (_isBatchMode == 0)
            {
                _isBatchMode = Application.isBatchMode ? 1 : 2;
            }
            return _isBatchMode == 1 ? true : false;
        }
    }

    private static void PrintCallerInfo(LogLevel level, string caller, string file, int lineNumber)
    {
        // Time
        SetColor(LogColor.Inverted);
        Console.Write(DateTime.Now.ToString());
        SetColor();
        Console.Write(" ");

        // Log level / verbosity
        switch (level)
        {
            case LogLevel.Info:
                SetColor(LogColor.Inverted);
                Console.Write("[INFO]");
                SetColor();
                Console.Write(" ");
                break;
            case LogLevel.Warning:
                SetColor(LogColor.Warning);
                Console.Write("[WARNING]");
                SetColor();
                Console.Write(" ");
                break;
            case LogLevel.Error:
                SetColor(LogColor.Error);
                Console.Write("[ERROR]");
                SetColor();
                Console.Write(" ");
                break;
            default:
                break;
        }

        // If log level is "Info" or lower, don't bother printing out function information.
        if (level <= LogLevel.Info) return;

        Console.Write(" || ");

        // Caller file name
        SetColor(LogColor.File);
        Console.Write(Path.GetFileName(file));
        SetColor();
        Console.Write(" => ");

        // Caller function
        SetColor(LogColor.Function);
        Console.Write(caller + "()");
        SetColor();
        Console.Write(" => ");

        // Caller line number
        SetColor(LogColor.Line);
        Console.Write("Line " + lineNumber.ToString());
        SetColor();
        Console.WriteLine();
    }

    private static void SetColor(LogColor color = LogColor.Default)
    {
        switch (color)
        {
            case LogColor.Default:
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogColor.Warning:
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                break;
            case LogColor.WarningDark:
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogColor.Error:
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogColor.ErrorDark:
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogColor.Inverted:
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                break;
            case LogColor.File:
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogColor.Function:
                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogColor.Line:
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                break;
            default:
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }
    }

    public static void Log(string message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (IsBatchMode)
        {
            if (batchModeConsoleVerbosity <= LogLevel.Info)
            {
                PrintCallerInfo(LogLevel.Info, caller, file, lineNumber);
                Console.WriteLine(message);
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("(" + Path.GetFileName(file) + ") " + caller + "() Line " + lineNumber + ": " + message);
#else
            if (verbosity <= LogLevel.Info) Debug.Log("(" + Path.GetFileName(file) + ") " + caller + "() Line " + lineNumber + ": " + message);
#endif
        }
    }

    public static void LogWarning(string message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (IsBatchMode)
        {
            if (batchModeConsoleVerbosity <= LogLevel.Warning)
            {
                Console.WriteLine();

                PrintCallerInfo(LogLevel.Warning, caller, file, lineNumber);

                SetColor(LogColor.Warning);
                Console.WriteLine(message);
                SetColor();

                Console.WriteLine();
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("(" + Path.GetFileName(file) + ") " + caller + "() Line " + lineNumber + ": " + message);
#else
            if (verbosity <= LogLevel.Warning) Debug.LogWarning("(" + Path.GetFileName(file) + ") " + caller + "() Line " + lineNumber + ": " + message);
#endif
        }
    }

    public static void LogError(string message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (IsBatchMode)
        {
            if (batchModeConsoleVerbosity <= LogLevel.Error)
            {
                Console.WriteLine();
                SetColor(LogColor.Error);
                Console.Write(HorizontalLine);

                PrintCallerInfo(LogLevel.Error, caller, file, lineNumber);

                SetColor(LogColor.ErrorDark);
                Console.WriteLine(message);

                SetColor(LogColor.Error);
                Console.Write(HorizontalLine);
                SetColor();
                Console.WriteLine();
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("(" + Path.GetFileName(file) + ") " + caller + "() Line " + lineNumber + ": " + message);
#else
            if (verbosity <= LogLevel.Error) Debug.LogError("(" + Path.GetFileName(file) + ") " + caller + "() Line " + lineNumber + ": " + message);
#endif
        }
    }

    public static void LogChat(string playerName, string message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (IsBatchMode)
        {
            if (batchModeConsoleVerbosity <= LogLevel.Info)
            {
                PrintCallerInfo(LogLevel.Chat, caller, file, lineNumber);
                Console.WriteLine("(" + playerName + ") " + message);
            }
        }
    }
}
