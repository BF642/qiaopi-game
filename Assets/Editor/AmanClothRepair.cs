using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Qiaopi.Editor
{
    // Reviewed, localized skin-weight repair. Original FBX, vertices, topology,
    // UVs, bind poses and animation keys remain available unchanged.
    public static class AmanClothRepair
    {
        const string Folder="Assets/Resources/Models/AmanMeshyMotion/";
        [Serializable] sealed class Report {
            public int vertices,weldedVertices,changedVertices,protectedVertices;
            public bool geometryUnchanged,uvUnchanged,protectedWeightsUnchanged;
            public string source,output,method;
        }
        static float Ramp(float a,float b,float x){return Mathf.Clamp01((x-a)/(b-a));}
        public static void Build()
        {
            GameObject instance=null;
            try {
                string source=Folder+"AmanMeshyMotion.fbx",output=Folder+"AmanReviewedCloth.asset";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);instance=Object.Instantiate(prefab);
                SkinnedMeshRenderer skin=null;
                foreach(var candidate in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if(candidate.sharedMesh && candidate.sharedMesh.vertexCount>1000){if(skin)throw new Exception("Expected one main Aman skin");skin=candidate;}
                if(!skin)throw new Exception("Aman skin missing");
                Mesh original=skin.sharedMesh;var points=original.vertices;var bones=skin.bones;int n=points.Length,b=bones.Length;
                var world=new Vector3[n];var weights=new float[n*b];var matrices=new Matrix4x4[b];
                for(int j=0;j<b;j++)matrices[j]=bones[j].localToWorldMatrix*original.bindposes[j];
                using(var counts=original.GetBonesPerVertex())using(var sourceWeights=original.GetAllBoneWeights()){
                    int cursor=0;for(int i=0;i<n;i++)for(int k=0;k<counts[i];k++){
                        var w=sourceWeights[cursor++];weights[i*b+w.boneIndex]+=w.weight;world[i]+=matrices[w.boneIndex].MultiplyPoint3x4(points[i])*w.weight;
                    }
                }
                // Weld only coincident positions for the smoothing calculation.
                // The render mesh still keeps every original UV/normal seam.
                var map=new Dictionary<Vector3Int,int>();var groups=new List<List<int>>();var vertexGroup=new int[n];
                for(int i=0;i<n;i++){
                    var p=world[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                    if(!map.TryGetValue(key,out int g)){g=groups.Count;map.Add(key,g);groups.Add(new List<int>());}
                    vertexGroup[i]=g;groups[g].Add(i);
                }
                int count=groups.Count;var old=new float[count*b];var current=new float[count*b];var region=new float[count];
                var bounds=new Bounds(world[0],Vector3.zero);foreach(var p in world)bounds.Encapsulate(p);
                float scale=1.26f/bounds.size.y;
                for(int g=0;g<count;g++){
                    foreach(int v in groups[g])for(int j=0;j<b;j++)old[g*b+j]+=weights[v*b+j]/groups[g].Count;
                    var p=world[groups[g][0]];float x=Mathf.Abs(p.x-bounds.center.x)*scale,z=(p.y-bounds.min.y)*scale;
                    region[g]=Ramp(.54f,.61f,z)*(1-Ramp(.91f,.97f,z));
                    region[g]*=1-Ramp(.19f,.235f,x)*(1-Ramp(.63f,.70f,z));
                    float central=(1-Ramp(.09f,.145f,x))*Ramp(.63f,.69f,z)*(1-Ramp(.84f,.89f,z));
                    float outer=Ramp(.135f,.18f,x)*Ramp(.60f,.65f,z)*(1-Ramp(.84f,.90f,z));
                    float total=0;
                    for(int j=0;j<b;j++){
                        string name=bones[j].name;float w=old[g*b+j];
                        if(name.Contains("Arm")||name.Contains("Hand"))w*=1-central;
                        if(name.EndsWith("Hips",StringComparison.Ordinal)||name.Contains("UpLeg"))w*=1-outer;
                        current[g*b+j]=w;total+=w;
                    }
                    for(int j=0;j<b;j++)current[g*b+j]=total>1e-6f?current[g*b+j]/total:old[g*b+j];
                }
                var edgeKeys=new HashSet<ulong>();var triangles=original.triangles;
                for(int t=0;t<triangles.Length;t+=3)for(int j=0;j<3;j++){
                    int a=vertexGroup[triangles[t+j]],c=vertexGroup[triangles[t+(j+1)%3]];if(a==c)continue;
                    edgeKeys.Add(((ulong)(uint)Mathf.Min(a,c)<<32)|(uint)Mathf.Max(a,c));
                }
                var edges=new List<Vector2Int>();var degree=new int[count];
                foreach(ulong key in edgeKeys){int a=(int)(key>>32),c=(int)(key&0xffffffff);edges.Add(new Vector2Int(a,c));degree[a]++;degree[c]++;}
                var sums=new float[current.Length];
                for(int step=0;step<24;step++){
                    Array.Clear(sums,0,sums.Length);
                    foreach(var edge in edges)for(int j=0;j<b;j++){sums[edge.x*b+j]+=current[edge.y*b+j];sums[edge.y*b+j]+=current[edge.x*b+j];}
                    for(int g=0;g<count;g++)if(degree[g]>0)for(int j=0;j<b;j++)current[g*b+j]+=(sums[g*b+j]/degree[g]-current[g*b+j])*(.45f*region[g]);
                }
                var final=new List<BoneWeight1>();var finalCounts=new NativeArray<byte>(n,Allocator.Temp);var order=new int[b];
                var report=new Report {source=source,output=output,vertices=n,weldedVertices=count,geometryUnchanged=true,uvUnchanged=true,protectedWeightsUnchanged=true,method="Local arm/leg leakage correction and24pass adjacency smoothing; UV-seam weld used only for calculation; no facial/shoe geometry or keys changed."};
                for(int i=0;i<n;i++){
                    int g=vertexGroup[i];var row=new float[b];for(int j=0;j<b;j++){row[j]=region[g]<.0001f?weights[i*b+j]:Mathf.Lerp(old[g*b+j],current[g*b+j],region[g]);order[j]=j;}
                    Array.Sort(order,(a,c)=>row[c].CompareTo(row[a]));float total=0;int kept=0;
                    for(int k=0;k<Mathf.Min(4,b);k++)if(row[order[k]]>1e-6f){total+=row[order[k]];kept++;}
                    if(kept==0)throw new Exception("Empty skin weight");finalCounts[i]=(byte)kept;
                    float delta=0;var normalized=new float[b];for(int k=0;k<kept;k++){int j=order[k];normalized[j]=row[j]/total;final.Add(new BoneWeight1 {boneIndex=j,weight=normalized[j]});}
                    for(int j=0;j<b;j++)delta+=Mathf.Abs(normalized[j]-weights[i*b+j]);
                    if(delta>.0001f)report.changedVertices++;
                    if(region[g]<.0001f){report.protectedVertices++;if(delta>.0001f)report.protectedWeightsUnchanged=false;}
                }
                if(!report.protectedWeightsUnchanged)throw new Exception("Protected face/feet weights changed");
                var repaired=Object.Instantiate(original);repaired.name=original.name+"_ReviewedCloth";
                using(var all=new NativeArray<BoneWeight1>(final.ToArray(),Allocator.Temp))repaired.SetBoneWeights(finalCounts,all);finalCounts.Dispose();
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(output);
                if(existing){EditorUtility.CopySerialized(repaired,existing);Object.DestroyImmediate(repaired);repaired=existing;}else AssetDatabase.CreateAsset(repaired,output);
                var set=AssetDatabase.LoadAssetAtPath<NativeMotionSet>(Folder+"NativeMotionSet.asset");set.reviewedSkin=repaired;set.reviewedSkinSourceName=original.name;EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
                string destination=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/replacement-v287/qa/unity-cloth-repair.json"));File.WriteAllText(destination,JsonUtility.ToJson(report,true));
                Debug.Log("AMAN_CLOTH_REPAIR changed="+report.changedVertices+" protected="+report.protectedVertices+" topologyAndUVUnchanged=true");
                Object.DestroyImmediate(instance);instance=null;NativeMotionSourceChecks.QualifyRole();
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{if(instance)Object.DestroyImmediate(instance);}
        }
    }
}
