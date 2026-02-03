using UnityEngine;

public class SpriteTintOnPossession : MonoBehaviour, IPossessionCallbacks
{
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private Color possessedTint = Color.yellow;

    private Color[] original;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        original = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            original[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    public void OnPossessed()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = possessedTint;
    }

    public void OnUnpossessed()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = original[i];
    }
}
