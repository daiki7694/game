using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CrowdDistanceLOD : MonoBehaviour
{
    [Header("対象カメラ（未設定なら自動で Camera.main）")]
    public Camera targetCamera;

    [Header("距離しきい値（メートル）")]
    [Tooltip("この距離まではアニメON＆描画ON")]
    public float lod1Distance = 20f;
    [Tooltip("この距離を超えたら描画OFF（将来はビルボード推奨）")]
    public float lod2Distance = 40f;

    [Header("更新間隔（秒）")]
    [Tooltip("0.25〜0.5 くらいでOK。小さすぎるとCPU負荷が増えます")]
    public float checkInterval = 0.25f;

    private readonly List<Entry> entries = new List<Entry>();
    private float timer;

    [System.Serializable]
    class Entry
    {
        public Transform root;
        public Animator anim;
        public Renderer[] renderers;
        public State state;
    }

    enum State { LOD0_Near, LOD1_Mid, LOD2_Far }

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        // このGameObject配下の「各キャラのAnimator」を1体ずつ拾う
        var anims = GetComponentsInChildren<Animator>(true);
        entries.Clear();
        foreach (var a in anims)
        {
            var e = new Entry
            {
                root = a.transform,
                anim = a,
                renderers = a.GetComponentsInChildren<Renderer>(true),
                state = State.LOD0_Near
            };
            entries.Add(e);
        }
        // 念のため距離の整合
        if (lod2Distance < lod1Distance) lod2Distance = lod1Distance + 1f;
        if (checkInterval < 0.05f) checkInterval = 0.05f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        if (targetCamera == null) return;
        var camPos = targetCamera.transform.position;
        float d1Sqr = lod1Distance * lod1Distance;
        float d2Sqr = lod2Distance * lod2Distance;

        foreach (var e in entries)
        {
            if (e.root == null) continue;
            float distSqr = (e.root.position - camPos).sqrMagnitude;

            // 近: アニメON＆描画ON
            if (distSqr <= d1Sqr)
            {
                if (e.state != State.LOD0_Near)
                {
                    SetAnim(e, true);
                    SetRender(e, true);
                    e.state = State.LOD0_Near;
                }
            }
            // 中: アニメOFF＆描画ON（ポーズ固定）
            else if (distSqr <= d2Sqr)
            {
                if (e.state != State.LOD1_Mid)
                {
                    SetAnim(e, false);
                    SetRender(e, true);
                    e.state = State.LOD1_Mid;
                }
            }
            // 遠: アニメOFF＆描画OFF（将来はビルボードに置換推奨）
            else
            {
                if (e.state != State.LOD2_Far)
                {
                    SetAnim(e, false);
                    SetRender(e, false);
                    e.state = State.LOD2_Far;
                }
            }
        }
    }

    void SetAnim(Entry e, bool on)
    {
        if (e.anim == null) return;
        e.anim.enabled = on;
        e.anim.cullingMode = on
            ? AnimatorCullingMode.CullUpdateTransforms
            : AnimatorCullingMode.CullCompletely;
        e.anim.applyRootMotion = false;
        e.anim.updateMode = AnimatorUpdateMode.Normal;
    }

    void SetRender(Entry e, bool on)
    {
        if (e.renderers == null) return;
        for (int i = 0; i < e.renderers.Length; i++)
        {
            if (e.renderers[i] != null) e.renderers[i].enabled = on;
        }
    }
}
