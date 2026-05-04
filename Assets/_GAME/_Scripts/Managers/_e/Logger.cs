#if UNITY_EDITOR
using System;
using UnityEngine;

public enum LogPrefix { NULL, Audio, Environment, Player, Enemy, Networking }

/// <summary>
/// Central logging event system.
/// Allows filtered logs with prefixes.
/// </summary>
public static class LoggerEvent
{
    public static event Action<LogPrefix, object, UnityEngine.Object> OnLog;
    public static event Action<LogPrefix, object, UnityEngine.Object> OnLogWarning;
    public static event Action<LogPrefix, object, UnityEngine.Object> OnLogError;

    public static void Log(LogPrefix prefix, object message, UnityEngine.Object sender)
        => OnLog?.Invoke(prefix, message, sender);

    public static void LogWarning(LogPrefix prefix, object message, UnityEngine.Object sender)
        => OnLogWarning?.Invoke(prefix, message, sender);

    public static void LogError(LogPrefix prefix, object message, UnityEngine.Object sender)
        => OnLogError?.Invoke(prefix, message, sender);
}

/// <summary>
/// Component that listens for logs and displays them conditionally.
/// </summary>
public class Logger : MonoBehaviour
{
    #region Inspector

    [Header("Settings")]
    [Tooltip("Enable or disable logging.")]
    [SerializeField] private bool showLogs = false;

    [Tooltip("Which log prefix this logger listens to.")]
    [SerializeField] private LogPrefix prefix = LogPrefix.NULL;

    [Tooltip("Color used for prefix display.")]
    [SerializeField] private Color prefixColor = Color.white;

    #endregion

    #region Private Fields
    private string hexColor;
    #endregion

    #region Unity Events
    private void Awake()
    {
        hexColor = ColorUtility.ToHtmlStringRGB(prefixColor);
    }

    private void OnEnable()
    {
        LoggerEvent.OnLog += HandleLog;
        LoggerEvent.OnLogWarning += HandleLogWarning;
        LoggerEvent.OnLogError += HandleLogError;
    }

    private void OnDisable()
    {
        LoggerEvent.OnLog -= HandleLog;
        LoggerEvent.OnLogWarning -= HandleLogWarning;
        LoggerEvent.OnLogError -= HandleLogError;
    }
    #endregion

    #region Handlers

    private void HandleLog(LogPrefix logPrefix, object message, UnityEngine.Object sender)
    {
        if (!showLogs || logPrefix != prefix) return;

        Debug.Log(FormatMessage(message), sender);
    }

    private void HandleLogWarning(LogPrefix logPrefix, object message, UnityEngine.Object sender)
    {
        if (!showLogs || logPrefix != prefix) return;

        Debug.LogWarning(FormatMessage(message), sender);
    }

    private void HandleLogError(LogPrefix logPrefix, object message, UnityEngine.Object sender)
    {
        if (!showLogs || logPrefix != prefix) return;

        Debug.LogError(FormatMessage(message), sender);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Formats log message with colored prefix.
    /// </summary>
    private string FormatMessage(object message)
    {
        if (prefix == LogPrefix.NULL)
            return message.ToString();

        return $"<color=#{hexColor}>[{prefix}]</color> {message}";
    }

    #endregion
}
#endif