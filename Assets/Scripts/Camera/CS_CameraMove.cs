using UnityEngine;

public class CS_CameraMove : MonoBehaviour
{
    [Header("プレイヤーとの距離")]
    [SerializeField] private float _distance = 10f;

    [Header("プレイヤーとの高さ")]
    [SerializeField] private float _height = 5f;

    [Header("プレイヤーのTransform")]
    private Transform _playerTransform;

    [Header("マウス感度")]
    [SerializeField] private float _mouseSensitivity = 0.2f;

    private void Start()
    {
        if (_playerTransform == null)
        {
            _playerTransform = GameObject.FindAnyObjectByType<CS_PlayerController>().transform;
        }

        // マウスのカーソルを非表示にしてロックする
        Cursor.lockState = CursorLockMode.Locked;

    }

    private void Update()
    {
        if (_playerTransform == null) return;

        Vector2 inputVector2 = CS_InputManager.readInstance.customInputSystem.Player.Look.ReadValue<Vector2>();

        // カメラの回転を計算
        float rotationX = inputVector2.x * _mouseSensitivity; // 水平方向の回転速度を調整

        float rotationY = inputVector2.y * (_mouseSensitivity * 0.5f); // 垂直方向の回転速度を調整

        // カメラの回転を適用
        transform.RotateAround(_playerTransform.position, Vector3.up, rotationX);
        transform.RotateAround(_playerTransform.position, transform.right, -rotationY);

        // カメラの高さを調整
        Vector3 desiredPosition = _playerTransform.position - transform.forward * _distance + Vector3.up * _height;

        // カメラの位置を更新
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f); // スムーズに移動するためにLerpを使用
    }
}
