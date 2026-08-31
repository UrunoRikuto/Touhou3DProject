using UnityEngine;

/// <summary>
/// CS_BarragePatternが管理する、特定のサブGenerator(GameObject)を指定した時間帯だけ
/// アクティブにするための合図(キュー)。
///
/// Unity Timeline(PlayableDirector / ActivationTrack)を使わずに、単純な時間比較だけで
/// 「どのサブGeneratorをいつ有効化するか」を組めるようにするための軽量な代替手段。
/// CS_BarrageDesignerWindow(弾幕デザイナーウィンドウ)のシーケンサーUIから、
/// ガントチャート風のバーをドラッグして視覚的に編集する想定。
/// </summary>
[System.Serializable]
public class CS_BarrageCue
{
    /// <summary>この時間帯にアクティブ/非アクティブを切り替える対象のGameObject。</summary>
    public GameObject targetGenerator;

    /// <summary>アクティブにする開始時間(秒、パターン開始からの経過時間)。</summary>
    public float startTime;

    /// <summary>アクティブを終了する時間(秒)。startTime以上であること。</summary>
    public float endTime;
}
