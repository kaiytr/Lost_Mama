using UnityEngine;

public class SpriteTintOnPossession : MonoBehaviour, IPossessionCallbacks, IChargeInputReceiver
{
    [Header("Targets")]
    [SerializeField, Tooltip("켜면 아래 renderers 배열에 넣은 SpriteRenderer만 색을 바꿉니다.\n끄면 자동 수집(자식 포함 여부는 Tint Children로 결정)합니다.")]
    private bool useManualRenderers = false;

    [SerializeField, Tooltip("useManualRenderers가 켜져 있을 때만 사용")]
    private SpriteRenderer[] renderers;

    [SerializeField, Tooltip("자동 수집 모드일 때: 자식 오브젝트까지 포함할지")]
    private bool tintChildren = true;

    [SerializeField, Tooltip("자동 수집 모드일 때: 비활성(비활성 GameObject) 자식의 SpriteRenderer도 포함할지")]
    private bool includeInactiveChildren = true;

    [Header("Tints")]
    [SerializeField] private Color possessedTint = Color.yellow;
    [SerializeField] private Color chargingTint = new Color(1f, 0.6f, 0.1f);

    private Color[] original;
    private bool isPossessed;
    private bool isCharging;

    private void Awake()
    {
        RefreshRendererCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 인스펙터에서 토글 바꿨을 때 바로 반영되게(플레이 중엔 Awake/ContextMenu로)
        if (!Application.isPlaying)
            RefreshRendererCache();
    }
#endif

    [ContextMenu("Refresh Renderer Cache")]
    public void RefreshRendererCache()
    {
        if (!useManualRenderers || renderers == null || renderers.Length == 0)
        {
            // ✅ 자동 수집
            renderers = tintChildren
                ? GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren)
                : GetComponents<SpriteRenderer>();
        }

        // 원본 색 저장
        original = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            original[i] = renderers[i] != null ? renderers[i].color : Color.white;

        // 플레이 중 상태에 맞춰 색 다시 적용
        if (Application.isPlaying)
        {
            if (isPossessed) ApplyTint();
            else RestoreOriginal();
        }
    }

    public void SetCharging(bool charging)
    {
        isCharging = charging;
        ApplyTint();
    }

    public void OnPossessed()
    {
        isPossessed = true;
        ApplyTint();
    }

    public void OnUnpossessed()
    {
        isPossessed = false;
        isCharging = false;
        RestoreOriginal();
    }

    private void ApplyTint()
    {
        if (!isPossessed) return;

        Color tint = isCharging ? chargingTint : possessedTint;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = tint;
    }

    private void RestoreOriginal()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = original[i];
    }
}