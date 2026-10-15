using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        readonly Dictionary<string,string> inquiryQAFiles=new Dictionary<string,string>();
        readonly Dictionary<string,int> inquiryQAPrefs=new Dictionary<string,int>();
        string inquiryQAOutput,inquiryQATemp;
        int inquiryChecks,inquiryCheckpoints;
        void InquiryRequire(bool ok,string label){inquiryChecks++;if(!ok)throw new Exception("INQUIRY_QA_"+label);}
        IEnumerator RunInquiryQA()
        {
            var stack=new Stack<IEnumerator>();stack.Push(RunInquiryChecks());
            while(stack.Count>0) {
                object next=null;Exception failure=null;
                try{if(!stack.Peek().MoveNext())stack.Pop();else next=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){
                    while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch{}}
                    try{CheckInquiryIsolation();}catch(Exception e){Debug.LogException(e);}
                    Debug.LogError("INQUIRY_RUNTIME_FAILED: "+failure);Application.Quit(1);yield break;
                }
                if(next is IEnumerator)stack.Push((IEnumerator)next);else if(stack.Count>0)yield return next;
            }
            try{CheckInquiryIsolation();}catch(Exception e){Debug.LogError("INQUIRY_RUNTIME_FAILED: "+e);Application.Quit(1);yield break;}
            Debug.Log("INQUIRY_RUNTIME_ALL_PASSED assertions="+inquiryChecks+" checkpoints="+inquiryCheckpoints+"; three physical routes, normal-speed walking, isolated saves, wrong answers, helper and final decision. Automated duration is not human playtime.");
            Application.Quit(0);
        }
        IEnumerator RunInquiryChecks()
        {
            InquiryRequire(InquiryQA&&DetailQA&&QA,"ISOLATION_ENTRY");
            foreach(string slot in new[]{ChapterSlotPath(false),ChapterSlotPath(true),InquirySlotPath})foreach(string suffix in new[]{"",".bak",".tmp"})
                inquiryQAFiles[slot+suffix]=CharacterFileSnapshot(slot+suffix);
            foreach(string key in new[]{"qiaopi-music-enabled","qiaopi-sound-enabled","qiaopi-voice-enabled","qiaopi-mobile-initialized"})inquiryQAPrefs[key]=PlayerPrefs.GetInt(key,-1);
            AudioListener.pause=true;soundEnabled=voiceEnabled=false;WorldAtmosphere.SetSoundEnabled(false);musicDirector.SetEnabled(false,false);voiceSource.Stop();sfx.Stop();
            inquiryQATemp=Path.Combine(Path.GetTempPath(),"qiaopi-inquiry-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(inquiryQATemp);
            inquiryQAOutput=Path.Combine(inquiryQATemp,"captures");string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-qiaopi-inquiry-output")inquiryQAOutput=Path.GetFullPath(args[i+1]);
            Directory.CreateDirectory(inquiryQAOutput);Debug.Log("INQUIRY_QA_OUTPUT="+inquiryQAOutput);
            string[] routes={"desk","harbor","family"},destinations={"singapore","penang","rangoon"},resolutions={"reconcile","witness","match"};
            // All backgrounds are created by production rules, never patched into an impossible state.
            for(int route=0;route<3;route++) {
                ClearInquiryQA();state=InquiryDemo.Create(destinations[route]);loadedWorld="";
                InquiryRequire(StoryEngine.Choose(state,routes[route])&&StoryEngine.Continue(state),"SELECT_ROUTE_"+routes[route]);
                EnterNode(true);CheckInquirySameFrameNavigation(routes[route]+"_entry");ResetView();yield return null;
                InquiryRequire(InInquiryScene&&state.inquiry.stage=="collect"&&inquiryHelper,"ENTER_"+routes[route]);
                foreach(var choice in StoryEngine.GetScene(state).choices)InquiryRequire(!choice.enabled,"EARLY_STORY_LOCK");
                string prefix=Path.Combine(inquiryQAOutput,routes[route]);
                yield return CaptureWorld(prefix+"_01_entry");
                int money=state.money,health=state.health,trust=state.trust,family=state.family;
                for(int page=0;page<3;page++) {
                    int requestedPage=page;
                    ClearInquiryQA();yield return InquiryWalkTo(InquiryPlaces.Evidence(state.nodeId,page),routes[route]+" evidence "+page,()=>InquiryCanReadAt(requestedPage));
                    Interact();yield return null;
                    InquiryRequire(inquiryPanel&&inquiryPanelMode=="evidence"&&inquiryPage==page,"PHYSICAL_READ_"+page);
                    InquiryRequire((state.inquiry.evidenceMask&(1<<page))!=0,"EVIDENCE_RECORDED");
                    yield return CaptureWorld(prefix+"_02_paper_"+page);
                    string notice;int mask=state.inquiry.evidenceMask;
                    InquiryRequire(Investigation.ReadEvidence(state,page,out notice)&&state.inquiry.evidenceMask==mask,"REREAD_IDEMPOTENT");
                    InquiryCheckpoint(routes[route]+"_"+page);
                }
                InquiryRequire(state.money==money&&state.health==health&&state.trust==trust&&state.family==family,"READ_NO_REWARD");
                OpenInquiryPanel("board");ChooseInquiryValue("amount",1);ChooseInquiryValue("date",2);ChooseInquiryValue("address",0);
                SubmitInquiry();InquiryRequire(!Investigation.IsComplete(state)&&inquiryNotice.Length>0,"REMOTE_SUBMIT_REJECTED");
                ClearInquiryQA();
                Vector3 helperApproach=InquiryPlaces.Evidence(state.nodeId,0)+new Vector3(-2.4f,0,-.65f);
                yield return InquiryWalkTo(helperApproach,routes[route]+" helper",()=>InquiryHelperNearest());Interact();yield return null;
                InquiryRequire(inquiryPanel&&inquiryPanelMode=="help","PHYSICAL_HELPER");RequestInquiryHelp();
                InquiryRequire(state.inquiry.helpUsed&&Investigation.Validate(state),"HELP_SETTLED_PROVENANCE");
                string helped=JsonUtility.ToJson(state);RequestInquiryHelp();InquiryRequire(JsonUtility.ToJson(state)==helped,"HELP_NO_REPEAT");
                yield return CaptureWorld(prefix+"_03_old_friend");
                ClearInquiryQA();map=true;mapTab=0;yield return CaptureWorld(prefix+"_04_map");map=false;
                yield return WalkFirstPerson(mission.npcPosition,routes[route]+" return clerk");Interact();yield return null;
                InquiryRequire(inquiryPanel&&inquiryPanelMode=="board","PHYSICAL_BOARD");
                // Pick a still-available wrong option; assistance only removes one distractor.
                int[] correct={1,2,0};string[] fields={"amount","date","address"};bool pickedWrong=false;
                for(int field=0;field<3&&!pickedWrong;field++)foreach(var option in Investigation.Choices(state,fields[field])){
                    int index=int.Parse(option.id);if(option.enabled&&index!=correct[field]){ChooseInquiryValue(fields[field],index);pickedWrong=true;break;}
                }
                InquiryRequire(pickedWrong,"WRONG_OPTION_EXISTS");SubmitInquiry();
                InquiryRequire(!Investigation.IsComplete(state)&&inquiryNotice.Length>0,"WRONG_ANSWER_NO_ADVANCE");
                for(int field=0;field<3;field++)ChooseInquiryValue(fields[field],correct[field]);
                yield return CaptureWorld(prefix+"_05_board");SubmitInquiry();yield return null;
                InquiryRequire(Complete&&dialogue&&!inquiryPanel&&state.money==money&&state.trust==trust,"COMPLETE_WITHOUT_FREE_REWARD");
                InquiryCheckpoint(routes[route]+"_complete");dialogue=true;EnsureConversation();UpdateConversation();
                yield return CaptureWorld(prefix+"_06_decision");Choose(resolutions[route]);
                InquiryRequire(state.awaitingContinue&&state.flags.Contains("resolved"),"ORIGINAL_RESOLUTION");Next();
                InquiryRequire(state.nodeId=="storms"&&StoryEngine.Validate(state),"CONTINUES_TO_LIFE");
                Debug.Log("INQUIRY_ROUTE_PASSED "+routes[route]+" destination="+destinations[route]);
            }
        }
        bool InquiryCanReadAt(int index)
        {
            // Mirror the real E resolver: help wins only when nearer, then
            // evidence is checked in order. A readable wrong page is not arrival.
            if(InquiryHelperNearest())return false;
            for(int i=0;i<3;i++)if(FirstDayCanReach(InquiryPlaces.Evidence(state.nodeId,i),2.1f))return i==index;
            return false;
        }
        IEnumerator InquiryWalkTo(Vector3 target,string label,Func<bool> canInteract)
        {
            // Walk until the actual scene interaction is available. The 0.55m
            // coordinate tolerance was stricter than the player-facing 2.1/2.3m
            // rules and could reject a clear, usable place inside the doorway.
            // The caller still presses real E and verifies the exact panel/page.
            Physics.SyncTransforms();walkRoute=WalkPath.Find(player.transform.position,target,loadedWorld);routeIndex=0;
            InquiryRequire(walkRoute.Count>0||canInteract(),"PATH_"+label);
            Vector3 routeEnd=walkRoute.Count>0?walkRoute[walkRoute.Count-1]:player.transform.position;
            Debug.Log("INQUIRY_WALK_ROUTE "+label+" start="+player.transform.position+" target="+target+" end="+routeEnd+" points="+walkRoute.Count);
            if(new Vector2(routeEnd.x-target.x,routeEnd.z-target.z).magnitude>.55f)LogInquiryWalkGeometry(target,label+" non-exact route endpoint");
            float deadline=Time.realtimeSinceStartup+65,lastProgress=Time.realtimeSinceStartup;
            Vector3 lastPosition=player.transform.position;bool loggedStall=false;
            while(!canInteract()&&Time.realtimeSinceStartup<deadline) {
                yield return null;RequireFirstPerson(label);
                foreach(var hit in Physics.OverlapSphere(cam.transform.position,.045f,~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore))
                    InquiryRequire(!world||!hit.transform.IsChildOf(world.transform),"EYE_CLEAR_"+label);
                if(new Vector2(lastPosition.x-player.transform.position.x,lastPosition.z-player.transform.position.z).magnitude>.08f){lastPosition=player.transform.position;lastProgress=Time.realtimeSinceStartup;}
                else if(!loggedStall&&Time.realtimeSinceStartup-lastProgress>3){loggedStall=true;LogInquiryWalkGeometry(target,label+" stalled");}
            }
            bool reached=canInteract();
            walkRoute.Clear();routeIndex=0;
            if(!reached) {
                LogInquiryWalkGeometry(target,label+" failed");
                yield return CaptureWorld(Path.Combine(inquiryQAOutput,"failure_"+label.Replace(" ","_").Replace("/","_")));
            }
            InquiryRequire(reached,"REACHED_INTERACTION_"+label+" at="+player.transform.position+" target="+target);
            yield return null;
            InquiryRequire(canInteract(),"INTERACTION_STILL_AVAILABLE_"+label);
            Debug.Log("INQUIRY_WALK_PASSED "+label+" position="+player.transform.position+" targetDistance="+Dist(target).ToString("F3")+" prompt="+InquiryPrompt());
        }
        void LogInquiryWalkGeometry(Vector3 target,string label)
        {
            const int mask=~((1<<2)|(1<<29)|(1<<30));
            var details=new List<string>();
            foreach(var collider in Physics.OverlapCapsule(target+Vector3.up*.55f,target+Vector3.up*1.65f,.35f,mask,QueryTriggerInteraction.Ignore))
                details.Add(collider.name+" center="+collider.bounds.center+" size="+collider.bounds.size);
            RaycastHit ground;string floor=Physics.Raycast(target+Vector3.up*4,Vector3.down,out ground,8,mask,QueryTriggerInteraction.Ignore)?ground.collider.name+" at="+ground.point:"none";
            RaycastHit sight;string line=Physics.Linecast(player.transform.position+Vector3.up*.9f,target+Vector3.up*.9f,out sight,mask,QueryTriggerInteraction.Ignore)?sight.collider.name+" at="+sight.point:"clear";
            Debug.LogWarning("INQUIRY_WALK_DIAGNOSTIC "+label+" player="+player.transform.position+" target="+target+" remaining="+(walkRoute.Count-routeIndex)+" blockedUI="+Blocked+" targetCapsule=["+string.Join("; ",details)+"] ground="+floor+" interactionSightline="+line);
        }
        void ClearInquiryQA(){ClearChapterPanels();inquiryPanel=false;inquiryNotice="";walkRoute.Clear();routeIndex=0;}
        void CheckInquirySameFrameNavigation(string label)
        {
            // Intentionally before any frame yield: Destroy(collider) by itself
            // used to leave these markers active while the path cache was built.
            foreach(var collider in targetMarker.GetComponentsInChildren<Collider>(true))
                InquiryRequire(!collider.enabled,"MARKER_COLLISION_DISABLED_IMMEDIATELY_"+label);
            if(InquiryReadCount>=3)return;
            Physics.SyncTransforms();Vector3 target=InquiryTarget();
            var route=WalkPath.Find(player.transform.position,target,loadedWorld);
            bool exact=route.Count>0&&new Vector2(route[route.Count-1].x-target.x,route[route.Count-1].z-target.z).sqrMagnitude<.0025f;
            if(!exact)LogInquiryWalkGeometry(target,"same-frame checkpoint "+label);
            InquiryRequire(exact,"SAME_FRAME_EXACT_PATH_"+label);
            Debug.Log("INQUIRY_SAME_FRAME_PATH_PASSED "+label+" target="+target);
        }
        void InquiryCheckpoint(string name)
        {
            Vector3 at=player.transform.position;
            var snapshot=new WorldSave{story=state,missionNode=state.nodeId,layoutVersion=2,progress=progress,
                carrying=false,sideCarrying=false,inspected=new List<int>(),px=at.x,pz=at.z};
            string path=Path.Combine(inquiryQATemp,"checkpoint.json");WorldSaveStore.Write(path,snapshot);
            bool recovered;var loaded=WorldSaveStore.Load(path,out recovered);
            InquiryRequire(loaded!=null&&!recovered&&JsonUtility.ToJson(loaded.story)==JsonUtility.ToJson(state),"DISK_ROUNDTRIP_"+name);
            ClearInquiryQA();state=loaded.story;EnterNode(true);RestoreWorldSnapshot(loaded);ClearInquiryQA();
            CheckInquirySameFrameNavigation(name);
            InquiryRequire(StoryEngine.Validate(state)&&progress==InquiryReadCount,"RESTORE_"+name);inquiryCheckpoints++;
        }
        void CheckInquiryIsolation()
        {
            foreach(var f in inquiryQAFiles)InquiryRequire(CharacterFileSnapshot(f.Key)==f.Value,"USER_SAVE_UNCHANGED_"+Path.GetFileName(f.Key));
            foreach(var p in inquiryQAPrefs)InquiryRequire(PlayerPrefs.GetInt(p.Key,-1)==p.Value,"PREF_UNCHANGED_"+p.Key);
            InquiryRequire(AudioListener.pause&&!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"SILENT");
        }
    }
}
