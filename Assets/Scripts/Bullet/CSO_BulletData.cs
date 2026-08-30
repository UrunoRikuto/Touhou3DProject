using UnityEngine;

/// <summary>
/// 弾の基本パラメータを定義するScriptableObject。
/// </summary>
[CreateAssetMenu(menuName = "Bullet/Basic Attack Data", fileName = "BulletData_Basic")]
public class CSO_BulletData : ScriptableObject
{
    /// <summary>
    /// 弾の移動速度。
    /// </summary>
    public float speed = 20f;

    /// <summary>
    /// 弾の生存時間(秒)。
    /// </summary>
    public float lifetime = 5f;

    /// <summary>
    /// 判定用の球体半径。
/// </summary>
    public float hitRadius = 0.5f;

    /// <summary>
    /// 弾の与えるダメージ。
    /// </summary>
    public float damage = 10f;

    /// <summary>
    /// 弾のプレハブ。
    /// </summary>
    public GameObject prefab;
}
