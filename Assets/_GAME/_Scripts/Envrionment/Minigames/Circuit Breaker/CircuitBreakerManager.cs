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
    [SerializeField, Tooltip("A list of objective index's the circuits should be enabled")] private int[] ObjectiveList;
    [SerializeField, Tooltip("A list of task index's the circuits should be enabled")] private int[] TaskList;

    private static readonly char[] LetterPool = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] DigitPool = "23456789".ToCharArray();

    private List<CircuitBreaker> _activeCircuitBreakers = new();
    private List<Whiteboard> _activeWhiteboards = new();
    private List<string> serialNumbers = new();

    private string ScriptTag => "[CircuitBreakerManager]".Color(Color.deepPink);

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

        string correctSerial = GenerateBaseSerial();

        foreach (var cb in circuitBreakers)
        {
            bool active = cb != null && cb.transform.parent.GetComponent<RandomObject>().isSpawned.Value;
            if (active)
                _activeCircuitBreakers.Add(cb);
        }

        foreach (var wb in whiteboards)
        {
            bool active = wb != null && wb.gameObject.activeInHierarchy;
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, string.Format("{0} : Whiteboard '{1}' is {2}", ScriptTag, wb.name, active), this);
#endif
            if (active)
                _activeWhiteboards.Add(wb);
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, string.Format("{0} : Active circuit breakers: {1}, active whiteboards: {2}", ScriptTag, _activeCircuitBreakers.Count, _activeWhiteboards.Count), this);
#endif

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
        }

        // Split the correct serial across active whiteboards
        string[] segments = SplitSerial(correctSerial, _activeWhiteboards.Count);

        for (int i = 0; i < _activeWhiteboards.Count; i++)
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, string.Format("{0} : Setting serial of whiteboard {1}, too {2}", ScriptTag, _activeWhiteboards[i].name, segments[i]), this);
#endif
            _activeWhiteboards[i].SetSerial(segments[i]);
        }
    }

    public bool IsObjective()
    {
        Vector2 current = Helper.GetCurrentObjectiveAndTaskIndex();

        if (current == new Vector2(-1, -1))
        {
#if UNITY_EDITOR
            LoggerEvent.LogError(LogPrefix.Environment, "Cannot find current task objective and index", this);
#endif
            return false;
        }

        foreach (var x in ObjectiveList)
        {
            foreach (var y in TaskList)
            {
                Vector2 potentialTask = new(x, y);
                if (potentialTask == current)
                    return true;
            }
        }

        return false;
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