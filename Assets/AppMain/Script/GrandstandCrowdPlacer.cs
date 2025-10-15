using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class GrandstandCrowdPlacer : MonoBehaviour
{
    [Header("対象")]
    public Transform grandstand;
    public string seatLayerName = "Seat";

    [Header("配置サイズ")]
    public int columns = 20;
    public int rows = 12;

    [Header("ランダム")]
    [Range(0f, 1f)] public float emptySeatRatio = 0.15f;
    public float scaleJitterPct = 0.05f;
    public float lateralJitter = 0.06f;
    public float depthJitter = 0.00f;
    public int randomSeed = 12345;

    [Header("向き")]
    public Transform lookTarget;
    public bool alignToSurfaceNormal = true;
    [Range(0f, 1f)] public float normalAlignStrength = 0.5f;

    [Header("レイ/範囲/オフセット")]
    public float raycastStartHeight = 20f;
    public float marginX = 0.8f;
    [Tooltip("前後の固定マージン(m)")]
    public float marginZ = 0.02f;
    [Tooltip("接地位置から法線方向に持ち上げる距離（足のめり込み防止）")]
    public float surfaceLift = 0.02f;

    [Header("行オフセット（Easy Only）")]
    [Range(0f, 1f)] public float easyCenter01 = 0.55f;   // 段Box内の基準（0=前/1=後）
    public float easySlopeMeters = -0.02f;               // 段が1つ後ろに行くごとに奥へ動く量（m）
    public float easyGlobalOffsetMeters = 0.00f;         // 全段一括の前後オフセット（m）
    [Range(0f, 0.4f)] public float autoMarginZPercent = 0.10f; // 段奥行きに対する自動Zマージン割合

    [Header("詰まり防止")]
    [Tooltip("横方向の最小間隔（m）。これより狭くなる配置はスキップ")]
    public float minSpacingX = 0.00f;

    [Header("プレハブ/アニメ")]
    public GameObject[] prefabs;
    public Transform container;
    public RuntimeAnimatorController animatorController;
    public int layerForCrowd = 0;

    const string ContainerName = "__CrowdContainer";

    Transform EnsureContainer()
    {
        if (container != null) return container;
        var t = transform.Find(ContainerName);
        if (t == null)
        {
            var go = new GameObject(ContainerName);
            go.transform.SetParent(transform, false);
            t = go.transform;
        }
        container = t;
        return container;
    }

    [ContextMenu("Clear Crowd")]
    public void ClearCrowd()
    {
        var t = EnsureContainer();
        var tmp = new List<GameObject>();
        foreach (Transform c in t) tmp.Add(c.gameObject);
#if UNITY_EDITOR
        if (!Application.isPlaying) foreach (var go in tmp) UnityEditor.Undo.DestroyObjectImmediate(go);
        else foreach (var go in tmp) Destroy(go);
#else
        foreach (var go in tmp) DestroyImmediate(go);
#endif
    }

    struct Basis { public Vector3 right, up, forward; }
    Basis GetBasis()
    {
        var gs = grandstand ? grandstand : transform;
        var f = Vector3.ProjectOnPlane(gs.forward, Vector3.up).normalized;
        var r = Vector3.ProjectOnPlane(gs.right, Vector3.up).normalized;
        if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
        if (r.sqrMagnitude < 1e-6f) r = Vector3.right;
        return new Basis { forward = f, right = r, up = Vector3.up };
    }
    float ProjectAlong(Vector3 p, Vector3 axis) => Vector3.Dot(p, axis);

    [ContextMenu("Place Crowd")]
    public void PlaceCrowd()
    {
        if (grandstand == null || prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[CrowdPlacer] grandstand/prefabs 未設定");
            return;
        }

        int seatLayer = LayerMask.NameToLayer(seatLayerName);
        if (seatLayer == -1) { Debug.LogWarning($"Layer \"{seatLayerName}\" がありません"); return; }
        int seatMask = 1 << seatLayer;

        var allCols = grandstand.GetComponentsInChildren<Collider>(true)
            .Where(c => c.gameObject.layer == seatLayer)
            .ToList();
        if (allCols.Count == 0)
        {
            Debug.LogWarning("[CrowdPlacer] Seat レイヤーの Collider が見つかりません");
            return;
        }

        var basis = GetBasis();
        allCols.Sort((a, b) =>
            ProjectAlong(a.bounds.center, basis.forward)
                .CompareTo(ProjectAlong(b.bounds.center, basis.forward)));

        // 段のグルーピング
        var rowGroups = new List<List<Collider>>();
        float avgDepth = Mathf.Max(0.4f, allCols.Average(c => c.bounds.size.z));
        foreach (var c in allCols)
        {
            float cz = ProjectAlong(c.bounds.center, basis.forward);
            bool put = false;
            foreach (var g in rowGroups)
            {
                float gz = ProjectAlong(g[0].bounds.center, basis.forward);
                if (Mathf.Abs(gz - cz) <= avgDepth * 0.6f) { g.Add(c); put = true; break; }
            }
            if (!put) rowGroups.Add(new List<Collider> { c });
        }

        rowGroups.Sort((g1, g2) =>
            ProjectAlong(g1[0].bounds.center, basis.forward)
                .CompareTo(ProjectAlong(g2[0].bounds.center, basis.forward)));

        if (rowGroups.Count > rows) rowGroups = rowGroups.Take(rows).ToList();
        while (rowGroups.Count < rows) rowGroups.Add(new List<Collider>(rowGroups.Last()));

        float startY = grandstand.GetComponentInChildren<Renderer>().bounds.max.y + raycastStartHeight;

        ClearCrowd();
        var tContainer = EnsureContainer();
        var rnd = new System.Random(randomSeed);

        Vector3 flatFwd = Vector3.ProjectOnPlane(grandstand.forward, Vector3.up).normalized;
        if (flatFwd == Vector3.zero) flatFwd = basis.forward;

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(tContainer.gameObject, "Place Crowd");
#endif

        for (int r = 0; r < rows; r++)
        {
            var colsThisRow = rowGroups[r];
            Bounds rowAll = EncapsulateAll(colsThisRow.Select(c => c.bounds));

            float midU = ProjectAlong(rowAll.center, basis.right);
            var leftBoxes = colsThisRow.Where(c => ProjectAlong(c.bounds.center, basis.right) <= midU).ToList();
            var rightBoxes = colsThisRow.Where(c => ProjectAlong(c.bounds.center, basis.right) > midU).ToList();
            if (leftBoxes.Count == 0) leftBoxes = colsThisRow;
            if (rightBoxes.Count == 0) rightBoxes = colsThisRow;

            int leftCount = columns / 2;
            int rightCount = columns - leftCount;

            // —— Easyのみ —— //
            float center01 = Mathf.Clamp01(easyCenter01);
            float biasMeters = easyGlobalOffsetMeters + easySlopeMeters * r;

            void PlaceOnSide(List<Collider> boxes, int count)
            {
                if (boxes.Count == 0 || count <= 0) return;

                float totalWidth = boxes.Sum(b =>
                {
                    var bb = b.bounds;
                    float uMin0 = ProjectAlong(bb.min, basis.right);
                    float uMax0 = ProjectAlong(bb.max, basis.right);
                    return Mathf.Max(0.0001f, (uMax0 - uMin0) - marginX * 2f);
                });

                int placed = 0;
                float lastUPlaced = float.NegativeInfinity;

                foreach (var box in boxes)
                {
                    var b = box.bounds;
                    float uMin = ProjectAlong(b.min, basis.right) + marginX;
                    float uMax = ProjectAlong(b.max, basis.right) - marginX;
                    float vMin = ProjectAlong(b.min, basis.forward) + marginZ;
                    float vMax = ProjectAlong(b.max, basis.forward) - marginZ;

                    // 踏面が浅い段向けに自動Zマージン
                    if (autoMarginZPercent > 0f)
                    {
                        float vDepthRaw = Mathf.Max(0.0001f, ProjectAlong(b.max, basis.forward) - ProjectAlong(b.min, basis.forward));
                        float extra = vDepthRaw * autoMarginZPercent * 0.5f;
                        vMin += extra;
                        vMax -= extra;
                    }

                    float uWidth = Mathf.Max(0.0001f, uMax - uMin);
                    int targetForThisBox = Mathf.RoundToInt(count * (uWidth / totalWidth));
                    if (placed + targetForThisBox > count) targetForThisBox = count - placed;
                    if (targetForThisBox <= 0 && placed < count) targetForThisBox = 1;

                    float vCenter = Mathf.Lerp(vMin, vMax, center01) + biasMeters;
                    vCenter = Mathf.Clamp(vCenter, vMin, vMax);

                    for (int i = 0; i < targetForThisBox; i++)
                    {
                        if (rnd.NextDouble() < emptySeatRatio) continue;

                        float tU = (i + 0.5f) / targetForThisBox;
                        float u = Mathf.Lerp(uMin, uMax, tU)
                                  + Mathf.Lerp(-lateralJitter, lateralJitter, (float)rnd.NextDouble());

                        if (minSpacingX > 0f && (u - lastUPlaced) < minSpacingX)
                            continue;

                        float v = vCenter + Mathf.Lerp(-depthJitter, depthJitter, (float)rnd.NextDouble());

                        Vector3 origin = basis.right * u + basis.forward * v;
                        origin.y = startY;

                        if (!Physics.Raycast(origin, Vector3.down, out var hit, 5000f, seatMask))
                            continue;

                        Vector3 pos = hit.point + hit.normal * surfaceLift;
                        var prefab = prefabs[rnd.Next(prefabs.Length)];
                        var go = (GameObject)Instantiate(prefab, pos, Quaternion.identity, tContainer);

                        Quaternion rot = Quaternion.LookRotation(flatFwd, Vector3.up);
                        if (alignToSurfaceNormal)
                        {
                            var nRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            rot = Quaternion.Slerp(rot, nRot * rot, normalAlignStrength);
                        }
                        go.transform.rotation = rot;

                        float s = 1f + ((float)rnd.NextDouble() * 2f - 1f) * scaleJitterPct;
                        go.transform.localScale *= s;

                        SetLayerRecursively(go, layerForCrowd);
                        var anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                        if (animatorController != null) anim.runtimeAnimatorController = animatorController;
                        anim.applyRootMotion = false;

                        lastUPlaced = u;
                        placed++;
                        if (placed >= count) break;
                    }
                    if (placed >= count) break;
                }
            }

            PlaceOnSide(leftBoxes, leftCount);
            PlaceOnSide(rightBoxes, rightCount);
        }
    }

    static Bounds EncapsulateAll(IEnumerable<Bounds> list)
    {
        var e = list.GetEnumerator();
        if (!e.MoveNext()) return new Bounds(Vector3.zero, Vector3.one);
        var b = e.Current;
        while (e.MoveNext()) b.Encapsulate(e.Current);
        return b;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0 || layer > 31) return;
        obj.layer = layer;
        foreach (Transform ch in obj.transform) SetLayerRecursively(ch.gameObject, layer);
    }
}
