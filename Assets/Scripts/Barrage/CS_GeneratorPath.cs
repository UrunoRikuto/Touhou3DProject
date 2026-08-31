using UnityEngine;

/// <summary>
/// 発生源(Generator)を、AnimationCurveで指定したローカルX/Y/Z座標の時間変化に沿って
/// 移動させるコンポーネント。Unity標準のInspector上のカーブエディタ(AnimationCurveの
/// ミニプレビュー→クリックで開くカーブエディタウィンドウ)でそのまま編集できるため、
/// 専用のSceneビュー編集ツール(点をクリックして配置する方式)を別途用意する必要がない。
///
/// CS_GeneratorRotator(定速回転)/ CS_GeneratorOscillator(往復回転)と並ぶ、
/// 3つ目のGenerator用モーションコンポーネント。CS_BarragePatternが他の2つと同様に
/// まとめて有効/無効化する。CS_BarrageDesignerWindowのパラメータパネルは
/// SerializedPropertyを汎用的に列挙して描画する仕組みのため、AnimationCurveフィールドも
/// 追加のコード無しでそのまま編集できる。
///
/// 設計変更履歴(2026-08-31): 当初はSceneビュー上で点をクリックして配置する方式
/// (ウェイポイント配列+専用エディタCS_GeneratorPathEditor)だったが、専用エディタが
/// プロジェクト側で認識されない事象が出たため、Unity標準機能(AnimationCurve)だけで
/// 完結する方式に変更した。専用エディタ(CS_GeneratorPathEditor.cs)はもう使わないため
/// プロジェクトから削除してよい。
///
/// 使い方:
///   1. Generator(空のGameObject)にこのコンポーネントを付けて選択する
///   2. Inspectorに表示される _curveX / _curveY / _curveZ の3本のカーブをクリックして開き、
///      各軸の「時間(横軸 0〜1) → ローカル座標の変化量(縦軸)」をカーブとして描く
///      (例: 円軌道にしたいなら _curveX にcos波形、_curveZ にsin波形に近いカーブを作る。
///       Unityの右クリックメニューから「Add Key」でキーを打ち、ハンドルで滑らかに調整できる)
///   3. _duration(1周の秒数)/ _loop(ループするか)/ _alignToDirection(進行方向を向くか)を設定する
/// </summary>
public class CS_GeneratorPath : MonoBehaviour
{
    [SerializeField] private AnimationCurve _curveX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [SerializeField] private AnimationCurve _curveY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [SerializeField] private AnimationCurve _curveZ = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [SerializeField] private float _duration = 4f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _alignToDirection = true;

    private float _elapsedTime;

    /// <summary>エディタ拡張がループ設定を参照するための読み取り専用プロパティ。</summary>
    public bool loopEnabled => _loop;

    private void OnEnable()
    {
        _elapsedTime = 0f;
    }

    private void Update()
    {
        if (_duration <= 0f) return;

        _elapsedTime += Time.deltaTime;

        float t = _elapsedTime / _duration;
        t = _loop ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);

        Vector3 localPos = EvaluateCurvePathStatic(_curveX, _curveY, _curveZ, t, out Vector3 direction);

        if (transform.parent != null)
        {
            transform.localPosition = localPos;
        }
        else
        {
            transform.position = localPos;
        }

        if (_alignToDirection && direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// X/Y/Zの3本のAnimationCurveを、0〜1の正規化時間tで評価してローカル座標を返す。
    /// 進行方向は t の前後をわずかにずらして評価した位置の差分から近似する。
    /// CS_BarrageDesignerWindowのEditモードプレビューからも全く同じロジックを使えるように、
    /// staticかつAnimationCurveのみで完結する形で公開している(ランタイムとプレビューの
    /// 挙動を一致させるための唯一の実装)。
    /// </summary>
    public static Vector3 EvaluateCurvePathStatic(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, float t, out Vector3 direction)
    {
        Vector3 pos = EvaluateAt(curveX, curveY, curveZ, t);

        const float epsilon = 0.001f;
        Vector3 posAhead = EvaluateAt(curveX, curveY, curveZ, Mathf.Min(t + epsilon, 1f));
        Vector3 posBehind = EvaluateAt(curveX, curveY, curveZ, Mathf.Max(t - epsilon, 0f));
        Vector3 delta = posAhead - posBehind;

        direction = delta.sqrMagnitude > 0.0000001f ? delta.normalized : Vector3.forward;
        return pos;
    }

    private static Vector3 EvaluateAt(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, float t)
    {
        float x = curveX != null ? curveX.Evaluate(t) : 0f;
        float y = curveY != null ? curveY.Evaluate(t) : 0f;
        float z = curveZ != null ? curveZ.Evaluate(t) : 0f;
        return new Vector3(x, y, z);
    }
}