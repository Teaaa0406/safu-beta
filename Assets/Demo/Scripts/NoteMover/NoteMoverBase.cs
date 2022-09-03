using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Models;

public interface INoteMover
{
    public SusNotePlaybackDataBase NotePlaybackData { get; set; }

    public void Initialize(bool playGuideSe, Action guideSeAction = null);

    public void MoveClock(long timing);
}

public class NoteMoverBase : MonoBehaviour
{
    private SusNotePlaybackDataBase notePlaybackData;
    protected bool playGuideSe;
    protected Action guideSeAction;
    protected bool playedSe = false;
    protected bool destroyed = false;

    public SusNotePlaybackDataBase NotePlaybackData { get => notePlaybackData; set => notePlaybackData = value; }
    public bool Destroyed { get => destroyed; set => destroyed = value; }



    public void Initialize(bool playGuideSe, Action guideSeAction = null)
    {
        this.playGuideSe = playGuideSe;
        this.guideSeAction = guideSeAction;
    }

    public virtual void MoveClock(long timing)
    {
        if (destroyed) return;
        if(notePlaybackData != null)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, notePlaybackData.CalNotePositionByTiming(timing));
        }
        if (timing >= notePlaybackData.EnabledTiming && !playedSe && playGuideSe)
        {
            guideSeAction();
            playedSe = true;
        }
        if(timing > notePlaybackData.EnabledTiming + 250) Destroy();
    }

    public void Destroy()
    {
        destroyed = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
