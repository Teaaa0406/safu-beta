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

        // Air�m�[�g�̐���
        if (!instantiatedAirNote)
        {
            isUp = isAirUpNote();
            GenerateAir();
            instantiatedAirNote = true;
        }

        if (NotePlaybackData != null)
        {
            // ���g�̈ړ�
            transform.position = new Vector3(transform.position.x, transform.position.y, NotePlaybackData.CalNotePositionByTiming(timing));

            // Air�̃e�N�X�`���I�t�Z�b�g��ݒ�
            if (setting == null) setting = mmm5xPlaybackData.Setting;
            SetTextureOffset();
        }
        if (timing >= NotePlaybackData.EnabledTiming && !playedSe)
        {
            guideSeAction();
            playedSe = true;
        }
        //if (timing > NotePlaybackData.EnabledTiming + 250) Destroy();
        if (timing > NotePlaybackData.EnabledTiming) Destroy();
    }

    // Air�̐���
    private void GenerateAir()
    {
        if (mmm5xPlaybackData == null) mmm5xPlaybackData = NotePlaybackData.NoteData as SusNotePlaybackDataMMM5X;

        Mesh airMesh = new Mesh();
        meshFilter.mesh = airMesh;

        int[] triangles = new int[6] { 0, 2, 1, 3, 1, 2 };
        Vector2[] uvs = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        Vector3[] vertices = new Vector3[4];

        float size = mmm5xPlaybackData.Size;

        // ������
        if(AirType == 3 || AirType == 5)
        {
            vertices[0] = new Vector3(0, 0, 0); //�n�_�̍��[
            vertices[1] = new Vector3(size, 0, 0); //�n�_�̉E�[
            vertices[2] = new Vector3(-inclinationOffset, 0, -ariHeight); //�I�_�̍��[
            vertices[3] = new Vector3(size - inclinationOffset, 0, -ariHeight); //�I�_�̉E�[
        }
        // �E����
        else if (AirType == 4 || AirType == 4)
        {
            vertices[0] = new Vector3(0, 0, 0); //�n�_�̍��[
            vertices[1] = new Vector3(size, 0, 0); //�n�_�̉E�[
            vertices[2] = new Vector3(inclinationOffset, 0, -ariHeight); //�I�_�̍��[
            vertices[3] = new Vector3(size + inclinationOffset, 0, -ariHeight); //�I�_�̉E�[
        }
        else
        {
            vertices[0] = new Vector3(0, 0, 0); //�n�_�̍��[
            vertices[1] = new Vector3(size, 0, 0); //�n�_�̉E�[
            vertices[2] = new Vector3(0, 0, -ariHeight); //�I�_�̍��[
            vertices[3] = new Vector3(size, 0, -ariHeight); //�I�_�̉E�[
        }

        airMesh.vertices = vertices;
        airMesh.triangles = triangles;
        airMesh.SetUVs(0, uvs);
        airMesh.RecalculateNormals();

        if (isUp) meshRenderer.material = airUpMaterial;
        else meshRenderer.material = airDownMaterial;

    }


    // �ݒ肷��I�t�Z�b�g�l���v�Z
    // �I�t�Z�b�g�̓m�[�g�̈ʒu�ɂ���Č��܂�
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
