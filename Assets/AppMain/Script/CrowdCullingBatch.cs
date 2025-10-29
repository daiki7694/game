using UnityEditor;
using UnityEngine;

public static class CrowdCullingBatch
{
    [MenuItem("ツール/Crowd/選択にカリング適用 %#k")]  // Ctrl+Shift+K のショートカット
    public static void ApplyToSelection()
    {
        int total = 0;

        foreach (var go in Selection.gameObjects)
        {
            ApplyRecursive(go.transform);
            total++;
        }

        Debug.Log($"[Crowd] 選択中の {total} 個にカリング設定を適用しました。");
    }

    static void ApplyRecursive(Transform t)
    {
        foreach (var anim in t.GetComponentsInChildren<Animator>(true))
        {
            anim.cullingMode = AnimatorCullingMode.CullCompletely;
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.applyRootMotion = false;
        }

        foreach (var skin in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skin.updateWhenOffscreen = false;
            skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skin.receiveShadows = false;
        }
    }
}
