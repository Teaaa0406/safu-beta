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
        /// // �L���^�C�~���O���v�Z���܂��B
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
        /// // ���ߐ��̗L���^�C�~���O���v�Z���܂��B
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
        /// 1tick������̕b�������߂܂��B
        /// </summary>
        /// <param name="tpb"></param>
        /// <param name="bpm"></param>
        /// <returns></returns>
        public float CaltimePerTick(int tpb, float bpm)
        {
            // �ꔏ�̎���(�b)
            float timePerBeat = 60f / bpm;
            return timePerBeat / tpb;
        }

        /// <summary>
        /// ���̏��߂̏��ߒ������߂܂��B
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
        /// ���̏��߂�BPM�����߂܂��B
        /// ���̏��ߒ���BPM�ω��͍l�����܂���B
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
        /// ���̏��߂��J�n�����^�C�~���O���v�Z���܂��B
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
        /// ���ߓ��Ŏw��`�b�N�ɂȂ�܂ł̎��Ԃ�BPM�ϓ����l�����Čv�Z���܂��B
        /// </summary>
        public float CalTimeInMeasureByTick(int measureNumber, int tick, float measureLength)
        {
            // ���̏��߂�BPM
            float measureBpm = GetMeasureBPM(measureNumber);

            // ���̏��ߒ���BPM�ω�
            SusNoteDataBase bpmChangeInMeasure = bpmChanges.Find((x) => x.MeasureNumber == measureNumber);
            float enabledTimeInMeasure = 0;

            if (bpmChangeInMeasure != null)
            {
                // BPM�̕ω�Tick & BPM ���Z�b�g�ɔz��Ɋi�[
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

        /// <summary>
        /// �^�C�~���O���̈ړ��������v�Z���܂�
        /// </summary>
        /// <returns></returns>
        public float CalDistancePerTiming(SusAnalyzer.SusAnalyzeSetting setting)
        {
            return (setting.InstantiatePosition - setting.JudgmentPosition) / (setting.Speed * 1000f);
        }
    }
}