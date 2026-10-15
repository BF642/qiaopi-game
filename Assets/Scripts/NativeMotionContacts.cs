using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // Local, reviewed adaptation of the retained skin, never a weight rewrite.
    // Probes evaluate the same inverse-bind matrices and all original influences.
    public sealed class NativeMotionContacts
    {
        sealed class Probe {
            public Transform[] bones; public Vector3[] positions; public float[] weights; public float rankingHeight;
            public Vector3 World() { Vector3 p=Vector3.zero; for(int i=0;i<bones.Length;i++) p+=bones[i].TransformPoint(positions[i])*weights[i]; return p; }
        }
        readonly Transform actor, hips;
        readonly Transform[] thighs=new Transform[2], knees=new Transform[2], feet=new Transform[2], hands=new Transform[2], forearms=new Transform[2];
        readonly Quaternion[] footRest=new Quaternion[2], handSupportRest=new Quaternion[2];
        readonly Transform[] handSupports=new Transform[2];
        readonly Vector3[] handSupportOffsets=new Vector3[2];
        readonly Vector3[] palmNormal=new Vector3[2], handAxis=new Vector3[2];
        readonly List<Probe>[] soles={new List<Probe>(),new List<Probe>()};
        readonly List<Probe> seat=new List<Probe>();
        public NativeMotionContacts(Transform root, Transform source, AnimationClip chair, bool reviewedWorkerSupports, float proportion)
        {
            actor=root;
            var byName=new Dictionary<string,Transform>();
            foreach(var t in source.GetComponentsInChildren<Transform>(true)) byName[t.name]=t;
            hips=byName["mixamorig:Hips"];
            for(int side=0;side<2;side++) {
                string prefix="mixamorig:"+(side==0 ? "Left":"Right");
                thighs[side]=byName[prefix+"UpLeg"]; knees[side]=byName[prefix+"Leg"]; feet[side]=byName[prefix+"Foot"];
                hands[side]=byName[prefix+"Hand"]; forearms[side]=byName[prefix+"ForeArm"];
                // Actual Worker source inspection: these forearm-parented
                // branches influence only the hand/thenar surface. Preserve
                // their hierarchy and original bind offsets while following the wrist.
                if(reviewedWorkerSupports) byName.TryGetValue(side==0 ? "Bone_018":"Bone_023",out handSupports[side]);
                if(handSupports[side]) {
                    handSupportOffsets[side]=hands[side].InverseTransformPoint(handSupports[side].position);
                    handSupportRest[side]=Quaternion.Inverse(hands[side].rotation)*handSupports[side].rotation;
                }
                footRest[side]=Quaternion.Inverse(actor.rotation)*feet[side].rotation;
                // Some Meshy child rigs have no separate finger chains. They
                // still support feet/seat contact without a wrist retwist.
                if(!byName.TryGetValue(prefix+"HandIndex1",out var index) ||
                    !byName.TryGetValue(prefix+"HandPinky1",out var pinky)) continue;
                // This cross product points through the back of the retained hand.
                palmNormal[side]=hands[side].InverseTransformDirection(Vector3.Cross(index.position-hands[side].position,
                    pinky.position-hands[side].position).normalized*(side==0 ? 1:-1));
                handAxis[side]=hands[side].InverseTransformDirection((index.position+pinky.position)*.5f-hands[side].position).normalized;
            }
            float hipHeight=actor.InverseTransformPoint(hips.position).y;
            foreach(var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                var mesh=skin.sharedMesh; var points=mesh.vertices; var bind=mesh.bindposes; var bones=skin.bones;
                using(var counts=mesh.GetBonesPerVertex()) using(var weights=mesh.GetAllBoneWeights()) {
                    int cursor=0;
                    for(int v=0;v<points.Length;v++) {
                        int start=cursor; Vector3 world=Vector3.zero;
                        for(int w=0;w<counts[v];w++) { var b=weights[cursor++]; world+=bones[b.boneIndex].TransformPoint(bind[b.boneIndex].MultiplyPoint3x4(points[v]))*b.weight; }
                        Vector3 p=actor.InverseTransformPoint(world);
                        int region=p.y<.13f*proportion ? (p.x<0 ? 0:1) : -1;
                        bool rear=p.y>hipHeight-.22f*proportion && p.y<hipHeight+.02f*proportion && p.z<actor.InverseTransformPoint(hips.position).z-.025f*proportion && Mathf.Abs(p.x)<.16f*proportion;
                        if(region<0 && !rear) continue;
                        var probe=new Probe {bones=new Transform[counts[v]],positions=new Vector3[counts[v]],weights=new float[counts[v]]};
                        for(int w=0;w<counts[v];w++) { var b=weights[start+w]; probe.bones[w]=bones[b.boneIndex]; probe.positions[w]=bind[b.boneIndex].MultiplyPoint3x4(points[v]); probe.weights[w]=b.weight; }
                        if(region>=0) soles[region].Add(probe); if(rear) seat.Add(probe);
                    }
                }
            }
            // Convex support points retain shoe extrema without skinning the
            // complete figure on the CPU for every seated NPC every frame.
            for(int side=0;side<2;side++) Reduce(soles[side]);
            if(chair) {
                var supported=new HashSet<Probe>(); var ranked=new List<Probe>(seat);
                for(int frame=0;frame<=64;frame++) {
                    chair.SampleAnimation(source.gameObject,chair.length*frame/64f);
                    foreach(var point in ranked) point.rankingHeight=point.World().y;
                    ranked.Sort((a,b)=>a.rankingHeight.CompareTo(b.rankingHeight));
                    for(int i=0;i<Mathf.Min(32,ranked.Count);i++) supported.Add(ranked[i]);
                }
                seat.Clear(); seat.AddRange(supported);
            }
            Debug.Log("NATIVE_CONTACT_PROBES left="+soles[0].Count+" right="+soles[1].Count+" seat="+seat.Count);
        }
        static void Reduce(List<Probe> points)
        {
            var keep=new HashSet<Probe>();
            for(int sample=0;sample<96;sample++) {
                float y=1-2*(sample+.5f)/96, radius=Mathf.Sqrt(1-y*y), angle=sample*2.3999632f;
                Vector3 direction=new Vector3(radius*Mathf.Cos(angle),y,radius*Mathf.Sin(angle));
                Probe best=null; float maximum=float.NegativeInfinity;
                foreach(var p in points) {float dot=Vector3.Dot(p.World(),direction);if(dot>maximum){maximum=dot;best=p;}}
                if(best!=null) keep.Add(best);
            }
            points.Clear(); points.AddRange(keep);
        }
        float Height(Vector3 world) {return Vector3.Dot(world-actor.position,actor.up);}
        float Sole(int side) { float minimum=float.PositiveInfinity;foreach(var p in soles[side]) minimum=Mathf.Min(minimum,Height(p.World()));return minimum; }
        float SeatBottom()
        {
            float minimum=float.PositiveInfinity;
            foreach(var p in seat) {
                Vector3 world=p.World(); Vector3 local=Quaternion.Inverse(actor.rotation)*(world-actor.position);
                if(Mathf.Abs(local.x)<.22f && local.z<.14f) minimum=Mathf.Min(minimum,local.y);
            }
            return minimum;
        }
        public void GroundIdle()
        {
            float minimum=Mathf.Min(Sole(0),Sole(1));
            if(float.IsFinite(minimum)) hips.position+=actor.up*Mathf.Clamp(.004f-minimum,-.12f,.12f);
        }
        public void Seat(float height)
        {
            // Preserve the official upper-body performance, fit the actual
            // trouser underside to the requested chair, then solve each leg.
            for(int iteration=0;iteration<3;iteration++) {
                float bottom=SeatBottom();
                if(float.IsFinite(bottom)) hips.position+=actor.up*Mathf.Clamp(height+.004f-bottom,-.20f,.20f);
                for(int side=0;side<2;side++) {
                    Quaternion flat=actor.rotation*footRest[side]; feet[side].rotation=flat;
                    for(int pass=0;pass<2;pass++) {
                        float minimum=Sole(side); if(!float.IsFinite(minimum)) continue;
                        Vector3 target=feet[side].position+actor.up*(.006f-minimum);
                        SolveLeg(thighs[side],knees[side],feet[side],target,actor.forward);
                        feet[side].rotation=flat;
                    }
                }
            }
        }
        static void SolveLeg(Transform thigh,Transform knee,Transform foot,Vector3 target,Vector3 fallback)
        {
            Vector3 a=thigh.position,b=knee.position,c=foot.position;
            float upper=(b-a).magnitude, lower=(c-b).magnitude;
            Vector3 line=target-a; float distance=Mathf.Clamp(line.magnitude,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
            Vector3 direction=line.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(b-a,direction).normalized;
            if(bend.sqrMagnitude<.01f) bend=Vector3.ProjectOnPlane(fallback,direction).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 desiredKnee=a+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            thigh.rotation=Quaternion.FromToRotation(b-a,desiredKnee-a)*thigh.rotation;
            knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,target-knee.position)*knee.rotation;
        }
        public void SynchronizeHandSupports()
        {
            for(int side=0;side<2;side++) if(handSupports[side]) {
                handSupports[side].position=hands[side].TransformPoint(handSupportOffsets[side]);
                handSupports[side].rotation=hands[side].rotation*handSupportRest[side];
            }
        }
        public void RelaxWrists(bool seated)
        {
            for(int side=0;side<2;side++) {
                var hand=hands[side]; Vector3 axis=hand.TransformDirection(handAxis[side]);
                Vector3 normal=Vector3.ProjectOnPlane(hand.TransformDirection(palmNormal[side]),axis);
                Vector3 desired=Vector3.ProjectOnPlane(seated ? actor.up : actor.right*(side==0 ? -1:1),axis);
                if(normal.sqrMagnitude<.001f || desired.sqrMagnitude<.001f) continue;
                float twist=Vector3.SignedAngle(normal,desired,axis);
                // Pronation belongs mostly to the forearm. Applying the whole
                // turn at the wrist alone pinches this Meshy skin at the cuff.
                var forearm=forearms[side];
                Vector3 roll=(hand.position-forearm.position).normalized;
                forearm.rotation=Quaternion.AngleAxis(twist*.65f,roll)*forearm.rotation;
                axis=hand.TransformDirection(handAxis[side]);
                normal=Vector3.ProjectOnPlane(hand.TransformDirection(palmNormal[side]),axis);
                desired=Vector3.ProjectOnPlane(seated ? actor.up : actor.right*(side==0 ? -1:1),axis);
                hand.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(normal,desired,axis),axis)*hand.rotation;
            }
        }
    }
}
