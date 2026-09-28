using System;
using System.IO;
using UnityEngine;
namespace Qiaopi.Editor
{
    public static class SaveChecks
    {
        public static void Run()
        {
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/save-checks"));Directory.CreateDirectory(dir);
            string path=Path.Combine(dir,"state.json");
            foreach(var p in new[]{path,path+".bak",path+".tmp"})if(File.Exists(p))File.Delete(p);
            var s=new WorldSave{story=StoryEngine.NewGame(19060923),missionNode="peace",px=0,pz=-10};
            WorldSaveStore.Write(path,s);
            s.sideCarrying=true;s.px=3;WorldSaveStore.Write(path,s);
            bool recovered;var read=WorldSaveStore.Load(path,out recovered);
            Require(read!=null&&read.sideCarrying&&read.px==3&&!recovered,"carrying roundtrip");
            Require(StoryEngine.Choose(s.story,"meal"),"new prologue choice");s.progress=1;WorldSaveStore.Write(path,s);
            read=WorldSaveStore.Load(path,out recovered);Require(read.story.awaitingContinue,"pending result roundtrip");
            File.WriteAllText(path,"{broken");read=WorldSaveStore.Load(path,out recovered);
            Require(recovered&&read!=null&&read.sideCarrying,"backup recovery");
            read.inspected.Add(8);Require(!WorldSaveStore.IsValid(read),"invalid inspected index");read.inspected.Clear();
            read.px=float.NaN;Require(!WorldSaveStore.IsValid(read),"NaN position");
            var legacy=new WorldSave{story=new GameState(),missionNode="home",px=0,pz=-10};
            WorldSaveStore.Write(path,legacy);read=WorldSaveStore.Load(path,out recovered);
            Require(read!=null&&read.story.nodeId=="home"&&read.story.journeyVersion==0&&LifeJourney.DestinationId(read.story)=="singapore"&&!LifeJourney.IsActive(read.story),"legacy home save keeps its original story and destination");
            LifeJourney.Ensure(read.story);
            Require(read.story.journeyVersion==1&&read.story.journey.legacy&&read.story.journey.completed&&read.story.nodeId=="home"&&LifeJourney.DestinationId(read.story)=="singapore","legacy schema marker migrates once without restarting");
            string migrated=JsonUtility.ToJson(read.story);LifeJourney.Ensure(read.story);
            Require(migrated==JsonUtility.ToJson(read.story),"legacy migration is idempotent");
            Require(StoryEngine.Choose(read.story,"ledger"),"legacy first choice remains available");
            VerifyExistingDesktopBackup();
            var unknown=StoryEngine.NewGame(1);unknown.journeyVersion=2;
            Require(!StoryEngine.Validate(unknown),"unknown journey schema rejected");
            var unversioned=StoryEngine.NewGame(1);unversioned.journeyVersion=0;unversioned.journey.turns=1;
            Require(!StoryEngine.Validate(unversioned),"unversioned attached progress rejected");
            foreach(var p in new[]{path,path+".bak",path+".tmp"})if(File.Exists(p))File.Delete(p);
            Debug.Log("QIAOPI SAVE CHECKS PASSED: task/side parcel state, new prologue pending choice, explicit journey-version migration, legacy home save/choice, backup recovery, invalid-state rejection.");
        }

        static void VerifyExistingDesktopBackup()
        {
            // This is a read-only compatibility fixture. Never load through the
            // application's live persistentDataPath, and never rewrite this backup.
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/v200-backup/desktop-save.json"));
            if(!File.Exists(path)){Debug.Log("QIAOPI LEGACY DESKTOP FIXTURE: not present in this checkout; synthetic legacy coverage remains active.");return;}
            string original=File.ReadAllText(path);
            var saved=JsonUtility.FromJson<WorldSave>(original);
            Require(saved!=null&&WorldSaveStore.IsValid(saved),"existing desktop backup is accepted before migration");
            string node=saved.story.nodeId,mission=saved.missionNode;
            int decisions=saved.story.decisions,money=saved.story.money,progress=saved.progress;
            float px=saved.px,pz=saved.pz;
            Require(saved.story.journeyVersion==0&&LifeJourney.DestinationId(saved.story)=="singapore","existing desktop backup retains Singapore before migration");
            LifeJourney.Ensure(saved.story);
            Require(saved.story.journeyVersion==1&&saved.story.journey.legacy&&saved.story.journey.completed,"existing desktop backup receives explicit legacy marker");
            var restored=JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(saved));
            Require(WorldSaveStore.IsValid(restored)&&restored.story.nodeId==node&&restored.missionNode==mission&&restored.story.decisions==decisions&&restored.story.money==money&&restored.progress==progress&&restored.px==px&&restored.pz==pz,"existing desktop backup keeps node, decisions, resources and position");
            Require(LifeJourney.DestinationId(restored.story)=="singapore"&&restored.story.journeyVersion==1&&restored.story.journey.legacy,"existing desktop backup does not reroll after roundtrip");
            Require(File.ReadAllText(path)==original,"existing desktop backup remains untouched");
            Debug.Log("QIAOPI LEGACY DESKTOP SAVE VERIFIED: original node="+node+", destination=singapore, decisions="+decisions+", read-only migration and roundtrip passed.");
        }
        static void Require(bool v,string label){if(!v)throw new Exception("Save check failed: "+label);}
    }
}
