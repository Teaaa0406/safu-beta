using System.Collections.Generic;
using Tea.Safu.Analyze;
using Tea.Safu.Models;
using Tea.Safu.SusDebug;

namespace Tea.Safu.Util
{
    public class SusPlaybackUtility
    {
        public delegate void OnInstantiateNotesReceivedEventHandler(List<SusNotePlaybackDataBase> notes);
        public event OnInstantiateNotesReceivedEventHandler OnInstantiateNotesReceived;

        private bool initialized = false;
        private SusAnalyzer.SusAnalyzeSetting setting;

        private int readIndex = 0;
        private long nextInstantiateTiming;
        private bool finished = false;

        private long timing;
        private List<SusNotePlaybackDataBase> noteList;

        public SusPlaybackUtility(SusAnalyzer.SusAnalyzeSetting setting)
        {
            this.setting = setting;
        }



        public void Initialize(List<SusNotePlaybackDataBase> noteList)
        {
            this.noteList = noteList;
            readIndex = 0;
            nextInstantiateTiming = setting.StartTiming;
            finished = false;
            initialized = true;
        }

        public void UpdateTiming(long timing)
        {
            if (!initialized) SusDebugger.LogWarning("Please execute \"Initialize()\" before updating the timing");
            this.timing = timing;
            OnTimingUpdated();
        }

        public void OnTimingUpdated()
        {
            if (finished) return;

            nextInstantiateTiming = noteList[readIndex].InstantiateTiming;
            if(nextInstantiateTiming <= timing && readIndex < noteList.Count)
            {
                List<SusNotePlaybackDataBase> nextNotes = new List<SusNotePlaybackDataBase>();
                while (!finished && noteList[readIndex].InstantiateTiming == nextInstantiateTiming)
                {
                    nextNotes.Add(noteList[readIndex]);
                    readIndex += 1;
                    if (readIndex >= noteList.Count) finished = true;
                }
                OnInstantiateNotesReceived(nextNotes);
            }
        }
    }
}