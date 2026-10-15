using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool chapterPreview = Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-chapter-preview") >= 0;
        string ChapterSlotPath(bool preview) => Path.Combine(Application.persistentDataPath,
            preview ? "qiaopi-chapter-preview-v1.json" : "qiaopi-world-v1.json");
        string ChapterSlotButton => chapterPreview ? "返回原旅程" : "首章试玩 · 独立存档";

        void SwitchChapterSlot() { SwitchPreviewSlot(false,!chapterPreview); }
        void SwitchInquirySlot() { SwitchPreviewSlot(!inquiryPreview,false); }
        bool inquiryPreview = Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-inquiry-preview") >= 0;
        string InquirySlotPath => Path.Combine(Application.persistentDataPath,"qiaopi-inquiry-preview-v1.json");

        void SwitchPreviewSlot(bool nextInquiry,bool nextChapter)
        {
            if(QA||WorkPerformanceBusy||HomePerformanceBusy)return;
            if(letterEditor)ClosePersonalLetter();
            Save();if(!saveHealthy)return;
            string path=nextInquiry?InquirySlotPath:ChapterSlotPath(nextChapter);
            bool recovered;var target=WorldSaveStore.Load(path,out recovered);
            if(target==null&&(File.Exists(path)||File.Exists(path+".bak"))){Toast("这一程的存档暂时无法读取，仍留在当前旅程。");help=false;return;}
            inquiryPreview=nextInquiry;chapterPreview=nextChapter;
            state=target==null?(nextInquiry?InquiryDemo.Create():StoryEngine.NewGame()):target.story;
            LifeJourney.Ensure(state);
            inquiryPanel=dialogue=journal=map=help=resetAsk=puzzle=fieldNote=gallery=lifePanel=false;
            loadedWorld="";EnterNode(true);RestoreWorldSnapshot(target);
            dialogue=state.awaitingContinue||StoryEngine.GetScene(state).isEnding;Save();
            Toast(inquiryPreview?"独立调查试玩：从预设的谋生经历开始，原旅程另存。":chapterPreview?"首章试玩单独保存，可随时返回原旅程。":"已回到原旅程，两个试玩进度分别保留。");
        }

        void RestoreWorldSnapshot(WorldSave saved)
        {
            if (saved == null || saved.missionNode != state.nodeId) return;
            progress = Mathf.Clamp(saved.progress, 0, mission.required);
            carrying = saved.carrying; sideCarrying = saved.sideCarrying;
            inspected = saved.inspected ?? new List<int>();
            if(InInquiryScene) {
                // The versioned inquiry owns its progress. A legacy transport
                // or inspection task must not restore a carried bundle into it.
                progress=InquiryReadCount;carrying=false;sideCarrying=false;inspected.Clear();
            }
            controller.enabled = false;
            player.transform.position = saved.layoutVersion >= 2
                ? region.Clamp(new Vector3(saved.px, .1f, saved.pz)) : region.spawn + Vector3.up * .1f;
            player.transform.position = ResolveWorldSavedPosition(player.transform.position);
            controller.enabled = true;
            RefreshProps(); FaceCurrentTask(); UpdateCamera(true);
        }

        Vector3 ResolveWorldSavedPosition(Vector3 at)
        {
            at=ResolveHomeSavedPosition(at);Physics.SyncTransforms();
            if(!SavedPositionObstructed(at))return at;
            // New room furniture can occupy a former standing spot. Keep valid
            // saves exactly where they were; move only obstructed ones nearby.
            for(float radius=.65f;radius<=5.2f;radius+=.65f)for(int i=0;i<24;i++) {
                float angle=i*Mathf.PI/12;
                Vector3 next=at+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
                if(region.IsGround(next)&&!SavedPositionObstructed(next)&&
                    Physics.Raycast(next+Vector3.up*.2f,Vector3.down,.8f,~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore))return next;
            }
            return region.spawn+Vector3.up*.1f;
        }
        bool SavedPositionObstructed(Vector3 at)
        {
            foreach(var hit in Physics.OverlapCapsule(at+Vector3.up*.34f,at+Vector3.up*1.40f,.28f,
                ~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore))
                if(world&&hit.transform.IsChildOf(world.transform))return true;
            return false;
        }

        Vector3 ResolveHomeSavedPosition(Vector3 at)
        {
            if(!homeArt||loadedWorld!="quanzhou")return at;
            Physics.SyncTransforms();
            if(!HomeLoadObstructed(at,true))return at;
            // Old coordinates can now be occupied by a stove, bed or tabletop.
            // Move only those positions to nearby clear ground; preserve normal saves.
            for(float radius=.65f;radius<=3.9f;radius+=.65f)for(int i=0;i<16;i++) {
                float angle=i*Mathf.PI/8;
                Vector3 next=at+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
                if(region.IsGround(next)&&!HomeLoadObstructed(next,false))return next;
            }
            return new Vector3(-23.8f,.1f,-14f);
        }
        bool HomeLoadObstructed(Vector3 at,bool householdOnly)
        {
            foreach(var hit in Physics.OverlapCapsule(at+Vector3.up*.34f,at+Vector3.up*1.40f,.28f,
                ~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore))
                if(!householdOnly||hit.transform.IsChildOf(homeArt))return true;
            return false;
        }
    }
}
