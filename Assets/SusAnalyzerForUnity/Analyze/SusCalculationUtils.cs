using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Models;

namespace Tea.Safu.Analyze
{
    public class SusCalculationUtils
    {
        private SusChartDatas chartDatas;
        private List<SusNoteDataBase> bpmChanges;

        public SusCalculationUtils(SusChartDatas chartDatas, List<SusNoteDataBase> bpmChanges)
        {
            this.chartDatas = chartDatas;
            this.bpmChanges = bpmChanges;
        }

        /// <summary>
        /// // 有効タイミングを計算します。
        /// </summary>
        public long CalEnabledTiming(SusNoteDataBase noteData)
        {
            float measureLength = GetMeasureLength(noteData.MeasureNumber);
            int enabledTick = (int)Math.Round(chartDatas.TicksPerBeat * measureLength * noteData.DataIndex / noteData.LineDataCount, 0);
            float enabledTimeInMeasure = CalTimeInMeasureByTick(noteData.MeasureNumber, enabledTick, measureLength);
            int enabledTimingInMeasure = (int)Math.Round(enabledTimeInMeasure * 1000f, 0);
            return enabledTimingInMeasure + CalMeasureStartTiming(noteData.MeasureNumber);
        }

        public long CalEnabledTiming(int measureNumber, int tick)
        {
            float measureLength = GetMeasureLength(measureNumber);
            float enabledTimeInMeasure = CalTimeInMeasureByTick(measureNumber, tick, measureLength);
            int enabledTimingInMeasure = (int)Math.Round(enabledTimeInMeasure * 1000f, 0);
            return enabledTimingInMeasure + CalMeasureStartTiming(measureNumber);
        }

        /// <summary>
        /// // 小節線の有効タイミングを計算します。
        /// </summary>
        public long CalMeasureEnabledTiming(int measureNumber)
        {
            float measureLength = GetMeasureLength(measureNumber);
            int enabledTick = 0;
            float enabledTimeInMeasure = CalTimeInMeasureByTick(measureNumber, enabledTick, measureLength);
            int enabledTimingInMeasure = (int)Math.Round(enabledTimeInMeasure * 1000f, 0);
            return enabledTimingInMeasure + CalMeasureStartTiming(measureNumber);
        }

        /// <summary>
        /// 1tickあたりの秒数を求めます。
        /// </summary>
        /// <param name="tpb"></param>
        /// <param name="bpm"></param>
        /// <returns></returns>
        public float CaltimePerTick(int tpb, float bpm)
        {
            // 一拍の時間(秒)
            float timePerBeat = 60f / bpm;
            return timePerBeat / tpb;
        }

        /// <summary>
        /// その小節の小節長を求めます。
        /// </summary>
        public float GetMeasureLength(int measureNumber)
        {
            float measureLength = 0;
            for (int i = 0; i < chartDatas.MeasureDefinitions.Count; i++)
            {
                if (chartDatas.MeasureDefinitions[i].MeasureNumber > measureNumber) break;
                else measureLength = chartDatas.MeasureDefinitions[i].MeasureLength;
            }
            return measureLength;
        }

        /// <summary>
        /// その小節のBPMを求めます。
        /// その小節中のBPM変化は考慮しません。
        /// </summary>
        public float GetMeasureBPM(int measureNumber)
        {
            float bpm = 0;
            int currentMeasureNumber = 0;
            int currentDataIndex = 0;
            for (int i = 0; i < bpmChanges.Count; i++)
            {
                if (bpmChanges[i].MeasureNumber >= measureNumber) break;
                else
                {
                    if (currentMeasureNumber != bpmChanges[i].MeasureNumber) currentDataIndex = 0;
                    currentMeasureNumber = bpmChanges[i].MeasureNumber;
                    if (currentDataIndex > bpmChanges[i].DataIndex) continue;
                    currentDataIndex = bpmChanges[i].DataIndex;

                    string zz = bpmChanges[i].Data[0] + bpmChanges[i].Data[1];
                    bpm = chartDatas.bpmDefinitions.Find((x) => x.ZZ == zz).Bpm;
                }
            }
            return bpm;
        }

        /// <summary>
        /// その小節が開始されるタイミングを計算します。
        /// </summary>
        public long CalMeasureStartTiming(int measureNumber)
        {
            float startTime = 0;
            for (int i = 0; i < measureNumber; i++)
            {
                float measureLength = GetMeasureLength(i);
                startTime += CalTimeInMeasureByTick(i, (int)Math.Round(chartDatas.TicksPerBeat * measureLength, 0), measureLength);
            }
            return (long)Math.Round(startTime * 1000f, 0);
        }

        /// <summary>
        /// 小節内で指定チックになるまでの時間をBPM変動を考慮して計算します。
        /// </summary>
        public float CalTimeInMeasureByTick(int measureNumber, int tick, float measureLength)
        {
            // その小節のBPM
            float measureBpm = GetMeasureBPM(measureNumber);

            // その小節中のBPM変化
            SusNoteDataBase bpmChangeInMeasure = bpmChanges.Find((x) => x.MeasureNumber == measureNumber);
            float enabledTimeInMeasure = 0;

            if (bpmChangeInMeasure != null)
            {
                // BPMの変化Tick & BPM をセットに配列に格納
                // [0]:Tick [1]: BPM
                List<float[]> bpmArr = new List<float[]>();

                for (int i = 0; i < bpmChangeInMeasure.LineDataCount; i++)
                {
                    string zz = bpmChangeInMeasure.Data[0] + bpmChangeInMeasure.Data[1];
                    if (zz == "00") continue;

                    float[] added = new float[2]
                    {
                            chartDatas.TicksPerBeat * measureLength * bpmChangeInMeasure.DataIndex / bpmChangeInMeasure.LineDataCount,
                            chartDatas.bpmDefinitions.Find((x) => x.ZZ == zz).Bpm
                    };
                    bpmArr.Add(added);
                }

                int bpmChangeIndex = 0;
                float timePerTick = CaltimePerTick(chartDatas.TicksPerBeat, measureBpm);
                for (int i = 0; i < tick; i++)
                {
                    if (bpmChangeIndex < bpmArr.Count && i >= bpmArr[bpmChangeIndex][0])
                    {
                        timePerTick = CaltimePerTick(chartDatas.TicksPerBeat, bpmArr[bpmChangeIndex][1]);
                        bpmChangeIndex += 1;
                    }
                    enabledTimeInMeasure += timePerTick;
                }
            }
            else
            {
                enabledTimeInMeasure = tick * CaltimePerTick(chartDatas.TicksPerBeat, measureBpm);
            }
            return enabledTimeInMeasure;
        }
    }
}