using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 照準UIの表示位置と見た目を管理するコンポーネント。
/// ロックオン状態に応じて照準の位置、スプライト、色を更新します。
/// ロックオンしていない時はデフォルトスプライトと色で表示し、
/// ロックオン中はロックオン用スプライトと強調色で表示されます。
/// </summary>
public class CS_AimReticleUI : MonoBehaviour
{
    [SerializeField]
    private CS_AimController _aimController;

    [SerializeField]
    private RectTransform _reticleRect;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private Image _reticleImage;

    [SerializeField]
    private Sprite _defaultSprite;

    [SerializeField]
    private Sprite _lockOnSprite;

    private bool _wasLockedOn;

    private void OnEnable()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _wasLockedOn = false;
    }

    private void Update()
    {
        UpdateReticlePosition();
        UpdateReticleAppearance();
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

    /// <summary>
    /// 照準UIのスプライトと色を更新します。
    /// ロックオン状態が変化した場合のみスプライトを切り替えます。
    /// </summary>
    private void UpdateReticleAppearance()
    {
        // ロックオン状態が前フレームから変化したかチェック
        if (_wasLockedOn != _aimController.hasLockOn)
        {
            // スプライトを切り替え
            if (_aimController.hasLockOn && _lockOnSprite != null)
            {
                _reticleImage.sprite = _lockOnSprite;
                _reticleImage.color = Color.white;
            }
            else if (!_aimController.hasLockOn && _defaultSprite != null)
            {
                _reticleImage.sprite = _defaultSprite;
                _reticleImage.color = Color.white;
            }

            // 前フレームの状態を更新
            _wasLockedOn = _aimController.hasLockOn;
        }
    }
}
