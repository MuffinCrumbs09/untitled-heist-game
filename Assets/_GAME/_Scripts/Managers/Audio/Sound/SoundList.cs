using UnityEngine;

/// <summary>
/// ScriptableObject that stores a list of audio clips.
/// Used by SoundManager for flexible sound assignment.
/// </summary>
[CreateAssetMenu(fileName = "New SoundList", menuName = "SoundList")]
public class SoundList : ScriptableObject
{
    [Tooltip("List of audio clips.")]
    public AudioClip[] soundList;
}