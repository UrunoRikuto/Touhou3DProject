using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 弾幕パターン(プレハブ)のルートに付けるオーケストレーター。
/// 配下のCS_BulletEmitter / CS_GeneratorRotator / CS_GeneratorOscillator / CS_GeneratorPathを
/// まとめて開始・終了し、ライフサイクルを管理する。
///
/// サブGeneratorの時間差での有効/無効切り替えは、_cues(CS_BarrageCue配列)による
/// 単純な時間比較で行う。CS_BarrageDesignerWindow(弾幕デザイナーウィンドウ)の
/// シーケンサーUIでガントチャート風に視覚編集できる。
///
/// [レガシー] 任意でPlayableDirector(Unity Timeline)を同じGameObjectに付けておくと、
/// Activate()時に自動でTimelineの再生も開始する。既存のTimelineベースのパターン
/// (SpellCard01等)との互換性のために残しているが、新規パターンでは_cuesの使用を推奨する
/// (Timeline内部APIの保護レベルに起因する不具合を過去に踏んでいるため)。
/// </summary>
public class CS_BarragePattern : MonoBehaviour
{
    [SerializeField] private float _duration;
    [SerializeField] private CS_BulletEmitter[] _emitters;
    [SerializeField] private CS_GeneratorRotator[] _rotators;
    [SerializeField] private CS_GeneratorOscillator[] _oscillators;
    [SerializeField] private CS_GeneratorPath[] _paths;
    [SerializeField] private CS_BarrageCue[] _cues;
    [SerializeField] private PlayableDirector _director;

    /// <summary>パターンが終了した(継続時間経過、または外部からDeactivateされた)ときに発火。</summary>
    public event Action onPatternFinished;

    private float _elapsedTime;
    private bool _isActive;

    private List<string> _hitTagList = new List<string>();
    public List<string> hitTagList => _hitTagList;

    /// <summary>パターンの継続時間(秒)。CS_BarrageDesignerWindowがプレビュー範囲を知るために参照する。</summary>
    public float duration => _duration;

    /// <summary>現在の経過時間(秒)。パターンが非アクティブなときは意味を持たない。</summary>
    public float elapsedTime => _elapsedTime;

    private void Awake()
    {
        if (_emitters == null || _emitters.Length == 0)
        {
            _emitters = GetComponentsInChildren<CS_BulletEmitter>(true);
        }
        if (_rotators == null || _rotators.Length == 0)
        {
            _rotators = GetComponentsInChildren<CS_GeneratorRotator>(true);
        }
        if (_oscillators == null || _oscillators.Length == 0)
        {
            _oscillators = GetComponentsInChildren<CS_GeneratorOscillator>(true);
        }
        if (_paths == null || _paths.Length == 0)
        {
            _paths = GetComponentsInChildren<CS_GeneratorPath>(true);
        }
        if (_director == null)
        {
            _director = GetComponent<PlayableDirector>();
        }
    }

    private void Update()
    {
        if (!_isActive) return;

        _elapsedTime += Time.deltaTime;
        UpdateCues();

        if (_duration > 0f && _elapsedTime >= _duration)
        {
            Deactivate();
        }
    }

    /// <summary>パターンを開始する。全Emitter/Rotator/Oscillator/Pathを有効化し、キューとTimelineがあれば再生する。</summary>
    [ContextMenu("Activate Pattern")]
    public void Activate()
    {
        _isActive = true;
        _elapsedTime = 0f;
        SetComponentsEnabled(true);
        UpdateCues();

        if (_director != null && _director.playableAsset != null)
        {
            _director.time = 0;
            _director.Play();
        }
    }

    public void Activate(List<string> hitTagList)
    {
        _hitTagList = hitTagList;
        Activate();
    }

    /// <summary>パターンを停止する。全Emitter/Rotator/Oscillator/Pathを無効化し、onPatternFinishedを発火する。</summary>
    public void Deactivate()
    {
        _isActive = false;
        SetComponentsEnabled(false);

        if (_director != null && _director.playableAsset != null)
        {
            _director.Stop();
        }

        onPatternFinished?.Invoke();
    }

    /// <summary>_cuesの時間帯に従って、対象GameObjectのアクティブ状態を更新する。</summary>
    private void UpdateCues()
    {
        if (_cues == null) return;

        for (int i = 0; i < _cues.Length; i++)
        {
            CS_BarrageCue cue = _cues[i];
            if (cue == null || cue.targetGenerator == null) continue;

            bool shouldBeActive = _elapsedTime >= cue.startTime && _elapsedTime < cue.endTime;
            if (cue.targetGenerator.activeSelf != shouldBeActive)
            {
                cue.targetGenerator.SetActive(shouldBeActive);
            }
        }
    }

    private void SetComponentsEnabled(bool isEnabled)
    {
        foreach (var emitter in _emitters)
        {
            if (emitter != null) emitter.enabled = isEnabled;
        }
        foreach (var rotator in _rotators)
        {
            if (rotator != null) rotator.enabled = isEnabled;
        }
        foreach (var oscillator in _oscillators)
        {
            if (oscillator != null) oscillator.enabled = isEnabled;
        }
        foreach (var path in _paths)
        {
            if (path != null) path.enabled = isEnabled;
        }
    }
}