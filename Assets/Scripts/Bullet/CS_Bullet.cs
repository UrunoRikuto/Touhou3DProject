using UnityEngine;
using System;

/// <summary>
/// 弾の挙動を制御するコンポーネント。
/// 移動、衝突判定、ライフタイムを管理します。
/// </summary>
public class CS_Bullet : MonoBehaviour
{
    private CS_BulletData _data;
    private Vector3 _direction;
    private float _elapsedTime;
    private Action _returnCallback;

    /// <summary>
    /// 弾を初期化します。
    /// </summary>
    public void Initialize(CS_BulletData data, Vector3 position, Vector3 direction)
    {
        _data = data;
        _direction = direction.normalized;
        _elapsedTime = 0f;
        transform.position = position;
    }

    /// <summary>
    /// プールに戻すときのコールバックを設定します。
    /// </summary>
    public void SetReturnCallback(Action callback)
    {
        _returnCallback = callback;
    }

    private void Update()
    {
        if (_data == null)
            return;

        // 移動
        transform.position += _direction * _data.speed * Time.deltaTime;

        // ライフタイム管理
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _data.lifetime)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 衝突したときの処理。
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // ここで敵への当たり判定やダメージ処理を行う
        // 現在は簡易実装のため、衝突時にプールに戻す
        if (other.CompareTag("Enemy"))
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 弾をプールに戻します。
    /// </summary>
    private void ReturnToPool()
    {
        _returnCallback?.Invoke();
    }
}
