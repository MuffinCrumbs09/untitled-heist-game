using UnityEngine;

[CreateAssetMenu(fileName = "New SoundList", menuName = "SoundList")]
public class SoundList : ScriptableObject
{
    public AudioClip[] soundList;
}
