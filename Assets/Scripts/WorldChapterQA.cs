using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool ChapterQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-chapter-test") >= 0;
        bool ChapterArtReview => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-chapter-art-review") >= 0;
        bool ChapterHomeReview => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-chapter-home-review") >= 0;
        readonly Dictionary<string,string> chapterQAFiles = new Dictionary<string,string>();
        readonly Dictionary<string,int> chapterQAPrefs = new Dictionary<string,int>();
        string chapterQAOutput, chapterQATemp;
        int chapterChecks, chapterCheckpoints, chapterReadCharacters;
        float chapterStarted;

        void ChapterRequire(bool condition, string message)
        { chapterChecks++; if (!condition) throw new Exception("CHAPTER_QA_" + message); }

        IEnumerator RunChapterQA()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(RunChapterChecks());
            while (stack.Count > 0) {
                object next = null; Exception failure = null;
                try { if (!stack.Peek().MoveNext()) stack.Pop(); else next = stack.Peek().Current; }
                catch(Exception e) { failure = e; }
                if (failure != null) {
                    while(stack.Count>0) { try { (stack.Pop() as IDisposable)?.Dispose(); } catch {} }
                    try { CheckChapterIsolation(); } catch(Exception e) { Debug.LogException(e); }
                    Debug.LogError("CHAPTER_RUNTIME_FAILED: " + failure); Application.Quit(1); yield break;
                }
                if(next is IEnumerator) stack.Push((IEnumerator)next);
                else if(stack.Count>0) yield return next;
            }
            try { CheckChapterIsolation(); }
            catch(Exception e) { Debug.LogError("CHAPTER_RUNTIME_FAILED: " + e); Application.Quit(1); yield break; }
            SaveChapterMeasurements();
            Debug.Log((ChapterHomeReview ? "CHAPTER_HOME_REVIEW_PASSED" : "CHAPTER_RUNTIME_ALL_PASSED") +
                " checkpoints="+chapterCheckpoints+" assertions="+chapterChecks+" destination="+LifeJourney.DestinationId(state)+
                " elapsedSeconds="+(Time.realtimeSinceStartup-chapterStarted).ToString("F1")+
                " readableCharacters="+chapterReadCharacters+
                "; normal-speed physical walking, choices, isolated disk reloads; automated input skips human reading/typing; not a human playtime measurement.");
            Application.Quit(0);
        }

        IEnumerator RunChapterChecks()
        {
            ChapterRequire(ChapterQA && DetailQA && QA, "ISOLATION_ENTRY");
            foreach(bool preview in new[]{false,true}) foreach(string suffix in new[]{"",".bak",".tmp"}) {
                string path=ChapterSlotPath(preview)+suffix; chapterQAFiles[path]=CharacterFileSnapshot(path);
            }
            foreach(string key in new[]{"qiaopi-music-enabled","qiaopi-sound-enabled","qiaopi-voice-enabled","qiaopi-mobile-initialized"})
                chapterQAPrefs[key]=PlayerPrefs.GetInt(key,-1);
            AudioListener.pause=true; soundEnabled=voiceEnabled=false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false,false);voiceSource.Stop();sfx.Stop();
            chapterQATemp=Path.Combine(Path.GetTempPath(),"qiaopi-chapter-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(chapterQATemp);
            chapterQAOutput=Path.Combine(chapterQATemp,"captures");
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-qiaopi-chapter-output")chapterQAOutput=Path.GetFullPath(args[i+1]);
            Directory.CreateDirectory(chapterQAOutput);Debug.Log("CHAPTER_QA_OUTPUT="+chapterQAOutput);
            chapterStarted=Time.realtimeSinceStartup;
            ClearChapterPanels(); state=StoryEngine.NewGame(2026100825); loadedWorld="";EnterNode(true);ResetView();BeginChapterMeasurements();yield return null;
            ChapterRequire(InHomeChapter&&state.nodeId=="peace", "NEW_GAME_HOME");
            ChapterRequire(homeWife&&homeMother&&homeSister,"THREE_FAMILY_MEMBERS");
            if(Array.IndexOf(args,"-qiaopi-native-wife-test")>=0){
                var wifeRig=homeWife.GetComponent<NativeCharacterRig>();
                ChapterRequire(wifeRig&&wifeRig.HasImportedMotion,"WIFE_NATIVE_MOTION_IN_GAME");
                string resource=ModelLibrary.MeshyResource("Wife");
                ChapterRequire(resource=="Models/WifeMeshyMotion/WifeMeshyMotion","WIFE_EXPECTED_RESOURCE");
                var wifeSet=NativeMotion.LoadQualifiedSet(resource);
                bool reviewed=false,original8K=false;
                if(wifeSet&&wifeSet.reviewedSkin)foreach(var skin in homeWife.GetComponentsInChildren<SkinnedMeshRenderer>()){
                    reviewed|=skin.sharedMesh==wifeSet.reviewedSkin;
                    original8K|=skin.sharedMaterial&&skin.sharedMaterial.mainTexture&&skin.sharedMaterial.mainTexture.width==8192;
                }
                ChapterRequire(reviewed,"WIFE_FACE_PRESERVED_SKIN_IN_GAME");
                ChapterRequire(original8K,"WIFE_NATIVE_8K_IN_GAME");
                Debug.Log("WIFE_PRODUCTION_NATIVE_RESOURCE="+resource+" reviewedSkin=true native8K=true");
            }
            if(Array.IndexOf(args,"-qiaopi-native-aman-test")>=0){
                var amanRig=homeSister.GetComponent<NativeCharacterRig>();
                ChapterRequire(amanRig&&amanRig.HasImportedMotion,"AMAN_NATIVE_MOTION_IN_GAME");
                var amanSet=NativeMotion.LoadQualifiedSet(ModelLibrary.MeshyResource("Aman"));
                bool cloth=false;
                if(amanSet&&amanSet.reviewedSkin)foreach(var skin in homeSister.GetComponentsInChildren<SkinnedMeshRenderer>())cloth|=skin.sharedMesh==amanSet.reviewedSkin;
                ChapterRequire(cloth,"AMAN_REVIEWED_CLOTH_IN_GAME");
                Debug.Log("AMAN_PRODUCTION_NATIVE_RESOURCE="+ModelLibrary.MeshyResource("Aman"));
            }
            Vector3 validAt=player.transform.position;
            ChapterRequire(ResolveHomeSavedPosition(validAt)==validAt,"VALID_HOME_POSITION_UNCHANGED");
            Vector3 legacyTable=new Vector3(-23f,.1f,-4.65f);
            Vector3 recoveredTable=ResolveHomeSavedPosition(legacyTable);
            ChapterRequire(Vector3.Distance(recoveredTable,legacyTable)<4&&!HomeLoadObstructed(recoveredTable,false),"OLD_TABLE_POSITION_RECOVERED_NEARBY");
            yield return CaptureWorld(Path.Combine(chapterQAOutput,"01_quanzhou_home"));
            string[] actions={"walk_market","tea","put_basket","lift_grain","store_grain","return","share_meal"};
            for(int step=0;step<actions.Length;step++) {
                ChapterRequire(InHomeChapter&&HomeChapter.Progress(state)==step,"HOME_STEP_"+step);
                yield return ChapterReach(HomeTarget(),"home_"+state.homeChapter.stage);
                string action=actions[step];
                if(step==0||step==1||step==5||step==6) {
                    ChapterRequire(homePanel,"HOME_PANEL_"+step);chapterReadCharacters+=HomeWords().Length;
                    yield return CaptureWorld(Path.Combine(chapterQAOutput,"home_"+state.homeChapter.stage));
                    SelectHomeAction(action);
                }
                ChapterRequire(HomeChapter.Progress(state)==step+1,"HOME_ACTION_"+action);
                string committed=JsonUtility.ToJson(state);
                SelectHomeAction(action);
                ChapterRequire(JsonUtility.ToJson(state)==committed,"HOME_REPEAT_"+action);
                if(HomePerformanceBusy) {
                    yield return new WaitForSeconds(.20f);
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(chapterQAOutput,"home_action_"+action+".png"));
                    yield return null;
                    if(step==2) {
                        CancelHomePresentation();
                        ChapterRequire(JsonUtility.ToJson(state)==committed&&!HomePerformanceBusy,"CANCEL_PRESENTATION_PRESERVES_COMMIT");
                    }
                    float until=Time.realtimeSinceStartup+4;
                    while(HomePerformanceBusy&&Time.realtimeSinceStartup<until)yield return null;
                    ChapterRequire(!HomePerformanceBusy,"HOME_PRESENTATION_FINISH_"+action);
                    ChapterRequire(JsonUtility.ToJson(state)==committed,"PRESENTATION_NO_BUSINESS_MUTATION_"+action);
                }
                if(step==1||step==3) {
                    UpdateFirstPersonPresentation();ChapterRequire(HomeCarrying&&heldRig.activeInHierarchy,"HOME_HELD_ITEM_"+step);
                    yield return CaptureWorld(Path.Combine(chapterQAOutput,"home_carry_"+step));
                }
                if(ChapterArtReview&&step==1&&homeMarketSign) {
                    AimNanyangQACamera(homeMarketSign.transform.position);
                    yield return CaptureWorld(Path.Combine(chapterQAOutput,"detail_stall_sign"));FaceCurrentTask();
                }
                if(ChapterArtReview&&step==5&&homeSlate) {
                    AimNanyangQACamera(homeSlate.transform.position);
                    yield return CaptureWorld(Path.Combine(chapterQAOutput,"detail_written_slate"));FaceCurrentTask();
                }
                if(step==5)ChapterRequire(heldHands&&heldHands.activeSelf,"HANDS_RESTORED_AFTER_LESSON");
                // Leave the first walk uninterrupted so the wife must really
                // travel to the stall before the market conversation can open.
                // Keep the last household walk continuous: a reload would place
                // relatives directly at dinner and hide a broken physical route.
                if(step>0&&step<5){ChapterCheckpoint("home_"+step);yield return null;}
            }
            ChapterRequire(!InHomeChapter&&HomeChapter.HasMemory(state,"meal")&&dialogue,"HOME_MEAL_TO_CONVERSATION");
            if(ChenCourtyardHouse.Ready)ChapterRequire(Vector3.Distance(mission.npcPosition,HomeDinner)<.01f,"FAMILY_STORY_ANCHOR_AT_INDOOR_TABLE");
            ClearChapterPanels();
            float familyDeadline=Time.realtimeSinceStartup+20;
            while(!ChapterFamilySeated()&&Time.realtimeSinceStartup<familyDeadline)yield return null;
            if(!ChapterFamilySeated()) {
                Debug.Log("FAMILY_SEAT_DIAGNOSTIC mother="+homeMother.position+" route="+homeMotherRouteIndex+"/"+homeMotherRoute.Count+" wife="+homeWife.position+" route="+homeWifeRouteIndex+"/"+homeWifeRoute.Count+" sister="+homeSister.position+" route="+homeSisterRouteIndex+"/"+homeSisterRoute.Count+" blocked="+Blocked+" shared="+state.homeChapter.mealShared+" player="+player.transform.position);
                yield return CaptureWorld(Path.Combine(chapterQAOutput,"family_seat_failure"));
            }
            ChapterRequire(ChapterFamilySeated(),"THREE_FAMILY_MEMBERS_REACH_SEATS");
            yield return new WaitForSeconds(.7f);
            AimNanyangQACamera(new Vector3(-23f,1.05f,ChenCourtyardHouse.Ready ? -.3f : -4.65f));
            yield return CaptureWorld(Path.Combine(chapterQAOutput,"family_seated"));
            ChapterCheckpoint("home_complete");yield return null;
            ChapterRequire(!HomeCarrying&&HomeChapter.HasMemory(state,"market")&&HomeChapter.HasMemory(state,"lesson"),"HOME_RELOAD_MEMORIES");
            if(ChapterArtReview)yield return ChapterHomeArtShots();
            if(ChapterHomeReview){yield return CaptureWorld(Path.Combine(chapterQAOutput,"home_finished"));yield break;}

            string[] nodes={"peace","pressure","home","funding","farewell","passage","shore"};
            string[] choices={"meal","neighbors","ledger","guarantor","mother","share","dock"};
            for(int i=0;i<nodes.Length;i++) {
                ClearChapterPanels();ChapterRequire(state.nodeId==nodes[i],"STORY_NODE_"+nodes[i]);
                if(loadedWorld=="quanzhou") {
                    ChapterRequire(homeArt&&homeArt.Find("陈家可生活的廊屋"),"HOUSE_PERSISTS_"+state.nodeId);
                    if(state.nodeId!="peace")ChapterRequire(!homeWife&&!homeMother&&!homeSister&&npc&&Vector3.Distance(npc.transform.position,mission.npcPosition)<.6f,"STORY_FAMILY_REMAINS_AT_TASK_"+state.nodeId);
                }
                if(state.nodeId=="home") {
                    yield return ChapterReach(mission.itemPosition,"collect_luggage");
                    ChapterRequire(Complete,"LUGGAGE_COLLECTED");
                }
                if(state.nodeId=="passage"&&loadedWorld=="harbor") {
                    yield return ChapterReach(mission.npcPosition,"board_ship");
                    ChapterRequire(loadedWorld=="ship"&&!dialogue,"BOARDED_PHYSICAL_SHIP");
                }
                yield return ChapterReach(mission.npcPosition,"story_"+nodes[i]);
                ChapterRequire(dialogue,"STORY_CONVERSATION_"+nodes[i]);EnsureConversation();
                foreach(var line in spokenLines)chapterReadCharacters+=line.text.Length;
                spokenIndex=Math.Max(0,spokenLines.Count-1);UpdateConversation();
                if(i==1||i==4||i==6)yield return CaptureWorld(Path.Combine(chapterQAOutput,"story_"+nodes[i]));
                int decisions=state.decisions;Choose(choices[i]);
                ChapterRequire(state.awaitingContinue&&state.decisions==decisions+1,"STORY_CHOICE_"+nodes[i]);
                chapterReadCharacters+=state.lastOutcome.Length;Next();yield return null;
                ChapterCheckpoint("story_"+nodes[i]);yield return null;
            }
            ChapterRequire(InFirstDay&&state.firstDay.stage=="work"&&state.firstDay.route=="dock","FIRST_DAY_FROM_ACTUAL_SHORE");
            yield return CompleteFirstDayQAUnit("chapter_first_work");TickFirstDay();yield return null;
            ChapterRequire(DayStage=="encounter","FIRST_ENCOUNTER");
            yield return ReachFirstDayQAInteraction(FirstDayContact,"chapter meet coworker");
            ChapterRequire(firstDayPanel,"FIRST_WORK_CHOICE_PANEL");chapterReadCharacters+=FirstDaySituation().Length;
            yield return CaptureWorld(Path.Combine(chapterQAOutput,"first_work_choice"));
            SelectFirstDayWork("help");ChapterCheckpoint("first_work_chosen");yield return null;
            int count=0;while(!Complete&&count++<4)yield return CompleteFirstDayQAUnit("chapter_help_"+count);
            ChapterRequire(Complete,"FIRST_WORK_DONE");
            yield return ReachFirstDayQAInteraction(mission.npcPosition,"chapter collect wages");
            ChapterRequire(DayStage=="evening"&&state.firstDay.workSettled,"FIRST_WAGES_SETTLED");ChapterCheckpoint("first_wages");yield return null;
            yield return ReachFirstDayQAInteraction(mission.npcPosition,"chapter evening meal");
            chapterReadCharacters+=FirstDaySituation().Length;ChooseFirstDayEvening("together");yield return null;
            ChapterRequire(DayStage=="letter","EVENING_TO_LETTER");
            yield return WalkToWritingDeskForQA();
            ChapterRequire(letterEditor&&writingSeated&&NearWritingDesk,"REAL_WRITING_SEAT");
            var draft=PersonalLetters.EnsureDraft(state);
            draft.recipient="wife";draft.intent="truth";draft.amount=2;
            draft.body="给你：\n还记得咱们一起买茶的下午。今日帮伤脚的同工搬完货，才知道一份工钱要用多少力气。\n我先留够饭钱，把余下的一点寄回家。阿满写的归字，我一直记着。";
            string original=draft.body,token=draft.token,notice;int money=state.money,incoming=state.letters.FindAll(x=>x.incoming).Count;
            personalLetterPreview=true;yield return CaptureWorld(Path.Combine(chapterQAOutput,"own_letter_at_desk"));
            ChapterRequire(PersonalLetters.TrySend(state,token,out notice),"SEND_OWN_LETTER_"+notice);
            ChapterRequire(state.money==money-2&&state.letters.Exists(x=>!x.incoming&&x.body==original),"ORIGINAL_TEXT_AND_SILVER");
            ChapterRequire(!PersonalLetters.TrySend(state,token,out notice)&&state.money==money-2,"NO_DUPLICATE_SEND");
            string reply=state.firstDay.pendingReply.body;
            ChapterRequire(reply.Contains("茶")&&!string.IsNullOrWhiteSpace(reply),"HOME_MEMORY_IN_REPLY");
            AfterFirstDayLetterSent();ChapterCheckpoint("letter_sent");yield return null;
            yield return ReachFirstDayQAInteraction(FirstDayBedPoint,"chapter sleep");
            ChapterRequire(DayStage=="morning"&&state.letters.FindAll(x=>x.incoming).Count==incoming,"NO_OVERNIGHT_CROSS_SEA_REPLY");
            yield return ReachFirstDayQAInteraction(mission.npcPosition,"chapter next morning");
            chapterReadCharacters+=FirstDaySituation().Length;FinishFirstDayMorning();yield return null;
            ChapterRequire(DayStage=="reply","WEEKS_LATER");ChapterCheckpoint("waiting_reply");yield return null;
            yield return ReachFirstDayQAInteraction(mission.npcPosition,"chapter collect Quanzhou reply");
            ChapterRequire(state.firstDay.replyCollected&&journal&&!InFirstDay,"FIRST_CHAPTER_COMPLETE");
            ChapterRequire(state.letters.FindAll(x=>x.incoming).Count==incoming+1&&state.letters.Exists(x=>x.incoming&&x.body==reply),"ONE_SNAPSHOT_REPLY");
            chapterReadCharacters+=reply.Length;yield return CaptureWorld(Path.Combine(chapterQAOutput,"first_reply"));
            ChapterCheckpoint("first_chapter_complete");yield return null;
            ChapterRequire(LifeJourney.IsActive(state)&&LifeJourney.Actions(state).FindAll(x=>x.physical&&x.enabled).Count==3,"FREEDOM_AFTER_FIRST_REPLY");
        }

        bool ChapterFamilySeated()
        {
            return homeMother&&homeWife&&homeSister&&CharacterPerformance.For(homeMother.gameObject).IsSeated&&
                CharacterPerformance.For(homeWife.gameObject).IsSeated&&CharacterPerformance.For(homeSister.gameObject).IsSeated;
        }
        IEnumerator ChapterHomeArtShots()
        {
            // Inspection detours are opt-in and are not part of the required route.
            Vector3[] stops={new Vector3(-25.15f,0,-9.2f),new Vector3(-27.7f,0,-5.5f),new Vector3(-23,0,-7.15f)};
            Vector3[] looks={new Vector3(-29.75f,1.1f,-7.2f),new Vector3(-30.55f,.78f,-3.1f),new Vector3(-23,1.1f,-4.4f)};
            if(ChenCourtyardHouse.Ready) {
                stops[0]=new Vector3(-27.4f,0,-8.9f);stops[1]=new Vector3(-28.7f,0,-3.4f);stops[2]=HomeDinner;
                looks[2]=new Vector3(-23,1.1f,-.3f);
            }
            string[] labels={"detail_kitchen","detail_bedroom","detail_family_meal"};
            for(int i=0;i<stops.Length;i++) {
                ClearChapterPanels();yield return WalkFirstPerson(stops[i],labels[i]);
                AimNanyangQACamera(looks[i]);toastUntil=0;
                yield return CaptureWorld(Path.Combine(chapterQAOutput,labels[i]));
            }
        }

        IEnumerator ChapterReach(Vector3 target,string label)
        {
            ClearChapterPanels();yield return WalkFirstPerson(target,label);
            if(ChenCourtyardHouse.Ready && (label=="home_lesson" || label=="home_meal")) {
                walkRoute=WalkPath.Find(player.transform.position,target,loadedWorld);routeIndex=0;
                float arrival=Time.time+15;
                while(Dist(target)>.4f && Time.time<arrival)yield return null;
                ChapterRequire(Dist(target)<.5f,"ROOM_ANCHOR_"+label);walkRoute.Clear();
            }
            if(InHomeChapter&&state.homeChapter.stage=="market") {
                float deadline=Time.realtimeSinceStartup+25;
                while(!HomeWifeAtMarket()&&Time.realtimeSinceStartup<deadline)yield return null;
                ChapterRequire(HomeWifeAtMarket(),"WIFE_WALKS_TO_MARKET");
            }
            Interact();yield return null;
            if(homePanel&&HomeSpeaker()=="wife") {
                float settleDeadline=Time.realtimeSinceStartup+4;
                while(HomeConversationSettling&&Time.realtimeSinceStartup<settleDeadline)yield return null;
                ChapterRequire(!HomeConversationSettling,"WIFE_CONVERSATION_SETTLED");
                ChapterRequire(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),
                    new Vector2(homeWife.position.x,homeWife.position.z))>=1.35f,"WIFE_CONVERSATION_CLEARANCE");
                float faceDeadline=Time.realtimeSinceStartup+2f;
                while(!ChapterWifeFacesPlayer()&&Time.realtimeSinceStartup<faceDeadline)yield return null;
                ChapterRequire(ChapterWifeFacesPlayer(),"WIFE_FACES_PLAYER_WHILE_TALKING");
            }
        }
        bool ChapterWifeFacesPlayer()
        {
            if(!homeWife)return false;
            Vector3 toward=player.transform.position-homeWife.position;toward.y=0;
            return toward.sqrMagnitude>.01f&&Vector3.Dot(homeWife.forward,toward.normalized)>.85f;
        }
        void ClearChapterPanels()
        {
            ClearFirstDayQAPanels();homePanel=nanyangPanel=tradePanel=workTalkPanel=false;
        }
        void ChapterCheckpoint(string name)
        {
            Vector3 at=player.transform.position;
            var snapshot=new WorldSave{story=state,missionNode=state.nodeId,layoutVersion=2,progress=progress,
                carrying=carrying,sideCarrying=sideCarrying,inspected=new List<int>(inspected),px=at.x,pz=at.z};
            string path=Path.Combine(chapterQATemp,"checkpoint.json");
            WorldSaveStore.Write(path,snapshot);bool recovered;var loaded=WorldSaveStore.Load(path,out recovered);
            ChapterRequire(loaded!=null&&!recovered&&JsonUtility.ToJson(loaded.story)==JsonUtility.ToJson(state),"DISK_CHECKPOINT_"+name);
            ClearChapterPanels();state=loaded.story;EnterNode(true);RestoreWorldSnapshot(loaded);ClearChapterPanels();
            ChapterRequire(StoryEngine.Validate(state)&&WorldSaveStore.IsValid(loaded),"VALID_RELOAD_"+name);
            chapterCheckpoints++;Debug.Log("CHAPTER_CHECKPOINT "+name+" node="+state.nodeId+" firstDay="+DayStage);
        }
        void CheckChapterIsolation()
        {
            foreach(var item in chapterQAFiles)ChapterRequire(CharacterFileSnapshot(item.Key)==item.Value,"PLAYER_FILE_UNCHANGED_"+Path.GetFileName(item.Key));
            foreach(var item in chapterQAPrefs)ChapterRequire(PlayerPrefs.GetInt(item.Key,-1)==item.Value,"PREFERENCE_UNCHANGED_"+item.Key);
            ChapterRequire(AudioListener.pause&&!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"MUTED");
        }
    }
}
