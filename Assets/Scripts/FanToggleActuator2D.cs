using UnityEngine;

public class FanToggleActuator2D : MonoBehaviour, IActuator, IPossessionCallbacks
{
    [Header("What to toggle (drag your fan script here, e.g., FanPushZone2D)")]
    [SerializeField] private Behaviour fanBehaviour;

    [Header("Optional: also toggle the trigger collider (wind zone)")]
    [SerializeField] private Collider2D windTriggerCollider;

    [Header("Visual: range indicator (child object like WindRangeVisual)")]
    [SerializeField] private GameObject windRangeVisualRoot;

    private bool isPossessed;
    private int lastDir; // 0이면 '안 누름' 상태

    private void Awake()
    {
        if (fanBehaviour == null)
            fanBehaviour = GetComponent<FanPushZone2D>();

        if (windTriggerCollider == null)
            windTriggerCollider = GetComponent<Collider2D>();

        // 자동 연결(자식 이름이 WindRangeVisual이면 자동으로 잡힘)
        if (windRangeVisualRoot == null)
        {
            var t = transform.Find("WindRangeVisual");
            if (t) windRangeVisualRoot = t.gameObject;
        }

        // 시작 상태 동기화
        SyncVisual();
    }

    public void OnPossessed()
    {
        isPossessed = true;
        lastDir = 0;
    }

    public void OnUnpossessed()
    {
        isPossessed = false;
        lastDir = 0;
    }

    public void SetInput(float x)
    {
        if (!isPossessed) return;

        int dir = (x > 0.5f) ? 1 : (x < -0.5f ? -1 : 0);

        if (dir == 0)
        {
            lastDir = 0;
            return;
        }

        if (lastDir == 0)
        {
            if (dir > 0) SetFan(true);   // D
            else SetFan(false);          // A
        }

        lastDir = dir;
    }

    private void SetFan(bool on)
    {
        // 꺼질 때 캐시 정리(있으면 실행, 없으면 무시)
        if (!on && fanBehaviour != null)
            fanBehaviour.SendMessage("ClearInside", SendMessageOptions.DontRequireReceiver);

        if (fanBehaviour != null) fanBehaviour.enabled = on;

        if (windTriggerCollider != null) windTriggerCollider.enabled = on;

        // ✅ 범위 비주얼도 같이
        if (windRangeVisualRoot != null) windRangeVisualRoot.SetActive(on);
    }

    private void SyncVisual()
    {
        bool on = (fanBehaviour != null) ? fanBehaviour.enabled : true;
        if (windRangeVisualRoot != null) windRangeVisualRoot.SetActive(on);
    }
}