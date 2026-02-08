using UnityEngine;

public class SpriteTintOnPossession : MonoBehaviour, IPossessionCallbacks, IChargeInputReceiver
{
    [SerializeField] private SpriteRenderer[] renderers;

    [Header("Tints")]
    [SerializeField] private Color possessedTint = Color.yellow;
    [SerializeField] private Color chargingTint = new Color(1f, 0.6f, 0.1f); // 주황 느낌(원하는 색으로)

    private Color[] original;

    private bool isPossessed;
    private bool isCharging;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        original = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            original[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    // ✅ 차지 입력 들어오면(스페이스 홀드/릴리즈) 여기서 색 갱신
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
        isCharging = false; // 안전하게 리셋
        RestoreOriginal();
    }

    private void ApplyTint()
    {
        if (!isPossessed) return; // 빙의 중일 때만 색 바꾸고 싶으면 유지

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