using UnityEngine;

[RequireComponent(typeof(PrometeoCarController))]
public class AIDriver : MonoBehaviour
{
    [Header("経路")]
    public WaypointPath path;          // 親に付けた WaypointPath をドラッグ
    public int currentIndex = 0;

    [Header("挙動パラメータ")]
    public float lookAheadDistance = 12f;   // 次のWPへ進む距離
    public float maxSpeed = 60f;            // 目標最高速(km/h)
    public float cornerSlowdown = 0.6f;     // コーナー減速係数(0~1)
    public float brakeDistance = 8f;        // 目標点手前でのブレーキ距離

    PrometeoCarController car;

    void Awake()
    {
        car = GetComponent<PrometeoCarController>();
    }

    void FixedUpdate()
    {
        if (path == null || car == null) return;

        var target = path.GetPoint(currentIndex);
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        // 進行方向に対する目標の左右度合い
        Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);

        // ステア（とても単純な左右判定）
        if (localDir.x > 0.05f) car.TurnRight();
        else if (localDir.x < -0.05f) car.TurnLeft();
        else car.ResetSteeringAngle();

        // 目標速度（コーナーでは少し落とす）
        float desired = maxSpeed * Mathf.Lerp(1f, cornerSlowdown, Mathf.Abs(localDir.x));

        // 加減速（Prometeo の公開メソッドを呼ぶ）
        if (car.carSpeed < desired - 1f) car.GoForward();
        else if (dist < brakeDistance) car.Brakes();
        else car.ThrottleOff();

        // 近づいたら次のウェイポイントへ
        if (dist < lookAheadDistance) currentIndex++;
    }
}
