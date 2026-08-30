using UnityEngine;

/// <summary>
/// プレイヤー移動システムの基底クラス
/// CharacterControllerを使用した移動、ジャンプ、ダッシュ機能を提供
/// 飛行機能は派生クラスでオーバーライドして実装
/// </summary>
[RequireComponent(typeof(CharacterController))]
public abstract class CS_PlayerMoverBase : MonoBehaviour
{
    /// <summary>
    /// プレイヤーの移動状態
    /// </summary>
    protected enum MovementState
    {
        Grounded,
        Airborne,
        Flying
    }

    /// <summary>
    /// 移動性能データ(派生クラスで実装)
    /// </summary>
    protected abstract CSO_CharacterMobilityData _mobilityData { get; }

    protected CharacterController _controller;
    protected MovementState _state = MovementState.Grounded;
    protected float _verticalVelocity = 0f;

    private bool _clampToBounds = false;
    private Vector2 _boundsMinXZ = Vector2.zero;
    private Vector2 _boundsMaxXZ = Vector2.zero;
    private float _minAltitude = 0f;
    private float _maxAltitude = 100f;

    private float _dashTimeRemaining = 0f;
    private float _dashCooldownRemaining = 0f;
    private Vector3 _dashDirection = Vector3.zero;

    protected virtual void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// 毎フレーム呼び出される更新メソッド
    /// 入力に基づいて移動、ジャンプ、ダッシュを処理
    /// </summary>
    public void Tick(Vector2 moveInput, bool jumpPressedThisFrame, bool jumpHeld, bool descendHeld, float deltaTime)
    {
        // ダッシュタイマーの更新
        if (_dashTimeRemaining > 0f)
        {
            _dashTimeRemaining -= deltaTime;
        }

        if (_dashCooldownRemaining > 0f)
        {
            _dashCooldownRemaining -= deltaTime;
        }

        // ジャンプ入力の処理
        HandleJumpInput(jumpPressedThisFrame);

        // 水平移動の計算
        Vector3 horizontalDelta = ComputeHorizontalDelta(moveInput, deltaTime);

        // ダッシュ中の移動を優先
        if (_dashTimeRemaining > 0f)
        {
            horizontalDelta = _dashDirection * _mobilityData.dashSpeed * deltaTime;
        }

        // 垂直移動の計算
        float verticalDelta = ComputeVerticalDelta(jumpHeld, descendHeld, deltaTime);

        // 移動の適用
        Vector3 moveDelta = new Vector3(horizontalDelta.x, verticalDelta, horizontalDelta.z);
        _controller.Move(moveDelta);

        // 着地判定
        if (_controller.isGrounded)
        {
            _state = MovementState.Grounded;
            _verticalVelocity = 0f;
        }

        // プレイエリア制限
        ClampPosition();
    }

    /// <summary>
    /// ダッシュの実行を試みる
    /// ダッシュ中またはクールダウン中でない場合のみ実行
    /// </summary>
    public void TryDash(Vector2 direction)
    {
        if (_dashTimeRemaining > 0f || _dashCooldownRemaining > 0f)
        {
            return;
        }

        _dashDirection = new Vector3(direction.x, 0f, direction.y).normalized;
        _dashTimeRemaining = _mobilityData.dashDuration;
        _dashCooldownRemaining = _mobilityData.dashCooldown;
    }

    /// <summary>
    /// ジャンプ入力の処理(派生クラスでオーバーライド可能)
    /// </summary>
    protected virtual void HandleJumpInput(bool jumpPressedThisFrame)
    {
        if (jumpPressedThisFrame && _state == MovementState.Grounded)
        {
            _verticalVelocity = _mobilityData.jumpPower;
            _state = MovementState.Airborne;
        }
    }

    /// <summary>
    /// 水平移動量の計算
    /// </summary>
    protected virtual Vector3 ComputeHorizontalDelta(Vector2 moveInput, float deltaTime)
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection * _mobilityData.moveSpeed * deltaTime;
    }

    /// <summary>
    /// 垂直移動量の計算(派生クラスでオーバーライド可能)
    /// </summary>
    protected virtual float ComputeVerticalDelta(bool jumpHeld, bool descendHeld, float deltaTime)
    {
        _verticalVelocity += _mobilityData.gravity * deltaTime;
        return _verticalVelocity * deltaTime;
    }

    /// <summary>
    /// プレイエリア範囲内に位置をクランプ
    /// </summary>
    private void ClampPosition()
    {
        if (!_clampToBounds)
        {
            return;
        }

        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, _boundsMinXZ.x, _boundsMaxXZ.x);
        currentPos.y = Mathf.Clamp(currentPos.y, _minAltitude, _maxAltitude);
        currentPos.z = Mathf.Clamp(currentPos.z, _boundsMinXZ.y, _boundsMaxXZ.y);

        transform.position = currentPos;
    }

    /// <summary>
    /// プレイエリアの制限を設定
    /// </summary>
    public void SetBounds(Vector2 minXZ, Vector2 maxXZ, float minAltitude, float maxAltitude)
    {
        _boundsMinXZ = minXZ;
        _boundsMaxXZ = maxXZ;
        _minAltitude = minAltitude;
        _maxAltitude = maxAltitude;
        _clampToBounds = true;
    }

    /// <summary>
    /// プレイエリアの制限を無効化
    /// </summary>
    public void DisableBounds()
    {
        _clampToBounds = false;
    }
}
