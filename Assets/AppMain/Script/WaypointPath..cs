using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WaypointPath : MonoBehaviour
{
    public bool loop = true;
    public List<Transform> points = new List<Transform>();

    // 子オブジェクトを自動収集（インスペクター変更や保存時に実行）
    void OnValidate()
    {
        points.Clear();
        foreach (Transform c in transform) points.Add(c);
    }

    public Transform GetPoint(int index)
    {
        if (points.Count == 0) return null;
        if (!loop) return (index >= 0 && index < points.Count) ? points[index] : null;
        index = (index % points.Count + points.Count) % points.Count;
        return points[index];
    }

    // エディタ表示
    void OnDrawGizmos()
    {
        for (int i = 0; i < points.Count; i++)
        {
            var a = points[i];
            if (!a) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(a.position, 0.5f);

            var b = GetPoint(i + 1);
            if (b)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(a.position, b.position);
            }
        }
    }

    // 手動で再収集したいとき用のメニュー（任意）
    [ContextMenu("Collect Children As Waypoints")]
    void Collect()
    {
        OnValidate();
    }
}
