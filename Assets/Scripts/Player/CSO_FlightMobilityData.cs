using UnityEngine;

/// <summary>
/// 飛行可能キャラクター向けの移動性能データを定義するScriptableObject
/// CS_CharacterMobilityDataを拡張し、ダブルジャンプと飛行の速度パラメータを追加
/// </summary>
[CreateAssetMenu(menuName = "Character/Flight Mobility Data", fileName = "CS_FlightMobilityData")]
public class CSO_FlightMobilityData : CSO_CharacterMobilityData
{
    [Header("ダブルジャンプ判定猶予時間")]
    [SerializeField]
    public float doubleJumpWindow = 0.5f;

    [Header("飛行上昇速度")]
    [SerializeField]
    public float flyAscendSpeed = 3f;

    [Header("飛行下降速度")]
    [SerializeField]
    public float flyDescendSpeed = 2f;
}
