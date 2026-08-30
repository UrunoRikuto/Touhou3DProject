using UnityEngine;

/// <summary>
/// プレイヤーコントローラーの統合クラス
/// CS_PlayerInputReaderから入力を取得し、CS_PlayerMoverBaseに指令を出す
/// 入力と移動ロジックの中間層として機能
/// </summary>
[RequireComponent(typeof(CS_PlayerInputReader))]
public class CS_PlayerController : MonoBehaviour
{
    [SerializeField]
    private CS_PlayerMoverBase _mover;

    private CS_PlayerInputReader _input;

    protected virtual void Awake()
    {
        _input = GetComponent<CS_PlayerInputReader>();

        if (_mover == null)
        {
            Debug.LogError("CS_PlayerController: _mover is not assigned!", gameObject);
        }

        if (_input == null)
        {
            Debug.LogError("CS_PlayerController: CS_PlayerInputReader component not found!", gameObject);
        }
    }

    protected virtual void OnEnable()
    {
        if (_input != null)
        {
            _input.onDashTriggered += _mover.TryDash;
        }
    }

    protected virtual void OnDisable()
    {
        if (_input != null)
        {
            _input.onDashTriggered -= _mover.TryDash;
        }
    }

    protected virtual void Update()
    {
        if (_mover == null || _input == null)
        {
            return;
        }

        _mover.Tick(
            _input.moveInput,
            _input.jumpPressedThisFrame,
            _input.jumpHeld,
            _input.descendHeld,
            Time.deltaTime
         );
    }
}
