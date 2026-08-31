using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 弾幕パターン(CS_BarragePattern)を直感的に作成・調整するための統合エディタウィンドウ。
///
/// 機能:
///  1. サブGenerator一覧とパラメータ編集(Emitter/Rotator/Oscillator/Pathを選んでその場で調整)
///  2. 弾の発射方向プレビュー(CS_BulletEmitterの burstCount/burstAngleSpread を
///     Sceneビュー上にRayとして常時表示。数値を変えると即座に反映される)
///  3. キューシーケンサー(_cuesをガントチャート風のバーで視覚的に編集。ドラッグで
///     開始/終了時間を調整できる。Unity Timelineを使わない軽量な時間差演出の仕組み)
///  4. Editモード中のスクラブプレビュー(実際にPlayしなくても、時間スライダーを動かすと
///     Rotator/Oscillator/Pathの動きとキューによる有効/無効切り替えをシミュレートして
///     Sceneビュー上で確認できる。プレビュー開始時の状態を記録しておき、停止時に復元する)
///  5. 初回作成機能(新規パターンGameObjectの作成、サブGeneratorのワンクリック追加、
///     プレハブとしての保存)。ゼロから弾幕パターンを組み立てるときに、Hierarchy上での
///     手作業(空のGameObjectを作ってコンポーネントを付けて…)を省略できる
///
/// Tools/東方3D弾幕/弾幕デザイナー から開く。
/// Assets/Editor/ 以下に置くこと(Editor専用スクリプトのため)。
/// </summary>
public class CS_BarrageDesignerWindow : EditorWindow
{
    // ---- 選択状態 ----
    private CS_BarragePattern _pattern;
    private SerializedObject _patternSO;
    private GameObject _selectedComponentHolder;

    // ---- 初回作成 ----
    private string _newPatternName = "NewBarragePattern";
    private const string kDefaultPrefabFolder = "Assets/Prefabs/BarragePatterns";

    // ---- UIスクロール ----
    private Vector2 _leftScroll;
    private Vector2 _paramScroll;

    // ---- プレビュー ----
    private bool _isPreviewPlaying;
    private float _previewTime;
    private bool _previewLoop = true;
    private double _lastEditorTimeSinceStartup;
    private readonly Dictionary<Transform, PreviewTransformSnapshot> _previewSnapshots = new Dictionary<Transform, PreviewTransformSnapshot>();
    private readonly Dictionary<GameObject, bool> _previewActiveSnapshots = new Dictionary<GameObject, bool>();

    private struct PreviewTransformSnapshot
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    // ---- シーケンサー(ドラッグ操作) ----
    private enum CueDragMode { None, Move, ResizeLeft, ResizeRight }
    private int _draggingCueIndex = -1;
    private CueDragMode _cueDragMode = CueDragMode.None;
    private float _dragStartMouseX;
    private float _dragStartTime0;
    private float _dragStartTime1;

    private const float kPreviewStepDt = 0.02f;
    private const float kSequencerRowHeight = 22f;
    private const float kSequencerLabelWidth = 140f;

    [MenuItem("Tools/東方3D弾幕/弾幕デザイナー")]
    private static void Open()
    {
        var window = GetWindow<CS_BarrageDesignerWindow>("弾幕デザイナー");
        window.minSize = new Vector2(440, 560);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += OnEditorUpdate;
        TryAutoAssignFromSelection();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= OnEditorUpdate;
        if (_isPreviewPlaying || _previewSnapshots.Count > 0)
        {
            StopPreview();
        }
    }

    private void OnSelectionChange()
    {
        TryAutoAssignFromSelection();
        Repaint();
    }

    private void TryAutoAssignFromSelection()
    {
        if (Selection.activeGameObject == null) return;
        var pattern = Selection.activeGameObject.GetComponentInParent<CS_BarragePattern>();
        if (pattern != null && pattern != _pattern)
        {
            SetPattern(pattern);
        }
    }

    private void SetPattern(CS_BarragePattern pattern)
    {
        if (_isPreviewPlaying || _previewSnapshots.Count > 0)
        {
            StopPreview();
        }
        _pattern = pattern;
        _patternSO = pattern != null ? new SerializedObject(pattern) : null;
        _selectedComponentHolder = null;
        _previewTime = 0f;
    }

    // =========================================================================
    // OnGUI
    // =========================================================================
    private void OnGUI()
    {
        DrawPatternSelector();
        DrawCreationToolbar();

        if (_pattern == null)
        {
            _patternSO = null;
            EditorGUILayout.HelpBox("CS_BarragePatternが付いたGameObjectを選択するか、上のフィールドに指定するか、上の「新規パターンを作成」から始めてください。", MessageType.Info);
            return;
        }

        if (_patternSO == null || _patternSO.targetObject != _pattern)
        {
            _patternSO = new SerializedObject(_pattern);
        }
        _patternSO.Update();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSubGeneratorList();
            DrawParameterPanel();
        }

        EditorGUILayout.Space();
        DrawSequencer();

        EditorGUILayout.Space();
        DrawPreviewControls();

        _patternSO.ApplyModifiedProperties();
    }

    private void DrawPatternSelector()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            var newPattern = (CS_BarragePattern)EditorGUILayout.ObjectField(_pattern, typeof(CS_BarragePattern), true, GUILayout.MinWidth(200));
            if (EditorGUI.EndChangeCheck())
            {
                SetPattern(newPattern);
            }
            if (GUILayout.Button("選択中から設定", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                TryAutoAssignFromSelection();
            }
        }
    }

    // ---- 初回作成 ----
    private void DrawCreationToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            _newPatternName = EditorGUILayout.TextField(_newPatternName, GUILayout.MinWidth(120));
            if (GUILayout.Button("新規パターンを作成", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                CreateNewPattern(_newPatternName);
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_pattern == null))
            {
                if (GUILayout.Button("プレハブとして保存", EditorStyles.toolbarButton, GUILayout.Width(130)))
                {
                    SaveAsPrefab();
                }
            }
        }
    }

    /// <summary>空のGameObjectを作ってCS_BarragePatternを付け、そのままデザイナーウィンドウの対象にする。</summary>
    private void CreateNewPattern(string patternName)
    {
        string name = string.IsNullOrWhiteSpace(patternName) ? "NewBarragePattern" : patternName;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Barrage Pattern");
        var pattern = Undo.AddComponent<CS_BarragePattern>(go);

        Selection.activeGameObject = go;
        SetPattern(pattern);
    }

    /// <summary>現在のパターンGameObjectをAssets/Prefabs/BarragePatterns以下にプレハブとして保存し、シーン上のインスタンスと接続する。</summary>
    private void SaveAsPrefab()
    {
        if (_pattern == null) return;

        EnsureFolder(kDefaultPrefabFolder);

        string path = EditorUtility.SaveFilePanelInProject(
            "弾幕パターンをプレハブとして保存",
            _pattern.gameObject.name,
            "prefab",
            "保存先のファイル名を選択してください",
            kDefaultPrefabFolder);

        if (string.IsNullOrEmpty(path)) return;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(_pattern.gameObject, path, InteractionMode.UserAction);
        if (savedPrefab != null)
        {
            Debug.Log($"弾幕パターンをプレハブとして保存しました: {path}", savedPrefab);
        }
        else
        {
            Debug.LogWarning("プレハブの保存に失敗しました。");
        }
    }

    /// <summary>folder(例: "Assets/Prefabs/BarragePatterns")までのフォルダを、無ければ階層ごと作成する。</summary>
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    // ---- サブGenerator一覧 ----
    private void DrawSubGeneratorList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(200)))
        {
            EditorGUILayout.LabelField("サブGenerator", EditorStyles.boldLabel);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUILayout.Height(180));

            DrawSubGeneratorGroup<CS_BulletEmitter>("Emitter");
            DrawSubGeneratorGroup<CS_GeneratorRotator>("Rotator");
            DrawSubGeneratorGroup<CS_GeneratorOscillator>("Oscillator");
            DrawSubGeneratorGroup<CS_GeneratorPath>("Path");

            EditorGUILayout.EndScrollView();

            DrawCreateSubGeneratorButtons();
        }
    }

    /// <summary>Emitter/Rotator/Oscillator/Pathをワンクリックで新規追加するボタン群。
    /// 追加先の親は、サブGeneratorを選択中ならそのGameObject、未選択ならパターンのルート。</summary>
    private void DrawCreateSubGeneratorButtons()
    {
        EditorGUILayout.LabelField("追加", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+Emitter", EditorStyles.miniButton))
            {
                CreateSubGenerator<CS_BulletEmitter>("Muzzle");
            }
            if (GUILayout.Button("+Rotator", EditorStyles.miniButton))
            {
                CreateSubGenerator<CS_GeneratorRotator>("Rotator");
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+Oscillator", EditorStyles.miniButton))
            {
                CreateSubGenerator<CS_GeneratorOscillator>("Oscillator");
            }
            if (GUILayout.Button("+Path", EditorStyles.miniButton))
            {
                CreateSubGenerator<CS_GeneratorPath>("Path");
            }
        }

        string parentName = _selectedComponentHolder != null ? _selectedComponentHolder.name : _pattern.gameObject.name;
        EditorGUILayout.LabelField($"追加先: {parentName}", EditorStyles.miniLabel);
    }

    /// <summary>新しい子GameObjectを作り、型Tのコンポーネントを付けて選択状態にする。</summary>
    private void CreateSubGenerator<T>(string defaultName) where T : Component
    {
        if (_pattern == null) return;

        GameObject parent = _selectedComponentHolder != null ? _selectedComponentHolder : _pattern.gameObject;

        var go = new GameObject(defaultName);
        Undo.RegisterCreatedObjectUndo(go, "Create " + defaultName);
        Undo.SetTransformParent(go.transform, parent.transform, "Create " + defaultName);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Undo.AddComponent<T>(go);

        _selectedComponentHolder = go;
        Selection.activeGameObject = go;
    }

    private void DrawSubGeneratorGroup<T>(string label) where T : Component
    {
        var components = _pattern.GetComponentsInChildren<T>(true);
        if (components.Length == 0) return;

        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        foreach (var component in components)
        {
            bool isSelected = _selectedComponentHolder == component.gameObject;
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? new Color(0.6f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(component.gameObject.name, EditorStyles.miniButton))
            {
                _selectedComponentHolder = component.gameObject;
                Selection.activeGameObject = component.gameObject;
            }
            GUI.backgroundColor = prevColor;
        }
    }

    // ---- パラメータパネル ----
    private void DrawParameterPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("パラメータ", EditorStyles.boldLabel);

            if (_selectedComponentHolder == null)
            {
                EditorGUILayout.HelpBox("左のリストからサブGeneratorを選んでください。", MessageType.None);
                return;
            }

            _paramScroll = EditorGUILayout.BeginScrollView(_paramScroll, GUILayout.Height(180));

            DrawGenericComponentFields<CS_BulletEmitter>();
            DrawGenericComponentFields<CS_GeneratorRotator>();
            DrawGenericComponentFields<CS_GeneratorOscillator>();
            DrawGenericComponentFields<CS_GeneratorPath>();

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawGenericComponentFields<T>() where T : Component
    {
        if (_selectedComponentHolder == null) return;
        var component = _selectedComponentHolder.GetComponent<T>();
        if (component == null) return;

        EditorGUILayout.LabelField(typeof(T).Name, EditorStyles.miniBoldLabel);

        var so = new SerializedObject(component);
        so.Update();
        SerializedProperty prop = so.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.propertyPath == "m_Script") continue;
            EditorGUILayout.PropertyField(prop, true);
        }
        so.ApplyModifiedProperties();

        EditorGUILayout.Space(4);
    }

    // =========================================================================
    // シーケンサー(キュー)
    // =========================================================================
    private void DrawSequencer()
    {
        EditorGUILayout.LabelField("キューシーケンサー(サブGeneratorの有効/無効を時間で切り替え)", EditorStyles.boldLabel);

        SerializedProperty durationProp = _patternSO.FindProperty("_duration");
        EditorGUILayout.PropertyField(durationProp, new GUIContent("パターン全体の長さ(秒)"));
        float duration = Mathf.Max(0.01f, durationProp.floatValue);

        SerializedProperty cuesProp = _patternSO.FindProperty("_cues");

        if (GUILayout.Button("＋ 選択中のGeneratorをキューに追加", GUILayout.Width(240)))
        {
            AddCueForSelected(cuesProp, duration);
        }

        Rect rulerRect = GUILayoutUtility.GetRect(position.width - 24, 18);
        DrawSequencerRuler(rulerRect, duration);

        for (int i = 0; i < cuesProp.arraySize; i++)
        {
            DrawCueRow(cuesProp, i, duration);
        }

        if (cuesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("キューがありません。左のリストでサブGeneratorを選び、「キューに追加」を押してください。", MessageType.None);
        }
    }

    private void AddCueForSelected(SerializedProperty cuesProp, float duration)
    {
        if (_selectedComponentHolder == null)
        {
            Debug.LogWarning("キューを追加するサブGeneratorが選択されていません。左のリストから選んでください。");
            return;
        }

        int index = cuesProp.arraySize;
        cuesProp.arraySize++;
        SerializedProperty newCueProp = cuesProp.GetArrayElementAtIndex(index);
        newCueProp.FindPropertyRelative("targetGenerator").objectReferenceValue = _selectedComponentHolder;
        newCueProp.FindPropertyRelative("startTime").floatValue = 0f;
        newCueProp.FindPropertyRelative("endTime").floatValue = duration;
    }

    private void DrawSequencerRuler(Rect rect, float duration)
    {
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));

        int tickCount = Mathf.Max(1, Mathf.RoundToInt(duration));
        for (int i = 0; i <= tickCount; i++)
        {
            float t = Mathf.Min(i, duration);
            float x = rect.x + (t / duration) * rect.width;
            EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), new Color(0.45f, 0.45f, 0.45f));
            GUI.Label(new Rect(x + 2, rect.y, 40, rect.height), t.ToString("0.#") + "s", EditorStyles.miniLabel);
            if (t >= duration) break;
        }

        float playheadX = rect.x + Mathf.Clamp01(_previewTime / duration) * rect.width;
        EditorGUI.DrawRect(new Rect(playheadX, rect.y, 2, rect.height), Color.red);
    }

    private void DrawCueRow(SerializedProperty cuesProp, int index, float duration)
    {
        SerializedProperty cueProp = cuesProp.GetArrayElementAtIndex(index);
        SerializedProperty targetProp = cueProp.FindPropertyRelative("targetGenerator");
        SerializedProperty startProp = cueProp.FindPropertyRelative("startTime");
        SerializedProperty endProp = cueProp.FindPropertyRelative("endTime");

        Rect rowRect = GUILayoutUtility.GetRect(position.width - 24, kSequencerRowHeight);
        Rect labelRect = new Rect(rowRect.x, rowRect.y, kSequencerLabelWidth, rowRect.height);
        Rect barAreaRect = new Rect(rowRect.x + kSequencerLabelWidth, rowRect.y, rowRect.width - kSequencerLabelWidth - 22, rowRect.height);
        Rect removeButtonRect = new Rect(rowRect.xMax - 20, rowRect.y + 2, 18, rowRect.height - 4);

        string targetName = targetProp.objectReferenceValue != null ? targetProp.objectReferenceValue.name : "(未設定)";
        GUI.Label(labelRect, targetName, EditorStyles.miniLabel);

        EditorGUI.DrawRect(barAreaRect, new Color(0.1f, 0.1f, 0.1f));

        float start = Mathf.Clamp(startProp.floatValue, 0f, duration);
        float end = Mathf.Clamp(endProp.floatValue, 0f, duration);
        float barX = barAreaRect.x + (start / duration) * barAreaRect.width;
        float barW = Mathf.Max(2f, ((end - start) / duration) * barAreaRect.width);
        Rect barRect = new Rect(barX, barAreaRect.y + 2, barW, barAreaRect.height - 4);

        EditorGUI.DrawRect(barRect, new Color(0.3f, 0.65f, 0.9f, 0.9f));

        HandleCueDrag(index, barRect, barAreaRect, duration, startProp, endProp);

        if (GUI.Button(removeButtonRect, "×"))
        {
            cuesProp.DeleteArrayElementAtIndex(index);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(kSequencerLabelWidth);
            EditorGUILayout.LabelField("開始", GUILayout.Width(30));
            EditorGUI.BeginChangeCheck();
            float newStart = EditorGUILayout.FloatField(startProp.floatValue, GUILayout.Width(50));
            EditorGUILayout.LabelField("終了", GUILayout.Width(30));
            float newEnd = EditorGUILayout.FloatField(endProp.floatValue, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                float clampedStart = Mathf.Clamp(newStart, 0f, duration);
                float clampedEnd = Mathf.Clamp(newEnd, clampedStart, duration);
                startProp.floatValue = clampedStart;
                endProp.floatValue = clampedEnd;
            }
        }
    }

    private void HandleCueDrag(int index, Rect barRect, Rect barAreaRect, float duration, SerializedProperty startProp, SerializedProperty endProp)
    {
        Event e = Event.current;
        const float edgeGrabWidth = 6f;

        Rect leftEdge = new Rect(barRect.x - edgeGrabWidth * 0.5f, barRect.y, edgeGrabWidth, barRect.height);
        Rect rightEdge = new Rect(barRect.xMax - edgeGrabWidth * 0.5f, barRect.y, edgeGrabWidth, barRect.height);

        EditorGUIUtility.AddCursorRect(barRect, MouseCursor.MoveArrow);
        EditorGUIUtility.AddCursorRect(leftEdge, MouseCursor.ResizeHorizontal);
        EditorGUIUtility.AddCursorRect(rightEdge, MouseCursor.ResizeHorizontal);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            CueDragMode mode = CueDragMode.None;
            if (leftEdge.Contains(e.mousePosition)) mode = CueDragMode.ResizeLeft;
            else if (rightEdge.Contains(e.mousePosition)) mode = CueDragMode.ResizeRight;
            else if (barRect.Contains(e.mousePosition)) mode = CueDragMode.Move;

            if (mode != CueDragMode.None)
            {
                _draggingCueIndex = index;
                _cueDragMode = mode;
                _dragStartMouseX = e.mousePosition.x;
                _dragStartTime0 = startProp.floatValue;
                _dragStartTime1 = endProp.floatValue;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDrag && _draggingCueIndex == index && _cueDragMode != CueDragMode.None)
        {
            float deltaPixels = e.mousePosition.x - _dragStartMouseX;
            float deltaTime = barAreaRect.width > 0f ? (deltaPixels / barAreaRect.width) * duration : 0f;

            switch (_cueDragMode)
            {
                case CueDragMode.Move:
                    float length = _dragStartTime1 - _dragStartTime0;
                    float newStart = Mathf.Clamp(_dragStartTime0 + deltaTime, 0f, duration - length);
                    startProp.floatValue = newStart;
                    endProp.floatValue = newStart + length;
                    break;
                case CueDragMode.ResizeLeft:
                    startProp.floatValue = Mathf.Clamp(_dragStartTime0 + deltaTime, 0f, _dragStartTime1 - 0.05f);
                    break;
                case CueDragMode.ResizeRight:
                    endProp.floatValue = Mathf.Clamp(_dragStartTime1 + deltaTime, _dragStartTime0 + 0.05f, duration);
                    break;
            }
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp && _draggingCueIndex == index)
        {
            _draggingCueIndex = -1;
            _cueDragMode = CueDragMode.None;
            e.Use();
        }
    }

    // =========================================================================
    // プレビュー
    // =========================================================================
    private void DrawPreviewControls()
    {
        EditorGUILayout.LabelField("プレビュー(Editモード中に時間経過をシミュレート)", EditorStyles.boldLabel);

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Playモード中はプレビューできません。実際の動作はゲームを再生して確認してください。", MessageType.Info);
            return;
        }

        float duration = Mathf.Max(0.01f, _patternSO.FindProperty("_duration").floatValue);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(_isPreviewPlaying ? "■ 停止" : "▶ 再生", GUILayout.Width(70)))
            {
                if (_isPreviewPlaying) StopPreview();
                else StartPreview();
            }
            if (GUILayout.Button("先頭へ", GUILayout.Width(60)))
            {
                _previewTime = 0f;
                if (_previewSnapshots.Count > 0) ApplyPreviewAtTime(_previewTime);
                SceneView.RepaintAll();
            }
            _previewLoop = GUILayout.Toggle(_previewLoop, "ループ", GUILayout.Width(60));
        }

        EditorGUI.BeginChangeCheck();
        float newTime = EditorGUILayout.Slider(_previewTime, 0f, duration);
        if (EditorGUI.EndChangeCheck())
        {
            _previewTime = newTime;
            if (_previewSnapshots.Count == 0) CapturePreviewSnapshots();
            ApplyPreviewAtTime(_previewTime);
            SceneView.RepaintAll();
        }

        if (!_isPreviewPlaying && _previewSnapshots.Count > 0)
        {
            if (GUILayout.Button("プレビューを終了して元の状態に戻す"))
            {
                RestorePreviewSnapshots();
            }
        }
    }

    private void StartPreview()
    {
        if (_pattern == null) return;
        if (_previewSnapshots.Count == 0) CapturePreviewSnapshots();
        _isPreviewPlaying = true;
        _lastEditorTimeSinceStartup = EditorApplication.timeSinceStartup;
    }

    private void StopPreview()
    {
        _isPreviewPlaying = false;
        RestorePreviewSnapshots();
    }

    private void CapturePreviewSnapshots()
    {
        _previewSnapshots.Clear();
        _previewActiveSnapshots.Clear();

        if (_pattern == null) return;

        foreach (var rotator in _pattern.GetComponentsInChildren<CS_GeneratorRotator>(true))
        {
            CaptureTransformSnapshot(rotator.transform);
        }
        foreach (var oscillator in _pattern.GetComponentsInChildren<CS_GeneratorOscillator>(true))
        {
            CaptureTransformSnapshot(oscillator.transform);
        }
        foreach (var path in _pattern.GetComponentsInChildren<CS_GeneratorPath>(true))
        {
            CaptureTransformSnapshot(path.transform);
        }

        if (_patternSO != null)
        {
            SerializedProperty cuesProp = _patternSO.FindProperty("_cues");
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                var target = cuesProp.GetArrayElementAtIndex(i).FindPropertyRelative("targetGenerator").objectReferenceValue as GameObject;
                if (target != null && !_previewActiveSnapshots.ContainsKey(target))
                {
                    _previewActiveSnapshots[target] = target.activeSelf;
                }
            }
        }
    }

    private void CaptureTransformSnapshot(Transform t)
    {
        if (_previewSnapshots.ContainsKey(t)) return;
        _previewSnapshots[t] = new PreviewTransformSnapshot
        {
            localPosition = t.localPosition,
            localRotation = t.localRotation
        };
    }

    private void RestorePreviewSnapshots()
    {
        foreach (var kvp in _previewSnapshots)
        {
            if (kvp.Key == null) continue;
            kvp.Key.localPosition = kvp.Value.localPosition;
            kvp.Key.localRotation = kvp.Value.localRotation;
        }
        foreach (var kvp in _previewActiveSnapshots)
        {
            if (kvp.Key == null) continue;
            kvp.Key.SetActive(kvp.Value);
        }
        _previewSnapshots.Clear();
        _previewActiveSnapshots.Clear();
        SceneView.RepaintAll();
    }

    private void OnEditorUpdate()
    {
        if (!_isPreviewPlaying) return;
        if (EditorApplication.isPlaying)
        {
            StopPreview();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - _lastEditorTimeSinceStartup);
        _lastEditorTimeSinceStartup = now;

        float duration = (_patternSO != null) ? Mathf.Max(0.01f, _patternSO.FindProperty("_duration").floatValue) : 1f;

        _previewTime += dt;
        if (_previewLoop)
        {
            _previewTime %= duration;
        }
        else if (_previewTime > duration)
        {
            _previewTime = duration;
            ApplyPreviewAtTime(_previewTime);
            StopPreview();
            SceneView.RepaintAll();
            Repaint();
            return;
        }

        ApplyPreviewAtTime(_previewTime);
        SceneView.RepaintAll();
        Repaint();
    }

    private void ApplyPreviewAtTime(float time)
    {
        if (_pattern == null || _patternSO == null) return;

        foreach (var rotator in _pattern.GetComponentsInChildren<CS_GeneratorRotator>(true))
        {
            if (!_previewSnapshots.TryGetValue(rotator.transform, out var snapshot)) continue;
            Vector3 angularVelocity = GetSerializedVector3(rotator, "_angularVelocity");
            rotator.transform.localRotation = SimulateRotatorRotation(snapshot.localRotation, angularVelocity, time);
        }

        foreach (var oscillator in _pattern.GetComponentsInChildren<CS_GeneratorOscillator>(true))
        {
            if (!_previewSnapshots.TryGetValue(oscillator.transform, out var snapshot)) continue;
            Vector3 axis = GetSerializedVector3(oscillator, "_oscillationAxis");
            float amplitude = GetSerializedFloat(oscillator, "_amplitudeDegrees");
            float frequency = GetSerializedFloat(oscillator, "_frequency");
            oscillator.transform.localRotation = SimulateOscillatorRotation(snapshot.localRotation, axis, amplitude, frequency, time);
        }

        foreach (var path in _pattern.GetComponentsInChildren<CS_GeneratorPath>(true))
        {
            if (!_previewSnapshots.ContainsKey(path.transform)) continue;

            var so = new SerializedObject(path);
            SerializedProperty waypointsProp = so.FindProperty("_localWaypoints");
            if (waypointsProp.arraySize < 2) continue;

            var waypoints = new Vector3[waypointsProp.arraySize];
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = waypointsProp.GetArrayElementAtIndex(i).vector3Value;
            }
            bool loop = so.FindProperty("_loop").boolValue;
            bool alignToDirection = so.FindProperty("_alignToDirection").boolValue;
            float pathDuration = Mathf.Max(0.01f, so.FindProperty("_duration").floatValue);

            float t = time / pathDuration;
            t = loop ? t % 1f : Mathf.Clamp01(t);

            Vector3 localPos = CS_GeneratorPath.EvaluatePathStatic(waypoints, loop, t, out Vector3 direction);
            path.transform.localPosition = localPos;
            if (alignToDirection && direction.sqrMagnitude > 0.0001f)
            {
                path.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }

        SerializedProperty cuesProp = _patternSO.FindProperty("_cues");
        for (int i = 0; i < cuesProp.arraySize; i++)
        {
            SerializedProperty cueProp = cuesProp.GetArrayElementAtIndex(i);
            var target = cueProp.FindPropertyRelative("targetGenerator").objectReferenceValue as GameObject;
            if (target == null) continue;
            float start = cueProp.FindPropertyRelative("startTime").floatValue;
            float end = cueProp.FindPropertyRelative("endTime").floatValue;
            bool shouldBeActive = time >= start && time < end;
            if (target.activeSelf != shouldBeActive)
            {
                target.SetActive(shouldBeActive);
            }
        }
    }

    /// <summary>
    /// CS_GeneratorRotatorのUpdate()(transform.Rotate(_angularVelocity * dt, Space.Self)の毎フレーム蓄積)を
    /// 固定ステップで再現することで、実際のランタイム挙動とプレビューを一致させる。
    /// </summary>
    private static Quaternion SimulateRotatorRotation(Quaternion baseLocalRotation, Vector3 angularVelocity, float time)
    {
        if (time <= 0f) return baseLocalRotation;

        Quaternion rotation = baseLocalRotation;
        int steps = Mathf.Max(1, Mathf.CeilToInt(time / kPreviewStepDt));
        float dt = time / steps;
        for (int i = 0; i < steps; i++)
        {
            rotation *= Quaternion.Euler(angularVelocity * dt);
        }
        return rotation;
    }

    /// <summary>CS_GeneratorOscillatorの数式(サインカーブ)は閉形式なのでそのまま評価する。</summary>
    private static Quaternion SimulateOscillatorRotation(Quaternion baseLocalRotation, Vector3 axis, float amplitudeDegrees, float frequency, float time)
    {
        float angle = Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitudeDegrees;
        return baseLocalRotation * Quaternion.AngleAxis(angle, axis);
    }

    private static Vector3 GetSerializedVector3(Component component, string fieldName)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(fieldName);
        return prop != null ? prop.vector3Value : Vector3.zero;
    }

    private static float GetSerializedFloat(Component component, string fieldName)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(fieldName);
        return prop != null ? prop.floatValue : 0f;
    }

    // =========================================================================
    // Sceneビュー: 弾の発射方向プレビュー(常時表示、CS_BulletEmitter.Fire()と同じ角度計算)
    // =========================================================================
    private void OnSceneGUI(SceneView sceneView)
    {
        if (_pattern == null) return;

        foreach (var emitter in _pattern.GetComponentsInChildren<CS_BulletEmitter>(true))
        {
            DrawEmitterPreview(emitter);
        }
    }

    private void DrawEmitterPreview(CS_BulletEmitter emitter)
    {
        var so = new SerializedObject(emitter);
        int burstCount = so.FindProperty("_burstCount").intValue;
        float burstAngleSpread = so.FindProperty("_burstAngleSpread").floatValue;

        Transform t = emitter.transform;
        float rayLength = HandleUtility.GetHandleSize(t.position) * 2.5f;

        Handles.color = new Color(1f, 0.5f, 0.2f, 0.9f);

        if (burstCount <= 1)
        {
            Handles.DrawLine(t.position, t.position + t.forward * rayLength);
            return;
        }

        float startAngle = -burstAngleSpread * 0.5f;
        float step = burstCount > 1 ? burstAngleSpread / (burstCount - 1) : 0f;

        for (int i = 0; i < burstCount; i++)
        {
            float angle = startAngle + step * i;
            Vector3 direction = Quaternion.AngleAxis(angle, t.up) * t.forward;
            Handles.DrawLine(t.position, t.position + direction * rayLength);
        }
    }
}