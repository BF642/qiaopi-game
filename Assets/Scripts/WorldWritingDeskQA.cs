using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool WritingQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-writing-test") >= 0;

        IEnumerator WalkToWritingDeskForQA()
        {
            CameraRequire(writingPlace != null, "WRITING_PLACE_AVAILABLE_" + loadedWorld);
            Vector3 before = player.transform.position;
            OpenPersonalLetter();
            CameraRequire(Vector3.Distance(before, player.transform.position) < .001f, "WRITING_REQUEST_NO_TELEPORT");
            if (!letterEditor) {
                CameraRequire(writingRequested && !Blocked, "WRITING_WALK_STARTED_" + loadedWorld);
                float deadline = Time.time + 50;
                while (Time.time < deadline && routeIndex < walkRoute.Count) { yield return null; RequireFirstPerson("walking to writing desk"); }
                CameraRequire(NearWritingDesk, "WRITING_DESK_REACHABLE_" + loadedWorld + " position=" + player.transform.position);
                CameraRequire(!letterEditor, "WRITING_REQUIRES_SEATED_INTERACTION");
                Interact();
            }
            CameraRequire(letterEditor && writingSeated, "WRITING_SEATED_" + loadedWorld);
            yield return new WaitForSeconds(.6f);
            CameraRequire(Vector3.Distance(cam.transform.position, writingPlace.seatEye) < .02f, "WRITING_CAMERA_AT_SEAT");
            CameraRequire(Blocked && !heldCamera.enabled && PersonalLetterTyping, "WRITING_LOCKS_MOVEMENT_AND_SHORTCUTS");
            foreach (var c in Physics.OverlapSphere(cam.transform.position, .06f, ~((1<<2)|(1<<29)|(1<<30)), QueryTriggerInteraction.Ignore))
                CameraRequire(!world || !c.transform.IsChildOf(world.transform), "WRITING_EYE_INSIDE_SCENERY_" + c.name);
        }

        readonly Dictionary<string,string> writingQAFiles = new Dictionary<string,string>();
        readonly Dictionary<string,int> writingQAPrefs = new Dictionary<string,int>();
        float writingQAMusicLevel;
        string writingQAOutput, writingQATemp;
        bool WritingShortReview => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-writing-review") >= 0;
        int writingQAChecks;
        void WritingRequire(bool okay,string label)
        { writingQAChecks++; if(!okay)throw new Exception("WRITING_QA_"+label); }

        IEnumerator RunWritingDeskQA()
        {
            var pending=new Stack<IEnumerator>();pending.Push(RunWritingDeskChecks());
            while(pending.Count>0) {
                object next=null;Exception failure=null;
                try { if(!pending.Peek().MoveNext())pending.Pop();else next=pending.Peek().Current; }
                catch(Exception error) { failure=error; }
                if(failure!=null) {
                    while(pending.Count>0){try{(pending.Pop() as IDisposable)?.Dispose();}catch{}}
                    try{CheckWritingIsolation();}catch(Exception isolation){Debug.LogError(isolation);}
                    Debug.LogError("WRITING_RUNTIME_FAILED: "+failure);Application.Quit(1);yield break;
                }
                if(next is IEnumerator)pending.Push((IEnumerator)next);else if(pending.Count>0)yield return next;
            }
            try{CheckWritingIsolation();}catch(Exception error){Debug.LogError("WRITING_RUNTIME_FAILED: "+error);Application.Quit(1);yield break;}
            Debug.Log((WritingShortReview?"WRITING_REVIEW_ALL_PASSED":"WRITING_DESK_ALL_PASSED")+": six reachable sheltered writing places, actual seated cameras, indoor inspection walks, disk reloads, draft/task preservation, collision-only position recovery; checks="+writingQAChecks+". User saves and preferences unchanged; silent. Automated duration is not human playtime.");
            Application.Quit(0);
        }

        IEnumerator RunWritingDeskChecks()
        {
            WritingRequire(WritingQA&&QA&&DetailQA,"ISOLATION_ENTRY");
            foreach(string slot in new[]{ChapterSlotPath(false),ChapterSlotPath(true),InquirySlotPath})
                foreach(string suffix in new[]{"",".bak",".tmp"})writingQAFiles[slot+suffix]=CharacterFileSnapshot(slot+suffix);
            foreach(string key in new[]{"qiaopi-music-enabled","qiaopi-sound-enabled","qiaopi-voice-enabled","qiaopi-mobile-initialized"})writingQAPrefs[key]=PlayerPrefs.GetInt(key,-1);
            writingQAMusicLevel=PlayerPrefs.GetFloat("qiaopi-music-level",-1);
            AudioListener.pause=true;soundEnabled=voiceEnabled=false;WorldAtmosphere.SetSoundEnabled(false);musicDirector.SetEnabled(false,false);voiceSource.Stop();sfx.Stop();
            writingQATemp=Path.Combine(Path.GetTempPath(),"qiaopi-writing-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(writingQATemp);
            writingQAOutput=Path.Combine(writingQATemp,"captures");string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-qiaopi-writing-output")writingQAOutput=Path.GetFullPath(args[i+1]);
            Directory.CreateDirectory(writingQAOutput);Debug.Log("WRITING_QA_OUTPUT="+writingQAOutput);
            yield return null;
            string[] nodes={"peace","passage","shore","shop","first_pay","first_letter"};
            for(int i=0;i<nodes.Length;i++) {
                ClearWritingQA();state=StoryEngine.NewGame(20260928);state.nodeId=nodes[i];
                state.journey.destination=i%3==0?"singapore":i%3==1?"penang":"rangoon";
                loadedWorld="";EnterNode(true);ResetView();yield return null;
                PersonalLetters.EnsureDraft(state).body="母亲：\n今晚在桌前给家里写一封信。";
                string originalDraft=state.personalLetterDraft.body;
                int money=state.money,health=state.health,steps=progress,turns=state.journey.turns;
                yield return WalkToWritingDeskForQA();
                WritingRequire(state.personalLetterDraft.body==originalDraft,"DRAFT_PRESERVED_"+loadedWorld);
                WritingRequire(state.money==money&&state.health==health&&progress==steps&&state.journey.turns==turns,"NO_GAMEPLAY_COST_"+loadedWorld);
                yield return CaptureWorld(Path.Combine(writingQAOutput,"writing_"+loadedWorld+"_seated"));
                CheckWritingShelter();
                Vector3 feet=player.transform.position;
                ClosePersonalLetter();
                WritingRequire(!letterEditor&&!writingSeated&&Vector3.Distance(player.transform.position,feet)<.001f,"STAND_WITHOUT_TELEPORT_"+loadedWorld);
                RequireFirstPerson("after writing "+loadedWorld);
                WritingCheckpoint("writing_"+loadedWorld,true);
                WritingRequire(state.personalLetterDraft.body==originalDraft,"RELOADED_DRAFT_"+loadedWorld);
                Debug.Log("WRITING_PLACE_PASSED "+loadedWorld+" "+state.journey.destination);
            }
            yield return RunIndoorInspectionReview();
            ClearWritingQA();state=StoryEngine.NewGame(20260928);state.nodeId="courier";EnterNode(true);
            carrying=true;journal=true;RefreshProps();Vector3 parcelPosition=player.transform.position;
            OpenPersonalLetter();WritingRequire(carrying&&!writingRequested&&!letterEditor&&!Blocked&&toastUntil>Time.time&&player.transform.position==parcelPosition,"CARRIED_JOB_PRESERVED");
            carrying=false;sideCarrying=true;OpenPersonalLetter();WritingRequire(sideCarrying&&!writingRequested&&!letterEditor,"SIDE_JOB_PRESERVED");
            sideCarrying=false;RefreshProps();OpenPersonalLetter();WritingRequire(writingRequested,"ROUTE_CAN_START");CancelWritingWalk();
            WritingRequire(!writingRequested&&walkRoute.Count==0,"ROUTE_CAN_CANCEL");
            state.nodeId="passage";state.flags.Add("aboard_passage");EnterNode(true);OpenPersonalLetter();
            WritingRequire(writingPlace==null&&!letterEditor&&!writingRequested,"SHIP_NO_ROADSIDE_EDITOR");
        }

        IEnumerator RunIndoorInspectionReview()
        {
            foreach(string node in new[]{"shop","records","dock"}) {
                ClearWritingQA();state=StoryEngine.NewGame(2700);state.nodeId=node;
                if(node=="dock") {
                    state.flags.Add("ledger");
                    WritingRequire(FirstDayChapter.TryBegin(state)&&FirstDayChapter.TriggerEncounter(state,1)&&FirstDayChapter.ChooseWork(state,"tally"),"DOCK_TALLY_FIXTURE");
                }
                loadedWorld="";EnterNode(true);ResetView();yield return null;
                WritingRequire(mission.activity=="inspect"&&mission.required>0,"INSPECTION_MISSION_"+node);
                for(int i=0;i<mission.required;i++) {
                    Vector3 destination=inspectionPositions[i];
                    yield return WritingWalkExactly(destination,node+"_paper_"+i);
                    AimInquiryAt(inspectionObjects[i].transform.position);
                    WritingRequire(RenderedSurfaceWithin(destination+Vector3.up*1.6f,Vector3.up,5),"INSPECTION_ROOF_"+node+"_"+i);
                    yield return CaptureWorld(Path.Combine(writingQAOutput,"inspection_"+node+"_"+i+"_room"));
                    Interact();yield return null;
                    WritingRequire(puzzle&&puzzleIndex==i,"PHYSICAL_INSPECTION_"+node+"_"+i);
                    int money=state.money,previous=progress,trust=state.trust;int answer=InspectionEntry().correct;
                    AnswerInspection((answer+1)%3);
                    WritingRequire(puzzle&&progress==previous&&state.money==money&&state.trust==trust,"WRONG_INSPECTION_NO_REWARD");
                    if(i==0)yield return CaptureWorld(Path.Combine(writingQAOutput,"inspection_"+node+"_evidence"));
                    AnswerInspection(answer);WritingRequire(!puzzle&&progress==previous+1&&inspected.Contains(i),"CORRECT_INSPECTION_PROGRESS");
                    int once=state.trust;AnswerInspection(answer);WritingRequire(state.trust==once&&progress==previous+1,"INSPECTION_REWARD_ONCE");
                    WritingCheckpoint("inspection_"+node+"_"+i,true);
                    WritingRequire(inspected.Contains(i)&&progress==previous+1,"INSPECTION_RELOAD_PRESERVES_INDEX");
                }
                WritingRequire(Complete,"INSPECTION_MISSION_COMPLETE_"+node);
                // A synthetic pre-update foot position inside the newly furnished
                // cabinet must recover nearby, without rewriting story progress.
                Collider furniture=null;
                foreach(var c in world.GetComponentsInChildren<Collider>()) {
                    if(!c.enabled||c.isTrigger||c.bounds.size.y<.5f||c.bounds.min.y>.6f||c.bounds.max.y<.7f)continue;
                    if(c.name.IndexOf("desk",StringComparison.OrdinalIgnoreCase)<0&&c.name.IndexOf("counter",StringComparison.OrdinalIgnoreCase)<0&&c.name.IndexOf("柜",StringComparison.Ordinal)<0)continue;
                    if(region.IsGround(c.bounds.center)){furniture=c;break;}
                }
                if(furniture) {
                    Vector3 blocked=new Vector3(furniture.bounds.center.x,.1f,furniture.bounds.center.z);
                    string story=JsonUtility.ToJson(state);Vector3 recovered=ResolveWorldSavedPosition(blocked);
                    WritingRequire(Vector3.Distance(blocked,recovered)>.05f&&Vector3.Distance(blocked,recovered)<=5.3f,"OBSTRUCTED_OLD_POSITION_RECOVERED_"+node);
                    WritingRequire(JsonUtility.ToJson(state)==story,"POSITION_RECOVERY_NO_STORY_EFFECTS");
                    Debug.Log("WRITING_OLD_POSITION_RECOVERY "+node+" object="+furniture.name+" before="+blocked+" after="+recovered);
                }
            }
        }

        IEnumerator WritingWalkExactly(Vector3 point,string label)
        {
            Physics.SyncTransforms();walkRoute=WalkPath.Find(player.transform.position,point,loadedWorld);routeIndex=0;
            WritingRequire(walkRoute.Count>0||Dist(point)<.55f,"PATH_"+label);
            float deadline=Time.realtimeSinceStartup+65;
            while(Dist(point)>.55f&&Time.realtimeSinceStartup<deadline) {yield return null;RequireFirstPerson(label);}
            WritingRequire(Dist(point)<=.55f,"ARRIVAL_"+label+" position="+player.transform.position);
            walkRoute.Clear();routeIndex=0;yield return null;
        }
        void ClearWritingQA()
        { if(letterEditor)ClosePersonalLetter();ClearChapterPanels();inquiryPanel=false;CancelWritingWalk(); }
        void WritingCheckpoint(string label,bool retainPosition)
        {
            Vector3 before=player.transform.position;
            var snapshot=new WorldSave{story=state,missionNode=state.nodeId,layoutVersion=2,progress=progress,carrying=carrying,sideCarrying=sideCarrying,
                inspected=new List<int>(inspected),px=before.x,pz=before.z};
            string file=Path.Combine(writingQATemp,"checkpoint.json");WorldSaveStore.Write(file,snapshot);
            bool recovered;WorldSave loaded=WorldSaveStore.Load(file,out recovered);
            WritingRequire(loaded!=null&&!recovered&&JsonUtility.ToJson(loaded.story)==JsonUtility.ToJson(state),"DISK_CHECKPOINT_"+label);
            ClearWritingQA();state=loaded.story;EnterNode(true);RestoreWorldSnapshot(loaded);ClearWritingQA();
            WritingRequire(WorldSaveStore.IsValid(loaded)&&StoryEngine.Validate(state),"VALID_RELOAD_"+label);
            if(retainPosition)WritingRequire(new Vector2(before.x-player.transform.position.x,before.z-player.transform.position.z).magnitude<.02f,"CLEAR_SAVED_POSITION_UNMOVED_"+label);
        }
        void CheckWritingShelter()
        {
            Transform room=world.transform.Find(writingPlace.title+" · sheltered writing place");
            WritingRequire(room,"REAL_WRITING_ROOM_"+loadedWorld);
            bool window=false;int walls=0;
            Transform enclosure = loadedWorld == "quanzhou" && ChenCourtyardHouse.Ready ? world.transform.Find("陈家整屋 · 红砖厝") : room;
            foreach(var c in enclosure.GetComponentsInChildren<BoxCollider>()) {
                if(c.name.IndexOf("window",StringComparison.OrdinalIgnoreCase)>=0)window=true;
                if(c.name.IndexOf("wall",StringComparison.OrdinalIgnoreCase)>=0)walls++;
            }
            WritingRequire((loadedWorld == "quanzhou" && ChenCourtyardHouse.Ready && enclosure.GetComponent<MeshCollider>()) || (window&&walls>=3),"WALLS_AND_WINDOW_"+loadedWorld);
            WritingRequire(RenderedSurfaceWithin(writingPlace.seatEye,Vector3.up,5),"VISIBLE_ROOF_ABOVE_SEAT_"+loadedWorld);
            WritingRequire(RenderedSurfaceWithin(writingPlace.approach+Vector3.up*1.6f,Vector3.up,5),"VISIBLE_ROOF_ABOVE_APPROACH_"+loadedWorld);
            WritingRequire(RenderedSurfaceWithin(writingPlace.seatEye,room.forward,3.3f),"VISIBLE_REAR_WALL_"+loadedWorld);
        }
        // Test rendered triangles rather than trusting a roof name or an invisible
        // collision box. No extra colliders are added to the live walking scene.
        bool RenderedSurfaceWithin(Vector3 origin,Vector3 direction,float distance)
        {
            Ray ray=new Ray(origin,direction.normalized);
            foreach(var filter in world.GetComponentsInChildren<MeshFilter>()) {
                Mesh mesh=filter.sharedMesh;Renderer renderer=filter.GetComponent<Renderer>();
                if(!mesh||!mesh.isReadable||!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||!renderer.bounds.IntersectRay(ray,out float near)||near>distance)continue;
                Matrix4x4 inverse=filter.transform.worldToLocalMatrix;
                Vector3 o=inverse.MultiplyPoint3x4(origin),d=inverse.MultiplyVector(ray.direction);
                Vector3[] vertices=mesh.vertices;int[] triangles=mesh.triangles;
                for(int i=0;i+2<triangles.Length;i+=3) {
                    Vector3 a=vertices[triangles[i]],e1=vertices[triangles[i+1]]-a,e2=vertices[triangles[i+2]]-a;
                    Vector3 p=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,p);
                    // Imported FBX meshes may store centimetres under a scale-100 root.
                    // A fixed local-space epsilon wrongly discarded their small triangles.
                    float parallelTolerance=1e-7f*e1.magnitude*e2.magnitude*d.magnitude;
                    if(det<=parallelTolerance)continue; // Still require an actual inward-facing surface.
                    Vector3 t=o-a;float u=Vector3.Dot(t,p)/det;if(u<0||u>1)continue;
                    Vector3 q=Vector3.Cross(t,e1);float v=Vector3.Dot(d,q)/det;if(v<0||u+v>1)continue;
                    float hit=Vector3.Dot(e2,q)/det;if(hit>.03f&&hit<=distance)return true;
                }
            }
            return false;
        }
        void CheckWritingIsolation()
        {
            foreach(var pair in writingQAFiles)WritingRequire(CharacterFileSnapshot(pair.Key)==pair.Value,"USER_SAVE_UNCHANGED_"+Path.GetFileName(pair.Key));
            foreach(var pair in writingQAPrefs)WritingRequire(PlayerPrefs.GetInt(pair.Key,-1)==pair.Value,"PREF_UNCHANGED_"+pair.Key);
            WritingRequire(PlayerPrefs.GetFloat("qiaopi-music-level",-1)==writingQAMusicLevel,"MUSIC_LEVEL_UNCHANGED");
            WritingRequire(AudioListener.pause&&!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"SILENT");
        }
    }
}
