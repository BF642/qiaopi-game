using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        [Serializable] sealed class ChapterSegment
        {
            public string stage;
            public float activeSeconds, walkingSeconds, panelSeconds, presentationSeconds, walkedMetres;
        }
        [Serializable] sealed class ChapterMeasurementReport
        {
            public string method = "Automated input and normal-speed walking. Reading, thinking and typing are skipped. Captures and loading affect duration. Not human playtime.";
            public List<ChapterSegment> segments = new List<ChapterSegment>();
        }
        ChapterMeasurementReport chapterMeasurement;
        Vector3 chapterLastMeasuredPosition;

        void BeginChapterMeasurements()
        {
            chapterMeasurement=new ChapterMeasurementReport();
            chapterLastMeasuredPosition=player.transform.position;
        }
        void TickChapterMeasurements()
        {
            // Opt-in QA only: do not record normal play or player-written text.
            if(!ChapterQA||chapterMeasurement==null||state==null||!player)return;
            string key=state.nodeId;
            if(InHomeChapter)key+="/home/"+state.homeChapter.stage;
            else if(InFirstDay)key+="/first-day/"+DayStage;
            ChapterSegment part=chapterMeasurement.segments.Find(x=>x.stage==key);
            if(part==null){part=new ChapterSegment{stage=key};chapterMeasurement.segments.Add(part);}
            float seconds=Mathf.Min(Time.unscaledDeltaTime,.15f);
            float moved=Vector3.Distance(player.transform.position,chapterLastMeasuredPosition);
            chapterLastMeasuredPosition=player.transform.position;
            part.activeSeconds+=seconds;
            // Scene changes and checkpoint restores are not walking.
            if(moved>.002f&&moved<.75f){part.walkingSeconds+=seconds;part.walkedMetres+=moved;}
            if(HomePerformanceBusy||WorkPerformanceBusy)part.presentationSeconds+=seconds;
            else if(Blocked)part.panelSeconds+=seconds;
        }
        void SaveChapterMeasurements()
        {
            if(chapterMeasurement==null||string.IsNullOrEmpty(chapterQAOutput))return;
            File.WriteAllText(Path.Combine(chapterQAOutput,"automated-pacing.json"),JsonUtility.ToJson(chapterMeasurement,true));
            chapterMeasurement=null;
        }
    }
}
