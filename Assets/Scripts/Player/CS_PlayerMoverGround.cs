using UnityEngine;

/// <summary>
/// 地上走行のみのキャラクター用プレイヤー移動クラス
/// 移動、ジャンプ、ダッシュが可能
/// 飛行機能は持たない
/// </summary>
public class CS_PlayerMoverGround : CS_PlayerMoverBase
{
    [SerializeField]
    private CSO_CharacterMobilityData _mobilityDataAsset;

    protected override CSO_CharacterMobilityData _mobilityData => _mobilityDataAsset;

    protected virtual void Start()
    {
        if (_mobilityDataAsset == null)
        {
            Debug.LogError("CS_PlayerMoverGround: _mobilityDataAsset is not assigned!", gameObject);
        }
    }
}
