using UnityEngine;

/// <summary>
/// スキル弾幕を管理するコンポーネント。
/// 基本弾攻撃の発射抑制に使用されます。
/// 本実装(コスト消費、弾幕再生ロジック等)は別途実装予定のため、
/// 現在はisBarrageActiveプロパティのみを提供するスタブです。
/// </summary>
public class CS_BarrageSkillController : MonoBehaviour
{
    /// <summary>
    /// スキル弾幕が現在発動中かどうかを示します。
    /// </summary>
    public bool isBarrageActive { get; private set; }

    private void Start()
    {
        // 初期状態は非発動
        isBarrageActive = false;
    }

    // 将来の実装用: スキル発動メソッドやコスト管理ロジック等を追加予定
}
