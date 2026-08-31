using UnityEngine;

/// <summary>
/// Generatorをサインカーブで往復回転(スイープ)させる補助コンポーネント。
/// CS_GeneratorRotator(定速回転)と異なり、一定範囲を行き来する動きになる。
/// </summary>
public class CS_GeneratorOscillator : MonoBehaviour
{
    /// <summary>
    /// 振動の回転軸(通常はVector3.up)。
    /// </summary>
    [SerializeField]
    private Vector3 _oscillationAxis = Vector3.up;

    /// <summary>
    /// 振れ幅(度)。両方向に振るため、±_amplitudeDegrees の範囲で動く。
    /// </summary>
    [SerializeField]
    private float _amplitudeDegrees = 45f;

    /// <summary>
    /// 振動の周波数(Hz、1秒あたりの往復数)。
    /// </summary>
[SerializeField]
    private float _frequency = 0.5f;

    private Quaternion _baseRotation;
    private float _elapsedTime;

    private void OnEnable()
    {
        // 有効化時に基準回転を記録し、経過時間をリセット
      _baseRotation = transform.localRotation;
        _elapsedTime = 0f;
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;

     // Mathf.Sin を使用してサインカーブで角度を計算
        // 周波数を適用して1秒間の往復数を制御
 float angle = Mathf.Sin(_elapsedTime * _frequency * Mathf.PI * 2f) * _amplitudeDegrees;

   // 基準回転から、指定軸周りに角度を加算
        Quaternion oscillation = Quaternion.AngleAxis(angle, _oscillationAxis);
        transform.localRotation = _baseRotation * oscillation;
    }
}
