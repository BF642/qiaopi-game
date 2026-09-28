using System;
using UnityEngine;
namespace Qiaopi.Editor
{
    public static class LifeIntegrationChecks
    {
        public static void Run()
        {
            var fresh=StoryEngine.NewGame(20260928);
            Require(WorldMissions.Get(fresh).world=="quanzhou","peace at home");
            foreach(var job in new[]{"dock","shop","courier"}){
                var s=StoryEngine.NewGame(20260928);s.nodeId="first_pay";s.journey.pendingJob=job;
                var m=WorldMissions.Get(s);Require(m.required==1,"short physical shift");
                string world=job=="dock"?"port":job=="shop"?"market":"postoffice";
                Require(m.world==world,"job selects actual district");
                var save=new WorldSave{story=s,missionNode=s.nodeId,layoutVersion=2,px=0,pz=-27};
                Require(WorldSaveStore.IsValid(save),"pending job save");
                if(m.activity=="deliver"){save.carrying=true;Require(WorldSaveStore.IsValid(save),"held work item saved");}
                var copy=JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(save));
                Require(WorldSaveStore.IsValid(copy)&&WorldMissions.Get(copy.story).world==world&&copy.story.journey.pendingJob==job,"job restored after load");
                Require(LifeJourney.CommitAction(s,job,out var _),"settle completed work");
                Require(string.IsNullOrEmpty(s.journey.pendingJob)&&WorldMissions.Get(s).world=="quarters","return to free living");
            }
            Debug.Log("LIFE_INTEGRATION_PASSED: three physical work districts, pending/carrying saves, one-task shifts, prologue home.");
        }
        static void Require(bool value,string text){if(!value)throw new Exception("LIFE_INTEGRATION "+text);}
    }
}
