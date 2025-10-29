using UnityEngine;
using UnityEngine.SceneManagement;

public class CourseManager : MonoBehaviour
{
    [Header("コースごとのバリア親 (0=外周,1=中周,2=内周)")]
    public GameObject[] barrierGroups;

    [Header("デフォルトコース (0=外周)")]
    public int defaultCourseIndex = 0;

    private int selectedCourseIndex;

    private void Awake()
    {
        selectedCourseIndex = PlayerPrefs.GetInt("SelectedCourse", defaultCourseIndex);
        ApplyCourse(selectedCourseIndex);
    }

    public void ApplyCourse(int index)
    {
        if (barrierGroups == null || barrierGroups.Length == 0) return;

        selectedCourseIndex = Mathf.Clamp(index, 0, barrierGroups.Length - 1);

        for (int i = 0; i < barrierGroups.Length; i++)
        {
            if (barrierGroups[i] == null) continue;
            // ✅ 修正ポイント：選択コースだけONにする
            barrierGroups[i].SetActive(i == selectedCourseIndex);
        }

        Debug.Log($"[CourseManager] コース{selectedCourseIndex + 1}を適用しました");
    }

    public void SelectCourseAndReload(int index)
    {
        PlayerPrefs.SetInt("SelectedCourse", index);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
