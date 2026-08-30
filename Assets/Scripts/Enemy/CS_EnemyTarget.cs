using UnityEngine;

/// <summary>
/// ロックオン対象になり得る敵に付けるマーカーコンポーネント。
/// OnEnable/OnDisableで自動的にCS_EnemyTargetRegistryに登録/解除される。
/// </summary>
public class CS_EnemyTarget : MonoBehaviour
{
    /// <summary>
    /// GameObject有効化時にレジストリに登録します。
    /// </summary>
    private void OnEnable()
    {
        CS_EnemyTargetRegistry.Register(this);
    }

    /// <summary>
    /// GameObject無効化時にレジストリから削除します。
    /// </summary>
    private void OnDisable()
    {
        CS_EnemyTargetRegistry.Unregister(this);
    }
}
