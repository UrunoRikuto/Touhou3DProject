using System.Collections.Generic;
using UnityEngine;

public abstract class CS_BaseBullet : MonoBehaviour
{
    [Header("＝＝＝＝＝ デバック ＝＝＝＝＝" +
    "\n時間経過後に初期位置に戻して同じ動作をさせる")]

    [Tooltip("デバック機能を有効にするか")]
    [SerializeField]
    protected bool _debug_enable;

    [Tooltip("初期速度")]
    protected float _debug_initialSpeed;

    [Tooltip("初期位置")]
    protected Vector3 _debug_initialPosition;

    [Header("＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝")]

    [Tooltip("進む速度")]
    [SerializeField]
    protected float _speed;
    public void SetSpeed(float speed) { _speed = speed; }

    [Tooltip("弾が残る時間")]
    [SerializeField]
    protected float _lifeTime;
    public void SetLifeTime(float lifeTime) { _lifeTime = lifeTime; }

    [Tooltip("弾の経過時間")]
    [SerializeField]
    protected float _elapsedTime;

    [Tooltip("命中判定を行うタグ名")]
    [SerializeField]
    protected List<string> _targetTag;


    protected void Awake()
    {
        _targetTag = new List<string> {
            "Enemy" 
        };
    }

    private void Start()
    {
        _debug_initialSpeed = _speed;
        _debug_initialPosition = transform.position;
    }

    protected void Update()
    {
        Move();
    }

    protected void LateUpdate()
    {
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime > _lifeTime)
        {
            if(!_debug_enable)
                Destroy(gameObject);
            else
            {
                _elapsedTime = 0;
                _speed = _debug_initialSpeed;
                transform.position = _debug_initialPosition;
            }
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (_targetTag.Contains(other.tag))
        {
            Destroy(gameObject);
        }
    }

    protected abstract void Move();
}
