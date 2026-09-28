using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    public partial class WorldGame
    {
        const int HeldItemLayer=29;
        Camera heldCamera;
        GameObject heldRig,heldLetter,heldBox,heldHands;
        Renderer[] hiddenBody;
        float walkVisual;

        void SetupFirstPersonBody()
        {
            // Preserve a human shadow without putting the inside of the head in view.
            hiddenBody=model.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in hiddenBody)renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            var cameraObject=new GameObject("手中物件镜头");cameraObject.transform.SetParent(cam.transform,false);
            heldCamera=cameraObject.AddComponent<Camera>();heldCamera.clearFlags=CameraClearFlags.Depth;
            heldCamera.depth=cam.depth+1;heldCamera.cullingMask=1<<HeldItemLayer;
            heldCamera.nearClipPlane=.02f;heldCamera.farClipPlane=2;heldCamera.fieldOfView=cam.fieldOfView;
            heldRig=new GameObject("第一人称持物");heldRig.transform.SetParent(cam.transform,false);
            heldRig.transform.localPosition=new Vector3(0,-.37f,.66f);
            heldHands=new GameObject("托住批件的双手");heldHands.transform.SetParent(heldRig.transform,false);
            for(int side=-1;side<=1;side+=2){
                var sleeve=Shape(PrimitiveType.Capsule,"布袖",heldHands.transform,new Vector3(side*.23f,-.11f,-.14f),new Vector3(.12f,.16f,.13f),teal);
                sleeve.transform.localRotation=Quaternion.Euler(58,side*-12,side*18);
                var hand=Shape(PrimitiveType.Sphere,"手掌",heldHands.transform,new Vector3(side*.185f,-.005f,0),new Vector3(.10f,.065f,.14f),skin);
                hand.transform.localRotation=Quaternion.Euler(0,side*15,0);
                Shape(PrimitiveType.Capsule,"拇指",heldHands.transform,new Vector3(side*.148f,.025f,.023f),new Vector3(.028f,.045f,.028f),skin).transform.localRotation=Quaternion.Euler(55,0,side*35);
            }
            heldLetter=ModelLibrary.Spawn("LetterBundle",heldRig.transform,Vector3.zero);
            if(heldLetter){
                Bounds bounds=new Bounds();bool first=true;
                foreach(var renderer in heldLetter.GetComponentsInChildren<Renderer>()){
                    if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                }
                Vector3 localCenter=heldLetter.transform.InverseTransformPoint(bounds.center);
                float size=.36f/Mathf.Max(.01f,Mathf.Max(bounds.size.x,bounds.size.z));
                heldLetter.transform.localScale=Vector3.one*size;
                heldLetter.transform.localPosition=-localCenter*size+new Vector3(0,.015f,.015f);
            }else heldLetter=Cube("手中侨批",heldRig.transform,new Vector3(0,.02f,0),new Vector3(.36f,.025f,.24f),cream);
            heldBox=new GameObject("手中货箱");heldBox.transform.SetParent(heldRig.transform,false);
            Cube("木箱",heldBox.transform,new Vector3(0,.025f,.04f),new Vector3(.39f,.24f,.31f),wood);
            Cube("横扎带",heldBox.transform,new Vector3(0,.025f,.04f),new Vector3(.40f,.05f,.32f),cream);
            Cube("竖扎带",heldBox.transform,new Vector3(0,.025f,.04f),new Vector3(.045f,.25f,.32f),cream);
            foreach(var t in heldRig.GetComponentsInChildren<Transform>(true))t.gameObject.layer=HeldItemLayer;
            foreach(var renderer in heldRig.GetComponentsInChildren<Renderer>(true)){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
            UpdateFirstPersonProps();
        }

        void UpdateFirstPersonProps()
        {
            if(!heldRig)return;
            bool hasBox=sideCarrying||(carrying&&state.nodeId=="dock");
            heldBox.SetActive(hasBox);heldLetter.SetActive(carrying&&!hasBox);
            heldRig.SetActive((carrying||sideCarrying)&&!Blocked);
        }

        void UpdateFirstPersonPresentation()
        {
            UpdateFirstPersonProps();
            if(heldRig&&heldRig.activeSelf){
                float sway=Mathf.Sin(stepTime*.5f)*.009f*walkVisual;
                heldRig.transform.localPosition=new Vector3(sway,-.37f+Mathf.Abs(sway)*.6f,.66f);
            }
            if(heldCamera)heldCamera.enabled=!Blocked;
        }

        void FirstPersonOverlay()
        {
            // A small centre mark gives a stable reference while looking around.
            Box(new Rect(797,497,6,6),new Color(.98f,.96f,.88f,.78f));
            string prompt=MobileControls?MobilePrompt():Prompt();
            Box(new Rect(340,898,920,MobileControls?66:55),new Color(.12f,.22f,.20f,.88f));
            Label(new Rect(357,905,886,MobileControls?52:37),prompt,MobileControls?27:20,paper,false,TextAnchor.MiddleCenter);
            Box(new Rect(0,964,1600,36),new Color(.10f,.17f,.15f,.76f));
            Label(new Rect(35,972,1120,24),MobileControls?"左侧摇杆行走 · 右侧滑动转头 · 点击「互动」交谈与取放":"第一人称 · WASD 行走 · 按住右键转头 · E 互动 · M 地图 · J 侨批匣 · G 图集 · Esc 帮助",15,paper);
            Label(new Rect(1240,972,312,25),saveHealthy?"自动存档 · 本机旅程":"存档未保存",14,saveHealthy?paper:red,false,TextAnchor.MiddleRight);
            DrawFirstPersonCompass();
            if(MobileControls)DrawMobileControls();
        }

        void DrawFirstPersonCompass()
        {
            Vector3 d=Target()-player.transform.position;
            float bearing=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            float difference=Mathf.DeltaAngle(cameraYaw,bearing);
            string direction=Mathf.Abs(difference)<18?"前方":Mathf.Abs(difference)>145?"身后":difference>0?"右侧":"左侧";
            float y=MobileControls?157:107;
            Box(new Rect(MobileControls?570:648,y,MobileControls?460:304,MobileControls?57:36),new Color(.12f,.22f,.20f,.73f));
            Label(new Rect(MobileControls?580:658,y+6,MobileControls?440:284,MobileControls?42:24),"目的地在"+direction+" · "+Mathf.CeilToInt(Dist(Target()))+" 米",MobileControls?27:17,paper,false,TextAnchor.MiddleCenter);
        }
    }
}
