using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;

/// <summary>
/// SpellCard01サンプルパターンを自動生成するEditor拡張。
/// Spiral01と同じテスト用弾を流用し、3つのサブGeneratorを持つパターンを作成する。
/// TimelineAssetを生成し、ActivationTrackで各Generatorの有効化タイミングを制御。
///
/// 修正版(2026-08-31): 以下の不具合を修正
///  1. AddActivationClipが未実装だった(クリップが1つも作られていなかった)
///  2. CS_BulletEmitterに存在しない可能性のある"_bulletPool"フィールドへ
///     SerializedPropertyの存在確認なしに書き込んでいた(NullReferenceExceptionの原因)。
///     CS_BulletEmitterは静的シングルトンCS_BulletPool.instanceを使う設計のため、
///     この明示的な代入自体が不要なので削除した
///  3. CS_BarragePatternに[RequireComponent(typeof(PlayableDirector))]が付いている場合、
///     root.AddComponent&lt;CS_BarragePattern&gt;()の時点で暗黙にPlayableDirectorが
///     追加されるため、その後のAddComponent&lt;PlayableDirector&gt;()が二重追加になっていた
///  4. すべてのFindProperty呼び出しを、見つからない場合に警告を出すだけで例外を投げない
///     安全なヘルパー(TrySetField)経由に統一した
/// </summary>
public class CS_SpellCard01PatternBuilder
{
    private const string MenuItemPath = "Tools/東方3D弾幕/Create Sample Pattern (SpellCard01)";
    private const string PrefabDir = "Assets/Prefabs/BarragePatterns";
    private const string DataDir = "Assets/Data/Bullets";
    private const string TimelineDir = "Assets/Timelines";
    private const string BulletTestPrefabPath = "Assets/Prefabs/Bullet/Bullet.prefab";
    private const string BulletDataAssetPath = "Assets/Data/Bullets/CS_BulletData_Test.asset";
    private const string SpellCardGeneratorPath = "Assets/Prefabs/BarragePatterns/SpellCard01_Generator.prefab";
    private const string TimelineAssetPath = "Assets/Timelines/CS_SpellCard01_Timeline.playable";

    [MenuItem(MenuItemPath)]
    public static void CreateSpellCardPattern()
    {
        CreateFoldersIfNeeded();

        GameObject bulletPrefab = CreateOrGetBulletTestPrefab();
        CSO_BulletData bulletData = CreateOrGetBulletData(bulletPrefab);

        CreateSpellCardGeneratorPrefab(bulletData);

        EditorUtility.DisplayDialog("Success", "SpellCard01 pattern created successfully!", "OK");
    }

    private static void CreateFoldersIfNeeded()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "BarragePatterns");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Bullet"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Bullet");
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(DataDir))
            AssetDatabase.CreateFolder("Assets/Data", "Bullets");
        if (!AssetDatabase.IsValidFolder(TimelineDir))
            AssetDatabase.CreateFolder("Assets", "Timelines");
    }

    private static GameObject CreateOrGetBulletTestPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BulletTestPrefabPath);
        if (existing != null)
            return existing;

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "CS_Bullet_Test";
        bullet.transform.localScale = Vector3.one * 0.3f;

        SphereCollider collider = bullet.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
            collider.radius = 0.15f;
        }

        MeshRenderer renderer = bullet.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(Random.value, Random.value, Random.value, 1f);
            renderer.material = mat;
        }

        if (bullet.GetComponent<CS_Bullet>() == null)
        {
            bullet.AddComponent<CS_Bullet>();
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bullet.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;

        PrefabUtility.SaveAsPrefabAsset(bullet, BulletTestPrefabPath, out bool success);
        if (!success)
        {
            EditorUtility.DisplayDialog("Error", "Failed to save bullet prefab", "OK");
            GameObject.DestroyImmediate(bullet);
            return null;
        }
        GameObject.DestroyImmediate(bullet);
        return AssetDatabase.LoadAssetAtPath<GameObject>(BulletTestPrefabPath);
    }

    private static CSO_BulletData CreateOrGetBulletData(GameObject bulletPrefab)
    {
        CSO_BulletData existing = AssetDatabase.LoadAssetAtPath<CSO_BulletData>(BulletDataAssetPath);
        if (existing != null)
            return existing;

        CSO_BulletData data = ScriptableObject.CreateInstance<CSO_BulletData>();
        data.speed = 15f;
        data.lifetime = 3f;
        data.hitRadius = 0.15f;
        data.damage = 1f;
        data.prefab = bulletPrefab;

        AssetDatabase.CreateAsset(data, BulletDataAssetPath);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<CSO_BulletData>(BulletDataAssetPath);
    }

    private static void CreateSpellCardGeneratorPrefab(CSO_BulletData bulletData)
    {
        if (bulletData == null)
        {
            Debug.LogError("CreateSpellCardGeneratorPrefab: bulletDataがnullです。弾データ/弾プレハブの作成に失敗している可能性があります。処理を中止します。");
            return;
        }

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(SpellCardGeneratorPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(SpellCardGeneratorPath);
        }

        TimelineAsset existingTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelineAssetPath);
        if (existingTimeline != null)
        {
            AssetDatabase.DeleteAsset(TimelineAssetPath);
        }

        GameObject root = new GameObject("SpellCard01_Generator");

        // CS_BarragePatternを追加(_duration = 12秒)
        // 注意: CS_BarragePatternに[RequireComponent(typeof(PlayableDirector))]が付いている場合、
        // この時点で暗黙にPlayableDirectorも追加される
        CS_BarragePattern pattern = root.AddComponent<CS_BarragePattern>();
        SerializedObject patternSo = new SerializedObject(pattern);
        TrySetField(patternSo, "_duration", 12f);

        // TimelineAssetを作成
        TimelineAsset timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timelineAsset, TimelineAssetPath);

        // PlayableDirectorを取得(CS_BarragePatternのRequireComponentで既に追加済みの可能性があるため
        // AddComponentで二重追加しない)
        PlayableDirector director = root.GetComponent<PlayableDirector>();
        if (director == null)
        {
            director = root.AddComponent<PlayableDirector>();
        }
        director.playOnAwake = false;
        director.playableAsset = timelineAsset;

        // 3つのサブGenerator構造を作成
        GameObject ringBurstGen = CreateRingBurstGenerator(root, bulletData);
        GameObject spiralGen = CreateSpiralGenerator(root, bulletData);
        GameObject sweepFanGen = CreateSweepFanGenerator(root, bulletData);

        // ActivationTrackを作成して各Generatorをバインド
        SetupActivationTracks(director, timelineAsset, ringBurstGen, spiralGen, sweepFanGen);

        AssetDatabase.SaveAssets();

        PrefabUtility.SaveAsPrefabAsset(root, SpellCardGeneratorPath, out bool success);
        if (!success)
        {
            EditorUtility.DisplayDialog("Error", "Failed to save SpellCard01 prefab", "OK");
        }
        GameObject.DestroyImmediate(root);

        AssetDatabase.Refresh();
    }

    private static GameObject CreateRingBurstGenerator(GameObject parent, CSO_BulletData bulletData)
    {
        GameObject gen = new GameObject("RingBurst_Generator");
        gen.transform.SetParent(parent.transform);
        gen.transform.localPosition = Vector3.zero;
        gen.SetActive(false);

        CS_BulletEmitter emitter = gen.AddComponent<CS_BulletEmitter>();
        SerializedObject so = new SerializedObject(emitter);
        TrySetField(so, "_bulletData", bulletData);
        TrySetField(so, "_fireInterval", 1.0f);
        TrySetField(so, "_autoFire", true);
        TrySetField(so, "_burstCount", 16);
        TrySetField(so, "_burstAngleSpread", 360f);
        // 注意: CS_BulletEmitterは静的シングルトンCS_BulletPool.instanceを使う設計のため、
        // ここでの明示的なプール参照設定は不要(お手元のCS_BulletEmitterがまだ旧版で
        // _bulletPoolフィールドを持つ場合は、そちらを先にシングルトン方式へ更新してください)

        return gen;
    }

    private static GameObject CreateSpiralGenerator(GameObject parent, CSO_BulletData bulletData)
    {
        GameObject gen = new GameObject("Spiral_Generator");
        gen.transform.SetParent(parent.transform);
        gen.transform.localPosition = Vector3.zero;
        gen.SetActive(false);

        CS_GeneratorRotator rotator = gen.AddComponent<CS_GeneratorRotator>();
        SerializedObject rotatorSo = new SerializedObject(rotator);
        TrySetField(rotatorSo, "_angularVelocity", new Vector3(0, 180, 0));

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(gen.transform);
        muzzle.transform.localPosition = new Vector3(0, 0, 0.5f);

        CS_BulletEmitter emitter = muzzle.AddComponent<CS_BulletEmitter>();
        SerializedObject emitterSo = new SerializedObject(emitter);
        TrySetField(emitterSo, "_bulletData", bulletData);
        TrySetField(emitterSo, "_fireInterval", 0.08f);
        TrySetField(emitterSo, "_autoFire", true);
        TrySetField(emitterSo, "_burstCount", 1);
        TrySetField(emitterSo, "_burstAngleSpread", 0f);

        return gen;
    }

    private static GameObject CreateSweepFanGenerator(GameObject parent, CSO_BulletData bulletData)
    {
        GameObject gen = new GameObject("SweepFan_Generator");
        gen.transform.SetParent(parent.transform);
        gen.transform.localPosition = Vector3.zero;
        gen.SetActive(false);

        CS_GeneratorOscillator oscillator = gen.AddComponent<CS_GeneratorOscillator>();
        SerializedObject oscillatorSo = new SerializedObject(oscillator);
        TrySetField(oscillatorSo, "_oscillationAxis", Vector3.up);
        TrySetField(oscillatorSo, "_amplitudeDegrees", 60f);
        TrySetField(oscillatorSo, "_frequency", 0.25f);

        CS_BulletEmitter emitter = gen.AddComponent<CS_BulletEmitter>();
        SerializedObject emitterSo = new SerializedObject(emitter);
        TrySetField(emitterSo, "_bulletData", bulletData);
        TrySetField(emitterSo, "_fireInterval", 0.15f);
        TrySetField(emitterSo, "_autoFire", true);
        TrySetField(emitterSo, "_burstCount", 5);
        TrySetField(emitterSo, "_burstAngleSpread", 30f);

        return gen;
    }

    private static void SetupActivationTracks(PlayableDirector director, TimelineAsset timelineAsset,
      GameObject ringBurstGen, GameObject spiralGen, GameObject sweepFanGen)
    {
        var ringBurstTrack = timelineAsset.CreateTrack<ActivationTrack>(null, "RingBurst Track");
        var spiralTrack = timelineAsset.CreateTrack<ActivationTrack>(null, "Spiral Track");
        var sweepFanTrack = timelineAsset.CreateTrack<ActivationTrack>(null, "SweepFan Track");

        director.SetGenericBinding(ringBurstTrack, ringBurstGen);
        director.SetGenericBinding(spiralTrack, spiralGen);
        director.SetGenericBinding(sweepFanTrack, sweepFanGen);

        // RingBurst Track: 0-4秒、8-12秒
        AddActivationClip(ringBurstTrack, 0, 4);
        AddActivationClip(ringBurstTrack, 8, 12);

        // Spiral Track: 4-8秒、8-12秒
        AddActivationClip(spiralTrack, 4, 8);
        AddActivationClip(spiralTrack, 8, 12);

        // SweepFan Track: 8-12秒
        AddActivationClip(sweepFanTrack, 8, 12);
    }

    /// <summary>
    /// ActivationTrackにクリップを追加し、指定した時間範囲だけバインド先のGameObjectをアクティブにする。
    /// (修正版2: ActivationPlayableAssetはinternalでプロジェクト外から直接型指定できないため、
    ///  トラックのデフォルトクリップ型を自動で使うCreateDefaultClip()を使う)
    /// </summary>
    private static void AddActivationClip(ActivationTrack track, double startTime, double endTime)
    {
        if (track == null)
        {
            Debug.LogWarning("AddActivationClip: track が null です。トラックの作成に失敗している可能性があります。");
            return;
        }

        var clip = track.CreateDefaultClip();
        if (clip == null)
        {
            Debug.LogWarning("AddActivationClip: クリップの作成に失敗しました。");
            return;
        }

        clip.start = startTime;
        clip.duration = endTime - startTime;
    }

    /// <summary>
    /// [SerializeField]のprivateフィールドに値を設定する安全なヘルパー。
    /// フィールドが見つからない場合は例外を投げず、警告ログを出すだけにする。
    /// </summary>
    private static void TrySetField(SerializedObject so, string fieldName, object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            string typeName = so.targetObject != null ? so.targetObject.GetType().Name : "unknown";
            Debug.LogWarning($"TrySetField: フィールド '{fieldName}' が {typeName} に見つかりませんでした。フィールド名が一致しているか確認してください。");
            return;
        }

        switch (value)
        {
            case float f: prop.floatValue = f; break;
            case int i: prop.intValue = i; break;
            case bool b: prop.boolValue = b; break;
            case Vector3 v: prop.vector3Value = v; break;
            case Object o: prop.objectReferenceValue = o; break;
            case null: prop.objectReferenceValue = null; break;
            default: Debug.LogWarning("TrySetField: 未対応の型です: " + value.GetType()); break;
        }
        so.ApplyModifiedProperties();
    }
}