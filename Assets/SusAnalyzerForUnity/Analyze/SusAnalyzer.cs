using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Parse;
using Tea.Safu.Models;
using Tea.Safu.SusDebug;

namespace Tea.Safu.Analyze
{
    public class SusAnalyzer
    {
        private SusAnalyzeSetting setting;

        public SusAnalyzer(SusAnalyzeSetting setting)
        {
            this.setting = setting;
        }



        /// <summary>
        /// SUS��͎��̐ݒ�ł��B
        /// </summary>
        [Serializable]
        public class SusAnalyzeSetting
        {
            [SerializeField] private float speed;
            [SerializeField] private float instantiatePosition;
            [SerializeField] private float judgmentPosition;
            [SerializeField] private long startTiming;

            public SusAnalyzeSetting(float speed, float instantiatePosition, float judgmentPosition, long startTiming)
            {
                this.speed = speed;
                this.instantiatePosition = instantiatePosition;
                this.judgmentPosition = judgmentPosition;
                this.startTiming = startTiming;
            }

            public float Speed { get => speed; set => speed = value; }
            public float InstantiatePosition { get => instantiatePosition; set => instantiatePosition = value; }
            public float JudgmentPosition { get => judgmentPosition; set => judgmentPosition = value; }
            public long StartTiming { get => startTiming; set => startTiming = value; }
        }

        /// <summary>
        /// SUS�̉�͌��ʂł��B
        /// </summary>
        public class SusAnalyzeResult
        {
            public SusMetadatas MetaDatas { get; set; }
            public SusNotePlaybackDatas NotePlaybackDatas { get; set; }
        }



        /*
         * 2022/08/28
         * �����_�ňȉ��̂��͍̂l�����Ă��܂���B
         * �E�X���C�h�m�[�c
         * �E�n�C�X�s�[�h�ω�
         * �E���ߐ��n�C�X�s�[�h�ω�
         */

        /// <summary>
        /// SusAsset�̃p�[�X&��͏������s���܂��B
        /// </summary>
        /// <param name="susAsset"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public SusAnalyzeResult Analyze(SusAsset susAsset)
        {
            SusAnalyzeResult analyzeResult = new SusAnalyzeResult();
            SusNotePlaybackDatas notePlaybackDatas = new SusNotePlaybackDatas();
            notePlaybackDatas.Notes = new List<SusNotePlaybackDataBase>();

            int measureCount = 0;

            // �p�[�X
            SusParser susParser = new SusParser();
            SusObject susObject = susParser.ToSusObject(susAsset);

            // ��͏���
            List<SusNotePlaybackDataBase> noteDates = new List<SusNotePlaybackDataBase>();
            SusChartDatas chartDatas = susObject.ChartDatas;

            List<SusNoteDataBase> bpmChanges = new List<SusNoteDataBase>();

            // ���O����
            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
            {
                // BPM�ω�(mmm08)���擾
                if (noteData.DataType == NoteDataType.mmm08) bpmChanges.Add(noteData);
                // �L���^�C�~���O�v�Z
                noteData.EnabledTiming = CalEnabledTiming(noteData, chartDatas, bpmChanges);
            }
            chartDatas.NoteDatas.Sort((x, y) => x.EnabledTiming.CompareTo(y.EnabledTiming));

            int readIndex = 0;
            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
            {
                if (noteData.DataType == NoteDataType.mmm08)
                {
                    readIndex += 1;
                    continue;
                }
                if (noteData.DataType == NoteDataType.mmm4xy)
                {
                    readIndex += 1;
                    continue;
                }

                if (noteData.DataType == NoteDataType.mmm1x)
                {
                    NoteDataMMM1X mmm1xData = noteData.NoteData as NoteDataMMM1X;
                    SusNotePlaybackDataMMM1X mmm1x = new SusNotePlaybackDataMMM1X();
                    mmm1x.Setting = setting;
                    mmm1x.EnabledTiming = noteData.EnabledTiming;
                    mmm1x.X = mmm1xData.X;
                    mmm1x.Type = mmm1xData.Type;
                    mmm1x.Size = mmm1xData.Size;
                    mmm1x.InstantiateTiming = mmm1x.CalInstantiateTiming(setting.StartTiming);
                    notePlaybackDatas.Notes.Add(mmm1x);
                }

                else if (noteData.DataType == NoteDataType.mmm2xy)
                {
                    NoteDataMMM2XY mmm2xyData = noteData.NoteData as NoteDataMMM2XY;
                    if (mmm2xyData.Type == 1)
                    {
                        SusNotePlaybackDataMMM2XY mmm2xy = new SusNotePlaybackDataMMM2XY();
                        mmm2xy.Setting = setting;
                        mmm2xy.EnabledTiming = noteData.EnabledTiming;
                        mmm2xy.X = mmm2xyData.X;
                        mmm2xy.Size = mmm2xyData.Size;
                        mmm2xy.InstantiateTiming = mmm2xy.CalInstantiateTiming(setting.StartTiming);

                        string y = mmm2xyData.Y;
                        for (int i = readIndex; i < chartDatas.NoteDatas.Count; i++)
                        {
                            SusNoteDataBase nextNoteData = chartDatas.NoteDatas[i];
                            if (nextNoteData.DataType != NoteDataType.mmm2xy) continue;

                            NoteDataMMM2XY nextMmm2xy = nextNoteData.NoteData as NoteDataMMM2XY;
                            if (nextMmm2xy.Y != y) continue;

                            if (nextMmm2xy.Type == 2)
                            {
                                SusNotePlaybackDataMMM2XYEnd mmm2xyEnd = new SusNotePlaybackDataMMM2XYEnd();
                                mmm2xyEnd.Setting = setting;
                                mmm2xyEnd.EnabledTiming = nextNoteData.EnabledTiming;
                                mmm2xyEnd.InstantiateTiming = mmm2xy.CalInstantiateTiming(setting.StartTiming);
                                mmm2xy.EndNote = mmm2xyEnd;
                                break;
                            }
                        }
                        notePlaybackDatas.Notes.Add(mmm2xy);
                    }
                }

                else if (noteData.DataType == NoteDataType.mmm3xy)
                {
                    NoteDataMMM3XY mmm3xyData = noteData.NoteData as NoteDataMMM3XY;
                    if (mmm3xyData.Type == 1)
                    {
                        SusNotePlaybackDataMMM3XY mmm3xy = new SusNotePlaybackDataMMM3XY();
                        mmm3xy.Setting = setting;
                        mmm3xy.EnabledTiming = noteData.EnabledTiming;
                        mmm3xy.X = mmm3xyData.X;
                        mmm3xy.Size = mmm3xyData.Size;
                        mmm3xy.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);

                        mmm3xy.Steps = new List<SusNotePlaybackDataMMM3XYStep>();
                        mmm3xy.CurveControls = new List<SusNotePlaybackDataMMM3XYCurveControl>();

                        string y = mmm3xyData.Y;
                        for (int i = readIndex + 1; i < chartDatas.NoteDatas.Count; i++)
                        {
                            SusNoteDataBase nextNoteData = chartDatas.NoteDatas[i];
                            if (nextNoteData.DataType != NoteDataType.mmm3xy) continue;

                            NoteDataMMM3XY nextMmm3xy = nextNoteData.NoteData as NoteDataMMM3XY;
                            if (nextMmm3xy.Y != y) continue;

                            bool end = false;
                            switch (nextMmm3xy.Type)
                            {
                                case 2:
                                    SusNotePlaybackDataMMM3XYStep mmm3xyEnd = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyEnd.Setting = setting;
                                    mmm3xyEnd.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyEnd.X = nextMmm3xy.X;
                                    mmm3xyEnd.Size = nextMmm3xy.Size;
                                    mmm3xyEnd.End = true;
                                    mmm3xyEnd.Invisible = false;
                                    mmm3xyEnd.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.Steps.Add(mmm3xyEnd);
                                    end = true;
                                    break;
                                case 3:
                                    SusNotePlaybackDataMMM3XYStep mmm3xyStep = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyStep.Setting = setting;
                                    mmm3xyStep.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyStep.X = nextMmm3xy.X;
                                    mmm3xyStep.Size = nextMmm3xy.Size;
                                    mmm3xyStep.End = false;
                                    mmm3xyStep.Invisible = false;
                                    mmm3xyStep.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.Steps.Add(mmm3xyStep);
                                    break;
                                case 4:
                                    SusNotePlaybackDataMMM3XYCurveControl mmm3xyCurveControl = new SusNotePlaybackDataMMM3XYCurveControl();
                                    mmm3xyCurveControl.Setting = setting;
                                    mmm3xyCurveControl.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyCurveControl.X = nextMmm3xy.X;
                                    mmm3xyCurveControl.Size = nextMmm3xy.Size;
                                    mmm3xyCurveControl.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.CurveControls.Add(mmm3xyCurveControl);
                                    break;
                                case 5:
                                    SusNotePlaybackDataMMM3XYStep mmm3xyInvisibleStep = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyInvisibleStep.Setting = setting;
                                    mmm3xyInvisibleStep.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyInvisibleStep.X = nextMmm3xy.X;
                                    mmm3xyInvisibleStep.Size = nextMmm3xy.Size;
                                    mmm3xyInvisibleStep.End = false;
                                    mmm3xyInvisibleStep.Invisible = true;
                                    mmm3xyInvisibleStep.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.Steps.Add(mmm3xyInvisibleStep);
                                    break;
                            }
                            if (end) break;
                        }
                        notePlaybackDatas.Notes.Add(mmm3xy);
                    }
                }

                else if (noteData.DataType == NoteDataType.mmm5x)
                {
                    NoteDataMMM5X mmm5xData = noteData.NoteData as NoteDataMMM5X;
                    SusNotePlaybackDataMMM5X mmm5x = new SusNotePlaybackDataMMM5X();
                    mmm5x.Setting = setting;
                    mmm5x.EnabledTiming = noteData.EnabledTiming;
                    mmm5x.X = mmm5xData.X;
                    mmm5x.Type = mmm5xData.Type;
                    mmm5x.Size = mmm5xData.Size;
                    mmm5x.InstantiateTiming = mmm5x.CalInstantiateTiming(setting.StartTiming);
                    notePlaybackDatas.Notes.Add(mmm5x);
                }

                // ���ߐ��𐔂���
                if (noteData.MeasureNumber > measureCount) measureCount = noteData.MeasureNumber;
                readIndex += 1;
            }

            // ���ߐ��f�[�^���쐬
            for(int i = 0; i < measureCount; i++)
            {
                SusNotePlaybackDataMeasureLine measureLine = new SusNotePlaybackDataMeasureLine();
                measureLine.Setting = setting;
                measureLine.EnabledTiming = CalMeasureEnabledTiming(i, chartDatas, bpmChanges);

                measureLine.InstantiateTiming = measureLine.CalInstantiateTiming(setting.StartTiming);
                notePlaybackDatas.Notes.Add(measureLine);
            }

            notePlaybackDatas.Notes.Sort((x, y) => x.EnabledTiming.CompareTo(y.EnabledTiming));
            analyzeResult.NotePlaybackDatas = notePlaybackDatas;
            analyzeResult.MetaDatas = susObject.MetaDatas;
            return analyzeResult;
        }



        /// <summary>
        /// // �L���^�C�~���O���v�Z���܂��B
        /// </summary>
        /// <param name="noteData"></param>
        /// <param name="chartDatas"></param>
        /// <param name="bpmChanges"></param>
        /// <returns></returns>
        private long CalEnabledTiming(SusNoteDataBase noteData, SusChartDatas chartDatas, List<SusNoteDataBase> bpmChanges)
        {
            float measureLength = GetMeasureLength(noteData.MeasureNumber, chartDatas.MeasureDefinitions);
            int enabledTick = (int)Math.Round(chartDatas.TicksPerBeat * measureLength * noteData.DataIndex / noteData.LineDataCount, 0);
            float enabledTimeInMeasure = CalTimeInMeasureByTick(noteData.MeasureNumber, enabledTick, chartDatas, bpmChanges, measureLength);
            int enabledTimingInMeasure = (int)Math.Round(enabledTimeInMeasure * 1000f, 0);
            return enabledTimingInMeasure + CalMeasureStartTiming(noteData.MeasureNumber, bpmChanges, chartDatas, chartDatas.TicksPerBeat);
        }

        /// <summary>
        /// // ���ߐ��̗L���^�C�~���O���v�Z���܂��B
        /// </summary>
        /// <param name="noteData"></param>
        /// <param name="chartDatas"></param>
        /// <param name="bpmChanges"></param>
        /// <returns></returns>
        private long CalMeasureEnabledTiming(int measureNumber, SusChartDatas chartDatas, List<SusNoteDataBase> bpmChanges)
        {
            float measureLength = GetMeasureLength(measureNumber, chartDatas.MeasureDefinitions);
            int enabledTick = 0;
            float enabledTimeInMeasure = CalTimeInMeasureByTick(measureNumber, enabledTick, chartDatas, bpmChanges, measureLength);
            int enabledTimingInMeasure = (int)Math.Round(enabledTimeInMeasure * 1000f, 0);
            return enabledTimingInMeasure + CalMeasureStartTiming(measureNumber, bpmChanges, chartDatas, chartDatas.TicksPerBeat);
        }

        /// <summary>
        /// 1tick������̕b�������߂܂��B
        /// </summary>
        /// <param name="tpb"></param>
        /// <param name="bpm"></param>
        /// <returns></returns>
        private float CaltimePerTick(int tpb, float bpm)
        {
            // �ꔏ�̎���(�b)
            float timePerBeat = 60f / bpm;
            return timePerBeat / tpb;
        }

        /// <summary>
        /// ���̏��߂̏��ߒ������߂܂��B
        /// </summary>
        /// <param name="measureNumber"></param>
        /// <returns></returns>
        private float GetMeasureLength(int measureNumber, List<MeasureLengthDefinition> definitions)
        {
            float measureLength = 0;
            for(int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].MeasureNumber > measureNumber) break;
                else measureLength = definitions[i].MeasureLength;
            }
            return measureLength;
        }

        /// <summary>
        /// ���̏��߂�BPM�����߂܂��B
        /// ���̏��ߒ���BPM�ω��͍l�����܂���B
        /// </summary>
        /// <param name="measureNumber"></param>
        /// <returns></returns>
        private float GetMeasureBPM(int measureNumber, List<SusNoteDataBase> bpmChanges, List<BpmDefinition> definitions)
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
                    bpm = definitions.Find((x) => x.ZZ == zz).Bpm;
                }
            }
            return bpm;
        }

        /// <summary>
        /// ���̏��߂��J�n�����^�C�~���O���v�Z���܂��B
        /// </summary>
        /// <returns></returns>
        private long CalMeasureStartTiming(int measureNumber, List<SusNoteDataBase> bpmChanges, SusChartDatas chartDatas, int tpb)
        {
            float startTime = 0;
            for (int i = 0; i < measureNumber; i++)
            {
                float measureLength = GetMeasureLength(i, chartDatas.MeasureDefinitions);
                startTime += CalTimeInMeasureByTick(i, (int)Math.Round(tpb * measureLength, 0), chartDatas, bpmChanges, measureLength);
            }
            return (long)Math.Round(startTime * 1000f, 0);
        }

        /// <summary>
        /// ���ߓ��Ŏw��`�b�N�ɂȂ�܂ł̎��Ԃ�BPM�ϓ����l�����Čv�Z���܂��B
        /// </summary>
        /// <returns></returns>
        private float CalTimeInMeasureByTick(int measureNumber, int tick, SusChartDatas chartDatas, List<SusNoteDataBase> bpmChanges, float measureLength)
        {
            // ���̏��߂�BPM
            float measureBpm = GetMeasureBPM(measureNumber, bpmChanges, chartDatas.BpmDefinitions);

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
                            chartDatas.BpmDefinitions.Find((x) => x.ZZ == zz).Bpm
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