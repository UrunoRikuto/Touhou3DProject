using UnityEngine;

/// <summary>
/// スペルカード(CS_SpellCardDefinition)を構成する1エントリ。
/// 「どのバリエーション(プレハブ)を」「いつ(startTime〜endTime)」
/// 「どんな角度(angle)で」再生するかを表す、純粋なデータ。
///
/// variationPrefabはCS_BarrageDesignerWindow等で個別に作成・プレハブ化された、
/// シーン配置に依存しない独立したCS_BarragePatternプレハブを参照する。
/// </summary>
[System.Serializable]
public class CS_SpellCardEntry
{
    /// <summary>CS_BarragePatternが付いたバリエーションプレハブ。</summary>
    public GameObject variationPrefab;

    /// <summary>再生を開始する時間(秒、スペルカード開始からの経過時間)。</summary>
    public float startTime;

    /// <summary>再生を終了する時間(秒)。startTime以上であること。</summary>
    public float endTime;

    /// <summary>Instantiate時に適用する回転(Euler角)。同じバリエーションでも向きを変えて使い回せる。</summary>
    public Vector3 angle;

    /// <summary>任意。スペルカードの原点(CS_SpellCardPlayerの位置)からのローカル位置オフセット。</summary>
    public Vector3 positionOffset;
}
