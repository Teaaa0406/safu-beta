using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Analyze;

namespace Tea.Safu.Models
{
    /// <summary>
    /// 譜面の再生に必要なものをすべてまとめたクラスです。
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
        public HispeedDefinition HispeedDefinition { get; set; }
        public long EnabledTiming { get; set; }
        public long InstantiateTiming { get; set; }
        public SusCalculationUtils calculationUtils { get; set; }

        /// <summary>
        /// 現在のタイミングからノートの位置を計算します。
        /// ノートの移動はこの関数を使って行います。
        /// </summary>
        /// <param name="timing"></param>
        /// <returns></returns>
        public float CalNotePositionByTiming(long timing)
        {
            long timingToJudgment = 0;

            if(HispeedDefinition == null)
            {
                timingToJudgment = EnabledTiming - timing;
            }
            else
            {
                
                // ハイスピードを考慮した計算

                // 計算対象のタイミング範囲
                long calculateStartTiming;
                long calculateEndTiming;
                bool afterEnabledTiming = false;
                if (timing <= EnabledTiming)
                {
                    calculateStartTiming = timing;
                    calculateEndTiming = EnabledTiming;
                }
                else
                {
                    calculateStartTiming = EnabledTiming;
                    calculateEndTiming = timing;
                    afterEnabledTiming = true;
                }

                // 対象範囲内でのノートの移動距離
                // ハイスピードの変化毎に処理
                long calculatedTiming = calculateStartTiming;
                while (calculatedTiming < calculateEndTiming)
                {
                    // ハイスピード適用情報
                    HispeedDefinition.HighSpeedApplyingInfo applyingInfo = HispeedDefinition.GetHighSpeedApplyingInfoByTiming(calculatedTiming, calculationUtils);

                    // ハイスピードの適用時間
                    long applyingTiming = 0;
                    if (applyingInfo.EndTiming == -1 || applyingInfo.EndTiming > calculateEndTiming) applyingTiming = calculateEndTiming - calculatedTiming;
                    else applyingTiming = applyingInfo.EndTiming - calculateStartTiming;

                    if(!afterEnabledTiming) timingToJudgment += (long)Math.Round(applyingTiming * applyingInfo.Hispeed, 0);
                    else timingToJudgment -= (long)Math.Round(applyingTiming * applyingInfo.Hispeed, 0);
                    calculatedTiming += applyingTiming;
                }
            }
            return timingToJudgment * CalDistancePerTiming();
        }

        /// <summary>
        /// タイミング毎の移動距離を計算します
        /// </summary>
        /// <returns></returns>
        public float CalDistancePerTiming()
        {
            return (Setting.InstantiatePosition - Setting.JudgmentPosition) / (Setting.Speed * 1000f);
        }

        /// <summary>
        /// ノートをインスタンス化するタイミングを計算します。
        /// </summary>
        public long CalInstantiateTiming(long startTiming)
        {
            long timing = startTiming;
            for (long i = startTiming; i < EnabledTiming; i += 500)
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
        /// ホールドノートの長さを計算します。
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
            NoteDataType = NoteDataType.mmm3xyCurveControl;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM4XY : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public List<SusNotePlaybackDataMMM4XYStep> Steps { get; set; }
        public List<SusNotePlaybackDataMMM4XYCurveControl> CurveControls { get; set; }

        public SusNotePlaybackDataMMM4XY()
        {
            NoteDataType = NoteDataType.mmm4xy;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM4XYStep : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public bool Invisible { get; set; }
        public bool End { get; set; }
        public SusNotePlaybackDataMMM4XYStep()
        {
            NoteDataType = NoteDataType.mmm4xyStep;
            NoteData = this;
        }
    }

    public class SusNotePlaybackDataMMM4XYCurveControl : SusNotePlaybackDataBase
    {
        public float X { get; set; }
        public float Size { get; set; }
        public SusNotePlaybackDataMMM4XYCurveControl()
        {
            NoteDataType = NoteDataType.mmm4xyCurveControl;
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
