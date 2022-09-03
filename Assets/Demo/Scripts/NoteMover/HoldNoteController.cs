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
            // �J�n�m�[�g�̈ړ�
            float startNotePosition = NotePlaybackData.CalNotePositionByTiming(timing);
            transform.position = new Vector3(transform.position.x, transform.position.y, startNotePosition);


            // �I���m�[�g�̈ړ�
            EndNoteMover.MoveClock(timing);

            // �z�[���h�̐���
            GenerateHold(startNotePosition, timing);

        }

        if (timing >= NotePlaybackData.EnabledTiming && !playedSe && playGuideSe)
        {
            guideSeAction();
            playedSe = true;
        }
        if (timing > EndNoteMover.NotePlaybackData.EnabledTiming + 250) Destroy();
    }

    // �z�[���h���b�V���̐���
    private void GenerateHold(float startPos, long timing)
    {
        if (mmm2xyPlaybackData == null) mmm2xyPlaybackData = NotePlaybackData.NoteData as SusNotePlaybackDataMMM2XY;
        if(meshFilter == null) meshFilter = Instantiate(holdMeshObject, transform, false).GetComponent<MeshFilter>();
        
        Mesh holdMesh = new Mesh();
        meshFilter.mesh = holdMesh;

        int[] triangles = new int[6] { 0, 2, 1, 3, 1, 2 };
        Vector3[] vertices = new Vector3[4];

        float size = mmm2xyPlaybackData.Size;
        float length = mmm2xyPlaybackData.CalHoldNoteLength(timing);
        vertices[0] = new Vector3(0, 0, 0);//�n�_�̍��[
        vertices[1] = new Vector3(size, 0, 0); //�n�_�̉E�[
        vertices[2] = new Vector3(0, length, 0); //�I�_�̍��[
        vertices[3] = new Vector3(size, length, 0); //�I�_�̉E�[

        holdMesh.vertices = vertices;
        holdMesh.triangles = triangles;
        holdMesh.RecalculateNormals();
    }
}
