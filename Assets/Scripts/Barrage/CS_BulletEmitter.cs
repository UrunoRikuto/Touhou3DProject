using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 実際に弾を発射する末端コンポーネント。
/// Generatorまたはその子(マズル)に付けられる。
/// Transformの位置・向きから弾を発射し、バースト(複数同時発射)に対応。
/// </summary>
public class CS_BulletEmitter : MonoBehaviour
{
    [SerializeField]
    private CS_BarragePattern _barragePattern;

    private List<string> _hitTagList = new List<string>();

    [SerializeField]
    private CSO_BulletData _bulletData;

    [SerializeField]
    private CS_BulletPool _bulletPool;

    [SerializeField]
    private float _fireInterval = 0.2f;
    public float fireInterval
    {
        get => _fireInterval;
        set => _fireInterval = value;
    }

    [SerializeField]
    private bool _autoFire;

    [SerializeField]
    private int _burstCount = 1;

    [SerializeField]
    private float _burstAngleSpread;

    private float _fireTimer;

    private void OnEnable()
    {
        _fireTimer = 0f;
        _hitTagList = _barragePattern != null ? _barragePattern.hitTagList : new List<string>();
        _bulletPool = GameObject.FindAnyObjectByType<CS_BulletPool>();
    }

    private void Update()
    {
        if (!_autoFire || _bulletData == null)
        {
            return;
        }

        if (_bulletPool == null) _bulletPool = GameObject.FindAnyObjectByType<CS_BulletPool>();

        _fireTimer += Time.deltaTime;

        if (_fireTimer >= _fireInterval)
        {
            Fire();
            _fireTimer = 0f;
        }
    }

    /// <summary>
    /// 弾を発射します。
    /// _burstCountが1なら1発、2以上なら指定数を角度均等配置で発射。
    /// </summary>
    public void Fire()
    {
        if (_bulletPool == null || _bulletData == null)
        {
            Debug.LogWarning("CS_BulletEmitter: BulletPool or BulletData is not set.", gameObject);
            return;
        }

        if (_burstCount <= 1)
        {
            // 単発発射
            FireSingleBullet(transform.forward);
        }
        else
        {
            // バースト発射
            FireBurst();
        }
    }

    /// <summary>
    /// 指定方向へ弾を1発発射します。
    /// </summary>
    private void FireSingleBullet(Vector3 direction)
    {
        _bulletPool.Fire(_bulletData, transform.position, direction.normalized, _hitTagList);
    }

    /// <summary>
    /// バースト(複数同時発射)を実行します。
    /// transform.forwardを中心に、_burstAngleSpreadの範囲へ均等配置。
    /// </summary>
    private void FireBurst()
    {
        float angleStep = _burstAngleSpread / (_burstCount - 1);
        float startAngle = -_burstAngleSpread / 2f;

        for (int i = 0; i < _burstCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // transform.upを回転軸として、指定角度だけ回転させた方向を計算
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, transform.up);
            Vector3 bulletDirection = rotation * transform.forward;

            FireSingleBullet(bulletDirection);
        }
    }
}
