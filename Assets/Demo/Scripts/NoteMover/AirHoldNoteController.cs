using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Models;

public class AirHoldNoteController: NoteMoverBase, INoteMover
{
    public List<INoteMover> StepNoteMovers { get; set; }
    public int AirHoldNoteSortingOrder { get; set; }

    [SerializeField] private GameObject AirHoldLineObject;
    [SerializeField] private float linePosOffset;

    private SpriteRenderer airHoldLineRenderer;



    public override void MoveClock(long timing)
    {
        if (destroyed) return;

        if (NotePlaybackData != null)
        {
            // 開始ノートの移動
            float startNotePosition = NotePlaybackData.CalNotePositionByTiming(timing);
            transform.position = new Vector3(transform.position.x, transform.position.y, startNotePosition);

            // 中継点の移動
            foreach (INoteMover step in StepNoteMovers) step.MoveClock(timing);

            // エアーホールドラインの生成
            GenerateAirHoldLine(timing);
        }

        if (timing >= NotePlaybackData.EnabledTiming && !playedSe && playGuideSe)
        {
            guideSeAction();
            playedSe = true;
        }
        if (timing > StepNoteMovers[StepNoteMovers.Count - 1].NotePlaybackData.EnabledTiming + 250) Destroy();
    }

    // エアーホールドラインの生成
    private void GenerateAirHoldLine(long timing)
    {
        if(airHoldLineRenderer == null)
        {
            airHoldLineRenderer = Instantiate(AirHoldLineObject).GetComponent<SpriteRenderer>();
            airHoldLineRenderer.sortingOrder = AirHoldNoteSortingOrder;
            airHoldLineRenderer.transform.SetParent(this.transform);
            Vector3 lineLocalPas = airHoldLineRenderer.gameObject.transform.localPosition;
            airHoldLineRenderer.gameObject.transform.localPosition = new Vector3(0, lineLocalPas.y, linePosOffset);
        }

        // ラインの長さ
        float lineLength = StepNoteMovers[StepNoteMovers.Count - 1].NotePlaybackData.CalNotePositionByTiming(timing) - NotePlaybackData.CalNotePositionByTiming(timing);
        airHoldLineRenderer.size = new Vector2(airHoldLineRenderer.size.x, lineLength - linePosOffset);
    }
}
