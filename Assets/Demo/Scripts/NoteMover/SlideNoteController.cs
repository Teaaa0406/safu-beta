using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Models;

public class SlideNoteController : NoteMoverBase, INoteMover
{
    public List<INoteMover> StepNoteMovers { get; set; }
    public List<SusNotePlaybackDataMMM3XYCurveControl> CurveControlMovers { get; set; }

    [SerializeField] private GameObject slideMeshObject;
    [SerializeField] private float splitlength;
    private MeshFilter meshFilter;



    public override void MoveClock(long timing)
    {
        if (destroyed) return;

        if (NotePlaybackData != null)
        {
            // �J�n�m�[�g�̈ړ�
            float startNotePosition = NotePlaybackData.CalNotePositionByTiming(timing);
            transform.position = new Vector3(transform.position.x, transform.position.y, startNotePosition);

            // ���p�_�̈ړ�
            foreach (INoteMover step in StepNoteMovers) step.MoveClock(timing);

            // �X���C�h�̐���
            GenerateSlide(timing);
        }

        if (timing >= NotePlaybackData.EnabledTiming && !playedSe && playGuideSe)
        {
            guideSeAction();
            playedSe = true;
        }
        //if (timing > StepNoteMovers[StepNoteMovers.Count - 1].NotePlaybackData.EnabledTiming + 250) Destroy();
        if (timing > StepNoteMovers[StepNoteMovers.Count - 1].NotePlaybackData.EnabledTiming) Destroy();
    }

    // �X���C�h���b�V���̐���
    private void GenerateSlide(long timing)
    {
        if (meshFilter == null) meshFilter = Instantiate(slideMeshObject, transform, false).GetComponent<MeshFilter>();

        Mesh slideMesh = new Mesh();
        meshFilter.mesh = slideMesh;

        // ���p�_�P�ʂŏ���
        int elemCount = 0;
        bool generatedFirstPoint = false;
        List<Vector3> vertices = new List<Vector3>();

        for (int i = 0; i < StepNoteMovers.Count; i++)
        {
            // �����ɕK�v�ȏ��
            float startEnabledTiming;
            float endEnabledTiming;

            float xOffset = (NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XY).X;
            float startX;
            float startSize;
            float startPos;
            float endX;
            float endSize;
            float endPos;

            if (i == 0)
            {
                startX = 0;
                startSize = (NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XY).Size;
                startPos = 0;

                endX = (StepNoteMovers[i].NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XYStep).X - xOffset;
                endSize = (StepNoteMovers[i].NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XYStep).Size;
                endPos = StepNoteMovers[i].NotePlaybackData.CalNotePositionByTiming(timing) - NotePlaybackData.CalNotePositionByTiming(timing);

                startEnabledTiming = NotePlaybackData.EnabledTiming;
                endEnabledTiming = StepNoteMovers[i].NotePlaybackData.EnabledTiming;
            }
            else
            {
                startX = (StepNoteMovers[i - 1].NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XYStep).X - xOffset;
                startSize = (StepNoteMovers[i - 1].NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XYStep).Size;
                startPos = StepNoteMovers[i - 1].NotePlaybackData.CalNotePositionByTiming(timing) - NotePlaybackData.CalNotePositionByTiming(timing);

                endX = (StepNoteMovers[i].NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XYStep).X - xOffset;
                endSize = (StepNoteMovers[i].NotePlaybackData.NoteData as SusNotePlaybackDataMMM3XYStep).Size;
                endPos = StepNoteMovers[i].NotePlaybackData.CalNotePositionByTiming(timing) - NotePlaybackData.CalNotePositionByTiming(timing);

                startEnabledTiming = StepNoteMovers[i - 1].NotePlaybackData.EnabledTiming;
                endEnabledTiming = StepNoteMovers[i].NotePlaybackData.EnabledTiming;
            }

            // ���݂̐����Ώ۔͈͂̋Ȑ�����_��T������
            SusNotePlaybackDataMMM3XYCurveControl curveControl = null;
            for (int j = 0; j < CurveControlMovers.Count; j++)
            {
                if (CurveControlMovers[j].EnabledTiming < startEnabledTiming) continue;
                if (CurveControlMovers[j].EnabledTiming < endEnabledTiming)
                {
                    curveControl = CurveControlMovers[j];
                    break;
                }
            }

            // �Ȑ�����_���Ȃ���΂��̂܂ܐ���
            if(curveControl == null)
            {
                Vector3[] verticesElem;
                if (!generatedFirstPoint)
                {
                    verticesElem = new Vector3[4];
                    verticesElem[0] = new Vector3(startX, startPos, 0);//�n�_�̍��[
                    verticesElem[1] = new Vector3(startX + startSize, startPos, 0); //�n�_�̉E�[
                    verticesElem[2] = new Vector3(endX, endPos, 0); //�I�_�̍��[
                    verticesElem[3] = new Vector3(endX + endSize, endPos, 0); //�I�_�̉E�[
                    generatedFirstPoint = true;
                }
                else
                {
                    verticesElem = new Vector3[2];
                    verticesElem[0] = new Vector3(endX, endPos, 0); //�I�_�̍��[
                    verticesElem[1] = new Vector3(endX + endSize, endPos, 0); //�I�_�̉E�[
                }

                vertices.AddRange(verticesElem);
                elemCount += 1;
            }
            // �x�W�F�Ȑ��Ő���
            else
            {
                // ����_�̒��S
                float controlPosX = curveControl.X - xOffset;
                float controlPosCentor = controlPosX + curveControl.Size / 2f;

                // �Ȑ������p�f�[�^
                Vector3 startPosV3 = new Vector3(startX + startSize / 2f, startPos, 0);
                Vector3 endPosV3 = new Vector3(endX + endSize / 2f, endPos, 0);
                Vector3 controlPosV3 = new Vector3(controlPosCentor, curveControl.CalNotePositionByTiming(timing) - NotePlaybackData.CalNotePositionByTiming(timing), 0);

                // �Ȑ�������
                int splitCount = Mathf.FloorToInt((endPos - startPos) / splitlength);
                if (splitCount < 0) splitCount *= -1;

                Vector3[] curvePoints = GetCurvePoints(startPosV3, controlPosV3, endPosV3, splitCount);

                float[] slideWidths = GetSlideWidths(startSize, endSize, splitCount);
                float splitedElemLength = (endPos - startPos) / splitCount;

                for (int j = 0; j < curvePoints.Length - 1; j++)
                {
                    float startSizeInCurve = slideWidths[j];
                    float endSizeInCurve = slideWidths[j + 1];
                    float startXInCurve = curvePoints[j].x - slideWidths[j] / 2f;
                    float endXInCurve = curvePoints[j + 1].x - slideWidths[j] / 2f;

                    // ���[������͂ݏo�Ȃ��悤�ɂ���
                    // ��������
                    if (startXInCurve + xOffset < 0)
                    {
                        startSizeInCurve += startXInCurve + xOffset;
                        startXInCurve = 0 - xOffset;
                    }
                    if (endXInCurve + xOffset < 0)
                    {
                        endSizeInCurve += endXInCurve + xOffset;
                        endXInCurve = 0 - xOffset;
                    }

                    // �E������
                    if (endXInCurve + endSizeInCurve + xOffset > 16)
                    {
                        endXInCurve += 16 - (endXInCurve + endSizeInCurve + xOffset);
                    }
                    if (endXInCurve + endSizeInCurve + xOffset > 16)
                    {
                        endXInCurve += 16 - (endXInCurve + endSizeInCurve + xOffset);
                    }

                    Vector3[] verticesElem;
                    if (!generatedFirstPoint)
                    {
                        verticesElem = new Vector3[4];
                        verticesElem[0] = new Vector3(startXInCurve, 0, 0);//�n�_�̍��[
                        verticesElem[1] = new Vector3(startXInCurve + startSizeInCurve, 0, 0); //�n�_�̉E�[
                        verticesElem[2] = new Vector3(endXInCurve, splitedElemLength * (i + 1), 0); //�I�_�̍��[
                        verticesElem[3] = new Vector3(endXInCurve + endSizeInCurve, splitedElemLength * (j + 1), 0); //�I�_�̉E�[
                        generatedFirstPoint = true;
                    }
                    else
                    {
                        verticesElem = new Vector3[2];
                        verticesElem[0] = new Vector3(endXInCurve, startPos + splitedElemLength * (j + 1), 0); //�I�_�̍��[
                        verticesElem[1] = new Vector3(endXInCurve + endSizeInCurve, startPos + splitedElemLength * (j + 1), 0); //�I�_�̉E�[
                    }

                    vertices.AddRange(verticesElem);
                    elemCount += 1;
                }
            }
        }

        slideMesh.vertices = vertices.ToArray();
        slideMesh.triangles = CreateTrianglesArray(elemCount);
        slideMesh.RecalculateNormals();
    }

    private int[] CreateTrianglesArray(int elemCount)
    {
        int[] triangles = new int[elemCount * 6];
        int valueBase = 0;
        for (int i = 0; i < elemCount * 6; i += 6)
        {
            triangles[i + 0] = 0 + valueBase;
            triangles[i + 1] = 2 + valueBase;
            triangles[i + 2] = 1 + valueBase;
            triangles[i + 3] = 3 + valueBase;
            triangles[i + 4] = 1 + valueBase;
            triangles[i + 5] = 2 + valueBase;
            valueBase += 2;
        }
        return triangles;
    }

    private Vector3[] GetCurvePoints(Vector3 startPos, Vector3 controlPos, Vector3 endPos, int splitCount)
    {
        Vector3[] curvePoints = new Vector3[splitCount + 1];
        for (int i = 0; i < splitCount + 1; i++)
        {
            float t = (float)i / splitCount;
            var a = Vector3.Lerp(startPos, controlPos, t);
            var b = Vector3.Lerp(controlPos, endPos, t);
            curvePoints[i] = Vector3.Lerp(a, b, t);
        }
        return curvePoints;
    }

    private float[] GetSlideWidths(float startSize, float endSize, int splitCount)
    {
        float[] widths = new float[splitCount + 1];
        float diff = endSize - startSize;
        for (int i = 0; i < splitCount + 1; i++)
        {
            widths[i] = startSize + diff / splitCount * i;
        }
        return widths;
    }
}
