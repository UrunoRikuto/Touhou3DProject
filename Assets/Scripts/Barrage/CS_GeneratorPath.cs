using UnityEngine;

/// <summary>
/// 発生源(Generator)を、あらかじめ配置したウェイポイント(点)を結ぶ経路に沿って移動させる
/// コンポーネント。エディタ上で「点を置いて線でつなぐ」ように弾幕の軌道を直感的にデザインできる
/// (専用インスペクタ CS_GeneratorPathEditor がSceneビュー上での編集を担当する。
/// CS_BarrageDesignerWindowのパラメータパネル/プレビューからも扱える)。
///
/// CS_GeneratorRotator(定速回転)/ CS_GeneratorOscillator(往復回転)と並ぶ、
/// 3つ目のGenerator用モーションコンポーネント。CS_BarragePatternが他の2つと同様に
/// まとめて有効/無効化する。
///
/// 注意: _alignToDirection を有効にすると進行方向へ自動的に向きを揃えるため、
/// 同じGameObjectにCS_GeneratorRotator/CS_GeneratorOscillatorを同時に付けると
/// 回転の取り合いになる。両方使いたい場合は、このコンポーネントを親Generatorに、
/// 回転系コンポーネントを子(マズル)に分けて付けること。
/// </summary>
public class CS_GeneratorPath : MonoBehaviour
{
    [SerializeField] private Vector3[] _localWaypoints = new Vector3[0];
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
        if (_localWaypoints == null || _localWaypoints.Length < 2 || _duration <= 0f) return;

        _elapsedTime += Time.deltaTime;

        float t = _elapsedTime / _duration;
        if (_loop)
        {
            t %= 1f;
        }
        else
        {
            t = Mathf.Clamp01(t);
        }

        Vector3 localPos = EvaluatePath(t, out Vector3 direction);

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

    /// <summary>0〜1の経路上の位置を、各区間の長さに応じた等速で評価する(インスタンス用の薄いラッパー)。</summary>
    private Vector3 EvaluatePath(float t, out Vector3 direction)
    {
        return EvaluatePathStatic(_localWaypoints, _loop, t, out direction);
    }

    /// <summary>
    /// ウェイポイント配列上の0〜1の位置tを、各区間の長さに応じた等速で評価する。
    /// CS_BarrageDesignerWindowのEditモードプレビューからも全く同じロジックを使えるように、
    /// staticかつローカル座標配列のみで完結する形で公開している(ランタイムとプレビューの
    /// 挙動を一致させるための唯一の実装)。
    /// </summary>
    public static Vector3 EvaluatePathStatic(Vector3[] waypoints, bool loop, float t, out Vector3 direction)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            direction = Vector3.forward;
            return Vector3.zero;
        }

        int segmentCount = loop ? waypoints.Length : waypoints.Length - 1;
        if (segmentCount <= 0)
        {
            direction = Vector3.forward;
            return waypoints[0];
        }

        var segmentLengths = new float[segmentCount];
        float totalLength = 0f;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 a = waypoints[i];
            Vector3 b = waypoints[(i + 1) % waypoints.Length];
            segmentLengths[i] = Vector3.Distance(a, b);
            totalLength += segmentLengths[i];
        }

        if (totalLength <= 0.0001f)
        {
            direction = Vector3.forward;
            return waypoints[0];
        }

        float targetDistance = t * totalLength;
        float accumulated = 0f;

        for (int i = 0; i < segmentCount; i++)
        {
            if (accumulated + segmentLengths[i] >= targetDistance || i == segmentCount - 1)
            {
                Vector3 a = waypoints[i];
                Vector3 b = waypoints[(i + 1) % waypoints.Length];
                float segT = segmentLengths[i] > 0.0001f
                    ? Mathf.Clamp01((targetDistance - accumulated) / segmentLengths[i])
                    : 0f;
                direction = (b - a).normalized;
                return Vector3.Lerp(a, b, segT);
            }
            accumulated += segmentLengths[i];
        }

        direction = Vector3.forward;
        return waypoints[segmentCount];
    }
}
