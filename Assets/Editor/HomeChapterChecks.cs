using System;
using System.IO;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class HomeChapterChecks
    {
        static int assertions, checkpoints;
        static void Require(bool ok,string message)
        { assertions++; if(!ok)throw new InvalidOperationException("Home chapter: "+message); }
        [UnityEditor.MenuItem("侨批/检查泉州家庭日常")]
        public static void Run()
        {
            assertions=checkpoints=0;
            string root=Path.Combine(Path.GetTempPath(),"qiaopi-home-checks-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                CheckLegacy();
                foreach(string purchase in new[]{"rice","tea"})foreach(string word in new[]{"home","return"})CheckRoute(root,purchase,word);
                CheckInvalid();
                Debug.Log("HOME_CHAPTER_CHECKS_PASSED: 4 household/word combinations, 7 physical stages, per-stage disk resume, one-shot memories without resource farming, original decisions/rewards, old saves and foreign-node fixtures; assertions="+assertions+", checkpoints="+checkpoints+". Player saves/preferences untouched.");
            } finally { if(Directory.Exists(root))Directory.Delete(root,true); }
        }
        static void CheckLegacy()
        {
            var old=new GameState { nodeId="peace", homeChapterVersion=0, homeChapter=new HomeChapterState() };
            Require(StoryEngine.Validate(old),"legacy peace accepted");
            Require(!HomeChapter.IsActive(old),"legacy peace not enrolled by default-object materialization");
            Require(!HomeChapter.TryAct(old,"walk_market"),"old progress not overwritten");
            foreach(string memory in new[]{"market","grain","lesson","meal"})Require(!HomeChapter.HasMemory(old,memory),"no fabricated old memory "+memory);
            var late=StoryEngine.NewGame(14);late.nodeId="first_pay";
            Require(StoryEngine.Validate(late)&&!HomeChapter.IsActive(late),"foreign-node old fixture stays valid");
            Require(StoryEngine.Choose(old,"meal")&&StoryEngine.Continue(old)&&old.nodeId=="pressure", "legacy original decision still proceeds");
        }
        static void CheckRoute(string root,string purchase,string word)
        {
            GameState state=StoryEngine.NewGame(37);
            int money=state.money,health=state.health,family=state.family,trust=state.trust;
            string[] stages={"wife","market","bring_home","grain","store","lesson","meal","complete"};
            string[] actions={"walk_market",purchase,"put_basket","lift_grain","store_grain",word,"share_meal"};
            string file=Path.Combine(root,purchase+"-"+word+".json");
            for(int i=0;i<stages.Length;i++){
                Require(state.homeChapter.stage==stages[i],"ordered stage "+stages[i]);
                Require(HomeChapter.Progress(state)==i,"progress "+i);
                Require(StoryEngine.Validate(state),"valid at "+stages[i]);
                Require(HomeChapter.HasMemory(state,"market")== (i>=3),"market memory only after basket delivery");
                Require(HomeChapter.HasMemory(state,"grain")== (i>=5),"grain memory only after storing");
                Require(HomeChapter.HasMemory(state,"lesson")== (i>=6),"word only after actual lesson");
                Require(HomeChapter.HasMemory(state,"meal")== (i>=7),"meal only after completing");
                var mission=WorldMissions.Get(state);
                var save=new WorldSave {story=state,missionNode="peace",layoutVersion=2,progress=Math.Min(i,mission.required),px=-23,pz=-6.4f};
                WorldSaveStore.Write(file,save);bool recovered;var restored=WorldSaveStore.Load(file,out recovered);checkpoints++;
                Require(restored!=null&&!recovered,"fresh disk checkpoint "+i);state=restored.story;
                Require(state.homeChapter.stage==stages[i],"stage persists");
                string before=JsonUtility.ToJson(state);
                Require(!HomeChapter.TryAct(state,"invalid"),"unknown action rejected");
                Require(JsonUtility.ToJson(state)==before,"invalid action atomic");
                // Looking at the mission/dialogue must never consume an activity.
                StoryEngine.GetScene(state);DialogueScript.Get(state);WorldMissions.Get(state);
                Require(JsonUtility.ToJson(state)==before,"presentation read-only");
                if(i<actions.Length){
                    Require(HomeChapter.TryAct(state,actions[i]),"expected action "+actions[i]);
                    string committed=JsonUtility.ToJson(state);
                    Require(!HomeChapter.TryAct(state,actions[i]),"repeat cannot replay "+actions[i]);
                    Require(JsonUtility.ToJson(state)==committed,"repeat atomic "+actions[i]);
                }
            }
            Require(state.money==money&&state.health==health&&state.family==family&&state.trust==trust,"no farming or hidden charges");
            Require(state.decisions==0&&state.history.Count==0&&state.flags.Count==0,"does not use story decision slots");
            Require(!HomeChapter.IsActive(state),"complete opens original story");
            var lines=DialogueScript.Get(state);
            Require(lines.Exists(l=>l.speakerId=="wife"&&l.clipKey==(purchase=="tea"?"peace_250_wife_tea":"peace_250_wife_rice")),"wife recalls selected purchase");
            Require(lines.Exists(l=>l.speakerId=="aman"&&l.clipKey==(word=="return"?"peace_250_aman_return":"peace_250_aman_home")),"sister recalls chosen word");
            Require(StoryEngine.Choose(state,"meal")&&state.decisions==1&&state.family==family+3,"original meal reward occurs exactly once");
            Require(!StoryEngine.Choose(state,"meal"),"original outcome repeat rejected");
            Require(StoryEngine.Continue(state)&&state.nodeId=="pressure"&&StoryEngine.Validate(state),"pressure continues without new decision slots");
            Require(HomeChapter.HasMemory(state,"meal"),"memory survives leaving peace");
        }
        static void CheckInvalid()
        {
            var s=StoryEngine.NewGame(1);s.homeChapterVersion=99;Require(!HomeChapter.Validate(s),"unknown home version rejected");
            s=StoryEngine.NewGame(1);s.homeChapter.stage="teleport";Require(!StoryEngine.Validate(s),"unknown stage rejected");
            s=StoryEngine.NewGame(1);s.homeChapter.mealShared=true;Require(!HomeChapter.Validate(s),"unearned meal rejected");
            s=StoryEngine.NewGame(1);s.homeChapter.purchase="tea";Require(!HomeChapter.Validate(s),"out-of-order purchase rejected");
            s=StoryEngine.NewGame(1);s.homeChapter.word="home";Require(!HomeChapter.Validate(s),"out-of-order lesson rejected");
            s=StoryEngine.NewGame(1);s.awaitingContinue=true;s.nextNodeId="pressure";s.lastOutcome="继续";
            Require(!HomeChapter.TryAct(s,"walk_market"),"pending story outcome never advances home");
            s=StoryEngine.NewGame(1);s.nodeId="first_pay";Require(!HomeChapter.TryAct(s,"walk_market"),"overseas cannot execute home task");
        }
    }
}
