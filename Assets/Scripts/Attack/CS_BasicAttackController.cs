using UnityEngine;

/// <summary>
/// 基本弾攻撃を常時自動発射するコンポーネント。
/// スキル弾幕が発動中は発射を停止します。
/// </summary>
public class CS_BasicAttackController : MonoBehaviour
{
    [SerializeField]
    private CS_AimController _aimController;

    [SerializeField]
    private CS_BarrageSkillController _skillController;

    [SerializeField]
    private CS_BulletData _bulletData;

    [SerializeField]
    private CS_BulletPool _bulletPool;

    [SerializeField]
    private Transform _fireOrigin;

    [SerializeField]
    private float _fireInterval = 0.2f;

    private float _fireTimer;

    private void OnEnable()
    {
        if (_fireOrigin == null)
        {
            _fireOrigin = transform;
        }

        _fireTimer = 0f;
    }

    private void Update()
    {
        UpdateAttack();
    }

    /// <summary>
    /// 毎フレーム攻撃を更新します。
    /// </summary>
    private void UpdateAttack()
    {
        // スキル弾幕が発動中の場合は攻撃を停止
        if (_skillController.isBarrageActive)
        {
            _fireTimer = 0f;
            return;
        }

        // タイマーを進める
        _fireTimer += Time.deltaTime;

        // 発射間隔に到達したら弾を発射
        if (_fireTimer >= _fireInterval)
        {
            FireBullet();
            _fireTimer = 0f;
        }
    }

    /// <summary>
    /// 弾を1発発射します。
    /// </summary>
    private void FireBullet()
    {
        if (_bulletPool == null || _bulletData == null || _aimController == null)
        {
            Debug.LogWarning("CS_BasicAttackController: 必要なコンポーネントが設定されていません");
            return;
        }

        _bulletPool.Fire(_bulletData, _fireOrigin.position, _aimController.aimDirection);
    }
}
