using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// スキル弾幕を管理するコンポーネント。
/// 基本弾攻撃の発射抑制に使用されます。
/// 本実装(コスト消費、弾幕再生ロジック等)は別途実装予定のため、
/// 現在はisBarrageActiveプロパティのみを提供するスタブです。
/// </summary>
public class CS_BarrageSkillController : MonoBehaviour
{
    /// <summary>
    /// スキル弾幕が現在発動中かどうかを示します。
    /// </summary>
    public bool isBarrageActive { get; private set; }

    private CS_PlayerInputReader _inputReader;

    private CS_SpellCardPlayer _spellCardPlayer;

    private void Start()
    {
        // 初期状態は非発動
        isBarrageActive = false;
        _inputReader = GetComponent<CS_PlayerInputReader>();
        _spellCardPlayer = GetComponentInChildren<CS_SpellCardPlayer>();
    }

    private void Update()
    {
        _spellCardPlayer.transform.forward = transform.forward;  // プレイヤーの向きに合わせる

        // スキル入力がいずれか押されている場合は弾幕発動中とみなす
        isBarrageActive = _spellCardPlayer.isActive;

        if (_spellCardPlayer.isActive) return;

        for (int i = 0; i < _inputReader.skillInput.Length; i++)
        {
            if (_inputReader.skillInput[i])
            {
                _spellCardPlayer.Play(i);
                break;
            }
        }
    }
}
