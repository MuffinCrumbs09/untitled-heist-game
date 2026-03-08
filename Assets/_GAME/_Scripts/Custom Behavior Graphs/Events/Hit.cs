using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/Hit")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "HitEvent", message: "Agent was [Hit]", category: "Events", id: "150872130ef92a8817dca770ea115200")]
public sealed partial class HitEvent : EventChannel
{
    [SerializeReference] public BlackboardVariable<GameObject> Hit;
}

