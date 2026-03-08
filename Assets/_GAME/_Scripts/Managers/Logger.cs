#if UNITY_EDITOR
using System;
using UnityEngine;

public enum LogPrefix { NULL, Audio, Environment, Player, Enemy, Networking }

public static class LoggerEvent
{
    public static event Action<LogPrefix, object, UnityEngine.Object> OnLog;
    public static event Action<LogPrefix, object, UnityEngine.Object> OnLogWarning;
    public static event Action<LogPrefix, object, UnityEngine.Object> OnLogError;

    public static void Log(LogPrefix prefix, object message, UnityEngine.Object sender)
    {
        OnLog?.Invoke(prefix, message, sender);
    }

    public static void LogWarning(LogPrefix prefix, object message, UnityEngine.Object sender)
    {
        OnLogWarning?.Invoke(prefix, message, sender);
    }

    public static void LogError(LogPrefix prefix, object message, UnityEngine.Object sender)
    {
        OnLogError?.Invoke(prefix, message, sender);
    }
}

public class Logger : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] bool _showLogs = false;
    [SerializeField] LogPrefix _prefix = LogPrefix.NULL;
    [SerializeField] Color _prefixColor = Color.white;

    private string _hexColor;

    #region Unity Events
    private void Awake()
    {
        _hexColor = ColorUtility.ToHtmlStringRGB(_prefixColor);
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

    #region Logging Functions
    private void HandleLog(LogPrefix prefix, object message, UnityEngine.Object sender)
    {
        if (!_showLogs || (prefix != _prefix)) return;

        Debug.Log(FormatMessage(message), sender);
    }

    private void HandleLogWarning(LogPrefix prefix, object message, UnityEngine.Object sender)
    {
        if (!_showLogs || (prefix != _prefix)) return;

        Debug.LogWarning(FormatMessage(message), sender);
    }

    private void HandleLogError(LogPrefix prefix, object message, UnityEngine.Object sender)
    {
        if (!_showLogs || (prefix != _prefix)) return;

        Debug.LogError(FormatMessage(message), sender);
    }
    #endregion

    #region Helper
    private string FormatMessage(object message)
    {
        if (_prefix == LogPrefix.NULL)
            return message.ToString();

        return $"<color=#{_hexColor}>[{_prefix}]</color> {message}";
    }
    #endregion
}
#endif