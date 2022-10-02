using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Tea.Safu.Parse;
using Tea.Safu.Models;
using Tea.Safu.SusDebug;

namespace Tea.Safu.Analyze
{
    public class SusAnalyzer
    {
        public delegate void OnReceivedAnalyzingMessageDelegate(string msg, bool overrideLine = false);
        private OnReceivedAnalyzingMessageDelegate onReceivedAnalyzingMessage;
        private SusAnalyzeSetting setting;

        public OnReceivedAnalyzingMessageDelegate OnReceivedAnalyzingMessage { get => onReceivedAnalyzingMessage; set => onReceivedAnalyzingMessage = value; }



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
            [SerializeField] private bool considerationHighSpeed;
            [SerializeField] private int instantiateCycle;

            public SusAnalyzeSetting(float speed, float instantiatePosition, float judgmentPosition, long startTiming, bool considerationHighSpeed)
            {
                this.speed = speed;
                this.instantiatePosition = instantiatePosition;
                this.judgmentPosition = judgmentPosition;
                this.startTiming = startTiming;
                this.considerationHighSpeed = considerationHighSpeed;
            }

            public float Speed { get => speed; set => speed = value; }
            public float InstantiatePosition { get => instantiatePosition; set => instantiatePosition = value; }
            public float JudgmentPosition { get => judgmentPosition; set => judgmentPosition = value; }
            public long StartTiming { get => startTiming; set => startTiming = value; }
            public bool ConsiderationHighSpeed { get => considerationHighSpeed; set => considerationHighSpeed = value; }
            public int InstantiateCycle {
                get {
                    if(instantiateCycle <= 1)
                    {
                        SusDebugger.LogWarning($"SusAnalyzeSetting: instantiateCycle cannot be set to a value less than 1 (value: {instantiateCycle})\n1 was applied instead.");
                        return 1;
                    }
                    return instantiateCycle;
                }
                set => instantiateCycle = value; }
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
         * 2022/09/07
         * �����_�ňȉ��̂��͍̂l�����Ă��܂���B
         * �E�n�C�X�s�[�h�ω�
         * �E���ߐ��n�C�X�s�[�h�ω�
         */

        /// <summary>
        /// SusAsset�̃p�[�X&��͏������s���܂��B
        /// </summary>
        /// <param name="susAsset"></param>
        /// <returns></returns>
        public SusAnalyzeResult Analyze(SusAsset susAsset)
        {
            
            // ��̓��b�Z�[�W�̑��M
            bool shouldSendAnalyzingMessage = false;
            System.Diagnostics.Stopwatch stopwatch = null;
            void sendAnalyzingMessage(string msg, bool overrideLine, bool addProcessingTime)
            {
                if(addProcessingTime) msg += $" ({stopwatch.ElapsedMilliseconds}ms)";
                if (shouldSendAnalyzingMessage) onReceivedAnalyzingMessage(msg, overrideLine);
            }

            // ��̓��b�Z�[�W�𑗐M���邩
            if (onReceivedAnalyzingMessage != null)
            {
                shouldSendAnalyzingMessage = true;
                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }

            SusAnalyzeResult analyzeResult = new SusAnalyzeResult();
            SusNotePlaybackDatas notePlaybackDatas = new SusNotePlaybackDatas();
            notePlaybackDatas.Notes = new List<SusNotePlaybackDataBase>();

            int measureCount = 0;

            /* �p�[�X���� */

            sendAnalyzingMessage("�p�[�X�������J�n", false, false);
            SusParser susParser = new SusParser();
            SusObject susObject = susParser.ToSusObject(susAsset, setting, (msg, overrideLine, addProcessingTime) => sendAnalyzingMessage(msg, overrideLine, addProcessingTime));

            /* ��͏����J�n */
            sendAnalyzingMessage("��͏������J�n", false, false);
            List<SusNotePlaybackDataBase> noteDates = new List<SusNotePlaybackDataBase>();
            SusChartDatas chartDatas = susObject.ChartDatas;

            // BPM�ω�(mmm08)���擾
            List<SusNoteDataBase> bpmChanges = new List<SusNoteDataBase>();
            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
                if (noteData.DataType == NoteDataType.mmm08) bpmChanges.Add(noteData);

            SusCalculationUtils calculationUtils = new SusCalculationUtils(chartDatas, bpmChanges);

            // �L���^�C�~���O���v�Z&�\�[�g
            sendAnalyzingMessage("�m�[�c�L���^�C�~���O�v�Z��...", false, true);
            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
                noteData.EnabledTiming = calculationUtils.CalEnabledTiming(noteData);
            chartDatas.NoteDatas.Sort((x, y) => x.EnabledTiming.CompareTo(y.EnabledTiming));

            sendAnalyzingMessage("�m�[�c�Đ��f�[�^�쐬��...", false, true);
            int readIndex = 0;
            float progress = 0;

            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
            {
                progress = readIndex / (float)chartDatas.NoteDatas.Count;
                sendAnalyzingMessage($"{(progress * 100f).ToString("f1")}% ( {readIndex} / {chartDatas.NoteDatas.Count} )", true, true);

                if (noteData.DataType == NoteDataType.mmm08)
                {
                    readIndex += 1;
                    continue;
                }

                if (noteData.DataType == NoteDataType.mmm1x)
                {
                    NoteDataMMM1X mmm1xData = noteData.NoteData as NoteDataMMM1X;
                    if (mmm1xData.HispeedDefinition != null) mmm1xData.HispeedDefinition.SetUp(calculationUtils);
                    SusNotePlaybackDataMMM1X mmm1x = new SusNotePlaybackDataMMM1X();
                    mmm1x.calculationUtils = calculationUtils;
                    mmm1x.Setting = setting;
                    mmm1x.HispeedDefinition = noteData.HispeedDefinition;
                    mmm1x.calculationUtils = calculationUtils;
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
                        if (mmm2xyData.HispeedDefinition != null) mmm2xyData.HispeedDefinition.SetUp(calculationUtils);
                        SusNotePlaybackDataMMM2XY mmm2xy = new SusNotePlaybackDataMMM2XY();
                        mmm2xy.calculationUtils = calculationUtils;
                        mmm2xy.Setting = setting;
                        mmm2xy.HispeedDefinition = noteData.HispeedDefinition;
                        mmm2xy.calculationUtils = calculationUtils;
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
                                if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                SusNotePlaybackDataMMM2XYEnd mmm2xyEnd = new SusNotePlaybackDataMMM2XYEnd();
                                mmm2xyEnd.calculationUtils = calculationUtils;
                                mmm2xyEnd.Setting = setting;
                                mmm2xyEnd.HispeedDefinition = nextNoteData.HispeedDefinition;
                                mmm2xyEnd.calculationUtils = calculationUtils;
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
                        if (mmm3xyData.HispeedDefinition != null) mmm3xyData.HispeedDefinition.SetUp(calculationUtils);
                        SusNotePlaybackDataMMM3XY mmm3xy = new SusNotePlaybackDataMMM3XY();
                        mmm3xy.calculationUtils = calculationUtils;
                        mmm3xy.Setting = setting;
                        mmm3xy.HispeedDefinition = noteData.HispeedDefinition;
                        mmm3xy.calculationUtils = calculationUtils;
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
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM3XYStep mmm3xyEnd = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyEnd.calculationUtils = calculationUtils;
                                    mmm3xyEnd.Setting = setting;
                                    mmm3xyEnd.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm3xyEnd.calculationUtils = calculationUtils;
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
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM3XYStep mmm3xyStep = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyStep.calculationUtils = calculationUtils;
                                    mmm3xyStep.Setting = setting;
                                    mmm3xyStep.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm3xyStep.calculationUtils = calculationUtils;
                                    mmm3xyStep.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyStep.X = nextMmm3xy.X;
                                    mmm3xyStep.Size = nextMmm3xy.Size;
                                    mmm3xyStep.End = false;
                                    mmm3xyStep.Invisible = false;
                                    mmm3xyStep.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.Steps.Add(mmm3xyStep);
                                    break;
                                case 4:
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM3XYCurveControl mmm3xyCurveControl = new SusNotePlaybackDataMMM3XYCurveControl();
                                    mmm3xyCurveControl.calculationUtils = calculationUtils;
                                    mmm3xyCurveControl.Setting = setting;
                                    mmm3xyCurveControl.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm3xyCurveControl.calculationUtils = calculationUtils;
                                    mmm3xyCurveControl.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyCurveControl.X = nextMmm3xy.X;
                                    mmm3xyCurveControl.Size = nextMmm3xy.Size;
                                    mmm3xyCurveControl.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.CurveControls.Add(mmm3xyCurveControl);
                                    break;
                                case 5:
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM3XYStep mmm3xyInvisibleStep = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyInvisibleStep.calculationUtils = calculationUtils;
                                    mmm3xyInvisibleStep.Setting = setting;
                                    mmm3xyInvisibleStep.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm3xyInvisibleStep.calculationUtils = calculationUtils;
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

                else if (noteData.DataType == NoteDataType.mmm4xy)
                {
                    NoteDataMMM4XY mmm4xyData = noteData.NoteData as NoteDataMMM4XY;
                    if (mmm4xyData.Type == 1)
                    {
                        if (mmm4xyData.HispeedDefinition != null) mmm4xyData.HispeedDefinition.SetUp(calculationUtils);
                        SusNotePlaybackDataMMM4XY mmm4xy = new SusNotePlaybackDataMMM4XY();
                        mmm4xy.calculationUtils = calculationUtils;
                        mmm4xy.Setting = setting;
                        mmm4xy.HispeedDefinition = noteData.HispeedDefinition;
                        mmm4xy.calculationUtils = calculationUtils;
                        mmm4xy.EnabledTiming = noteData.EnabledTiming;
                        mmm4xy.X = mmm4xyData.X;
                        mmm4xy.Size = mmm4xyData.Size;
                        mmm4xy.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);

                        mmm4xy.Steps = new List<SusNotePlaybackDataMMM4XYStep>();
                        mmm4xy.CurveControls = new List<SusNotePlaybackDataMMM4XYCurveControl>();

                        string y = mmm4xyData.Y;
                        for (int i = readIndex + 1; i < chartDatas.NoteDatas.Count; i++)
                        {
                            SusNoteDataBase nextNoteData = chartDatas.NoteDatas[i];
                            if (nextNoteData.DataType != NoteDataType.mmm4xy) continue;

                            NoteDataMMM4XY nextMmm4xy = nextNoteData.NoteData as NoteDataMMM4XY;
                            if (nextMmm4xy.Y != y) continue;

                            bool end = false;
                            switch (nextMmm4xy.Type)
                            {
                                case 2:
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM4XYStep mmm4xyEnd = new SusNotePlaybackDataMMM4XYStep();
                                    mmm4xyEnd.calculationUtils = calculationUtils;
                                    mmm4xyEnd.Setting = setting;
                                    mmm4xyEnd.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm4xyEnd.calculationUtils = calculationUtils;
                                    mmm4xyEnd.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm4xyEnd.X = nextMmm4xy.X;
                                    mmm4xyEnd.Size = nextMmm4xy.Size;
                                    mmm4xyEnd.End = true;
                                    mmm4xyEnd.Invisible = false;
                                    mmm4xyEnd.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm4xy.Steps.Add(mmm4xyEnd);
                                    end = true;
                                    break;
                                case 3:
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM4XYStep mmm4xyStep = new SusNotePlaybackDataMMM4XYStep();
                                    mmm4xyStep.calculationUtils = calculationUtils;
                                    mmm4xyStep.Setting = setting;
                                    mmm4xyStep.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm4xyStep.calculationUtils = calculationUtils;
                                    mmm4xyStep.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm4xyStep.X = nextMmm4xy.X;
                                    mmm4xyStep.Size = nextMmm4xy.Size;
                                    mmm4xyStep.End = false;
                                    mmm4xyStep.Invisible = false;
                                    mmm4xyStep.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm4xy.Steps.Add(mmm4xyStep);
                                    break;
                                case 4:
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM4XYCurveControl mmm4xyCurveControl = new SusNotePlaybackDataMMM4XYCurveControl();
                                    mmm4xyCurveControl.calculationUtils = calculationUtils;
                                    mmm4xyCurveControl.Setting = setting;
                                    mmm4xyCurveControl.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm4xyCurveControl.calculationUtils = calculationUtils;
                                    mmm4xyCurveControl.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm4xyCurveControl.X = nextMmm4xy.X;
                                    mmm4xyCurveControl.Size = nextMmm4xy.Size;
                                    mmm4xyCurveControl.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm4xy.CurveControls.Add(mmm4xyCurveControl);
                                    break;
                                case 5:
                                    if (nextNoteData.HispeedDefinition != null) nextNoteData.HispeedDefinition.SetUp(calculationUtils);
                                    SusNotePlaybackDataMMM4XYStep mmm4xyInvisibleStep = new SusNotePlaybackDataMMM4XYStep();
                                    mmm4xyInvisibleStep.calculationUtils = calculationUtils;
                                    mmm4xyInvisibleStep.Setting = setting;
                                    mmm4xyInvisibleStep.HispeedDefinition = nextNoteData.HispeedDefinition;
                                    mmm4xyInvisibleStep.calculationUtils = calculationUtils;
                                    mmm4xyInvisibleStep.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm4xyInvisibleStep.X = nextMmm4xy.X;
                                    mmm4xyInvisibleStep.Size = nextMmm4xy.Size;
                                    mmm4xyInvisibleStep.End = false;
                                    mmm4xyInvisibleStep.Invisible = true;
                                    mmm4xyInvisibleStep.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm4xy.Steps.Add(mmm4xyInvisibleStep);
                                    break;
                            }
                            if (end) break;
                        }
                        notePlaybackDatas.Notes.Add(mmm4xy);
                    }
                }

                else if (noteData.DataType == NoteDataType.mmm5x)
                {
                    NoteDataMMM5X mmm5xData = noteData.NoteData as NoteDataMMM5X;
                    if (mmm5xData.HispeedDefinition != null) noteData.HispeedDefinition.SetUp(calculationUtils);
                    SusNotePlaybackDataMMM5X mmm5x = new SusNotePlaybackDataMMM5X();
                    mmm5x.calculationUtils = calculationUtils;
                    mmm5x.Setting = setting;
                    mmm5x.HispeedDefinition = noteData.HispeedDefinition;
                    mmm5x.EnabledTiming = noteData.EnabledTiming;
                    mmm5x.X = mmm5xData.X;
                    mmm5x.Type = mmm5xData.Type;
                    mmm5x.Size = mmm5xData.Size;
                    mmm5x.InstantiateTiming = mmm5x.CalInstantiateTiming(setting.StartTiming);
                    notePlaybackDatas.Notes.Add(mmm5x);
                }

                else if (noteData.DataType == NoteDataType.MeasureLine)
                {
                    if (noteData.HispeedDefinition != null) noteData.HispeedDefinition.SetUp(calculationUtils);
                    SusNotePlaybackDataMeasureLine measureLine = new SusNotePlaybackDataMeasureLine();
                    measureLine.calculationUtils = calculationUtils;
                    measureLine.Setting = setting;
                    measureLine.HispeedDefinition = noteData.HispeedDefinition;
                    measureLine.EnabledTiming = noteData.EnabledTiming;

                    measureLine.InstantiateTiming = measureLine.CalInstantiateTiming(setting.StartTiming);
                    notePlaybackDatas.Notes.Add(measureLine);
                }

                    // ���ߐ��𐔂���
                    if (noteData.MeasureNumber > measureCount) measureCount = noteData.MeasureNumber;
                readIndex += 1;
            }

            /*
            sendAnalyzingMessage("���ߐ��f�[�^�쐬��...", false, true);
            // ���ߐ��f�[�^���쐬
            for (int i = 0; i < measureCount; i++)
            {
                SusNotePlaybackDataMeasureLine measureLine = new SusNotePlaybackDataMeasureLine();
                measureLine.Setting = setting;
                measureLine.EnabledTiming = calculationUtils.CalMeasureEnabledTiming(i);

                measureLine.InstantiateTiming = measureLine.CalInstantiateTiming(setting.StartTiming);
                notePlaybackDatas.Notes.Add(measureLine);
            }*/

            sendAnalyzingMessage("���ʃf�[�^���\�[�g��...", false, true);
            notePlaybackDatas.Notes.Sort((x, y) => x.EnabledTiming.CompareTo(y.EnabledTiming));
            analyzeResult.NotePlaybackDatas = notePlaybackDatas;
            analyzeResult.MetaDatas = susObject.MetaDatas;

            sendAnalyzingMessage("�A�i���C�Y����", false, false);
            return analyzeResult;
        }

        /// <summary>
        /// SusAsset�̃p�[�X&��͏�����񓯊��ōs���܂��B
        /// </summary>
        /// <param name="susAsset"></param>
        /// <returns></returns>
        public async Task<SusAnalyzeResult> AnalyzeAsync(SusAsset susAsset)
        {
            SusAnalyzeResult result = await Task.Run(() => Analyze(susAsset));
            return result;
        }
    }
}