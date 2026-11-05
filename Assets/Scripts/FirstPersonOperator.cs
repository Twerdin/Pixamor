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

        [SerializeField] private float mouseSensitivity = .10f;
        [SerializeField] private Vector2 monitorPhysicalSize = new Vector2(5.5f,3.575f);
        [SerializeField] private float interactionDistance = 2.1f;
        public float MonitorViewDistance => monitorViewDistance;
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
            monitorPhysicalSize.x = width;
        }

        public void ConfigureMonitorArea(Vector2 size) { monitorPhysicalSize=size; }

        // Geniş bir bakış açısı yerine gerçek ekran dikdörtgenini hedefleriz.
        public bool CanInteractWithMonitor
        {
            get
            {
                if(UIBlocked || IsUsingMonitor || IsChangingView || viewCamera==null || monitor==null)return false;
                var ray=new Ray(viewCamera.transform.position,viewCamera.transform.forward);
                if(Vector3.Dot(ray.origin-monitor.position,monitor.forward)>=0)return false;
                if(!new Plane(monitor.forward,monitor.position).Raycast(ray,out float distance) || distance>interactionDistance)return false;
                var local=Quaternion.Inverse(monitor.rotation)*(ray.GetPoint(distance)-monitor.position);
                if(Mathf.Abs(local.x)>monitorPhysicalSize.x*.5f || Mathf.Abs(local.y)>monitorPhysicalSize.y*.5f)return false;
                if(Physics.Raycast(ray,out var hit,distance) && hit.distance<distance-.1f && hit.collider.GetComponentInParent<FirstPersonOperator>()!=this)return false;
                return true;
            }
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
            if(GetComponent<OperatorInteractionHud>()==null)gameObject.AddComponent<OperatorInteractionHud>();
        }

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
                else if (CanInteractWithMonitor)
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
            float halfHeight=Mathf.Max(monitorPhysicalSize.y*.5f,monitorPhysicalSize.x*.5f/viewCamera.aspect);
            float focusedFov=2f*Mathf.Atan(halfHeight*1.04f/monitorViewDistance)*Mathf.Rad2Deg;
            BeginViewTransition(monitor.position-monitor.forward*monitorViewDistance,monitor.rotation,focusedFov);
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

    }
}


