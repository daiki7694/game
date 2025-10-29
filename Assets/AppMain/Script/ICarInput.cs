using UnityEngine;

// 車の入力をまとめる共通インターフェイス
public interface ICarInput
{
    float Steering { get; }   // ハンドル (-1～1)
    float Throttle { get; }   // アクセル (-1～1)
    bool Brake { get; }      // ブレーキ（押したらtrue）
}
