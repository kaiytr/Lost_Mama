using System.Collections.Generic;
using UnityEngine;

public class PossessableObject : MonoBehaviour
{
    private IActuator[] actuators;
    private IChargeInputReceiver[] chargers;
    private IPossessionCallbacks[] callbacks;

    public bool IsPossessed { get; private set; }

    private void Awake()
    {
        RefreshCache();
    }

    // ✅ 플레이 중에 컴포넌트 붙여도 잡히게 하고 싶으면 Start에서도 한 번 더
    private void Start()
    {
        RefreshCache();
    }

    public void RefreshCache()
    {
        var all = GetComponentsInChildren<MonoBehaviour>(true);

        var actList = new List<IActuator>(8);
        var chgList = new List<IChargeInputReceiver>(4);
        var cbList = new List<IPossessionCallbacks>(4);

        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;

            if (mb is IActuator a) actList.Add(a);
            if (mb is IChargeInputReceiver c) chgList.Add(c);
            if (mb is IPossessionCallbacks cb) cbList.Add(cb);
        }

        actuators = actList.ToArray();
        chargers = chgList.ToArray();
        callbacks = cbList.ToArray();
    }

    public void Handle(float x)
    {
        if (actuators == null) return;
        for (int i = 0; i < actuators.Length; i++)
            actuators[i].SetInput(x);
    }

    public void HandleCharging(bool isCharging)
    {
        if (chargers == null) return;
        for (int i = 0; i < chargers.Length; i++)
            chargers[i].SetCharging(isCharging);
    }

    public void SetPossessed(bool possessed)
    {
        if (IsPossessed == possessed) return;

        IsPossessed = possessed;

        if (callbacks == null) return;
        for (int i = 0; i < callbacks.Length; i++)
        {
            if (callbacks[i] == null) continue;
            if (possessed) callbacks[i].OnPossessed();
            else callbacks[i].OnUnpossessed();
        }
    }
}
