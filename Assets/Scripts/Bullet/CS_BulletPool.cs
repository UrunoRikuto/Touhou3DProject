using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弾のオブジェクトプールを管理するコンポーネント。
/// メモリ効率を高め、頻繁な生成/破棄のオーバーヘッドを減らします。
/// </summary>
public class CS_BulletPool : MonoBehaviour
{
    [SerializeField]
    private int _poolSize = 100;

    private Queue<CS_Bullet> _bulletPool = new Queue<CS_Bullet>();
    private Dictionary<CSO_BulletData, Queue<CS_Bullet>> _poolsByData = new Dictionary<CSO_BulletData, Queue<CS_Bullet>>();

    private void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// プールを初期化します。
    /// </summary>
    private void InitializePool()
    {
        _bulletPool.Clear();
        _poolsByData.Clear();
    }

    /// <summary>
    /// 指定位置・方向に弾を1発発射します。
    /// </summary>
    public void Fire(CSO_BulletData data, Vector3 position, Vector3 direction, List<string> hitTags)
    {
        CS_Bullet bullet = GetOrCreateBullet(data);
        bullet.Initialize(data, position, direction, hitTags);
        bullet.gameObject.SetActive(true);
    }

    /// <summary>
    /// プールから弾を取得するか、新しく生成します。
    /// </summary>
    private CS_Bullet GetOrCreateBullet(CSO_BulletData data)
    {
        // データ別のプールが存在しない場合は作成
        if (!_poolsByData.ContainsKey(data))
        {
            _poolsByData[data] = new Queue<CS_Bullet>();
        }

        var pool = _poolsByData[data];

        CS_Bullet bullet;
        if (pool.Count > 0)
        {
            bullet = pool.Dequeue();
        }
        else
        {
            // 新しい弾をインスタンス化
            if (data.prefab != null)
            {
                GameObject bulletObj = Instantiate(data.prefab, transform);
                bullet = bulletObj.GetComponent<CS_Bullet>();
                if (bullet == null)
                {
                    bullet = bulletObj.AddComponent<CS_Bullet>();
                }
            }
            else
            {
                // プレハブがない場合は簡易的に作成
                GameObject bulletObj = new GameObject("Bullet");
                bulletObj.transform.SetParent(transform);
                bullet = bulletObj.AddComponent<CS_Bullet>();

                SphereCollider collider = bulletObj.AddComponent<SphereCollider>();
                collider.radius = data.hitRadius;
                collider.isTrigger = true;

                Rigidbody rb = bulletObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            bullet.SetReturnCallback(() => ReturnBullet(bullet, data));
        }

        return bullet;
    }

    /// <summary>
    /// 使用済みの弾をプールに戻します。
    /// </summary>
    private void ReturnBullet(CS_Bullet bullet, CSO_BulletData data)
    {
        bullet.gameObject.SetActive(false);
        _poolsByData[data].Enqueue(bullet);
    }
}
