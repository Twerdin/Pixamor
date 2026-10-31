using UnityEngine;
using UnityEngine.InputSystem;
namespace Pixamor
{
    // Sadece deneme sahnesinde kullanılır; ana oyunun kontrollerine dokunmaz.
    [RequireComponent(typeof(CharacterController))]
    public class RoomTrialWalker : MonoBehaviour
    {
        public Camera viewCamera;
        float pitch, falling;
        void Start() { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
        void Update()
        {
            var k=Keyboard.current;var m=Mouse.current;if(k==null || m==null)return;
            if(k.escapeKey.wasPressedThisFrame) { Cursor.lockState=CursorLockMode.None;Cursor.visible=true; }
            if(m.leftButton.wasPressedThisFrame) { Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false; }
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            var delta=m.delta.ReadValue()*.08f;transform.Rotate(0,delta.x,0);pitch=Mathf.Clamp(pitch-delta.y,-75,75);viewCamera.transform.localRotation=Quaternion.Euler(pitch,0,0);
            var move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));move=Vector2.ClampMagnitude(move,1);
            var body=GetComponent<CharacterController>();if(body.isGrounded && falling<0)falling=-2;falling+=Physics.gravity.y*Time.deltaTime;
            body.Move(((transform.right*move.x+transform.forward*move.y)*1.5f+Vector3.up*falling)*Time.deltaTime);
        }
        void OnGUI() { GUI.Label(new Rect(20,20,680,35),"ODA DENEMESİ • WASD: yürü • Fare: bak • Esc: imleci bırak • Tıkla: devam et"); }
        void OnDisable() { Cursor.lockState=CursorLockMode.None;Cursor.visible=true; }
    }
}
