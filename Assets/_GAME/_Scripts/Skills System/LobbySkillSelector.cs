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
        CommitSkillsServerRpc(NetworkManager.Singleton.LocalClientId, _pendingMask);
    }

    public bool HasSkill(SkillType type) => (_pendingMask & (1 << (int)type)) != 0;

    public int PointsSpent()
    {
        int total = 0;
        int mask = _pendingMask;
        int index = 0;

        while (mask != 0)
        {
            if ((mask & 1) != 0)
            {
                SkillType type = (SkillType)index;
                int cost = Config.Get(type)?.PointCost ?? 1;
                total += cost;
            }

            mask >>= 1;
            index++;
        }

        return total;
    }

    public int PointsRemaining() => Config.TotalPoints - PointsSpent();

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