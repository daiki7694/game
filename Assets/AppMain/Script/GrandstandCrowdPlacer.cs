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
    public float lateralJitter = 0.10f;
    public float depthJitter = 0.00f;   // 検証時は0推奨
    public int randomSeed = 12345;   // ★ これが抜けていました

    [Header("向き")]
    public Transform lookTarget;
    public bool alignToSurfaceNormal = true;
    [Range(0f, 1f)] public float normalAlignStrength = 0.5f;

    [Header("レイ/範囲/オフセット")]
    public float raycastStartHeight = 20f;
    public float marginX = 0.5f;
    public float marginZ = 0.2f;
    public float seatSurfaceOffset = -0.12f;

    [Header("行オフセット（段Box内だけで効く）")]
    public AnimationCurve rowZOffsetCurve = AnimationCurve.Linear(0f, 0.00f, 1f, -0.40f);
    public AnimationCurve rowDepthCenterCurve = new AnimationCurve(
        new Keyframe(0f, 0.10f),
        new Keyframe(1f, 0.90f)
    );

    [Header("デバッグ/制御")]
    public bool invertRowOffsetDirection = false;
    public float forceExtremeShiftMeters = 0f;
    public bool verboseLog = true;

    [Header("プレハブ/アニメ")]
    public GameObject[] prefabs;
    public Transform container;
    public RuntimeAnimatorController animatorController;
    public int layerForCrowd = 0;

    const string ContainerName = "__CrowdContainer";
    const string VERSION = "GrandstandCrowdPlacer v4.1";

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

    static Bounds EncapsulateAll(IEnumerable<Bounds> list)
    {
        var e = list.GetEnumerator();
        if (!e.MoveNext()) return new Bounds(Vector3.zero, Vector3.one);
        var b = e.Current;
        while (e.MoveNext()) b.Encapsulate(e.Current);
        return b;
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

    [ContextMenu("Place Crowd")]
    public void PlaceCrowd()
    {
        Debug.Log($"{VERSION} : PlaceCrowd()");

        if (grandstand == null || prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[CrowdPlacer] grandstand/prefabs 未設定"); return;
        }

        int seatLayer = LayerMask.NameToLayer(seatLayerName);
        if (seatLayer == -1) { Debug.LogWarning($"Layer \"{seatLayerName}\" がありません"); return; }
        int seatMask = 1 << seatLayer;

        var allCols = grandstand.GetComponentsInChildren<Collider>(true)
                                .Where(c => c.gameObject.layer == seatLayer)
                                .ToList();
        if (allCols.Count == 0)
        {
            Debug.LogWarning("[CrowdPlacer] Seat レイヤーの Box Collider が見つかりません"); return;
        }

        allCols.Sort((a, b) => a.bounds.center.z.CompareTo(b.bounds.center.z));
        var rowGroups = new List<List<Collider>>();
        float zBand = Mathf.Max(0.4f, allCols.Average(c => c.bounds.size.z));
        foreach (var c in allCols)
        {
            bool put = false;
            foreach (var g in rowGroups)
            {
                if (Mathf.Abs(g[0].bounds.center.z - c.bounds.center.z) <= zBand * 0.6f) { g.Add(c); put = true; break; }
            }
            if (!put) rowGroups.Add(new List<Collider> { c });
        }
        rowGroups.Sort((g1, g2) => g1[0].bounds.center.z.CompareTo(g2[0].bounds.center.z));
        if (rowGroups.Count > rows) rowGroups = rowGroups.Take(rows).ToList();
        while (rowGroups.Count < rows) rowGroups.Add(new List<Collider>(rowGroups.Last()));

        var heightBounds = EncapsulateAll(
            grandstand.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds)
        );
        float startY = heightBounds.max.y + raycastStartHeight;

        ClearCrowd();
        var tContainer = EnsureContainer();
        var rnd = new System.Random(randomSeed);   // ← ここで使用

        Vector3 lookFwd = (lookTarget != null)
            ? (lookTarget.position - heightBounds.center).normalized
            : (-grandstand.forward);
        Vector3 flatFwd = new Vector3(lookFwd.x, 0, lookFwd.z).normalized;
        if (flatFwd == Vector3.zero) flatFwd = Vector3.forward;

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(tContainer.gameObject, "Place Crowd");
#endif

        for (int r = 0; r < rows; r++)
        {
            var colsThisRow = rowGroups[r];

            Bounds rowAll = EncapsulateAll(colsThisRow.Select(c => c.bounds));
            float midX = rowAll.center.x;

            var leftBoxes = colsThisRow.Where(c => c.bounds.center.x <= midX).OrderBy(c => c.bounds.min.x).ToList();
            var rightBoxes = colsThisRow.Where(c => c.bounds.center.x > midX).OrderBy(c => c.bounds.min.x).ToList();
            if (leftBoxes.Count == 0) leftBoxes = colsThisRow;
            if (rightBoxes.Count == 0) rightBoxes = colsThisRow;

            int leftCount = columns / 2;
            int rightCount = columns - leftCount;

            float rowT = (rows <= 1) ? 0f : (float)r / (rows - 1);
            float biasMeters = rowZOffsetCurve.Evaluate(rowT);
            if (invertRowOffsetDirection) biasMeters = -biasMeters;
            biasMeters += forceExtremeShiftMeters;
            float center01 = Mathf.Clamp01(rowDepthCenterCurve.Evaluate(rowT));

            if (verboseLog && (r == 0 || r == rows - 1))
            {
                Debug.Log($"[CrowdPlacer] row {r}/{rows - 1} : biasMeters={biasMeters:F3}, center01={center01:F2}");
            }

            void PlaceOnSide(List<Collider> boxes, int count)
            {
                if (boxes.Count == 0 || count <= 0) return;

                float totalWidth = boxes.Sum(b => Mathf.Max(0.0001f, (b.bounds.size.x - marginX * 2f)));

                int placed = 0;
                foreach (var box in boxes)
                {
                    var b = box.bounds;
                    float bxMinX = b.min.x + marginX;
                    float bxMaxX = b.max.x - marginX;
                    float bxMinZ = b.min.z + marginZ;
                    float bxMaxZ = b.max.z - marginZ;

                    float bxWidth = Mathf.Max(0.0001f, bxMaxX - bxMinX);
                    float bxDepth = Mathf.Max(0.0001f, bxMaxZ - bxMinZ);

                    int targetForThisBox = Mathf.RoundToInt(count * (bxWidth / totalWidth));
                    if (placed + targetForThisBox > count) targetForThisBox = count - placed;
                    if (targetForThisBox <= 0 && placed < count) targetForThisBox = 1;

                    float bias01 = Mathf.Clamp01(center01 + (biasMeters / bxDepth));

                    for (int i = 0; i < targetForThisBox; i++)
                    {
                        if (rnd.NextDouble() < emptySeatRatio) continue;

                        float tX = (i + 0.5f) / targetForThisBox;
                        float x = Mathf.Lerp(bxMinX, bxMaxX, tX)
                                  + Mathf.Lerp(-lateralJitter, lateralJitter, (float)rnd.NextDouble());
                        float z = Mathf.Lerp(bxMinZ, bxMaxZ, bias01)
                                  + Mathf.Lerp(-depthJitter, depthJitter, (float)rnd.NextDouble());

                        Vector3 origin = new Vector3(x, startY, z);
                        if (!Physics.Raycast(origin, Vector3.down, out var hit, 5000f, seatMask)) continue;

                        Vector3 pos = hit.point + new Vector3(0f, seatSurfaceOffset, 0f);
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

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0 || layer > 31) return;
        obj.layer = layer;
        foreach (Transform ch in obj.transform) SetLayerRecursively(ch.gameObject, layer);
    }
}
