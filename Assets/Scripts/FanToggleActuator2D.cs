using UnityEngine;

public class FanToggleActuator2D : MonoBehaviour, IActuator, IPossessionCallbacks
{
    [Header("What to toggle (drag your fan script here, e.g., FanPushZone2D)")]
    [SerializeField] private Behaviour fanBehaviour;

    [Header("Optional: also toggle the trigger collider (wind zone)")]
    [SerializeField] private Collider2D windTriggerCollider;

    private bool isPossessed;
    private int lastDir; // 0이면 '안 누름' 상태

    private void Awake()
    {
        // 자동 연결(원하면 수동으로 드래그해도 됨)
        if (fanBehaviour == null)
            fanBehaviour = GetComponent<FanPushZone2D>(); // 팬 스크립트 이름이 FanPushZone2D라면

        if (windTriggerCollider == null)
            windTriggerCollider = GetComponent<Collider2D>();
    }

    public void OnPossessed()
    {
        isPossessed = true;
        lastDir = 0; // 빙의 시작 시 엣지 초기화
    }

    public void OnUnpossessed()
    {
        isPossessed = false;
        lastDir = 0;
    }

    // PossessionManager가 매 프레임 x(-1/0/+1)를 넣어줌
    public void SetInput(float x)
    {
        if (!isPossessed) return; // ✅ 빙의 중일 때만

        int dir = (x > 0.5f) ? 1 : (x < -0.5f ? -1 : 0);

        // 키를 떼면 다음 입력을 다시 "누름"으로 인식하게 리셋
        if (dir == 0)
        {
            lastDir = 0;
            return;
        }

        // ✅ '0 -> ±1'로 바뀌는 순간만 처리(키 누르는 순간)
        if (lastDir == 0)
        {
            if (dir > 0) SetFan(true);   // D
            else SetFan(false);  // A
        }

        lastDir = dir;
    }

    private void SetFan(bool on)
    {
        // 팬 스크립트가 insideCount 같은 캐시를 갖고 있다면
        // 꺼질 때 ClearInside를 호출해 캐시 꼬임을 방지(없으면 그냥 무시됨)
        if (!on && fanBehaviour != null)
            fanBehaviour.SendMessage("ClearInside", SendMessageOptions.DontRequireReceiver);

        if (fanBehaviour != null) fanBehaviour.enabled = on;

        // (선택) 바람 트리거 콜라이더도 같이 껐다 켜고 싶으면
        if (windTriggerCollider != null) windTriggerCollider.enabled = on;
    }
}