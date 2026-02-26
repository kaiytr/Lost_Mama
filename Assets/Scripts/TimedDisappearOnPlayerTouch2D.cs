using System.Collections;
using UnityEngine;

public class TimedDisappearOnPlayerTouch2D : MonoBehaviour
{
    [Header("Detect (Player Layer)")]
    [SerializeField] private LayerMask playerMask;

    [Header("Timing")]
    [SerializeField] private float disappearDelay = 0.2f;  // 닿고 몇 초 뒤 사라짐
    [SerializeField] private float reappearDelay = 1.5f;   // 사라진 뒤 몇 초 후 복구

    [Header("What to toggle (비우면 자동으로 찾음)")]
    [SerializeField] private Collider2D[] collidersToToggle;
    [SerializeField] private Renderer[] renderersToToggle;

    [Header("Options")]
    [SerializeField] private bool triggerOnlyOnceUntilReappear = true; // 복구 전까지 재발동 방지

    private bool isActive = true;
    private bool routineRunning = false;

    private void Awake()
    {
        if (collidersToToggle == null || collidersToToggle.Length == 0)
            collidersToToggle = GetComponentsInChildren<Collider2D>(true);

        if (renderersToToggle == null || renderersToToggle.Length == 0)
            renderersToToggle = GetComponentsInChildren<Renderer>(true);
    }

    // ✅ 발판처럼 "밟는" 오브젝트면 Trigger가 아니라 Collision이 자연스러움
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTrigger(collision.collider);
    }

    // ✅ 트리거로 만들고 싶으면(감지용 존) 이쪽도 지원
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider2D other)
    {
        if (!isActive && triggerOnlyOnceUntilReappear) return;
        if (routineRunning) return;

        if (((1 << other.gameObject.layer) & playerMask) == 0) return;

        StartCoroutine(DisappearRoutine());
    }

    private IEnumerator DisappearRoutine()
    {
        routineRunning = true;

        // 닿은 뒤 잠깐 있다가 사라짐
        if (disappearDelay > 0f)
            yield return new WaitForSeconds(disappearDelay);

        SetVisibleAndSolid(false);

        // 사라진 상태 유지
        if (reappearDelay > 0f)
            yield return new WaitForSeconds(reappearDelay);

        SetVisibleAndSolid(true);

        routineRunning = false;
    }

    private void SetVisibleAndSolid(bool on)
    {
        isActive = on;

        // 렌더러 토글(보이기/숨기기)
        foreach (var r in renderersToToggle)
            if (r) r.enabled = on;

        // 콜라이더 토글(밟기/통과)
        foreach (var c in collidersToToggle)
            if (c) c.enabled = on;
    }
}