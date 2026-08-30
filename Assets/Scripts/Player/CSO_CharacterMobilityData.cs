using UnityEngine;

/// <summary>
/// キャラクターの移動性能データを定義するScriptableObject
/// 移動速度、ジャンプ力、重力、ダッシュ性能などの基本パラメータを保持
/// </summary>
[CreateAssetMenu(menuName = "Character/Mobility Data", fileName = "CS_CharacterMobilityData")]
public class CSO_CharacterMobilityData : ScriptableObject
{
    [Header("移動速度")]
    [SerializeField]
    public float moveSpeed = 5f;

    [Header("ジャンプ力")]
    [SerializeField]
    public float jumpPower = 5f;

    [Header("重力")]
    [SerializeField]
    public float gravity = -9.81f;

    [Header("ダッシュ速度")]
    [SerializeField]
    public float dashSpeed = 15f;

    [Header("ダッシュ持続時間")]
    [SerializeField]
    public float dashDuration = 0.2f;

    [Header("ダッシュクールダウン")]
    [SerializeField]
    public float dashCooldown = 1f;
}
