using UnityEngine;

/// <summary>
/// 照準とロックオンのロジックを管理するコンポーネント。
/// 画面内の敵を自動追跡し、ロックオン状態と照準方向を提供します。
/// </summary>
public class CS_AimController : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private Transform _fireOrigin;

    /// <summary>
    /// 敵にロックオン中かどうかを示します。
    /// </summary>
    public bool hasLockOn { get; private set; }

    /// <summary>
    /// ロックオン対象の敵のTransform。ロックオン中でなければnull。
    /// </summary>
    public Transform lockedTarget { get; private set; }

    /// <summary>
    /// 現在の照準方向(ワールド空間、正規化済み)。
    /// </summary>
    public Vector3 aimDirection { get; private set; }

    private void OnEnable()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_fireOrigin == null)
        {
            _fireOrigin = transform;
        }

        aimDirection = _camera.transform.forward;
    }

    private void Update()
    {
        UpdateAim();
    }

    /// <summary>
    /// 毎フレーム照準情報を更新します。
    /// </summary>
    private void UpdateAim()
    {
        var targets = CS_EnemyTargetRegistry.activeTargets;

        // ビューポート内に映っている敵を検索
        CS_EnemyTarget closestTarget = null;
        float closestDistance = float.MaxValue;
        const float viewportCenterX = 0.5f;
        const float viewportCenterY = 0.5f;

        foreach (var target in targets)
        {
            // ビューポート座標に変換
            var viewportPos = _camera.WorldToViewportPoint(target.transform.position);

            // ビューポート内(0～1の範囲)かつカメラより前方にあるかを確認
            if (viewportPos.x >= 0f && viewportPos.x <= 1f &&
                viewportPos.y >= 0f && viewportPos.y <= 1f &&
                viewportPos.z > 0f)
            {
                // 画面中央からの距離を計算
                float distanceFromCenter = Vector2.Distance(
             new Vector2(viewportPos.x, viewportPos.y),
             new Vector2(viewportCenterX, viewportCenterY)
             );

                if (distanceFromCenter < closestDistance)
                {
                    closestDistance = distanceFromCenter;
                    closestTarget = target;
                }
            }
        }

        // ロックオン状態を更新
        if (closestTarget != null)
        {
            hasLockOn = true;
            lockedTarget = closestTarget.transform;
            aimDirection = (lockedTarget.position - _fireOrigin.position).normalized;
        }
        else
        {
            hasLockOn = false;
            lockedTarget = null;
            aimDirection = _camera.transform.forward;
        }
    }
}
