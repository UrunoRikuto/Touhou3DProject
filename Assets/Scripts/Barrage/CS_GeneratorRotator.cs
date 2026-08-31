using UnityEngine;

/// <summary>
/// 発生源(Generator)を定速回転させるための軽量コンポーネント。
/// 螺旋・回転扇形などのパターン生成に使用。
/// Transformを毎フレーム回転させ、弾の発射位置・方向を動的に変化させる。
/// </summary>
public class CS_GeneratorRotator : MonoBehaviour
{
    /// <summary>
    /// 回転速度(度/秒、XYZ各軸、Space.Self基準)。
    /// </summary>
    [SerializeField]
    private Vector3 _angularVelocity;

    private void Update()
    {
        transform.Rotate(_angularVelocity * Time.deltaTime, Space.Self);
    }
}
