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
            Debug.Log(curTarget);
            if (curTarget != null)
                return;
        }

        Debug.Log(shooter.name);

        agent.BlackboardReference.SetVariableValue<GameObject>(k_targetValue, shooter);
        agent.BlackboardReference.SetVariableValue<MemberState>(k_memberState, MemberState.Attacking);
    }

    public void RegisterDeath(GameObject killer)
    {
        if(killer.GetComponent<NetworkObject>().IsLocalPlayer)
            NetStore.Instance.TotalKills++;
    }
}