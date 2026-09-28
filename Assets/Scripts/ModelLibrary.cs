using UnityEngine;
namespace Qiaopi
{
    public static class ModelLibrary
    {
        public static GameObject Spawn(string name,Transform parent,Vector3 position)
        {
            var prefab=Resources.Load<GameObject>("Models/"+name);
            if(!prefab)return null;
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=position;
            var visual=Object.Instantiate(prefab,root.transform,false);visual.name="BlenderModel";
            // Imported figures share one art rig, while the playable world uses metres.
            float height=CharacterHeight(name);
            if(height>0) {
                var bounds=LocalBounds(root.transform);
                if(bounds.size.y>.01f) {
                    visual.transform.localPosition-=Vector3.up*bounds.min.y;
                    root.transform.localScale=Vector3.one*(height/bounds.size.y);
                }
            }
            foreach(var c in visual.GetComponentsInChildren<Collider>()) {
                if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c);
            }
            return root;
        }
        public static float CharacterHeight(string name)
        {
            switch(name) {
                case "Wensheng":return 1.72f;
                case "Mother":return 1.58f;
                case "Aman":return 1.26f;
                case "Uncle":return 1.67f;
                case "Xusheng":return 1.70f;
                case "Guide":return 1.69f;
                case "Foreman":return 1.78f;
                case "Shopkeeper":return 1.66f;
                case "MasterHe":return 1.65f;
                case "Clerk":return 1.71f;
                case "Worker":return 1.74f;
                default:return 0;
            }
        }
        static Bounds LocalBounds(Transform root)
        {
            Bounds result=new Bounds();bool first=true;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
                if(!filter.sharedMesh)continue;
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++) {
                    var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var local=root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if(first){result=new Bounds(local,Vector3.zero);first=false;}else result.Encapsulate(local);
                }
            }
            return result;
        }
        public static Transform Find(Transform root,string name)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name==name)return t;
            return null;
        }
    }
    public sealed class CharacterIdle : MonoBehaviour
    {
        Transform l,r; Quaternion baseL,baseR;float phase;
        void Start(){l=ModelLibrary.Find(transform,"LeftArm");r=ModelLibrary.Find(transform,"RightArm");if(l)baseL=l.localRotation;if(r)baseR=r.localRotation;phase=transform.position.x*.73f;}
        void Update(){float a=Mathf.Sin(Time.time*1.5f+phase)*2.3f;if(l)l.localRotation=baseL*Quaternion.Euler(a,0,0);if(r)r.localRotation=baseR*Quaternion.Euler(-a,0,0);}
    }
}
