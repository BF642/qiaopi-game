using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        const float EyeHeight=1.60f, DefaultFieldOfView=72f;
        float cameraYaw, cameraPitch=3f, firstPersonFov=DefaultFieldOfView;
        int cameraDragButton=-1;
        public float ViewYaw=>cameraYaw;
        public float ViewPitch=>cameraPitch;

        Vector3 CameraEyePoint()=>player?player.transform.position+Vector3.up*EyeHeight:Vector3.up*EyeHeight;

        public void RotateView(Vector2 degrees)
        {
            if(Blocked||!cam)return;
            cameraYaw=Mathf.Repeat(cameraYaw+degrees.x,360f);
            cameraPitch=Mathf.Clamp(cameraPitch+degrees.y,-75f,75f);
            UpdateCamera(true);
        }

        public void ResetView()
        {
            if(Blocked||!cam)return;
            firstPersonFov=DefaultFieldOfView;
            FaceCurrentTask();
            ResetCameraTracking();
            UpdateCamera(true);
        }

        void FaceCurrentTask()
        {
            if(!player||mission==null)return;
            Vector3 d=Target()-player.transform.position;d.y=0;
            if(d.sqrMagnitude>.1f)cameraYaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            cameraPitch=3f;
        }

        void FaceConversationPartner()
        {
            if(!npc||!cam)return;
            float height=ModelLibrary.CharacterHeight(CharacterRoster.Get(CharacterRoster.ForNode(state.nodeId)).model);
            if(height<1)height=1.65f;
            Vector3 d=npc.transform.position+Vector3.up*(height*.90f)-CameraEyePoint();
            if(d.sqrMagnitude<.01f)return;
            cameraYaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            cameraPitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
            UpdateCamera(true);CancelCameraPointer();
        }

        void ResetCameraTracking(){CancelCameraPointer();ResetMobileControls();}
        void CancelCameraPointer()
        {
            cameraDragButton=-1;
            if(Cursor.lockState!=CursorLockMode.None)Cursor.lockState=CursorLockMode.None;
            if(!Cursor.visible)Cursor.visible=true;
        }

        bool PointerOverWorldUI()
        {
            GetUiMetrics(out float k,out float uiX,out float uiY);
            if(k<=0)return true;
            float x=(Input.mousePosition.x-uiX)/k;
            float y=(Screen.height-Input.mousePosition.y-uiY)/k;
            return new Rect(1019,29,533,51).Contains(new Vector2(x,y))||(LifeJourney.HasReachedOverseas(state)&&LifeEntryRect().Contains(new Vector2(x,y)));
        }

        void UpdateCameraInput()
        {
            if(MobileControls||Blocked||DetailQA||!Application.isFocused){CancelCameraPointer();return;}
            if(Input.GetKeyDown(KeyCode.Home)){ResetView();return;}
            if(cameraDragButton<0&&!PointerOverWorldUI())
                firstPersonFov=Mathf.Clamp(firstPersonFov-Input.mouseScrollDelta.y*2f,48f,82f);
            if(cameraDragButton<0&&!PointerOverWorldUI()&&Input.GetMouseButtonDown(1)){
                cameraDragButton=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
                return; // Ignore the pointer displacement on the frame that captures it.
            }
            if(cameraDragButton<0)return;
            if(!Input.GetMouseButton(1)){CancelCameraPointer();return;}
            Vector2 delta=new Vector2(Input.GetAxisRaw("Mouse X"),-Input.GetAxisRaw("Mouse Y"));
            RotateView(Vector2.ClampMagnitude(delta,35f)*2.2f);
        }

        void PositionControlledCamera(bool immediate)
        {
            if(!cam)return;
            cam.orthographic=false;
            cam.fieldOfView=immediate?firstPersonFov:Mathf.Lerp(cam.fieldOfView,firstPersonFov,1-Mathf.Exp(-Time.unscaledDeltaTime*10));
            cam.nearClipPlane=.035f;
            targetCamera=CameraEyePoint();
            cam.transform.SetPositionAndRotation(targetCamera,Quaternion.Euler(cameraPitch,cameraYaw,0));
            if(heldCamera){heldCamera.fieldOfView=cam.fieldOfView;heldCamera.enabled=!Blocked;}
        }
    }
}
