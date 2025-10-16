using TMPro;
using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    public Rigidbody targetRigidbody;
    public TMP_Text speedText;

    [Range(0, 2)] public int decimalDigits = 0;  // 0=整数表示
    public float msToKmh = 3.6f;                 // m/s → km/h 係数
    public string suffix = " km/h";              // 表示の単位

    void Reset()
    {
        if (!speedText) speedText = GetComponent<TMP_Text>();
        if (!targetRigidbody) targetRigidbody = FindObjectOfType<Rigidbody>();
    }

    void Update()
    {
        if (!targetRigidbody || !speedText) return;

        float kmh = targetRigidbody.linearVelocity.magnitude * msToKmh;

        if (decimalDigits <= 0)
            speedText.text = Mathf.RoundToInt(kmh).ToString() + suffix;
        else
            speedText.text = kmh.ToString("F" + decimalDigits) + suffix;
    }
}
