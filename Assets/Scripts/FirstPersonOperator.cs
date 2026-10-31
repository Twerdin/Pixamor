using System.Collections;
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
        [SerializeField] private float monitorViewDistance = 4.7f;
        [SerializeField] private float monitorHalfWidth = 3f;
        [SerializeField] private float mouseSensitivity = .10f;
        public bool IsUsingMonitor { get; private set; }
        public bool IsChangingView { get; private set; }
        [SerializeField, Min(.05f)] private float viewTransitionDuration = .45f;
        private Coroutine viewTransition;
        public bool UIBlocked { get; private set; }
        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value,.03f,.3f);
        }
        private GameFlowController flow;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
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

        public void ConfigureMonitorView(float distance, float width)
        {
            monitorViewDistance = Mathf.Max(.3f, distance);
            monitorHalfWidth = width * .55f;
        }

        public void ConfigureRoomScale(float height, float eyeHeight, float speed)
        {
            var controller=GetComponent<CharacterController>();
            controller.height=height;
            controller.center=new Vector3(0,height*.5f,0);
            controller.stepOffset=.2f;
            viewCamera.transform.localPosition=new Vector3(0,eyeHeight,0);
            walkSpeed=speed;
        }

        private void Start()
        {
            body = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            eyePosition = viewCamera.transform.localPosition;
            walkFov = viewCamera.fieldOfView;
            CaptureMouse(!UIBlocked);
        }

        private bool InReach => Vector3.Distance(viewCamera.transform.position,monitor.position) <= 5f;

        private void Update()
        {
            if (UIBlocked || IsChangingView) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (flow == null && keyboard.escapeKey.wasPressedThisFrame)
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

        public void EnterMonitor()
        {
            if (!Application.isPlaying || UIBlocked || IsUsingMonitor || IsChangingView || viewCamera == null || monitor == null) return;
            IsUsingMonitor = true;
            // Oyuncuyu taşımadan kamerayı okuma konumuna getiririz.
            BeginViewTransition(monitor.position - monitor.forward * monitorViewDistance, monitor.rotation,
                Mathf.Max(48f, 2f * Mathf.Atan(monitorHalfWidth / (monitorViewDistance * viewCamera.aspect)) * Mathf.Rad2Deg));
            CaptureMouse(false);
        }

        private void ExitMonitor()
        {
            crises.CloseCard();
            IsUsingMonitor = false;
            BeginViewTransition(transform.TransformPoint(eyePosition),
                transform.rotation * Quaternion.Euler(pitch,0,0),walkFov);
        }

        private void BeginViewTransition(Vector3 position, Quaternion rotation, float fov)
        {
            if(viewTransition!=null)StopCoroutine(viewTransition);
            viewTransition=StartCoroutine(BlendView(position,rotation,fov));
        }

        private IEnumerator BlendView(Vector3 position, Quaternion rotation, float fov)
        {
            IsChangingView=true;
            var startPosition=viewCamera.transform.position;
            var startRotation=viewCamera.transform.rotation;
            float startFov=viewCamera.fieldOfView;
            float elapsed=0;
            // Yavaş başlar, hızlanır ve hedefe yaklaşırken yeniden yavaşlar.
            while(elapsed<viewTransitionDuration)
            {
                elapsed+=Time.unscaledDeltaTime;
                float t=Mathf.SmoothStep(0,1,elapsed/viewTransitionDuration);
                viewCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition,position,t),Quaternion.Slerp(startRotation,rotation,t));
                viewCamera.fieldOfView=Mathf.Lerp(startFov,fov,t);
                yield return null;
            }
            viewCamera.transform.SetPositionAndRotation(position,rotation);
            viewCamera.fieldOfView=fov;
            IsChangingView=false;
            viewTransition=null;
            CaptureMouse(!UIBlocked && !IsUsingMonitor);
        }

        public void ConfigureFlow(GameFlowController controller)
        {
            flow = controller;
        }

        public void SetUIBlocked(bool blocked)
        {
            UIBlocked = blocked;
            CaptureMouse(!blocked && !IsUsingMonitor);
        }

        public void ResetForShift()
        {
            if (body == null) return;
            if(viewTransition!=null)StopCoroutine(viewTransition);
            viewTransition=null;
            IsChangingView=false;
            crises.CloseCard();
            body.enabled = false;
            transform.SetPositionAndRotation(spawnPosition,spawnRotation);
            body.enabled = true;
            IsUsingMonitor = false;
            pitch = verticalSpeed = 0;
            viewCamera.transform.localPosition = eyePosition;
            viewCamera.transform.localRotation = Quaternion.identity;
            viewCamera.fieldOfView = walkFov;
            CaptureMouse(!UIBlocked);
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

        private void OnDisable() { if(viewTransition!=null)StopCoroutine(viewTransition); viewTransition=null; IsChangingView=false; CaptureMouse(false); }

        private void OnGUI()
        {
            // Monitör kullanılırken talimatlar ekranın kendi alt şeridindedir.
            if (monitor == null || IsUsingMonitor || UIBlocked) return;
            string hint = InReach ? "Monitöre bak ve E'ye bas | WASD: yürü | Fare: bak | Shift: hızlan" :
                "WASD: yürü | Fare: bak | Monitöre yaklaş | Esc: duraklat";
            GUI.Box(new Rect(15, Screen.height - 50, Mathf.Min(780, Screen.width - 30), 35), GameLanguage.Translate(hint));
            if (!IsUsingMonitor && Cursor.lockState == CursorLockMode.Locked)
                GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }
    }
}
