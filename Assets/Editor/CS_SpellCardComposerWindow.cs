using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 複数の弾幕バリエーション(CS_BarragePatternプレハブ。CS_BarrageDesignerWindow等で
/// 個別に作成・プレハブ化された、シーン配置に依存しないもの)を、いつ・どんな角度で、
/// (任意で)どんな発射間隔で再生するかを設定して1つのスペルカード
/// (CS_SpellCardDefinitionアセット)にまとめるための専用エディタウィンドウ。
///
/// 「発射間隔を上書き」(2026-08-31追加): バリエーションプレハブ本体(CS_BulletEmitterの
/// _fireInterval)は編集せず、このスペルカードのこのエントリでだけ発射間隔を変えたい場合に
/// チェックを入れて秒数を指定する。実際の上書きはCS_SpellCardPlayer.Play()がInstantiate
/// 直後に、そのバリエーション配下の全CS_BulletEmitterへ一括で適用する。
/// チェックを入れた瞬間、「間隔(秒)」欄にはプレハブ本来の発射間隔(上書き前の値、
/// GetBaseFireIntervalで取得)が初期値として自動で入るので、そこから微調整すればよい。
///
/// CS_BarrageDesignerWindowが「1つのバリエーションの中身」を作るのに対し、
/// このウィンドウは「完成済みバリエーションをどう組み合わせるか」を扱う、一段上の
/// レイヤーのエディタ。編集対象はシーンのGameObjectではなくScriptableObjectアセット
/// (CS_SpellCardDefinition)なので、保存・再利用・差し替えがシーンの状態に左右されない。
///
/// Tools/東方3D弾幕/スペルカード作成 から開く。
/// Assets/Editor/ 以下に置くこと(Editor専用スクリプトのため)。
/// </summary>
public class CS_SpellCardComposerWindow : EditorWindow
{
    private CS_SpellCardDefinition _definition;
    private SerializedObject _definitionSO;

    private Vector2 _scroll;
    private float _previewTime;

    private enum DragMode { None, Move, ResizeLeft, ResizeRight }
    private int _draggingIndex = -1;
    private DragMode _dragMode = DragMode.None;
    private float _dragStartMouseX;
    private float _dragStartTime0;
    private float _dragStartTime1;

    private const string kDefaultDefinitionFolder = "Assets/Data/SpellCards";
    private const string kVariationSearchFolder = "Assets/Prefabs/BarragePatterns";
    private const float kRowHeight = 22f;

    [MenuItem("Tools/東方3D弾幕/スペルカード作成")]
    private static void Open()
    {
        var window = GetWindow<CS_SpellCardComposerWindow>("スペルカード作成");
        window.minSize = new Vector2(480, 420);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        DrawDefinitionSelector();

        if (_definition == null)
        {
            EditorGUILayout.HelpBox("CS_SpellCardDefinitionアセットを選択するか、上の「新規スペルカードを作成」から始めてください。", MessageType.Info);
            return;
        }

        if (_definitionSO == null || _definitionSO.targetObject != _definition)
        {
            _definitionSO = new SerializedObject(_definition);
        }
        _definitionSO.Update();

        SerializedProperty entriesProp = _definitionSO.FindProperty("entries");

        EditorGUILayout.Space();
        DrawAddEntryToolbar(entriesProp);

        EditorGUILayout.Space();
        float duration = ComputeDuration(entriesProp);
        Rect rulerRect = GUILayoutUtility.GetRect(position.width - 24, 18);
        DrawRuler(rulerRect, duration);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            if (!DrawEntry(entriesProp, i, duration)) break; // 削除された場合はこのフレームの描画を打ち切る
        }
        EditorGUILayout.EndScrollView();

        if (entriesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("エントリがありません。上の一覧からバリエーションプレハブのボタンを押して追加してください。", MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"プレビュー時間: {_previewTime:0.0}s / 全体: {duration:0.0}s", EditorStyles.miniLabel);
        _previewTime = EditorGUILayout.Slider(_previewTime, 0f, Mathf.Max(0.01f, duration));
        EditorGUILayout.HelpBox("Sceneビューでは、選択中のオブジェクト(スペルカードを再生する予定地)を原点として、各エントリの角度を矢印で表示します。", MessageType.None);

        _definitionSO.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_definition);
        }
    }

    private void DrawDefinitionSelector()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            var newDefinition = (CS_SpellCardDefinition)EditorGUILayout.ObjectField(_definition, typeof(CS_SpellCardDefinition), false, GUILayout.MinWidth(200));
            if (EditorGUI.EndChangeCheck())
            {
                _definition = newDefinition;
                _definitionSO = _definition != null ? new SerializedObject(_definition) : null;
            }

            if (GUILayout.Button("新規スペルカードを作成", EditorStyles.toolbarButton, GUILayout.Width(150)))
            {
                CreateNewDefinition();
            }
        }
    }

    private void CreateNewDefinition()
    {
        EnsureFolder(kDefaultDefinitionFolder);

        string path = EditorUtility.SaveFilePanelInProject(
            "スペルカードを新規作成",
            "NewSpellCard",
            "asset",
            "保存先のファイル名を選択してください",
            kDefaultDefinitionFolder);

        if (string.IsNullOrEmpty(path)) return;

        var asset = ScriptableObject.CreateInstance<CS_SpellCardDefinition>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        _definition = asset;
        _definitionSO = new SerializedObject(_definition);
        Selection.activeObject = asset;
    }

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

    /// <summary>Assets/Prefabs/BarragePatterns配下からCS_BarragePattern付きプレハブを探し、
    /// ボタン1つで追加できるようにする。</summary>
    private void DrawAddEntryToolbar(SerializedProperty entriesProp)
    {
        EditorGUILayout.LabelField("バリエーションを追加(クリックで末尾に追加):", EditorStyles.miniBoldLabel);

        var candidates = FindVariationPrefabs();
        if (candidates.Count == 0)
        {
            EditorGUILayout.HelpBox($"{kVariationSearchFolder} にCS_BarragePattern付きプレハブが見つかりません。先にCS_BarrageDesignerWindow等でバリエーションをプレハブ化してください。", MessageType.None);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var prefab in candidates)
            {
                if (GUILayout.Button(prefab.name, EditorStyles.miniButton))
                {
                    AddEntry(entriesProp, prefab);
                }
            }
        }
    }

    private List<GameObject> FindVariationPrefabs()
    {
        var results = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(kVariationSearchFolder)) return results;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { kVariationSearchFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<CS_BarragePattern>() != null)
            {
                results.Add(prefab);
            }
        }
        return results;
    }

    private void AddEntry(SerializedProperty entriesProp, GameObject prefab)
    {
        int index = entriesProp.arraySize;
        entriesProp.arraySize++;

        SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("variationPrefab").objectReferenceValue = prefab;
        entry.FindPropertyRelative("startTime").floatValue = 0f;
        entry.FindPropertyRelative("endTime").floatValue = 4f;
        entry.FindPropertyRelative("angle").vector3Value = Vector3.zero;
        entry.FindPropertyRelative("positionOffset").vector3Value = Vector3.zero;
        entry.FindPropertyRelative("overrideFireInterval").boolValue = false;
        entry.FindPropertyRelative("fireIntervalOverride").floatValue = GetBaseFireInterval(prefab);
    }

    /// <summary>プレハブ配下の最初のCS_BulletEmitterが持つ発射間隔(上書き前の本来の値)を返す。
    /// 「発射間隔を上書き」の初期値表示に使う。見つからない場合は0.2fを返す。</summary>
    private static float GetBaseFireInterval(GameObject prefab)
    {
        if (prefab == null) return 0.2f;
        var emitter = prefab.GetComponentInChildren<CS_BulletEmitter>(true);
        return emitter != null ? emitter.fireInterval : 0.2f;
    }

    private float ComputeDuration(SerializedProperty entriesProp)
    {
        float max = 4f;
        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            float end = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("endTime").floatValue;
            if (end > max) max = end;
        }
        return max;
    }

    private void DrawRuler(Rect rect, float duration)
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

    /// <summary>1エントリを描画する。削除ボタンが押された場合はfalseを返す(呼び出し側でループを打ち切る)。</summary>
    private bool DrawEntry(SerializedProperty entriesProp, int index, float duration)
    {
        SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);
        SerializedProperty prefabProp = entry.FindPropertyRelative("variationPrefab");
        SerializedProperty startProp = entry.FindPropertyRelative("startTime");
        SerializedProperty endProp = entry.FindPropertyRelative("endTime");
        SerializedProperty angleProp = entry.FindPropertyRelative("angle");
        SerializedProperty offsetProp = entry.FindPropertyRelative("positionOffset");
        SerializedProperty overrideIntervalProp = entry.FindPropertyRelative("overrideFireInterval");
        SerializedProperty intervalProp = entry.FindPropertyRelative("fireIntervalOverride");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(prefabProp, GUIContent.none, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    entriesProp.DeleteArrayElementAtIndex(index);
                    return false;
                }
            }

            Rect rowRect = GUILayoutUtility.GetRect(position.width - 40, kRowHeight);
            DrawTimeBar(rowRect, index, duration, startProp, endProp);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("開始", GUILayout.Width(30));
                EditorGUI.BeginChangeCheck();
                float newStart = EditorGUILayout.FloatField(startProp.floatValue, GUILayout.Width(50));
                EditorGUILayout.LabelField("終了", GUILayout.Width(30));
                float newEnd = EditorGUILayout.FloatField(endProp.floatValue, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck())
                {
                    float clampedStart = Mathf.Max(0f, newStart);
                    float clampedEnd = Mathf.Max(clampedStart + 0.05f, newEnd);
                    startProp.floatValue = clampedStart;
                    endProp.floatValue = clampedEnd;
                }
            }

            EditorGUILayout.PropertyField(angleProp, new GUIContent("角度(Euler)"));
            EditorGUILayout.PropertyField(offsetProp, new GUIContent("位置オフセット(任意)"));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(overrideIntervalProp, new GUIContent("発射間隔を上書き"), GUILayout.Width(160));
                if (EditorGUI.EndChangeCheck() && overrideIntervalProp.boolValue)
                {
                    // チェックを入れた瞬間、上書き前(プレハブ本来)の発射間隔を初期値として入れておく
                    intervalProp.floatValue = GetBaseFireInterval(prefabProp.objectReferenceValue as GameObject);
                }
                using (new EditorGUI.DisabledScope(!overrideIntervalProp.boolValue))
                {
                    EditorGUILayout.PropertyField(intervalProp, new GUIContent("間隔(秒)"));
                }
            }
        }

        return true;
    }

    private void DrawTimeBar(Rect barAreaRect, int index, float duration, SerializedProperty startProp, SerializedProperty endProp)
    {
        EditorGUI.DrawRect(barAreaRect, new Color(0.1f, 0.1f, 0.1f));

        float start = Mathf.Clamp(startProp.floatValue, 0f, duration);
        float end = Mathf.Clamp(endProp.floatValue, 0f, duration);
        float barX = barAreaRect.x + (start / duration) * barAreaRect.width;
        float barW = Mathf.Max(2f, ((end - start) / duration) * barAreaRect.width);
        Rect barRect = new Rect(barX, barAreaRect.y + 2, barW, barAreaRect.height - 4);

        EditorGUI.DrawRect(barRect, new Color(0.8f, 0.5f, 0.2f, 0.9f));

        HandleDrag(index, barRect, barAreaRect, duration, startProp, endProp);
    }

    private void HandleDrag(int index, Rect barRect, Rect barAreaRect, float duration, SerializedProperty startProp, SerializedProperty endProp)
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
            DragMode mode = DragMode.None;
            if (leftEdge.Contains(e.mousePosition)) mode = DragMode.ResizeLeft;
            else if (rightEdge.Contains(e.mousePosition)) mode = DragMode.ResizeRight;
            else if (barRect.Contains(e.mousePosition)) mode = DragMode.Move;

            if (mode != DragMode.None)
            {
                _draggingIndex = index;
                _dragMode = mode;
                _dragStartMouseX = e.mousePosition.x;
                _dragStartTime0 = startProp.floatValue;
                _dragStartTime1 = endProp.floatValue;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDrag && _draggingIndex == index && _dragMode != DragMode.None)
        {
            float deltaPixels = e.mousePosition.x - _dragStartMouseX;
            float deltaTime = barAreaRect.width > 0f ? (deltaPixels / barAreaRect.width) * duration : 0f;

            switch (_dragMode)
            {
                case DragMode.Move:
                    float length = _dragStartTime1 - _dragStartTime0;
                    float newStart = Mathf.Max(0f, _dragStartTime0 + deltaTime);
                    startProp.floatValue = newStart;
                    endProp.floatValue = newStart + length;
                    break;
                case DragMode.ResizeLeft:
                    startProp.floatValue = Mathf.Clamp(_dragStartTime0 + deltaTime, 0f, _dragStartTime1 - 0.05f);
                    break;
                case DragMode.ResizeRight:
                    endProp.floatValue = Mathf.Max(_dragStartTime0 + 0.05f, _dragStartTime1 + deltaTime);
                    break;
            }
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp && _draggingIndex == index)
        {
            _draggingIndex = -1;
            _dragMode = DragMode.None;
            e.Use();
        }
    }

    /// <summary>各エントリの角度を、選択中オブジェクトの位置を原点とした矢印でSceneビューに表示する
    /// (時間シミュレーションはしない、静的な向きの確認用)。</summary>
    private void OnSceneGUI(SceneView sceneView)
    {
        if (_definition == null || _definition.entries == null) return;

        Vector3 origin = Selection.activeTransform != null ? Selection.activeTransform.position : Vector3.zero;

        foreach (var entry in _definition.entries)
        {
            if (entry == null) continue;

            Vector3 worldPos = origin + entry.positionOffset;
            Quaternion rot = Quaternion.Euler(entry.angle);
            Vector3 dir = rot * Vector3.forward;

            float size = HandleUtility.GetHandleSize(worldPos) * 1.5f;
            Handles.color = new Color(0.9f, 0.6f, 0.2f, 0.9f);
            Handles.DrawLine(worldPos, worldPos + dir * size);
            Handles.ConeHandleCap(0, worldPos + dir * size, rot, size * 0.2f, EventType.Repaint);

            string label = entry.variationPrefab != null ? entry.variationPrefab.name : "(未設定)";
            Handles.Label(worldPos + Vector3.up * size * 0.3f, label);
        }
    }
}