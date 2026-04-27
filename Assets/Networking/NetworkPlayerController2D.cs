using System.Collections;
using System;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayerController2D : NetworkBehaviour
{
    public const int MaxHealth = 100;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Shooting")]
    public Transform firePoint;
    public float fireRate = 0.3f;

    [Header("Combat")]
    public int contactDamage = 10;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip hurtSound;

    [Header("Hit Flash")]
    public Renderer playerRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    public NetworkVariable<int> Score = new NetworkVariable<int>();
    public NetworkVariable<int> Health = new NetworkVariable<int>(MaxHealth);
    public NetworkVariable<bool> IsEliminated = new NetworkVariable<bool>();
    public NetworkVariable<FixedString32Bytes> PlayerLabel = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<Vector3> SyncedPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> SyncedRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private Rigidbody rb;
    private Vector3 moveInput;
    private float fireTimer;
    private Color originalColor;
    private bool hasOriginalColor;
    private AudioSource sfxAudioSource;
    private float lockedY;

    public override void OnNetworkSpawn()
    {
        NetworkObject.SynchronizeTransform = false;
        DisableLegacyControllers();
        DisableNetworkTransformSync();

        rb = GetComponent<Rigidbody>();
        lockedY = transform.position.y;
        ConfigurePhysicsForOwnership();

        if (firePoint == null)
        {
            var firePointObject = new GameObject("NetworkFirePoint");
            firePointObject.transform.SetParent(transform, false);
            firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 1.2f);
            firePointObject.transform.localRotation = Quaternion.identity;
            firePoint = firePointObject.transform;
        }

        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();

        ResolveAudioReferences();

        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
            hasOriginalColor = true;
        }

        if (IsServer)
        {
            Health.Value = MaxHealth;
            Score.Value = 0;
            IsEliminated.Value = false;
            PlayerLabel.Value = OwnerClientId == NetworkManager.ServerClientId ? "Host" : "Client";
        }

        if (IsOwner)
        {
            ClampVerticalPosition();
            SyncedPosition.Value = transform.position;
            SyncedRotation.Value = transform.rotation;
            AttachCamera();
        }
    }

    public override void OnGainedOwnership()
    {
        ConfigurePhysicsForOwnership();
        AttachCamera();
    }

    public override void OnLostOwnership()
    {
        ConfigurePhysicsForOwnership();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            Vector3 targetPosition = SyncedPosition.Value;
            targetPosition.y = lockedY;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 18f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, SyncedRotation.Value, 18f * Time.deltaTime);
            return;
        }

        if (IsEliminated.Value || PauseMenuController.IsPaused)
        {
            moveInput = Vector3.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0f, v).normalized;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 lookDir = hit.point - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                    SyncedRotation.Value = transform.rotation;
                }
            }
        }

        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            fireTimer = 0f;
            ShootServerRpc(firePoint.position, firePoint.rotation);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || rb == null || IsEliminated.Value)
            return;

        Vector3 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        newPos.y = lockedY;
        rb.MovePosition(newPos);
        SyncedPosition.Value = newPos;
        ClampVerticalVelocity();
    }

    private void DisableLegacyControllers()
    {
        foreach (PlayerController2D legacyController in GetComponentsInChildren<PlayerController2D>(true))
            legacyController.enabled = false;
    }

    private void DisableNetworkTransformSync()
    {
        OwnerNetworkTransform transformSync = GetComponent<OwnerNetworkTransform>();
        if (transformSync == null)
            return;

        transformSync.enabled = false;
    }

    private void ConfigurePhysicsForOwnership()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            return;

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = !IsOwner;
        rb.detectCollisions = true;
        ClampVerticalPosition();
        ClampVerticalVelocity();
    }

    private void ClampVerticalPosition()
    {
        Vector3 position = transform.position;
        if (Mathf.Approximately(position.y, lockedY))
            return;

        position.y = lockedY;
        transform.position = position;

        if (rb != null)
            rb.position = position;
    }

    private void ClampVerticalVelocity()
    {
        if (rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;
        if (Mathf.Approximately(velocity.y, 0f))
            return;

        velocity.y = 0f;
        rb.linearVelocity = velocity;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ShootServerRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        try
        {
            NetworkProjectile.Spawn(position, rotation, OwnerClientId);
            ShootClientRpc();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Network projectile spawn failed: {ex.Message}");
        }
    }

    public void AddScoreServer(int amount)
    {
        if (!IsServer || amount <= 0 || IsEliminated.Value)
            return;

        Score.Value += amount;
    }

    public void TakeDamageServer(int damage)
    {
        if (!IsServer || IsEliminated.Value)
            return;

        Health.Value = Mathf.Max(0, Health.Value - Mathf.Max(0, damage));
        DamageClientRpc();

        if (Health.Value == 0)
        {
            IsEliminated.Value = true;

            if (AreAllPlayersEliminated())
                TeamEliminatedClientRpc();
        }
    }

    [ClientRpc]
    private void DamageClientRpc()
    {
        PlayOneShot(hurtSound);

        if (isActiveAndEnabled)
            StartCoroutine(FlashDamage());
    }

    [ClientRpc]
    private void ShootClientRpc()
    {
        PlayOneShot(shootSound);
    }

    [ClientRpc]
    private void TeamEliminatedClientRpc()
    {
        Time.timeScale = 0f;
    }

    private IEnumerator FlashDamage()
    {
        if (playerRenderer == null || !hasOriginalColor)
            yield break;

        playerRenderer.material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        playerRenderer.material.color = originalColor;
    }

    private void AttachCamera()
    {
#if UNITY_2023_2_OR_NEWER
        CameraFollow cameraFollow = FindFirstObjectByType<CameraFollow>();
#else
        CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
#endif
        if (cameraFollow != null)
            cameraFollow.player = transform;
    }

    private void ResolveAudioReferences()
    {
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource source in sources)
        {
            if (source == null)
                continue;

            if (sfxAudioSource == null && !source.loop)
                sfxAudioSource = source;

            if (shootSound == null && !source.loop && source.clip != null)
                shootSound = source.clip;
            else if (hurtSound == null && !source.loop && source.clip != null && source.clip != shootSound)
                hurtSound = source.clip;
        }

        if (sfxAudioSource == null)
            sfxAudioSource = audioSource != null ? audioSource : gameObject.AddComponent<AudioSource>();

        if (audioSource == null)
            audioSource = sfxAudioSource;

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
        sfxAudioSource.spatialBlend = 0f;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        if (sfxAudioSource == null)
            ResolveAudioReferences();

        if (sfxAudioSource != null)
            sfxAudioSource.PlayOneShot(clip);
    }

    public static NetworkPlayerController2D FindByClientId(ulong ownerClientId)
    {
        NetworkPlayerController2D[] players = FindObjectsByType<NetworkPlayerController2D>(FindObjectsSortMode.None);
        foreach (NetworkPlayerController2D player in players)
        {
            if (player.OwnerClientId == ownerClientId)
                return player;
        }

        return null;
    }

    public static string BuildFinalScoreText()
    {
        int combined = 0;
        var builder = new StringBuilder();
        NetworkPlayerController2D[] players = FindObjectsByType<NetworkPlayerController2D>(FindObjectsSortMode.None);

        foreach (NetworkPlayerController2D player in players)
            combined += player.Score.Value;

        builder.AppendLine($"Combined Score: {combined}");
        foreach (NetworkPlayerController2D player in players)
        {
            string label = player.PlayerLabel.Value.ToString();
            if (string.IsNullOrWhiteSpace(label))
                label = player.OwnerClientId == NetworkManager.ServerClientId ? "Host" : "Client";

            builder.AppendLine($"{label} Score: {player.Score.Value}");
        }

        return builder.ToString().TrimEnd();
    }

    public static bool AreAllPlayersEliminated()
    {
        NetworkPlayerController2D[] players = FindObjectsByType<NetworkPlayerController2D>(FindObjectsSortMode.None);
        if (players.Length == 0)
            return false;

        if (TwoPlayerNetcodeBootstrap.CurrentSessionMode == TwoPlayerNetcodeBootstrap.SessionMode.Multiplayer && players.Length < 2)
            return false;

        foreach (NetworkPlayerController2D player in players)
        {
            if (!player.IsEliminated.Value)
                return false;
        }

        return true;
    }
}
