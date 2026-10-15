using UnityEngine;
namespace Qiaopi
{
    public static class ModelLibrary
    {
        public static GameObject Spawn(string name,Transform parent,Vector3 position,bool preferNative = true)
        {
            string nativeResource = preferNative ? MeshyResource(name) : null;
            var nativePrefab = string.IsNullOrEmpty(nativeResource) ? null : Resources.Load<GameObject>(nativeResource);
            bool nativeCharacter = nativePrefab;
            var prefab=nativeCharacter ? nativePrefab : Resources.Load<GameObject>("Models/"+name);
            if(!prefab)return null;
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=position;
            root.AddComponent<CharacterModelIdentity>().modelId = name;
            var visual=Object.Instantiate(prefab,root.transform,false);visual.name="BlenderModel";
            NativeCharacterRig nativeRig = null;
            if (nativeCharacter) {
                var motionSet=NativeMotion.LoadQualifiedSet(nativeResource);
                // Meshy's Separate Files include a static Icosphere preview
                // helper. It is not part of the person and must not affect
                // stature or floor placement when using a withSkin model.
                if (motionSet)
                    foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                        if (renderer.name == "Icosphere" && !renderer.GetComponent<SkinnedMeshRenderer>())
                            renderer.enabled = false;
                if(motionSet && motionSet.reviewedSkin)
                    foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if(skin.sharedMesh && skin.sharedMesh.name==motionSet.reviewedSkinSourceName) {
                            skin.sharedMesh=motionSet.reviewedSkin;
                            int separator=nativeResource.LastIndexOf('/');
                            var reviewedMaterial=Resources.Load<Material>(nativeResource.Substring(0,separator)+"/ReviewedMaterial");
                            if(reviewedMaterial) skin.sharedMaterial=reviewedMaterial;
                        }
                nativeRig = root.AddComponent<NativeCharacterRig>();
                if (name == "Wife" || name == "Mother") nativeRig.strideScale = .55f;
                if (name == "Wife") nativeRig.signWristTurn = 90f;
                // The mother's wide sleeve is fused to the source wrist surface.
                // Keep its wrist neutral while the elbow performs the gesture.
                if (name == "Mother") nativeRig.signWristTurn = 0f;
                if (name == "Mother") nativeRig.shoulderRelaxation = 16f;
                if (name == "Wife") nativeRig.shoulderRelaxation = 20f;
                nativeRig.ConfigureImportedMotion(nativeResource, visual);
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen = true;
            }
            // Gameplay drives the four exposed limb bones directly. Imported
            // generic rigs must not evaluate an Animator over those same poses.
            if (!nativeRig || !nativeRig.HasImportedMotion)
                foreach(var animator in visual.GetComponentsInChildren<Animator>(true))animator.enabled=false;
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
            CharacterPresentationPolish.Apply(root, name);
            return root;
        }
        // Optional imports retain each character's identity, scale and production
        // fallback. A missing new asset never attaches its rig to an old model.
        public static string MeshyResource(string name)
        {
            if (CharacterHeight(name) > 0) {
                string motion = "Models/" + name + "MeshyMotion/" + name + "MeshyMotion";
                if (Resources.Load<GameObject>(motion) && NativeMotion.HasQualifiedSet(motion))
                    return motion;
            }
            switch (name) {
                case "Wensheng": return "Models/WenshengMeshy/WenshengMeshy";
                case "Wife": return "Models/WifeMeshy/WifeMeshy";
                case "Mother": return "Models/MotherMeshy/MotherMeshy";
                case "Aman": return "Models/AmanMeshy/AmanMeshy";
                case "Xusheng": return "Models/XushengMeshy/XushengMeshy";
                case "MasterHe": return "Models/MasterHeMeshy/MasterHeMeshy";
                case "Clerk": return "Models/ClerkMeshy/ClerkMeshy";
                case "Uncle": return "Models/UncleMeshy/UncleMeshy";
                case "Guide": return "Models/GuideMeshy/GuideMeshy";
                case "Foreman": return "Models/ForemanMeshy/ForemanMeshy";
                case "Shopkeeper": return "Models/ShopkeeperMeshy/ShopkeeperMeshy";
                case "Worker": return "Models/WorkerMeshy/WorkerMeshy";
                default: return null;
            }
        }
        public static float CharacterHeight(string name)
        {
            switch(name) {
                case "Wensheng":return 1.72f;
                case "Mother":return 1.58f;
                case "Wife":return 1.62f;
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
        public static Bounds LocalBounds(Transform root)
        {
            Bounds result=new Bounds();bool first=true;
            bool native = root.GetComponent<NativeCharacterRig>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
                if(!filter.sharedMesh)continue;
                var visible = filter.GetComponent<MeshRenderer>();
                if (visible && !visible.enabled) continue;
                EncapsulateLocalBounds(root,filter.transform,filter.sharedMesh.bounds,ref result,ref first);
            }
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                if(!skin.sharedMesh)continue;
                if (native && skin.sharedMesh.isReadable) {
                    // This imported armature carries FBX unit transforms. Unity's
                    // BakeMesh snapshot includes inherited scale on this rig;
                    // measuring it again through TransformPoint squares the scale.
                    // Bind-pose skinning directly into root space measures metres
                    // once, regardless of the imported renderer's scale chain.
                    var vertices = skin.sharedMesh.vertices;
                    var weights = skin.sharedMesh.GetAllBoneWeights();
                    var counts = skin.sharedMesh.GetBonesPerVertex();
                    var bindings = skin.sharedMesh.bindposes;
                    var bones = skin.bones;
                    var matrices = new Matrix4x4[bones.Length];
                    for (int b = 0; b < bones.Length; b++) matrices[b] = root.worldToLocalMatrix * bones[b].localToWorldMatrix * bindings[b];
                    int cursor = 0;
                    for (int v = 0; v < vertices.Length; v++) {
                        Vector3 point = Vector3.zero;
                        for (int w = 0; w < counts[v]; w++) {
                            var influence = weights[cursor++];
                            point += matrices[influence.boneIndex].MultiplyPoint3x4(vertices[v]) * influence.weight;
                        }
                        if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                        else result.Encapsulate(point);
                    }
                    continue;
                }
                // Imported skin bounds may cover an entire animation envelope.
                // Measure the current body once when spawning, so adding a rig
                // cannot shrink its static head or place its feet underground.
                var posed=new Mesh {name="Character bounds snapshot",hideFlags=HideFlags.HideAndDontSave};
                try {
                    // Bake into renderer-local space. TransformPoint below adds
                    // its transform scale exactly once, including FBX unit scale.
                    skin.BakeMesh(posed,false);
                    if(posed.vertexCount==0)continue;
                    posed.RecalculateBounds();
                    EncapsulateLocalBounds(root,skin.transform,posed.bounds,ref result,ref first);
                } finally {
                    if(Application.isPlaying)Object.Destroy(posed);else Object.DestroyImmediate(posed);
                }
            }
            return result;
        }
        public static Bounds WorldBounds(Transform root)
        {
            Bounds local=LocalBounds(root);
            var result=new Bounds(root.TransformPoint(local.center),Vector3.zero);
            for(int i=0;i<8;i++)
                result.Encapsulate(root.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            return result;
        }
        static void EncapsulateLocalBounds(Transform root,Transform meshTransform,Bounds bounds,ref Bounds result,ref bool first)
        {
            for(int i=0;i<8;i++) {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var local=root.InverseTransformPoint(meshTransform.TransformPoint(corner));
                if(first){result=new Bounds(local,Vector3.zero);first=false;}else result.Encapsulate(local);
            }
        }
        public static Transform Find(Transform root,string name)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name==name)return t;
            var native = root.GetComponent<NativeCharacterRig>();
            return native ? native.Joint(name) : null;
        }
    }
    public sealed class CharacterModelIdentity : MonoBehaviour
    {
        public string modelId;
    }
    public sealed class CharacterIdle : MonoBehaviour
    {
        CharacterPerformance performance;
        void Awake() { performance = CharacterPerformance.For(gameObject); }
        void OnEnable() { if (!performance) performance = CharacterPerformance.For(gameObject); performance.enabled = true; }
        void OnDisable() { if (performance) { performance.ResetPose(); performance.enabled = false; } }
    }
}
