using UnityEngine;
using Unity.Cinemachine; // ← Cinemachine 3.x 用

public class CameraToggle : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [Tooltip("通常の追従視点（後方）カメラ")]
    public CinemachineCamera chaseCam;   // 後方カメラ
    [Tooltip("ボンネット視点カメラ")]
    public CinemachineCamera hoodCam;    // ボンネットカメラ

    [Header("切り替え設定")]
    [Tooltip("切り替えキー")]
    public KeyCode toggleKey = KeyCode.Space; // ← スペースキーで切り替え
    [Tooltip("有効カメラに与える優先度（他方は0になります）")]
    public int activePriority = 20;
    [Tooltip("起動時にボンネット視点から始めるかどうか")]
    public bool startInHoodView = false;

    private bool hoodActive;

    private void Start()
    {
        hoodActive = startInHoodView;
        Apply();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            hoodActive = !hoodActive;
            Apply();
        }
    }

    /// <summary>
    /// 外部から直接トグルする（UIボタンなど）
    /// </summary>
    public void Toggle()
    {
        hoodActive = !hoodActive;
        Apply();
    }

    /// <summary>
    /// 明示的に視点を設定する（true=ボンネット視点 / false=後方視点）
    /// </summary>
    public void SetHoodView(bool on)
    {
        hoodActive = on;
        Apply();
    }

    private void Apply()
    {
        if (hoodCam != null) hoodCam.Priority = hoodActive ? activePriority : 0;
        if (chaseCam != null) chaseCam.Priority = hoodActive ? 0 : activePriority;
    }
}
