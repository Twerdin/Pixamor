using UnityEngine;
using UnityEngine.InputSystem;

namespace Pixamor.COP31
{
    /// <summary>
    /// Birinci şahıs kamera ve oda içi serbest hareket:
    /// - WASD / Ok tuşları ile odanın içinde serbest yürüme (Shift ile koşma).
    /// - Fare kilitliyken ekran ortasından, imleç serbestken doğrudan fare okuyla tıklama/etkileşim.
    /// - İmleç serbestken sağ tıkla sürükleyerek veya sol tıkla kilitleyerek 360 derece bakış.
    /// - Escape ile imleci serbest bırakma.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Bakış (Mouse Look)")]
        public float sensitivity = 0.15f;
        public float pitchMin = -75f;
        public float pitchMax = 80f;
        public bool full360Look = true;

        [Header("Hareket (Movement)")]
        public bool enableMovement = true;
        public float moveSpeed = 3.6f;
        public float sprintMultiplier = 1.75f;
        public float eyeHeight = 1.45f;
        public Vector2 xBounds = new Vector2(-3.6f, 3.6f);
        public Vector2 zBounds = new Vector2(-2.4f, 3.6f);

        [Header("Etkileşim (Raycast)")]
        public float maxDistance = 25f;

        [Header("Monitör Tam Ekran (Fullscreen Mode)")]
        public bool isFullscreen = false;
        public Transform monitorTarget;
        public float fullscreenFOV = 50f;
        public float normalFOV = 65f;
        public float focusDistance = 0.90f;
        public float transitionSpeed = 9f;

        public static PlayerInteractor Instance { get; private set; }

        Camera cam;
        float yaw;
        float pitch;
        Interactable hovered;
        Interactable pressed;

        public Interactable Hovered => hovered;
        public static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Setup(Camera camera, float startPitch)
        {
            cam = camera;
            pitch = startPitch;
            yaw = 0f;
            ApplyRotation();
        }

        void Start()
        {
            LockCursor(true);
        }

        void OnDisable() => ReleasePressed();

        Vector3 savedDeskPos = new Vector3(0f, 1.45f, -1.22f);

        public void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            if (isFullscreen)
            {
                savedDeskPos = transform.position;
                LockCursor(false);
                ReleasePressed();
                ClimateOperatorManager.Instance?.Post("TAM EKRAN MODU: Bütün operasyonu fareyle yönetin [E / ESC: Çıkış]");
            }
            else
            {
                transform.position = savedDeskPos;
                LockCursor(true);
                pitch = 12f;
                yaw = 0f;
                ApplyRotation();
                ClimateOperatorManager.Instance?.Post("NORMAL GÖRÜNÜM: Serbest oda bakışı [E: Tam Ekran]");
            }
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            // E tuşu ile tam ekran odaklanma (Monitörün önüne geçip tüm hareketleri fareyle çözme)
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                ToggleFullscreen();
            }

            // Escape ile imleç serbest bırakma veya tam ekrandan çıkma
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (isFullscreen)
                {
                    ToggleFullscreen();
                }
                else
                {
                    LockCursor(false);
                    ReleasePressed();
                }
            }

            // İmleç serbestken sol tık yapıldığında ve tam ekranda DEĞİLKEN yeniden kilitle
            if (!isFullscreen && !IsCursorLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                // Eğer doğrudan bir 3D interactable'a tıklanmadıysa kilitle
                Ray freeRay = cam != null ? cam.ScreenPointToRay(mouse.position.ReadValue()) : default;
                if (!Physics.Raycast(freeRay, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore)
                    || hit.collider.GetComponentInParent<Interactable>() == null)
                {
                    LockCursor(true);
                }
            }

            // 1. TAM EKRAN VS NORMAL BAKIŞ
            if (isFullscreen)
            {
                // Tam ekranda fare daima serbest ve görünürdür (Oyuncu her şeyi fareyle tıklar)
                if (IsCursorLocked) LockCursor(false);

                Vector3 targetPos;
                Quaternion targetRot;
                if (monitorTarget != null)
                {
                    targetPos = monitorTarget.position - monitorTarget.forward * focusDistance;
                    targetRot = monitorTarget.rotation;
                }
                else
                {
                    targetPos = new Vector3(0f, 1.49f, -1.01f);
                    targetRot = Quaternion.Euler(12f, 0f, 0f);
                }

                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * transitionSpeed);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fullscreenFOV, Time.deltaTime * transitionSpeed);
            }
            else
            {
                if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, normalFOV, Time.deltaTime * transitionSpeed);

                // Serbest Fare Bakışı
                if (mouse != null)
                {
                    bool shouldRotate = IsCursorLocked || mouse.rightButton.isPressed;
                    if (shouldRotate)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        yaw = (yaw + delta.x * sensitivity) % 360f;
                        pitch = Mathf.Clamp(pitch - delta.y * sensitivity, pitchMin, pitchMax);
                        ApplyRotation();
                    }
                }

                // WASD Yürüme
                if (enableMovement)
                {
                    HandleMovement(keyboard);
                }
            }

            // 2. RAYCAST İLE ETKİLEŞİM (TAM EKRANDA TAMAMEN FARE OKU İLE)
            if (cam != null && mouse != null)
            {
                Ray ray = (IsCursorLocked && !isFullscreen)
                    ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                    : cam.ScreenPointToRay(mouse.position.ReadValue());

                hovered = null;
                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    hovered = hit.collider.GetComponentInParent<Interactable>();
                }

                // Oyun oynanıyor mu kontrolü
                bool playing = true;
                if (ClimateOperatorManager.Instance != null)
                    playing = ClimateOperatorManager.Instance.State == GameState.Playing;
                else if (ClimateStateManager.Instance != null)
                    playing = ClimateStateManager.Instance.State == GameState.Playing;

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

        void HandleMovement(Keyboard keyboard)
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

            if (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveZ) > 0.01f)
            {
                float currentSpeed = moveSpeed;
                if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                    currentSpeed *= sprintMultiplier;

                Vector3 forward = transform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 moveDir = (forward * moveZ + right * moveX).normalized;
                Vector3 p = transform.position + moveDir * currentSpeed * Time.deltaTime;

                // Masa sınırına takılma yumuşatması (Masa: x: [-1.45, 1.45], z: [-0.95, 0.25])
                // Oyuncunun masanın içine girmesini önler
                if (p.x > -1.45f && p.x < 1.45f && p.z > -0.95f && p.z < 0.25f)
                {
                    // Masanın dışına it
                    if (transform.position.z <= -0.95f) p.z = -0.95f;
                    else if (transform.position.z >= 0.25f) p.z = 0.25f;
                    else if (transform.position.x <= -1.45f) p.x = -1.45f;
                    else if (transform.position.x >= 1.45f) p.x = 1.45f;
                }

                p.x = Mathf.Clamp(p.x, xBounds.x, xBounds.y);
                p.z = Mathf.Clamp(p.z, zBounds.x, zBounds.y);
                p.y = eyeHeight;
                transform.position = p;
            }
        }

        void ReleasePressed()
        {
            if (pressed != null) pressed.OnRelease();
            pressed = null;
        }

        void ApplyRotation() => transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
