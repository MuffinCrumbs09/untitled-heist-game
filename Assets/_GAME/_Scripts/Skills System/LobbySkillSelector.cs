using Unity.Netcode;
using UnityEngine;

public class LobbySkillSelector : NetworkBehaviour
{
    public SkillTreeConfig Config;

    private int _pendingMask = 0;

    public System.Action<int> OnPendingMaskChanged;

    public void ToggleSkill(SkillType type)
    {
        int bit = 1 << (int)type;
        bool isSelected = (_pendingMask & bit) != 0;

        if (isSelected)
        {
            _pendingMask &= ~bit;
        }
        else
        {
            int cost = Config.Get(type)?.PointCost ?? 1;
            if (PointsSpent() + cost <= Config.TotalPoints)
                _pendingMask |= bit;
        }

        OnPendingMaskChanged?.Invoke(_pendingMask);
    }

    public bool HasSkill(SkillType type) => (_pendingMask & (1 << (int)type)) != 0;

    public int PointsSpent()
    {
        int mask = _pendingMask, count = 0;
        while (mask != 0) { count += mask & 1; mask >>= 1; }
        return count;
    }

    public int PointsRemaining() => Config.TotalPoints - PointsSpent();

    public void ConfirmSkills()
    {
        CommitSkillsServerRpc(NetworkManager.Singleton.LocalClientId, _pendingMask);
    }

    [Rpc(SendTo.Server)]
    private void CommitSkillsServerRpc(ulong clientId, int mask)
    {
        NetPlayerManager npm = NetPlayerManager.Instance;
        for (int i = 0; i < npm.playerData.Count; i++)
        {
            var data = npm.playerData[i];
            if (data.CLIENTID == clientId)
            {
                data.SKILLS = mask;
                npm.playerData[i] = data;
                break;
            }
        }
    }
}