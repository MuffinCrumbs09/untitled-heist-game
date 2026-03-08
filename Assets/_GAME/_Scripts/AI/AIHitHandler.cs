using UnityEngine;
using Unity.Behavior;
using Unity.Netcode;

public class AIHitHandler : MonoBehaviour
{
    private BehaviorGraphAgent agent;

    private const string k_targetValue = "Target";
    private const string k_memberState = "MemberState";

    private void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }

    public void RegisterHit(GameObject shooter)
    {
        if (agent.BlackboardReference.GetVariableValue(k_targetValue, out GameObject curTarget))
        {
            #if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Enemy, $"Already has target '{curTarget.name}'. Ignoring hit from '{shooter.name}'.", this);
            #endif
            if (curTarget != null)
                return;
        }

        #if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Enemy, $"Registering hit from '{shooter.name}'. Setting as new target.", this);
        #endif

        agent.BlackboardReference.SetVariableValue<GameObject>(k_targetValue, shooter);
        agent.BlackboardReference.SetVariableValue<MemberState>(k_memberState, MemberState.Attacking);
    }

    public void RegisterDeath(GameObject killer)
    {
        if(killer.GetComponent<NetworkObject>().IsLocalPlayer)
            NetPlayerManager.Instance.AddPlayerKillServerRpc();
    }
}