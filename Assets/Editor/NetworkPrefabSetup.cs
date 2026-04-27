#if UNITY_EDITOR
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NetworkPrefabSetup
{
    private const string OutputFolder = "Assets/Resources/NetworkPrefabs";
    private const string PlayerSourcePath = "Assets/Prefabs/PBRCharacter Variant.prefab";
    private const string EnemySourcePath = "Assets/TopDownScripts/Assets2D/Enemy.prefab";
    private const string ProjectileSourcePath = "Assets/TopDownScripts/Assets2D/Bullet.prefab";
    private const string PlayerOutputPath = OutputFolder + "/NetworkPlayer.prefab";
    private const string SinglePlayerOutputPath = "Assets/Resources/SinglePlayerPrefabs/SinglePlayerPlayer.prefab";
    private const string EnemyOutputPath = OutputFolder + "/NetworkEnemy.prefab";
    private const string ProjectileOutputPath = OutputFolder + "/NetworkProjectile.prefab";
    private const string PlayerGenerationMarker = PlayerSourcePath + "|network-audio-v5";

    static NetworkPrefabSetup()
    {
        EditorApplication.delayCall += EnsureNetworkPrefabsIfMissing;
    }

    [MenuItem("Tools/Project D/Build Network Prefabs")]
    public static void EnsureNetworkPrefabs()
    {
        Directory.CreateDirectory(OutputFolder);

        BuildPlayerPrefab();
        BuildSinglePlayerPrefab();
        BuildEnemyPrefab();
        BuildProjectilePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureNetworkPrefabsIfMissing()
    {
        bool missingPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerOutputPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(SinglePlayerOutputPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyOutputPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileOutputPath) == null;

        if (missingPrefab || IsGeneratedPlayerFromDifferentSource())
            EnsureNetworkPrefabs();
    }

    private static void BuildPlayerPrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSourcePath);
        if (source == null)
        {
            Debug.LogWarning($"Could not build network player prefab. Missing source prefab: {PlayerSourcePath}");
            return;
        }

        GameObject instance = InstantiateEditablePrefab(source, "NetworkPlayer");
        PlayerController2D originalController = instance.GetComponent<PlayerController2D>();
        RemoveComponents<PlayerController2D>(instance);
        RemoveComponents<NetworkTransform>(instance);
        NetworkObject networkObject = EnsureComponent<NetworkObject>(instance);
        networkObject.SynchronizeTransform = false;
        ConfigureTopDownPlayerRigidbody(EnsureComponent<Rigidbody>(instance));

        var controller = EnsureComponent<NetworkPlayerController2D>(instance);
        controller.playerRenderer = instance.GetComponentInChildren<Renderer>();
        controller.audioSource = originalController != null && originalController.audioSource != null
            ? originalController.audioSource
            : FindSfxAudioSource(instance);
        controller.shootSound = originalController != null ? originalController.shootSound : null;
        controller.hurtSound = originalController != null ? originalController.hurtSound : null;
        controller.fireRate = Mathf.Max(0.3f, originalController != null ? originalController.fireRate : controller.fireRate);
        controller.firePoint = EnsureFirePoint(instance.transform);

        SaveAndDestroy(instance, PlayerOutputPath);
        MarkGeneratedSource(PlayerOutputPath, PlayerGenerationMarker);
    }

    private static void BuildEnemyPrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(EnemySourcePath);
        if (source == null)
        {
            Debug.LogWarning($"Could not build network enemy prefab. Missing source prefab: {EnemySourcePath}");
            return;
        }

        GameObject instance = InstantiateEditablePrefab(source, "NetworkEnemy");
        EnemySeek2D original = instance.GetComponent<EnemySeek2D>();
        NetworkObject networkObject = EnsureComponent<NetworkObject>(instance);
        networkObject.SynchronizeTransform = false;
        EnsureComponent<Rigidbody>(instance);
        ConfigureNetworkTransform(EnsureComponent<NetworkTransform>(instance), true);
        var enemy = EnsureComponent<NetworkEnemy>(instance);

        if (original != null)
        {
            enemy.speed = original.speed;
            enemy.rotationSpeed = original.rotationSpeed;
            enemy.maxHealth = original.maxHealth;
            enemy.contactDamage = original.contactDamage;
            enemy.scoreValue = original.scoreValue;
            enemy.neighborRadius = original.neighborRadius;
            enemy.repulsionStrength = original.repulsionStrength;
            enemy.allyLayer = original.allyLayer;
            enemy.obstacleDetectDistance = original.obstacleDetectDistance;
            enemy.obstacleAvoidStrength = original.obstacleAvoidStrength;
            enemy.obstacleLayers = original.obstacleLayers;
            enemy.jitterStrength = original.jitterStrength;
        }

        RemoveComponents<EnemySeek2D>(instance);
        instance.tag = "Enemy";
        _ = networkObject;
        SaveAndDestroy(instance, EnemyOutputPath);
    }

    private static void BuildSinglePlayerPrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSourcePath);
        if (source == null)
        {
            Debug.LogWarning($"Could not build singleplayer prefab. Missing source prefab: {PlayerSourcePath}");
            return;
        }

        Directory.CreateDirectory("Assets/Resources/SinglePlayerPrefabs");
        GameObject instance = InstantiateEditablePrefab(source, "SinglePlayerPlayer");
        RemoveComponents<NetworkObject>(instance);
        RemoveComponents<NetworkTransform>(instance);
        RemoveComponents<OwnerNetworkTransform>(instance);
        RemoveComponents<NetworkPlayerController2D>(instance);
        ConfigureTopDownPlayerRigidbody(EnsureComponent<Rigidbody>(instance));
        PlayerController2D controller = EnsureComponent<PlayerController2D>(instance);
        controller.fireRate = Mathf.Max(0.3f, controller.fireRate);
        controller.firePoint = EnsureFirePoint(instance.transform);
        instance.tag = "Player";

        SaveAndDestroy(instance, SinglePlayerOutputPath);
    }

    private static void ConfigureTopDownPlayerRigidbody(Rigidbody rb)
    {
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private static void BuildProjectilePrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileSourcePath);
        if (source == null)
        {
            Debug.LogWarning($"Could not build network projectile prefab. Missing source prefab: {ProjectileSourcePath}");
            return;
        }

        GameObject instance = InstantiateEditablePrefab(source, "NetworkProjectile");
        Bullet3D original = instance.GetComponent<Bullet3D>();
        NetworkObject networkObject = EnsureComponent<NetworkObject>(instance);
        networkObject.SynchronizeTransform = false;
        EnsureComponent<Rigidbody>(instance);
        ConfigureNetworkTransform(EnsureComponent<NetworkTransform>(instance), true);
        var projectile = EnsureComponent<NetworkProjectile>(instance);

        if (original != null)
        {
            projectile.speed = original.speed;
            projectile.damage = original.damage;
            projectile.lifeTime = original.lifeTime;
        }

        RemoveComponents<Bullet3D>(instance);
        EnsureProjectileCollider(instance);
        SaveAndDestroy(instance, ProjectileOutputPath);
    }

    private static GameObject InstantiateEditablePrefab(GameObject source, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = name;
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        return instance;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void RemoveComponents<T>(GameObject gameObject) where T : Component
    {
        foreach (T component in gameObject.GetComponentsInChildren<T>(true))
            Object.DestroyImmediate(component, true);
    }

    private static Transform EnsureFirePoint(Transform root)
    {
        Transform firePoint = root.Find("NetworkFirePoint");
        if (firePoint != null)
            return firePoint;

        var firePointObject = new GameObject("NetworkFirePoint");
        firePointObject.transform.SetParent(root, false);
        firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 1.2f);
        firePointObject.transform.localRotation = Quaternion.identity;
        return firePointObject.transform;
    }

    private static void EnsureProjectileCollider(GameObject projectile)
    {
        Collider collider = projectile.GetComponentInChildren<Collider>();
        if (collider == null)
            collider = projectile.AddComponent<SphereCollider>();

        collider.isTrigger = true;
    }

    private static AudioSource FindSfxAudioSource(GameObject root)
    {
        foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
        {
            if (source != null && !source.loop)
                return source;
        }

        return root.GetComponentInChildren<AudioSource>(true);
    }

    private static void ConfigureNetworkTransform(NetworkTransform transformSync, bool syncRotation)
    {
        transformSync.SyncRotAngleX = syncRotation;
        transformSync.SyncRotAngleY = syncRotation;
        transformSync.SyncRotAngleZ = syncRotation;
        transformSync.SyncScaleX = false;
        transformSync.SyncScaleY = false;
        transformSync.SyncScaleZ = false;
    }

    private static void SaveAndDestroy(GameObject instance, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
    }

    private static bool IsGeneratedPlayerFromDifferentSource()
    {
        AssetImporter importer = AssetImporter.GetAtPath(PlayerOutputPath);
        return importer != null && importer.userData != PlayerGenerationMarker;
    }

    private static void MarkGeneratedSource(string outputPath, string sourcePath)
    {
        AssetImporter importer = AssetImporter.GetAtPath(outputPath);
        if (importer == null)
            return;

        importer.userData = sourcePath;
        importer.SaveAndReimport();
    }
}
#endif
