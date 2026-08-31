using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Collections.Generic;

/// <summary>
/// 弾幕パターン(プレハブ)のルートに付けるオーケストレーター。
/// パターン全体の開始・終了・ライフサイクルを管理し、
/// 内包するEmitterとRotatorの有効/無効を統括制御する。
/// PlayableDirector(Timeline)をサポートする。
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CS_BarragePattern : MonoBehaviour
{
    [SerializeField]
    private float _duration;

    [SerializeField]
    private CS_BulletEmitter[] _emitters;

    [SerializeField]
    private CS_GeneratorRotator[] _rotators;

    [SerializeField]
    private CS_GeneratorOscillator[] _oscillators;

    [SerializeField]
    private PlayableDirector _director;

    /// <summary>
    /// パターンが終了した時に発火するイベント。
    /// </summary>
    public event Action onPatternFinished;

    private float _elapsedTime;

    private bool _isActive;

    private List<string> _hitTagList = new List<string>();
    public List<string> hitTagList => _hitTagList;

    private void Awake()
    {
        // Emitterが手動設定されていない場合は自動収集
        if (_emitters == null || _emitters.Length == 0)
        {
            _emitters = GetComponentsInChildren<CS_BulletEmitter>(true);
        }

        // Rotatorが手動設定されていない場合は自動収集
        if (_rotators == null || _rotators.Length == 0)
        {
            _rotators = GetComponentsInChildren<CS_GeneratorRotator>(true);
        }

        // Oscillatorが手動設定されていない場合は自動収集
        if (_oscillators == null || _oscillators.Length == 0)
        {
            _oscillators = GetComponentsInChildren<CS_GeneratorOscillator>(true);
        }

        // PlayableDirectorが手動設定されていない場合は自動取得
        if (_director == null)
        {
            _director = GetComponent<PlayableDirector>();
        }

        _isActive = false;
        _elapsedTime = 0f;
    }

    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        // 持続時間管理(0以下なら無制限)
        if (_duration > 0f)
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _duration)
            {
                Deactivate();
            }
        }
    }

    /// <summary>
    /// パターンを開始します。
    /// 全Emitter・Rotator・Oscillatorを有効化し、タイマーをリセット。
    /// PlayableDirectorが設定されていれば再生を開始。
    /// </summary>
    [ContextMenu("Activate Pattern")]
    public void Activate()
    {
        _isActive = true;
        _elapsedTime = 0f;

        // 全Emitterを有効化
        foreach (var emitter in _emitters)
        {
            if (emitter != null)
            {
                emitter.enabled = true;
            }
        }

        // 全Rotatorを有効化
        foreach (var rotator in _rotators)
        {
            if (rotator != null)
            {
                rotator.enabled = true;
            }
        }

        // 全Oscillatorを有効化
        foreach (var oscillator in _oscillators)
        {
            if (oscillator != null)
            {
                oscillator.enabled = true;
            }
        }

        // PlayableDirectorを再生
        if (_director != null && _director.playableAsset != null)
        {
            _director.time = 0;
            _director.Play();
        }
    }

    /// <summary>
    /// パターンを開始します。
    /// </summary>
    /// <param name="hitTags">弾の衝突判定を行うタグのリスト</param>
    public void Activate(List<string> hitTags)
    {
        _hitTagList = hitTags;
        Activate();
    }

    /// <summary>
    /// パターンを終了します。
    /// 全Emitter・Rotator・Oscillatorを無効化し、終了イベントを発火。
    /// PlayableDirectorも停止。
    /// </summary>
    public void Deactivate()
    {
        _isActive = false;

        // 全Emitterを無効化
        foreach (var emitter in _emitters)
        {
            if (emitter != null)
            {
                emitter.enabled = false;
            }
        }

        // 全Rotatorを無効化
        foreach (var rotator in _rotators)
        {
            if (rotator != null)
            {
                rotator.enabled = false;
            }
        }

        // 全Oscillatorを無効化
        foreach (var oscillator in _oscillators)
        {
            if (oscillator != null)
            {
                oscillator.enabled = false;
            }
        }

        // PlayableDirectorを停止
        if (_director != null && _director.playableAsset != null)
        {
            _director.Stop();
        }

        // イベント発火
        onPatternFinished?.Invoke();
    }
}
