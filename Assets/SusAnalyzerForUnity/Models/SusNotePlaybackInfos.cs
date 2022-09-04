using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Analyze;

namespace Tea.Safu.Models
{
    /// <summary>
    /// ���ʂ̍Đ��ɕK�v�Ȃ��̂����ׂĂ܂Ƃ߂��N���X�ł��B
    /// </summary>
    public class SusNotePlaybackDatas
    {
        public List<SusNotePlaybackDataBase> Notes { get; set; }
    }

    public class SusNotePlaybackDataBase
    {
        public NoteDataType NoteDataType { get; set; }
        public object NoteData { get; set; }
        public SusAnalyzer.SusAnalyzeSetting Setting { get; set; }
        public long EnabledTiming { get; set; }
        public long InstantiateTiming { get; set; }

        /// <summary>
        /// ���݂̃^�C�~���O����m�[�g�̈ʒu���v�Z���܂��B
        /// �m�[�g�̈ړ��͂��̊֐����g���čs���܂��B
        /// </summary>
        /// <param name="timing"></param>
        /// <returns></returns>
        public float CalNotePositionByTiming(long timing)
        {
            long timingToJudgment = EnabledTiming - timing;
            return timingToJudgment * CalDistancePerTiming();
        }

        /// <summary>
        /// �^�C�~���O���̈ړ��������v�Z���܂�
        /// </summary>
        /// <returns></returns>
        public float CalDistancePerTiming()
        {
            return (Setting.InstantiatePosition - Setting.JudgmentPosition) / (Setting.Speed * 1000f);
        }

        /// <summary>
        /// �m�[�g���C���X�^���X������^�C�~���O���v�Z���܂��B
        /// </summary>
        public long CalInstantiateTiming(long startTiming)
        {
            long timing = startTiming;
            for (long i = startTiming; i < EnabledTiming; i++)
            {
                timing = i;
                if (CalNotePositionByTiming(i) <= Setting.InstantiatePosition) break;
            }
            return timing;
        }
    }

    public class SusNotePlaybackDataMMM1X : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public int Type { get; set; }
        public float Size { get; set; }

        public SusNotePlaybackDataMMM1X()
        {
            NoteDataType = NoteDataType.mmm1x;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM2XY : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public SusNotePlaybackDataMMM2XYEnd EndNote { get; set; }

        public SusNotePlaybackDataMMM2XY()
        {
            NoteDataType = NoteDataType.mmm2xy;
            NoteData = this;
        }

        /// <summary>
        /// �z�[���h�m�[�g�̒������v�Z���܂��B
        /// </summary>
        /// <returns></returns>
        public float CalHoldNoteLength(long timing)
        {
            return EndNote.CalNotePositionByTiming(timing) - CalNotePositionByTiming(timing);
        }
    }

    public class SusNotePlaybackDataMMM2XYEnd : SusNotePlaybackDataBase
    {
        public SusNotePlaybackDataMMM2XYEnd()
        {
            NoteDataType = NoteDataType.mmm2xyEnd;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM3XY : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public List<SusNotePlaybackDataMMM3XYStep> Steps { get; set; }
        public List<SusNotePlaybackDataMMM3XYCurveControl> CurveControls { get; set; }

        public SusNotePlaybackDataMMM3XY()
        {
            NoteDataType = NoteDataType.mmm3xy;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM3XYStep : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public bool Invisible { get; set; }
        public bool End { get; set; }
        public SusNotePlaybackDataMMM3XYStep()
        {
            NoteDataType = NoteDataType.mmm3xyStep;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM3XYCurveControl : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public SusNotePlaybackDataMMM3XYCurveControl()
        {
            NoteDataType = NoteDataType.mmm3xyStep;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM5X : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public int Type { get; set; }
        public float Size { get; set; }

        public SusNotePlaybackDataMMM5X()
        {
            NoteDataType = NoteDataType.mmm5x;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMeasureLine : SusNotePlaybackDataBase
    {
        public SusNotePlaybackDataMeasureLine()
        {
            NoteDataType = NoteDataType.MeasureLine;
            NoteData = this;
        }
    }
}