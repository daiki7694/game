using System.Collections;
using UnityEngine;
using TMPro;

public class LapCounter : MonoBehaviour
{
    [Header("周回設定")]
    [Tooltip("クリアに必要な総周回数")]
    public int totalLaps = 2;

    [Header("参照")]
    [Tooltip("右上のTextMeshProUGUI（例：LapText）")]
    public TextMeshProUGUI lapLabel;
    [Tooltip("プレイヤー車（Rigidbodyの付いた“ルート”Transform）")]
    public Transform playerCar;
    [Tooltip("スタート/フィニッシュゲート（LapGate）")]
    public Transform startFinishGate;

    [Header("誤カウント防止")]
    [Tooltip("ゲートからこの距離以上離れてから再カウントを許可")]
    public float rearmDistance = 20f;
    [Tooltip("連続通過のバウンス抑制用クールダウン[秒]")]
    public float cooldownSec = 1.0f;

    [Header("オプション")]
    [Tooltip("ゴール時に一時停止するなら ON")]
    public bool pauseOnGoal = false;

    [Header("デバッグ")]
    public bool verboseLog = false;

    private int currentLap = 0;
    private bool armed = false;         // カウント許可フラグ
    private bool coolingDown = false;   // 連続ヒット抑制
    private bool finished = false;

    void Start()
    {
        if (lapLabel != null)
            lapLabel.text = $"{currentLap}/{totalLaps}";

        StartCoroutine(ArmWhenFar());
    }

    // LapGate から呼ばれる
    public void TryCountLap(Transform who)
    {
        if (finished) return;
        if (who == null || playerCar == null) return;

        // ✅ タグではなく参照一致でプレイヤーのみ判定
        if (who != playerCar) return;

        if (verboseLog) Debug.Log($"[LapCounter] Enter by: {who.name}, armed={armed}, cd={coolingDown}");

        if (!armed) return;
        if (coolingDown) return;

        coolingDown = true;
        StartCoroutine(Cooldown());

        currentLap++;
        if (lapLabel != null)
            lapLabel.text = $"{currentLap}/{totalLaps}";

        if (verboseLog) Debug.Log($"[LapCounter] Laps: {currentLap}/{totalLaps}");

        if (currentLap >= totalLaps)
        {
            finished = true;
            if (verboseLog) Debug.Log("[LapCounter] GOAL!");
            if (pauseOnGoal) Time.timeScale = 0f;
            // TODO: ゴール演出やリザルト表示をここで呼び出す
            return;
        }

        armed = false;
        StartCoroutine(ArmWhenFar());
    }

    private IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(cooldownSec);
        coolingDown = false;
    }

    private IEnumerator ArmWhenFar()
    {
        if (playerCar == null || startFinishGate == null) yield break;

        // ゲートから一定距離離れるまで待機
        while (Vector3.Distance(playerCar.position, startFinishGate.position) < rearmDistance)
            yield return null;

        armed = true;
        if (verboseLog) Debug.Log("[LapCounter] armed = true");
    }
}
