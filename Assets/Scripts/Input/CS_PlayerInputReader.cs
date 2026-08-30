using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 新Input Systemを使用して入力を読み取るMonoBehaviour
/// 移動入力、ジャンプ、下降入力の読み取りを提供
/// Shift+移動入力によるダッシュトリガーイベントを発火
/// 移動ロジックには依存しない設計
/// </summary>
public class CS_PlayerInputReader : MonoBehaviour
{
    private CS_CustomInputSystem _controls;

    private Vector2 _moveInput = Vector2.zero;
    public Vector2 moveInput { get { return _moveInput; } private set { _moveInput = value; } }

    public bool jumpPressedThisFrame => _controls.Player.Jump.WasPressedThisFrame();

    public bool jumpHeld => _controls.Player.Jump.IsPressed();

    public bool descendHeld => _controls.Player.Descend.IsPressed();

    private bool[] _skillInput = new bool[3];
    public bool[] skillInput => _skillInput;

    public event Action<Vector2> onDashTriggered;

    [SerializeField]
    private float _dashInputThreshold = 0.5f;

    protected virtual void Awake()
    {
        _controls = new CS_CustomInputSystem();
        _controls.Enable();
    }

    protected virtual void OnEnable()
    {
        if (_controls != null)
        {
            _controls.Player.Move.started += OnMoveStarted;
            _controls.Player.Move.performed += OnMovePerformed;
            _controls.Player.Move.canceled += OnMoveCanceled;
        }
    }

    protected virtual void OnDisable()
    {
        if (_controls != null)
        {
            _controls.Player.Move.started -= OnMoveStarted;
            _controls.Player.Move.performed -= OnMovePerformed;
            _controls.Player.Move.canceled -= OnMoveCanceled;
            _controls.Disable();
        }
    }

    protected virtual void Update()
    {
        _moveInput = _controls.Player.Move.ReadValue<Vector2>();

        _skillInput[0] = _controls.Skill.A.IsPressed();
        _skillInput[1] = _controls.Skill.B.IsPressed();
        _skillInput[2] = _controls.Skill.C.IsPressed();

        CheckDashInput();
    }

    /// <summary>
    /// Move入力の開始を検知
    /// </summary>
    private void OnMoveStarted(InputAction.CallbackContext context)
    {
        // 開始時の処理（必要に応じて）
    }

    /// <summary>
    /// Move入力の実行中
    /// </summary>
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // 実行中の処理（必要に応じて）
    }

    /// <summary>
    /// Move入力のキャンセル
    /// </summary>
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        // キャンセル時の処理（必要に応じて）
    }

    /// <summary>
    /// ダッシュ入力を検知（Shift+移動方向）
    /// Shiftが押されており、移動入力が閾値以上なら発火
    /// </summary>
    private void CheckDashInput()
    {
        bool dashKeyPressed = _controls.Player.Dash.IsPressed();

        if (dashKeyPressed && _moveInput.magnitude >= _dashInputThreshold)
        {
            onDashTriggered?.Invoke(_moveInput.normalized);
        }
    }
}
