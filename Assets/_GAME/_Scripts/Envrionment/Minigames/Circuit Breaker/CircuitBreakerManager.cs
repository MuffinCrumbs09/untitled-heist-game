using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side manager that assigns serial numbers to circuit breakers and distributes parts of the correct serial to whiteboards.
/// </summary>
public class CircuitBreakerManager : NetworkBehaviour
{
    #region Variables

    public static CircuitBreakerManager Instance;

    private NetworkVariable<bool> _isHacking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool IsHacking => _isHacking.Value;

    [Header("Configuration")]
    [SerializeField]
    [Tooltip("List of all possible circuit breaker components in the level.")]
    private List<CircuitBreaker> circuitBreakers;

    [SerializeField]
    [Tooltip("List of whiteboard objects used to display parts of the serial.")]
    private List<Whiteboard> whiteboards;

    [SerializeField]
    [Tooltip("A list of objective indices where these circuits should be active.")]
    private int[] ObjectiveList;

    [SerializeField]
    [Tooltip("A list of task indices where these circuits should be active.")]
    private int[] TaskList;

    private static readonly char[] LetterPool = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] DigitPool = "23456789".ToCharArray();

    private List<CircuitBreaker> _activeCircuitBreakers = new();
    private List<Whiteboard> _activeWhiteboards = new();
    private List<string> serialNumbers = new();

    #endregion

    public override void OnNetworkSpawn()
    {
        if (Instance != null) Destroy(this);
        Instance = this;
    }

    #region Initialization Logic

    /// <summary>
    /// Scans for active breakers/whiteboards, generates a correct serial and decoy serials, and initializes the components.
    /// </summary>
    public void InitializeCircuitBreakers()
    {
        _activeCircuitBreakers.Clear();
        _activeWhiteboards.Clear();
        serialNumbers.Clear();

        string correctSerial = GenerateBaseSerial();

        // Detect active components
        foreach (var cb in circuitBreakers)
            if (cb != null && cb.transform.parent.GetComponent<RandomObject>().isSpawned.Value)
                _activeCircuitBreakers.Add(cb);

        foreach (var wb in whiteboards)
            if (wb != null && wb.gameObject.activeInHierarchy)
                _activeWhiteboards.Add(wb);

        // Shuffle for randomness
        for (int i = 0; i < _activeCircuitBreakers.Count; i++)
        {
            int swapIdx = Random.Range(i, _activeCircuitBreakers.Count);
            (_activeCircuitBreakers[i], _activeCircuitBreakers[swapIdx]) = (_activeCircuitBreakers[swapIdx], _activeCircuitBreakers[i]);
        }

        // Assign serials
        for (int i = 0; i < _activeCircuitBreakers.Count; i++)
        {
            string serialToAssign = (i == 0) ? correctSerial : (i == 1) ? GenerateDecoySerial(correctSerial) : GenerateBaseSerial();
            serialNumbers.Add(serialToAssign);
            _activeCircuitBreakers[i].Initialize(serialToAssign, serialToAssign == correctSerial);
        }

        // Distribute serial across whiteboards
        string[] segments = SplitSerial(correctSerial, _activeWhiteboards.Count);
        for (int i = 0; i < _activeWhiteboards.Count; i++)
        {
            _activeWhiteboards[i].SetSerial(segments[i]);
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if the current game objective and task match the requirements for circuit interaction.
    /// </summary>
    public bool IsObjective()
    {
        Vector2 current = Helper.GetCurrentObjectiveAndTaskIndex();
        if (current == new Vector2(-1, -1)) return false;

        foreach (var x in ObjectiveList)
            foreach (var y in TaskList)
                if (new Vector2(x, y) == current) return true;

        return false;
    }

    /// <summary>
    /// Splits a string into several segments to be displayed across multiple whiteboards.
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
            int len = baseLen + (i < remainder ? 1 : 0);
            parts[i] = serial.Substring(cursor, len);
            cursor += len;
        }
        return parts;
    }

    /// <summary> Sets the network hacking state. </summary>
    [Rpc(SendTo.Server)]
    public void SetHackingStateRpc(bool isHacking) => _isHacking.Value = isHacking;

    #endregion

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
            char[] decoyLetters = (char[])letters.Clone();
            decoyLetters[Random.Range(0, 3)] = RandomDifferentChar(LetterPool, decoyLetters[Random.Range(0, 3)]);

            char[] decoyDigits = (char[])digits.Clone();
            decoyDigits[Random.Range(0, 2)] = RandomDifferentChar(DigitPool, decoyDigits[Random.Range(0, 2)]);

            string candidate = $"{decoyLetters[0]}{decoyLetters[1]}{decoyLetters[2]}-{decoyDigits[0]}{decoyDigits[1]}";
            if (candidate != correct && !serialNumbers.Contains(candidate)) return candidate;
        }

        return GenerateBaseSerial();
    }

    private char RandomDifferentChar(char[] pool, char exclude)
    {
        var candidates = System.Array.FindAll(pool, c => c != exclude);
        return candidates.Length > 0 ? candidates[Random.Range(0, candidates.Length)] : exclude;
    }

    #endregion
}