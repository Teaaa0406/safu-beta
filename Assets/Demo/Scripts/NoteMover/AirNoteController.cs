using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Analyze;
using Tea.Safu.Models;

public class AirNoteController: NoteMoverBase, INoteMover
{
    public int AirType { get; set; }

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material airUpMaterial;
    [SerializeField] private Material airDownMaterial;

    [SerializeField] private float ariHeight;
    [SerializeField] private float inclinationOffset;
    [SerializeField] private float startTextureOffset;
    [SerializeField] private float endTextureOffset;

    private bool instantiatedAirNote = false;
    private bool isUp;
    private SusNotePlaybackDataMMM5X mmm5xPlaybackData;
    private SusAnalyzer.SusAnalyzeSetting setting;


    public override void MoveClock(long timing)
    {
        if (destroyed) return;

        // Airノートの生成
        if (!instantiatedAirNote)
        {
            isUp = isAirUpNote();
            GenerateAir();
            instantiatedAirNote = true;
        }

        if (NotePlaybackData != null)
        {
            // 自身の移動
            transform.position = new Vector3(transform.position.x, transform.position.y, NotePlaybackData.CalNotePositionByTiming(timing));

            // Airのテクスチャオフセットを設定
            if (setting == null) setting = mmm5xPlaybackData.Setting;
            SetTextureOffset();
        }
        if (timing >= NotePlaybackData.EnabledTiming && !playedSe && playGuideSe)
        {
            guideSeAction();
            playedSe = true;
        }
        //if (timing > NotePlaybackData.EnabledTiming + 250) Destroy();
        if (timing > NotePlaybackData.EnabledTiming) Destroy();
    }

    // Airの生成
    private void GenerateAir()
    {
        if (mmm5xPlaybackData == null) mmm5xPlaybackData = NotePlaybackData.NoteData as SusNotePlaybackDataMMM5X;

        Mesh airMesh = new Mesh();
        meshFilter.mesh = airMesh;

        int[] triangles = new int[6] { 0, 2, 1, 3, 1, 2 };
        Vector2[] uvs = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        Vector3[] vertices = new Vector3[4];

        float size = mmm5xPlaybackData.Size;

        // 左向き
        if(AirType == 3 || AirType == 5)
        {
            vertices[0] = new Vector3(0, 0, 0); //始点の左端
            vertices[1] = new Vector3(size, 0, 0); //始点の右端
            vertices[2] = new Vector3(-inclinationOffset, 0, -ariHeight); //終点の左端
            vertices[3] = new Vector3(size - inclinationOffset, 0, -ariHeight); //終点の右端
        }
        // 右向き
        else if (AirType == 4 || AirType == 4)
        {
            vertices[0] = new Vector3(0, 0, 0); //始点の左端
            vertices[1] = new Vector3(size, 0, 0); //始点の右端
            vertices[2] = new Vector3(inclinationOffset, 0, -ariHeight); //終点の左端
            vertices[3] = new Vector3(size + inclinationOffset, 0, -ariHeight); //終点の右端
        }
        else
        {
            vertices[0] = new Vector3(0, 0, 0); //始点の左端
            vertices[1] = new Vector3(size, 0, 0); //始点の右端
            vertices[2] = new Vector3(0, 0, -ariHeight); //終点の左端
            vertices[3] = new Vector3(size, 0, -ariHeight); //終点の右端
        }

        airMesh.vertices = vertices;
        airMesh.triangles = triangles;
        airMesh.SetUVs(0, uvs);
        airMesh.RecalculateNormals();

        if (isUp) meshRenderer.material = airUpMaterial;
        else meshRenderer.material = airDownMaterial;

    }


    // 設定するオフセット値を計算
    // オフセットはノートの位置によって決まる
    private void SetTextureOffset()
    {
        float moveDistance = setting.JudgmentPosition - setting.InstantiatePosition;
        float positioPercent = (setting.JudgmentPosition - transform.position.z) / moveDistance;

        Vector2 offset;
        if (isUp) offset = new Vector2(0, startTextureOffset + positioPercent * endTextureOffset);
        else offset = new Vector2(0, startTextureOffset + positioPercent * -endTextureOffset);
        meshRenderer.material.SetTextureOffset("_MainTex", offset);
    }

    private bool isAirUpNote()
    {
        if (AirType == 1 || AirType == 3 || AirType == 4) return true;
        else return false;
    }
}
