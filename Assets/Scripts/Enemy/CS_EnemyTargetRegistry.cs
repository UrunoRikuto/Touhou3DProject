using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アクティブな敵ターゲットをグローバルに管理する静的クラス。
/// 敵がシーンに登場/消滅する際、このレジストリに登録/解除される。
/// </summary>
public static class CS_EnemyTargetRegistry
{
    private static readonly List<CS_EnemyTarget> _activeTargets = new List<CS_EnemyTarget>();

    /// <summary>
    /// 現在アクティブな敵ターゲットのリストを読み取り専用で取得します。
    /// </summary>
    public static IReadOnlyList<CS_EnemyTarget> activeTargets => _activeTargets;

    /// <summary>
    /// ターゲットをレジストリに登録します。
    /// </summary>
    public static void Register(CS_EnemyTarget target)
    {
        if (!_activeTargets.Contains(target))
        {
            _activeTargets.Add(target);
        }
    }

    /// <summary>
    /// ターゲットをレジストリから削除します。
    /// </summary>
    public static void Unregister(CS_EnemyTarget target)
    {
        _activeTargets.Remove(target);
    }
}
