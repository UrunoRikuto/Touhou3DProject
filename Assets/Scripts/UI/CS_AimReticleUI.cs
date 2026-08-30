using UnityEngine;

/// <summary>
/// 照準UIの表示位置を管理するコンポーネント。
/// ロックオン状態に応じて照準の位置を画面内で更新します。
/// </summary>
public class CS_AimReticleUI : MonoBehaviour
{
    [SerializeField]
    private CS_AimController _aimController;

    [SerializeField]
    private RectTransform _reticleRect;

    [SerializeField]
    private Camera _camera;

    private void OnEnable()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateReticlePosition();
    }

    /// <summary>
    /// 照準UIの位置を更新します。
    /// </summary>
    private void UpdateReticlePosition()
    {
        Vector3 screenPosition;

        if (_aimController.hasLockOn && _aimController.lockedTarget != null)
        {
            // ロックオン中：ターゲット位置をスクリーン座標に変換
            screenPosition = _camera.WorldToScreenPoint(_aimController.lockedTarget.position);
        }
        else
        {
            // ロックオンなし：画面中央に配置
            screenPosition = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        }

        // RectTransformのアンカーが左下を原点と仮定して設定
        // (Canvasのレンダーモードに応じて調整が必要な場合がある)
        _reticleRect.position = screenPosition;
    }
}
