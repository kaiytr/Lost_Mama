using System.Collections.Generic;
using UnityEngine;

public class PossessableObject : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private bool allowSelectFromChildren = true;

    [SerializeField, Tooltip("OFF면 IsTrigger 콜라이더 클릭은 선택에서 제외됩니다.")]
    private bool allowSelectFromTriggerColliders = true;

    [Header("Optional: Explicit selection colliders")]
    [SerializeField, Tooltip("ON이면 아래 selectionColliders에 들어있는 콜라이더로 클릭했을 때만 선택됩니다.\n(Trigger/자식 여부와 관계없이 리스트가 최우선)")]
    private bool useExplicitSelectionColliders = false;

    [SerializeField, Tooltip("선택을 허용할 콜라이더 목록(비워두면 의미 없음)")]
    private Collider2D[] selectionColliders;

    public bool AllowSelectFromChildren => allowSelectFromChildren;
    public bool AllowSelectFromTriggerColliders => allowSelectFromTriggerColliders;
    public bool UseExplicitSelectionColliders => useExplicitSelectionColliders;

    public bool IsColliderSelectable(Collider2D clickedCol)
    {
        if (clickedCol == null) return false;

        // ✅ 1) 명시 리스트 모드면 "리스트에 있냐"만 본다 (Trigger/자식 규칙 무시)
        if (useExplicitSelectionColliders)
        {
            if (selectionColliders == null) return false;
            for (int i = 0; i < selectionColliders.Length; i++)
            {
                if (selectionColliders[i] == clickedCol) return true;
            }
            return false;
        }

        // ✅ 2) 일반 모드: 자식 클릭 허용/금지
        if (!allowSelectFromChildren && clickedCol.transform != transform)
            return false;

        // ✅ 3) 일반 모드: Trigger 클릭 허용/금지
        if (clickedCol.isTrigger && !allowSelectFromTriggerColliders)
            return false;

        return true;
    }

    // ===== 기존 코드 =====
    private IActuator[] actuators;
    private IChargeInputReceiver[] chargers;
    private IPossessionCallbacks[] callbacks;

    public bool IsPossessed { get; private set; }

    private void Awake() => RefreshCache();
    private void Start() => RefreshCache();

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