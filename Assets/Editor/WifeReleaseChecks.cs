using System;
using UnityEditor;
using UnityEngine;
namespace Qiaopi.Editor
{
    public static class WifeReleaseChecks
    {
        public static void Run()
        {
            try {
                foreach(string role in new[]{"Wife","Aman","Worker"}) {
                    var actor=ModelLibrary.Spawn(role,null,Vector3.zero);
                    try {
                        NativeMotionChecks.Check(actor);
                        if(role=="Wife") {
                            var set=NativeMotion.LoadQualifiedSet(ModelLibrary.MeshyResource(role));
                            if(!set || !set.reviewedSkin)throw new Exception("Wife reviewed high-detail skin missing");
                            bool applied=false;
                            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                                applied|=skin.sharedMesh==set.reviewedSkin&&skin.sharedMaterial.mainTexture.width==8192;
                            if(!applied)throw new Exception("Wife reviewed skin/native8K not active");
                        }
                    } finally { UnityEngine.Object.DestroyImmediate(actor); }
                }
                SeatedWritingPostureChecks.CheckAll();
                Debug.Log("WIFE_RELEASE_NATIVE8K_AND_CAST_REGRESSION_PASSED");
                NativeMotionContactChecks.RecordRole();
            } catch(Exception error) {Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
