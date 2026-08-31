using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CS_SpellCardDefinitionに登録された複数の弾幕バリエーション(プレハブ)を、
/// 指定された時間帯・角度でInstantiateして再生するランタイムプレイヤー。
///
/// 各バリエーションはCS_BarrageDesignerWindow等で個別に作成・プレハブ化された、
/// シーン配置に依存しない独立したCS_BarragePatternプレハブ。このコンポーネントは
/// Play()が呼ばれた時点でそれらをこの場にInstantiateし(SetActive(false)で待機させ)、
/// CS_SpellCardEntryのstartTime〜endTimeの間だけそのCS_BarragePattern.Activate()/
/// Deactivate()を呼び、angleで指定した向きに回転させて配置する。
///
/// バリエーション側のCS_BarragePattern._durationは0(無制限)にしておき、
/// タイミング制御はこちら(スペルカード側のstartTime/endTime)に任せる運用を推奨する。
/// </summary>
public class CS_SpellCardPlayer : MonoBehaviour
{
    [SerializeField] private List<CS_SpellCardDefinition> _definition;
    [SerializeField] private bool _loop;

    /// <summary>スペルカードが終了した(全エントリのendTimeを過ぎた、ループなしの場合)ときに発火。</summary>
    public event Action onSpellCardFinished;

    private class RuntimeEntry
    {
        public CS_SpellCardEntry source;
        public GameObject instance;
        public CS_BarragePattern pattern;
        public bool isPlaying;
    }

    private readonly List<RuntimeEntry> _runtimeEntries = new List<RuntimeEntry>();
    private float _elapsedTime;
    private bool _isActive;
    public bool isActive => _isActive;
    private float _totalDuration;

    /// <summary>定義済みのスペルカードを再生開始する。既に再生中なら一度停止してから作り直す。</summary>
    [ContextMenu("Play")]
    public void Play(int index = 0)
    {
        Stop();

        if (_definition == null || _definition[index].entries == null || _definition[index].entries.Length == 0)
        {
            Debug.LogWarning($"{name}: CS_SpellCardPlayerに_definitionが未設定、またはエントリが空です。", this);
            return;
        }

        _totalDuration = 0f;

        foreach (var entry in _definition[index].entries)
        {
            if (entry == null || entry.variationPrefab == null) continue;

            var instance = Instantiate(entry.variationPrefab, transform);
            instance.transform.localPosition = entry.positionOffset;
            instance.transform.localRotation = Quaternion.Euler(entry.angle);
            instance.SetActive(false);

            if (entry.overrideFireInterval)
            {
                foreach (var emitter in instance.GetComponentsInChildren<CS_BulletEmitter>(true))
                {
                    emitter.fireInterval = entry.fireIntervalOverride;
                }
            }

            var pattern = instance.GetComponent<CS_BarragePattern>();
            if (pattern == null)
            {
                Debug.LogWarning($"{name}: バリエーションプレハブ '{entry.variationPrefab.name}' にCS_BarragePatternが見つかりません。", this);
                Destroy(instance);
                continue;
            }

            _runtimeEntries.Add(new RuntimeEntry
            {
                source = entry,
                instance = instance,
                pattern = pattern,
                isPlaying = false
            });
            _totalDuration = Mathf.Max(_totalDuration, entry.endTime);
        }

        _elapsedTime = 0f;
        _isActive = _runtimeEntries.Count > 0;
    }

    /// <summary>再生を止め、Instantiateした全バリエーションのインスタンスを破棄する。</summary>
    [ContextMenu("Stop")]
    public void Stop()
    {
        _isActive = false;
        foreach (var runtime in _runtimeEntries)
        {
            if (runtime.instance != null) Destroy(runtime.instance);
        }
        _runtimeEntries.Clear();
    }

    private void Update()
    {
        if (!_isActive) return;

        _elapsedTime += Time.deltaTime;
        UpdateEntries();

        if (_elapsedTime < _totalDuration) return;

        if (_loop)
        {
            _elapsedTime = 0f;
            foreach (var runtime in _runtimeEntries)
            {
                if (!runtime.isPlaying) continue;
                runtime.pattern.Deactivate();
                runtime.instance.SetActive(false);
                runtime.isPlaying = false;
            }
        }
        else
        {
            _isActive = false;
            onSpellCardFinished?.Invoke();
        }
    }

    private void UpdateEntries()
    {
        foreach (var runtime in _runtimeEntries)
        {
            bool shouldBePlaying = _elapsedTime >= runtime.source.startTime && _elapsedTime < runtime.source.endTime;
            if (shouldBePlaying && !runtime.isPlaying)
            {
                runtime.instance.SetActive(true);
                runtime.pattern.Activate();
                runtime.isPlaying = true;
            }
            else if (!shouldBePlaying && runtime.isPlaying)
            {
                runtime.pattern.Deactivate();
                runtime.instance.SetActive(false);
                runtime.isPlaying = false;
            }
        }
    }

    private void OnDisable()
    {
        Stop();
    }
}