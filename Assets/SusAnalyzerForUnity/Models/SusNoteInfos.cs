using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tea.Safu.Models
{
    public enum NoteDataType
    {
        /// <summary>
        /// BPM変化
        /// </summary>
        mmm08,
        /// <summary>
        /// タップ
        /// </summary>
        mmm1x,
        /// <summary>
        /// ホールド
        /// </summary>
        mmm2xy,
        /// <summary>
        /// ホールド終了点
        /// </summary>
        mmm2xyEnd,
        /// <summary>
        /// スライド1
        /// </summary>
        mmm3xy,
        /// <summary>
        /// スライド1終了点
        /// </summary>
        mmm3xyEnd,
        /// <summary>
        /// スライド1中継点
        /// </summary>
        mmm3xyStep,
        /// <summary>
        /// スライド1曲線制御点
        /// </summary>
        mmm3xyCurveControl,
        /// <summary>
        /// スライド2
        /// </summary>
        mmm4xy,
        /// <summary>
        /// スライド2終了点
        /// </summary>
        mmm4xyEnd,
        /// <summary>
        /// スライド2中継点
        /// </summary>
        mmm4xyStep,
        /// <summary>
        /// スライド2曲線制御点
        /// </summary>
        mmm4xyCurveControl,
        /// <summary>
        /// ディレクショナル
        /// </summary>
        mmm5x,
        /// <summary>
        /// 小節線
        /// </summary>
        MeasureLine
    }

    /// <summary>
    /// 共通する譜面データです。
    /// 各譜面データはこれを継承します。
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
    /// 命令 mmm08 の情報 (BPM変化)
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
    /// 命令 mmm1x の情報 (タップ)
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

    /// <summary>
    /// 命令 mmm2xy の情報 (ホールド)
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
    /// 命令 mmm3xy の情報 (スライド1)
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
    /// 命令 mmm4xy の情報 (スライド1)
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
    /// 命令 mmm5x の情報 (ディレクショナル)
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
}