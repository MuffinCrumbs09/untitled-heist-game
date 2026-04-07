using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;

public class AIHitHandler : MonoBehaviour
{
    private BehaviorGraphAgent agent;

    private const string k_targetValue = "Target";
    private const string k_memberState = "MemberState";

    private void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }

    /// <summary>
    /// Called when this enemy is hit.
    /// shooterClientId is the NetworkClientId of the shooter.
    /// If the shooter is an AI, pass Health.AI_KILLER_ID and supply the shooter's
    /// NetworkObject directly so the behavior graph can still track a target GameObject.
    /// </summary>
    public void RegisterHit(ulong shooterClientId, NetworkObject shooterNetObj = null)
    {
        if (agent.BlackboardReference.GetVariableValue(k_targetValue, out GameObject curTarget))
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Enemy, $"Already has target. Ignoring hit from client {shooterClientId}.", this);
#endif
            if (curTarget != null)
                return;
        }

        // Resolve a GameObject for the behavior graph target.
        // For player shooters, look up their NetworkObject via NetworkManager.
        // For AI shooters, the caller passes the NetworkObject directly.
        GameObject shooterGO = null;

        if (shooterClientId != Health.AI_KILLER_ID)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client))
            {
                shooterGO = client.PlayerObject?.gameObject;
            }
        }
        else if (shooterNetObj != null)
        {
            shooterGO = shooterNetObj.gameObject;
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Enemy, $"Registering hit from client {shooterClientId}. Target GO: {(shooterGO != null ? shooterGO.name : "none")}.", this);
#endif

        agent.BlackboardReference.SetVariableValue<GameObject>(k_targetValue, shooterGO);
        agent.BlackboardReference.SetVariableValue<MemberState>(k_memberState, MemberState.Attacking);
    }
}
