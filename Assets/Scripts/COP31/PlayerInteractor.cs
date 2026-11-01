using UnityEngine;
using UnityEngine.InputSystem;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// İki Modlu Birinci Şahıs Karakter Kontrolcüsü (FirstPersonPlayer):
    /// 1. Serbest Mod (Free Roam): WASD + Mouse Look + CharacterController ile 3D odada serbestçe yürüme.
    ///    Bilgisayar istasyonuna (Computer_Workstation) yaklaşıp bakınca "[E] Bilgisayarı Kullan" uyarısı çıkar.
    /// 2. Bilgisayar Modu (Monitor Focus): E tuşu veya Sol Tık ile karakterin yürümesi durur, kamera
    ///    yumuşak bir geçişle (Lerp/Slerp) Computer_Workstation/CameraFocusPoint noktasına kilitlenir,
    ///    fare imleci serbest kalır (CursorLockMode.None) ve tüm OS arayüzü fareyle yönetilir.
    /// 3. Bilgisayardan Kalkma: ESC veya Sağ Tık ile kamera tekrar karakterin göz hizasına (PlayerCameraMount)
    ///    döner ve odada serbest yürüyüş kaldığı yerden devam eder.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public static PlayerInteractor Instance { get; private set; }

        [Header("Bileşen Referansları (Edit Mode Kalıcı)")]
        public CharacterController characterController;
        public Transform cameraMount;
        public Camera playerCamera;
        public MovableComputerStation workstation;
        public Transform monitorTarget;

        [Header("Bakış (Mouse Look - Free Roam)")]
        public float sensitivity = 0.15f;
        public float pitchMin = -75f;
        public float pitchMax = 80f;

        [Header("Hareket (WASD Free Roam)")]
        public bool enableMovement = true;
        public float moveSpeed = 2.8f;
        public float sprintMultiplier = 1.6f;
        public float gravity = -9.81f;

        [Header("Etkileşim & Bilgisayar İstasyonu")]
        public float maxDistance = 12f;
        public float workstationInteractDistance = 2.25f;
        public bool isNearWorkstation = false;

        [Header("Monitör Odak Modu (Monitor Focus Mode)")]
        public bool isFullscreen = false;
        public float fullscreenFOV = 46f;
        public float normalFOV = 62f;
        public float transitionSpeed = 10f;

        float yaw;
        float pitch;
        float verticalVelocity;
        Interactable hovered;
        Interactable pressed;

        public Interactable Hovered => hovered;
        public static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void OnEnable()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
            ResolveReferences();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void ResolveReferences()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (cameraMount == null)
            {
                var foundMount = transform.Find("PlayerCameraMount");
                cameraMount = foundMount != null ? foundMount : transform;
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            if (workstation == null)
            {
                workstation = MovableComputerStation.Instance;
                if (workstation == null)
                    workstation = FindAnyObjectByType<MovableComputerStation>();
            }

            if (monitorTarget == null && workstation != null)
            {
                monitorTarget = workstation.cameraFocusPoint != null ? workstation.cameraFocusPoint : workstation.monitorHead;
            }
        }

        public void Setup(Camera camera, float startPitch)
        {
            playerCamera = camera;
            ResolveReferences();
            yaw = transform.eulerAngles.y;
            pitch = startPitch;
            ApplyLookRotation();
        }

        void Start()
        {
            ResolveReferences();
            yaw = transform.eulerAngles.y;
            if (cameraMount != null && cameraMount != transform)
            {
                float localX = cameraMount.localEulerAngles.x;
                if (localX > 180f) localX -= 360f;
                pitch = Mathf.Clamp(localX, pitchMin, pitchMax);
            }

            // Başlangıçta Serbest Mod (Free Roam): İmleç kilitli, odada yürünebilir
            isFullscreen = false;
            LockCursor(true);
        }

        void OnDisable() => ReleasePressed();

        public void EnterComputerMode()
        {
            if (isFullscreen) return;
            ResolveReferences();
            isFullscreen = true;
            LockCursor(false);
            ReleasePressed();
            Sfx.Play(SfxType.Click);
            ClimateOperatorManager.Instance?.Post("BİLGİSAYAR MODU AKTİF — Fare ile OS panellerini yönetin (ESC veya Sağ Tık ile kalkın)");
        }

        public void ExitComputerMode()
        {
            if (!isFullscreen) return;
            isFullscreen = false;
            LockCursor(true);
            ReleasePressed();
            Sfx.Play(SfxType.Click);
            ClimateOperatorManager.Instance?.Post("SERBEST ODA MODU — WASD ile odada yürüyün (E veya Sol Tık ile Bilgisayarı Kullan)");
        }

        public void ToggleFullscreen()
        {
            if (isFullscreen) ExitComputerMode();
            else EnterComputerMode();
        }

        void Update()
        {
            ResolveReferences();

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (isFullscreen)
            {
                UpdateMonitorFocusMode(mouse, keyboard);
            }
            else
            {
                UpdateFreeRoamMode(mouse, keyboard);
            }
        }

        // ============================================================ 1. SERBEST MOD (FREE ROAM)

        void UpdateFreeRoamMode(Mouse mouse, Keyboard keyboard)
        {
            // ESC ile editörde imleci serbest bırak, ekrana tıklayınca geri kilitle
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                LockCursor(!IsCursorLocked);
            }

            // Kamerayı yumuşakça karakterin göz hizasına (PlayerCameraMount) geri çek
            if (playerCamera != null && cameraMount != null)
            {
                playerCamera.transform.position = Vector3.Lerp(
                    playerCamera.transform.position,
                    cameraMount.position,
                    Time.deltaTime * transitionSpeed);

                playerCamera.transform.rotation = Quaternion.Slerp(
                    playerCamera.transform.rotation,
                    cameraMount.rotation,
                    Time.deltaTime * transitionSpeed);

                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView,
                    normalFOV,
                    Time.deltaTime * transitionSpeed);
            }

            // Fare Bakışı (Mouse Look)
            if (mouse != null && (IsCursorLocked || mouse.rightButton.isPressed))
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw = (yaw + delta.x * sensitivity) % 360f;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, pitchMin, pitchMax);
                ApplyLookRotation();
            }

            // WASD Yürüme + Yerçekimi (CharacterController)
            if (enableMovement)
            {
                HandleCharacterMovement(keyboard);
            }

            // KURAL 1: Sadece bilgisayara yaklaşıp DOĞRUDAN monitöre baktığında (kitaplığa veya duvara bakarken değil!)
            // ekranın ortasında 'E' ikonu belirtsin.
            hovered = null;
            isNearWorkstation = false;

            if (playerCamera != null && workstation != null)
            {
                Transform monitorRef = workstation.monitorHead != null ? workstation.monitorHead : workstation.transform;

                Vector3 toMonitor = monitorRef.position - playerCamera.transform.position;
                float dist = toMonitor.magnitude;
                float dot = Vector3.Dot(playerCamera.transform.forward, toMonitor.normalized);

                // Monitör yönüne bakıyor mu? Geniş koni (dot > 0.35f: ~70 derece)
                bool isFacingMonitor = dot > 0.35f;

                if (dist <= 3.8f && isFacingMonitor)
                {
                    Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    if (Physics.Raycast(centerRay, out RaycastHit hit, 4.0f, ~0, QueryTriggerInteraction.Collide))
                    {
                        var station = hit.collider.GetComponentInParent<MovableComputerStation>();
                        if (station != null || hit.collider.transform.IsChildOf(workstation.transform) || hit.collider.transform == workstation.transform)
                        {
                            isNearWorkstation = true;
                            hovered = workstation;
                        }
                    }
                    if (!isNearWorkstation && dist <= 2.8f)
                    {
                        // Masanın önünde ve monitöre bakıyorsa raycast kaçsa bile etkileşime izin ver
                        isNearWorkstation = true;
                        hovered = workstation;
                    }
                }
            }

            // E tuşuna basınca (yalnızca monitöre bakarken) bilgisayara otur
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame && isNearWorkstation)
            {
                EnterComputerMode();
                return;
            }

            // Serbest modda Sol Tık: Monitöre bakıyorsa bilgisayara otur, değilse imleci kilitle
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (isNearWorkstation)
                {
                    EnterComputerMode();
                }
                else if (!IsCursorLocked)
                {
                    LockCursor(true);
                }
            }
        }

        void HandleCharacterMovement(Keyboard keyboard)
        {
            float moveX = 0f;
            float moveZ = 0f;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
            }

            float speed = moveSpeed;
            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                speed *= sprintMultiplier;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f) forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0f;
            if (right.sqrMagnitude > 0.001f) right.Normalize();

            Vector3 horizontalMove = (forward * moveZ + right * moveX);
            if (horizontalMove.sqrMagnitude > 1f) horizontalMove.Normalize();
            horizontalMove *= speed;

            if (characterController != null && characterController.enabled)
            {
                if (characterController.isGrounded && verticalVelocity < 0f)
                    verticalVelocity = -2f;
                else
                    verticalVelocity += gravity * Time.deltaTime;

                Vector3 velocity = horizontalMove + Vector3.up * verticalVelocity;
                characterController.Move(velocity * Time.deltaTime);
            }
            else if (horizontalMove.sqrMagnitude > 0.001f)
            {
                transform.position += horizontalMove * Time.deltaTime;
            }
        }

        void ApplyLookRotation()
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraMount != null && cameraMount != transform)
            {
                cameraMount.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        // ============================================================ 2. BİLGİSAYAR MODU (MONITOR FOCUS)

        void UpdateMonitorFocusMode(Mouse mouse, Keyboard keyboard)
        {
            if (IsCursorLocked) LockCursor(false);

            // ESC veya Sağ Tık ile Bilgisayardan Kalk -> Serbest Moda Dön
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                ExitComputerMode();
                return;
            }

            // Kamerayı yumuşakça (Lerp/Slerp) Computer_Workstation/CameraFocusPoint noktasına hizala
            if (playerCamera != null)
            {
                Transform focusPt = (workstation != null && workstation.cameraFocusPoint != null)
                    ? workstation.cameraFocusPoint
                    : monitorTarget;

                if (focusPt != null)
                {
                    playerCamera.transform.position = Vector3.Lerp(
                        playerCamera.transform.position,
                        focusPt.position,
                        Time.deltaTime * transitionSpeed);

                    playerCamera.transform.rotation = Quaternion.Slerp(
                        playerCamera.transform.rotation,
                        focusPt.rotation,
                        Time.deltaTime * transitionSpeed);
                }

                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView,
                    fullscreenFOV,
                    Time.deltaTime * transitionSpeed);
            }

            // Fare İmleci ile Monitör Üzerindeki OS Panellerine / Butonlarına / Kıtalara Tıklama
            if (playerCamera != null && mouse != null)
            {
                Ray ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());

                hovered = null;
                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    var inter = hit.collider.GetComponentInParent<Interactable>();
                    // Bilgisayar modundayken dış istasyon kutusunu değil, doğrudan iç OS butonlarını/haritayı hedefle
                    if (inter != null && !(inter is MovableComputerStation))
                    {
                        hovered = inter;
                    }
                }

                bool playing = ClimateOperatorManager.Instance == null || ClimateOperatorManager.Instance.State == GameState.Playing;

                if (mouse.leftButton.wasPressedThisFrame && playing && hovered != null && hovered.isInteractable)
                {
                    pressed = hovered;
                    pressed.OnPressStart();
                }

                if (pressed != null)
                {
                    if (mouse.leftButton.isPressed && playing)
                        pressed.OnHold(Time.deltaTime);
                    else
                        ReleasePressed();
                }
            }
        }

        void ReleasePressed()
        {
            if (pressed != null) pressed.OnRelease();
            pressed = null;
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
