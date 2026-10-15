using System;
using UnityEditor;
using UnityEngine;
namespace Qiaopi.Editor
{
    public static class AmanReleaseChecks
    {
        public static void Run()
        {
            try {
                foreach(string role in new[]{"Aman","Worker"}){
                    var actor=ModelLibrary.Spawn(role,null,Vector3.zero);
                    try {
                        NativeMotionChecks.Check(actor);
                        if(role=="Aman"){
                            var set=NativeMotion.LoadQualifiedSet(ModelLibrary.MeshyResource(role));
                            if(!set || !set.reviewedSkin)throw new Exception("Aman reviewed cloth missing");
                            bool applied=false;foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())applied|=skin.sharedMesh==set.reviewedSkin;
                            if(!applied)throw new Exception("Aman reviewed cloth not active");
                        }
                    }finally{UnityEngine.Object.DestroyImmediate(actor);}
                }
                Debug.Log("AMAN_RELEASE_SOURCE_AND_WORKER_REGRESSION_PASSED");
                NativeMotionContactChecks.RecordRole();
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
