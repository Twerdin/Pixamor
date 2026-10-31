using UnityEngine;
using UnityEngine.InputSystem;

namespace Pixamor
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonOperator : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform monitor;
        [SerializeField] private CrisisManager crises;
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float mouseSensitivity = .10f;
        public bool IsUsingMonitor { get; private set; }
        private CharacterController body;
        private float pitch;
        private float verticalSpeed;
        private Vector3 eyePosition;
        private float walkFov;

        public void Configure(Camera camera, Transform screen, CrisisManager manager)
        {
            viewCamera = camera;
            monitor = screen;
            crises = manager;
        }

        private void Start()
        {
            body = GetComponent<CharacterController>();
            eyePosition = viewCamera.transform.localPosition;
            walkFov = viewCamera.fieldOfView;
            CaptureMouse(true);
        }

        private bool InReach => Vector3.Distance(transform.position + Vector3.up * 1.65f,
            monitor.position) <= 5f;

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsUsingMonitor) ExitMonitor();
                CaptureMouse(false);
                return;
            }
            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (IsUsingMonitor) ExitMonitor();
                else if (InReach && Vector3.Dot(viewCamera.transform.forward,
                    (monitor.position - viewCamera.transform.position).normalized) > .5f)
                    EnterMonitor();
            }
            if (IsUsingMonitor) return;
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame) CaptureMouse(true);
                return;
            }

            // Mouse delta zaten kare başına harekettir; deltaTime ile tekrar çarpılmaz.
            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y++;
            if (keyboard.sKey.isPressed) move.y--;
            if (keyboard.dKey.isPressed) move.x++;
            if (keyboard.aKey.isPressed) move.x--;
            move = Vector2.ClampMagnitude(move, 1f);
            float speed = keyboard.leftShiftKey.isPressed ? walkSpeed * 1.5f : walkSpeed;
            if (body.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            Vector3 velocity = (transform.forward * move.y + transform.right * move.x) * speed;
            velocity.y = verticalSpeed;
            // CharacterController, masa ve duvarların içinden geçmeyi engeller.
            body.Move(velocity * Time.deltaTime);
        }

        private void EnterMonitor()
        {
            IsUsingMonitor = true;
            // Oyuncuyu taşımadan kamerayı okuma konumuna getiririz.
            viewCamera.transform.SetPositionAndRotation(monitor.position - monitor.forward * 4.7f,
                monitor.rotation);
            viewCamera.fieldOfView = Mathf.Max(48f,
                2f * Mathf.Atan(3f / (4.7f * viewCamera.aspect)) * Mathf.Rad2Deg);
            CaptureMouse(false);
        }

        private void ExitMonitor()
        {
            crises.CloseCard();
            IsUsingMonitor = false;
            viewCamera.transform.localPosition = eyePosition;
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            viewCamera.fieldOfView = walkFov;
            CaptureMouse(true);
        }

        private static void CaptureMouse(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) CaptureMouse(false);
        }

        private void OnDisable() { CaptureMouse(false); }

        private void OnGUI()
        {
            if (monitor == null) return;
            string hint = IsUsingMonitor ? "Monitör: fareyle tıkla | E: odada gezin | Esc: imleci bırak" :
                InReach ? "Monitöre bak ve E'ye bas | WASD: yürü | Fare: bak | Shift: hızlan" :
                "WASD: yürü | Fare: bak | Monitöre yaklaş | Esc: imleci bırak";
            GUI.Box(new Rect(15, Screen.height - 50, Mathf.Min(780, Screen.width - 30), 35), hint);
            if (!IsUsingMonitor && Cursor.lockState == CursorLockMode.Locked)
                GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }
    }
}
