using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LapGate : MonoBehaviour
{
    [Header("オプション")]
    [Tooltip("進行方向チェック。ゲートの+Z向きへ進んだ時だけカウント")]
    public bool checkDirection = false;

    [Tooltip("速度がこの値未満なら方向チェックをスキップ（停止/低速対策）")]
    public float minSpeedForDirCheck = 1.0f;

    [Tooltip("Gizmoでゲートを可視化")]
    public bool drawGizmo = true;

    private LapCounter counter;
    private Collider col;

    void Awake()
    {
        counter = FindObjectOfType<LapCounter>();
        col = GetComponent<Collider>();
        col.isTrigger = true; // 念のため
    }

    void OnTriggerEnter(Collider other)
    {
        if (counter == null) return;

        // ✅ 子コライダーでも親のRigidbody(=車ルート)を拾う
        Transform root = other.attachedRigidbody
            ? other.attachedRigidbody.transform
            : other.transform.root;

        // 方向チェック（必要なら）
        if (checkDirection && other.attachedRigidbody != null)
        {
            Vector3 gateForward = transform.forward;         // ゲートの+Zが「正しい通過方向」
            Vector3 vel = other.attachedRigidbody.linearVelocity;

            if (vel.magnitude >= minSpeedForDirCheck)
            {
                float sign = Vector3.Dot(gateForward, vel.normalized);
                if (sign <= 0f)
                {
                    // 逆走とみなして無視
                    // Debug.Log("[LapGate] 逆方向通過のため無視");
                    return;
                }
            }
        }

        counter.TryCountLap(root);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        Gizmos.color = new Color(0f, 1f, 0.8f, 0.35f);
        var c = GetComponent<Collider>();
        if (c is BoxCollider b)
        {
            Matrix4x4 m = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.matrix = m;
        }

        // 進行方向の矢印
        Gizmos.color = Color.cyan;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, p + transform.forward * 3f);
    }
}
