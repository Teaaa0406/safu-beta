using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Models;

public class HoldNoteController : NoteMoverBase, INoteMover
{
    public INoteMover EndNoteMover { get; set; }

    [SerializeField] private GameObject holdMeshObject;
    private MeshFilter meshFilter;
    private SusNotePlaybackDataMMM2XY mmm2xyPlaybackData;

    public override void MoveClock(long timing)
    {
        if (destroyed) return;

        if (NotePlaybackData != null)
        {
            // 開始ノートの移動
            float startNotePosition = NotePlaybackData.CalNotePositionByTiming(timing);
            transform.position = new Vector3(transform.position.x, transform.position.y, startNotePosition);


            // 終了ノートの移動
            EndNoteMover.MoveClock(timing);

            // ホールドの生成
            GenerateHold(timing);

        }

        if (timing >= NotePlaybackData.EnabledTiming && !playedSe)
        {
            guideSeAction();
            playedSe = true;
        }
        //if (timing > EndNoteMover.NotePlaybackData.EnabledTiming + 250) Destroy();
        if (timing > EndNoteMover.NotePlaybackData.EnabledTiming) Destroy();
    }

    // ホールドメッシュの生成
    private void GenerateHold(long timing)
    {
        if (mmm2xyPlaybackData == null) mmm2xyPlaybackData = NotePlaybackData.NoteData as SusNotePlaybackDataMMM2XY;
        if(meshFilter == null) meshFilter = Instantiate(holdMeshObject, transform, false).GetComponent<MeshFilter>();
        
        Mesh holdMesh = new Mesh();
        meshFilter.mesh = holdMesh;

        int[] triangles = new int[6] { 0, 2, 1, 3, 1, 2 };
        Vector3[] vertices = new Vector3[4];

        float size = mmm2xyPlaybackData.Size;
        float length = mmm2xyPlaybackData.CalHoldNoteLength(timing);
        vertices[0] = new Vector3(0, 0, 0);//始点の左端
        vertices[1] = new Vector3(size, 0, 0); //始点の右端
        vertices[2] = new Vector3(0, length, 0); //終点の左端
        vertices[3] = new Vector3(size, length, 0); //終点の右端

        holdMesh.vertices = vertices;
        holdMesh.triangles = triangles;
        holdMesh.RecalculateNormals();
    }
}
