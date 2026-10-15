using System;
using UnityEngine;
using UnityEditor;
namespace Qiaopi.Editor
{
    public static class ProportionChecks
    {
        [MenuItem("侨批/检查人物实尺")]
        public static void Run()
        {
            var stage=new GameObject("Scale verification");
            int checkedModels=0;
            try {
                foreach(var person in CharacterRoster.All()) {
                    if(person.id=="narrator")continue;
                    var actor=ModelLibrary.Spawn(person.model,stage.transform,Vector3.zero);
                    if(!actor)throw new Exception("Missing scale reference "+person.model);
                    // Measure the current skinned pose, not the importer's
                    // conservative envelope of all possible bone positions.
                    var bounds=ModelLibrary.WorldBounds(actor.transform);
                    float expected=ModelLibrary.CharacterHeight(person.model);
                    if(Mathf.Abs(bounds.size.y-expected)>.018f||Mathf.Abs(bounds.min.y)>.018f)
                        throw new Exception("Character unit/foot mismatch "+person.model+" bounds="+bounds+" expected="+expected);
                    foreach(string joint in new[]{"LeftArm","RightArm","LeftLeg","RightLeg"})
                        if(!ModelLibrary.Find(actor.transform,joint))throw new Exception("Missing animated joint "+person.model+"/"+joint);
                    Debug.Log("PROPORTION_HEIGHT "+person.model+"="+bounds.size.y.ToString("F3")+" m");
                    UnityEngine.Object.DestroyImmediate(actor);checkedModels++;
                }
                // Furniture must keep its authored metric dimensions, independent of human rig scaling.
                var desk=ModelLibrary.Spawn("PaperDesk",stage.transform,Vector3.zero);
                if(!desk||desk.transform.localScale!=Vector3.one)throw new Exception("Furniture accidentally resized with character rigs");
                var shader=Resources.Load<Shader>("HistoricSurface");
                if(!shader||!shader.isSupported)throw new Exception("Historic surface shader unavailable");
                Debug.Log("QIAOPI PROPORTION CHECKS PASSED: "+checkedModels+" model bounds, feet, animation joints; independent prop scale; detailed surface shader.");
            }finally{UnityEngine.Object.DestroyImmediate(stage);}
        }
    }
}
