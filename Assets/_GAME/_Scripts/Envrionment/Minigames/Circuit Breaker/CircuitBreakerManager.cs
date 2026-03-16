using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CircuitBreakerManager : NetworkBehaviour
{
    public static CircuitBreakerManager Instance;

    private NetworkVariable<bool> _isHacking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool IsHacking => _isHacking.Value;

    [Header("Configuration")]
    [SerializeField] private List<CircuitBreaker> circuitBreakers;
    [SerializeField] private List<Whiteboard> whiteboards;

    private static readonly char[] LetterPool = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] DigitPool = "23456789".ToCharArray();

    private List<CircuitBreaker> _activeCircuitBreakers = new();
    private List<Whiteboard> _activeWhiteboards = new();
    private List<string> serialNumbers = new();

    public override void OnNetworkSpawn()
    {
        if (Instance != null)
            Destroy(this);

        Instance = this;
    }

    public void InitializeCircuitBreakers()
    {
        _activeCircuitBreakers.Clear();
        _activeWhiteboards.Clear();
        serialNumbers.Clear();

        Debug.Log($"[CircuitBreakerManager] InitializeCircuitBreakers called. IsServer={IsServer}, IsSpawned={IsSpawned}");
        Debug.Log($"[CircuitBreakerManager] Configured circuit breakers: {circuitBreakers.Count}, whiteboards: {whiteboards.Count}");

        string correctSerial = GenerateBaseSerial();
        Debug.Log($"[CircuitBreakerManager] Correct serial: {correctSerial}");

        foreach (var cb in circuitBreakers)
        {
            bool active = cb != null && cb.gameObject.activeInHierarchy;
            Debug.Log($"[CircuitBreakerManager] CircuitBreaker '{cb?.name}' activeInHierarchy={active}");
            if (active)
                _activeCircuitBreakers.Add(cb);
        }

        foreach (var wb in whiteboards)
        {
            bool active = wb != null && wb.gameObject.activeInHierarchy;
            Debug.Log($"[CircuitBreakerManager] Whiteboard '{wb?.name}' activeInHierarchy={active}, IsSpawned={wb?.IsSpawned}, IsServer={wb?.IsServer}");
            if (active)
                _activeWhiteboards.Add(wb);
        }

        Debug.Log($"[CircuitBreakerManager] Active circuit breakers: {_activeCircuitBreakers.Count}, active whiteboards: {_activeWhiteboards.Count}");

        // Shuffle circuit breakers to randomize which gets the correct serial
        for (int i = 0; i < _activeCircuitBreakers.Count; i++)
        {
            int swapIdx = Random.Range(i, _activeCircuitBreakers.Count);
            (_activeCircuitBreakers[i], _activeCircuitBreakers[swapIdx]) =
                (_activeCircuitBreakers[swapIdx], _activeCircuitBreakers[i]);
        }

        // Assign serials to circuit breakers
        for (int i = 0; i < _activeCircuitBreakers.Count; i++)
        {
            string serialToAssign = i == 0 ? correctSerial
                                  : i == 1 ? GenerateDecoySerial(correctSerial)
                                  : GenerateBaseSerial();
            serialNumbers.Add(serialToAssign);
            _activeCircuitBreakers[i].Initialize(serialToAssign, serialToAssign == correctSerial);
            Debug.Log($"[CircuitBreakerManager] Assigned serial '{serialToAssign}' to '{_activeCircuitBreakers[i].name}' (isCorrect={serialToAssign == correctSerial})");
        }

        // Split the correct serial across active whiteboards
        string[] segments = SplitSerial(correctSerial, _activeWhiteboards.Count);
        Debug.Log($"[CircuitBreakerManager] Split '{correctSerial}' into {segments.Length} segment(s): [{string.Join(", ", segments)}]");

        for (int i = 0; i < _activeWhiteboards.Count; i++)
        {
            Debug.Log($"[CircuitBreakerManager] Calling SetSerial('{segments[i]}') on whiteboard '{_activeWhiteboards[i].name}'");
            _activeWhiteboards[i].SetSerial(segments[i]);
        }

        // RPC circuit breakers to clients
        NetString[] serialsArray = new NetString[serialNumbers.Count];
        NetString[] pathsArray = new NetString[_activeCircuitBreakers.Count];

        for (int i = 0; i < serialNumbers.Count; i++)
            serialsArray[i] = serialNumbers[i];

        for (int i = 0; i < _activeCircuitBreakers.Count; i++)
            pathsArray[i] = Helper.GetGameObjectPath(_activeCircuitBreakers[i].gameObject);

        InitalizeCircuitBreakersRpc(correctSerial, serialsArray, pathsArray);
    }

    /// <summary>
    /// Splits a serial as evenly as possible across <paramref name="partCount"/> parts.
    /// e.g. "ABC-47" with 3 parts → ["AB", "C-", "47"]
    ///      "ABC-47" with 2 parts → ["ABC", "-47"]
    ///      "ABC-47" with 1 part  → ["ABC-47"]
    /// </summary>
    private string[] SplitSerial(string serial, int partCount)
    {
        partCount = Mathf.Clamp(partCount, 1, serial.Length);
        string[] parts = new string[partCount];
        int baseLen = serial.Length / partCount;
        int remainder = serial.Length % partCount;
        int cursor = 0;

        for (int i = 0; i < partCount; i++)
        {
            // Distribute leftover characters one-by-one to the first segments
            int len = baseLen + (i < remainder ? 1 : 0);
            parts[i] = serial.Substring(cursor, len);
            cursor += len;
        }

        return parts;
    }

    /// <summary> Set wether a a hack is currently in progress. This can be used by circuit breakers to disable interaction during hacking. </summary>
    /// <param name="isHacking">Is the hack in progress</param> 
    [Rpc(SendTo.Server)]
    public void SetHackingStateRpc(bool isHacking)
    {
        _isHacking.Value = isHacking;
    }

    [Rpc(SendTo.NotServer)]
    private void InitalizeCircuitBreakersRpc(NetString correctSerial, NetString[] serials, NetString[] circuitPaths)
    {
        for (int i = 0; i < circuitPaths.Length; i++)
        {
            GameObject cbObj = GameObject.Find(circuitPaths[i]);
            if (cbObj == null) continue;

            CircuitBreaker cb = cbObj.GetComponent<CircuitBreaker>();
            if (cb != null)
                cb.Initialize(serials[i], serials[i] == correctSerial);
        }
    }

    #region Serial Generation
    private string GenerateBaseSerial()
    {
        char l0 = LetterPool[Random.Range(0, LetterPool.Length)];
        char l1 = LetterPool[Random.Range(0, LetterPool.Length)];
        char l2 = LetterPool[Random.Range(0, LetterPool.Length)];
        char d0 = DigitPool[Random.Range(0, DigitPool.Length)];
        char d1 = DigitPool[Random.Range(0, DigitPool.Length)];
        return $"{l0}{l1}{l2}-{d0}{d1}";
    }

    private string GenerateDecoySerial(string correct, int maxAttempts = 50)
    {
        char[] letters = { correct[0], correct[1], correct[2] };
        char[] digits = { correct[4], correct[5] };

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int swapLetterIdx = Random.Range(0, 3);
            char[] decoyLetters = (char[])letters.Clone();
            decoyLetters[swapLetterIdx] = RandomDifferentChar(LetterPool, decoyLetters[swapLetterIdx]);

            int swapDigitIdx = Random.Range(0, 2);
            char[] decoyDigits = (char[])digits.Clone();
            decoyDigits[swapDigitIdx] = RandomDifferentChar(DigitPool, decoyDigits[swapDigitIdx]);

            string candidate = $"{decoyLetters[0]}{decoyLetters[1]}{decoyLetters[2]}-{decoyDigits[0]}{decoyDigits[1]}";

            if (candidate != correct && !serialNumbers.Contains(candidate))
                return candidate;
        }

        string fallback;
        do { fallback = GenerateBaseSerial(); }
        while (fallback == correct || serialNumbers.Contains(fallback));
        return fallback;
    }

    private char RandomDifferentChar(char[] pool, char exclude)
    {
        var candidates = System.Array.FindAll(pool, c => c != exclude);
        return candidates.Length > 0
            ? candidates[Random.Range(0, candidates.Length)]
            : exclude;
    }
    #endregion
}