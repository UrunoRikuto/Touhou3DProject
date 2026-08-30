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

    private void Start()
    {
        // 初期状態は非発動
        isBarrageActive = false;
        _inputReader = GetComponent<CS_PlayerInputReader>();
    }

    private void Update()
    {
        if (_inputReader.skillInput[0])
        {

        }
        else if (_inputReader.skillInput[1])
        {

        }
        else if (_inputReader.skillInput[2])
        {

        }

        // スキル入力がいずれか押されている場合は弾幕発動中とみなす
        isBarrageActive = _inputReader.skillInput[0] || _inputReader.skillInput[1] || _inputReader.skillInput[2];
    }
}
