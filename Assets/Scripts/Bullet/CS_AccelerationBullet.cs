using UnityEngine;

public class CS_AccelerationBullet : CS_BaseBullet
{
    [Tooltip("加速度")]
    [SerializeField]
    private float _acceleration;
    public void SetAcceleration(float acceleration) { _acceleration = acceleration; }

    [Tooltip("加速のグラフ")]
    [SerializeField] 
    private AnimationCurve _accelerationCurve;

    private new void Update()
    {
        base.Update();
        UpdateSpeed();
    }

    protected override void Move()
    {
        transform.Translate(transform.forward * _speed * Time.deltaTime);
    }

    private void UpdateSpeed()
    {
        // 生存時間の割合を基に加速度を計算する
        float lifeTimeRatio = _elapsedTime / _lifeTime;

        // AnimationCurveを使用して加速度を計算する
        float accelerationFactor = _accelerationCurve.Evaluate(lifeTimeRatio);

        // 加速度を適用して速度を更新する
        _speed += _acceleration * accelerationFactor * Time.deltaTime;
    }
}
