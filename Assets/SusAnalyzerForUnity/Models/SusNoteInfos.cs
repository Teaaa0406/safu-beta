using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tea.Safu.Models
{
    public enum NoteDataType
    {
        /// <summary>
        /// BPM�ω�
        /// </summary>
        mmm08,
        /// <summary>
        /// �^�b�v
        /// </summary>
        mmm1x,
        /// <summary>
        /// �z�[���h
        /// </summary>
        mmm2xy,
        /// <summary>
        /// �z�[���h�I���_
        /// </summary>
        mmm2xyEnd,
        /// <summary>
        /// �X���C�h1
        /// </summary>
        mmm3xy,
        /// <summary>
        /// �X���C�h1�I���_
        /// </summary>
        mmm3xyEnd,
        /// <summary>
        /// �X���C�h1���p�_
        /// </summary>
        mmm3xyStep,
        /// <summary>
        /// �X���C�h1�Ȑ�����_
        /// </summary>
        mmm3xyCurveControl,
        /// <summary>
        /// �X���C�h2
        /// </summary>
        mmm4xy,
        /// <summary>
        /// �X���C�h2�I���_
        /// </summary>
        mmm4xyEnd,
        /// <summary>
        /// �X���C�h2���p�_
        /// </summary>
        mmm4xyStep,
        /// <summary>
        /// �X���C�h2�Ȑ�����_
        /// </summary>
        mmm4xyCurveControl,
        /// <summary>
        /// �f�B���N�V���i��
        /// </summary>
        mmm5x,
        /// <summary>
        /// ���ߐ�
        /// </summary>
        MeasureLine
    }

    /// <summary>
    /// ���ʂ��镈�ʃf�[�^�ł��B
    /// �e���ʃf�[�^�͂�����p�����܂��B
    /// </summary>
    public class SusNoteDataBase
    {
        public NoteDataType DataType { get; set; }
        public object NoteData { get; set; }
        public int MeasureNumber { get; set; }
        public int LineDataCount { get; set; }
        public int DataIndex { get; set; }
        public string[] Data { get; set; }
        public long EnabledTiming { get; set; }
        public HispeedDefinition HispeedDefinition { get; set; }
    }


    /// <summary>
    /// ���� mmm08 �̏�� (BPM�ω�)
    /// </summary>
    public class NoteDataMMM08 : SusNoteDataBase
    {
        public NoteDataMMM08()
        {
            DataType = NoteDataType.mmm08;
            NoteData = this;
        }
    }

    /// <summary>
    /// ���� mmm1x �̏�� (�^�b�v)
    /// </summary>
    public class NoteDataMMM1X : SusNoteDataBase
    {
        public int X { get; set; }
        public int Type { get; set; }
        public int Size { get; set; }

        public NoteDataMMM1X()
        {
            DataType = NoteDataType.mmm1x;
            NoteData = this;
        }
    }

    /// ���� mmm2xy �̏�� (�z�[���h)
    /// </summary>
    public class NoteDataMMM2XY : SusNoteDataBase
    {
        public int X { get; set; }
        public string Y { get; set; }
        public int Type { get; set; }
        public int Size { get; set; }

        public NoteDataMMM2XY()
        {
            DataType = NoteDataType.mmm2xy;
            NoteData = this;
        }
    }

    /// <summary>
    /// ���� mmm3xy �̏�� (�X���C�h1)
    /// </summary>
    public class NoteDataMMM3XY : SusNoteDataBase
    {
        public int X { get; set; }
        public string Y { get; set; }
        public int Type { get; set; }
        public int Size { get; set; }

        public NoteDataMMM3XY()
        {
            DataType = NoteDataType.mmm3xy;
            NoteData = this;
        }
    }

    /// <summary>
    /// ���� mmm4xy �̏�� (�X���C�h1)
    /// </summary>
    public class NoteDataMMM4XY : SusNoteDataBase
    {
        public int X { get; set; }
        public string Y { get; set; }
        public int Type { get; set; }
        public int Size { get; set; }

        public NoteDataMMM4XY()
        {
            DataType = NoteDataType.mmm4xy;
            NoteData = this;
        }
    }

    /// <summary>
    /// ���� mmm5x �̏�� (�f�B���N�V���i��)
    /// </summary>
    public class NoteDataMMM5X : SusNoteDataBase
    {
        public int X { get; set; }
        public int Type { get; set; }
        public int Size { get; set; }

        public NoteDataMMM5X()
        {
            DataType = NoteDataType.mmm5x;
            NoteData = this;
        }
    }

    /// <summary>
    /// ���ߐ��̏��
    /// </summary>
    public class NoteDataMeasureLine : SusNoteDataBase
    {
        public NoteDataMeasureLine()
        {
            DataType = NoteDataType.MeasureLine;
            NoteData = this;
        }
    }
}