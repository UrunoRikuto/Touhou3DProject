using UnityEngine;

/// <summary>
/// 飛行可能なキャラクター用プレイヤー移動クラス
/// 移動、ジャンプ、ダッシュ、飛行が可能
/// ダブルジャンプで飛行状態に遷移
/// </summary>
public class CS_PlayerMoverFlight : CS_PlayerMoverBase
{
    [SerializeField]
    private CSO_FlightMobilityData _flightDataAsset;

    private float _lastJumpTime = 0f;

    protected override CSO_CharacterMobilityData _mobilityData => _flightDataAsset;

    protected virtual void Start()
    {
        if (_flightDataAsset == null)
        {
            Debug.LogError("CS_PlayerMoverFlight: _flightDataAsset is not assigned!", gameObject);
        }
    }

    /// <summary>
    /// ジャンプ入力の処理(飛行対応)
    /// Grounded: 通常ジャンプ→Airborne
    /// Airborne: 猶予内にジャンプ→Flying
    /// Flying: 入力無視(上昇はComputeVerticalDeltaで処理)
    /// </summary>
    protected override void HandleJumpInput(bool jumpPressedThisFrame)
    {
        if (!jumpPressedThisFrame)
        {
            return;
        }

        switch (_state)
        {
            case MovementState.Grounded:
                _verticalVelocity = _mobilityData.jumpPower;
                _state = MovementState.Airborne;
                _lastJumpTime = Time.time;
                break;

            case MovementState.Airborne:
                if (Time.time - _lastJumpTime <= _flightDataAsset.doubleJumpWindow)
                {
                    _state = MovementState.Flying;
                    _verticalVelocity = 0f;
                }
                break;

            case MovementState.Flying:
                // 飛行中はジャンプ入力無視
                break;
        }
    }

    /// <summary>
    /// 垂直移動量の計算(飛行対応)
    /// Flying状態: jumpHeldで上昇、descendHeldで下降、どちらもなければホバー
    /// Flying以外: 重力を適用
    /// </summary>
    protected override float ComputeVerticalDelta(bool jumpHeld, bool descendHeld, float deltaTime)
    {
        if (_state == MovementState.Flying)
        {
            if (jumpHeld)
            {
                _verticalVelocity = _flightDataAsset.flyAscendSpeed;
            }
            else if (descendHeld)
            {
                _verticalVelocity = -_flightDataAsset.flyDescendSpeed;
            }
            else
            {
                _verticalVelocity = 0f;
            }

            return _verticalVelocity * deltaTime;
        }

        // Flying以外は基底クラスの重力処理を使用
        return base.ComputeVerticalDelta(jumpHeld, descendHeld, deltaTime);
    }
}
