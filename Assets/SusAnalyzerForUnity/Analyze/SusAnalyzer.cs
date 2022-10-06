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
        /// SUS解析時の設定です。
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

            public SusAnalyzeSetting(float speed, float instantiatePosition, float judgmentPosition, long startTiming, bool considerationHighSpeed, int instantiateCycle)
            {
                this.speed = speed;
                this.instantiatePosition = instantiatePosition;
                this.judgmentPosition = judgmentPosition;
                this.startTiming = startTiming;
                this.considerationHighSpeed = considerationHighSpeed;
                this.instantiateCycle = instantiateCycle;
            }

            public float Speed { get => speed; set => speed = value; }
            public float InstantiatePosition { get => instantiatePosition; set => instantiatePosition = value; }
            public float JudgmentPosition { get => judgmentPosition; set => judgmentPosition = value; }
            public long StartTiming { get => startTiming; set => startTiming = value; }
            public bool ConsiderationHighSpeed { get => considerationHighSpeed; set => considerationHighSpeed = value; }
            public int InstantiateCycle { get => instantiateCycle; set => instantiateCycle = value; }
        }

        /// <summary>
        /// SUSの解析結果です。
        /// </summary>
        public class SusAnalyzeResult
        {
            public SusMetadatas MetaDatas { get; set; }
            public SusNotePlaybackDatas NotePlaybackDatas { get; set; }
            public long EndTiming;
        }



        /*
         * 2022/09/07
         * 現時点で以下のものは考慮していません。
         * ・ハイスピード変化
         * ・小節線ハイスピード変化
         */

        /// <summary>
        /// SusAssetのパース&解析処理を行います。
        /// </summary>
        /// <param name="susAsset"></param>
        /// <returns></returns>
        public SusAnalyzeResult Analyze(SusAsset susAsset)
        {
            
            // 解析メッセージの送信
            bool shouldSendAnalyzingMessage = false;
            System.Diagnostics.Stopwatch stopwatch = null;
            void sendAnalyzingMessage(string msg, bool overrideLine, bool addProcessingTime)
            {
                if(addProcessingTime) msg += $" ({stopwatch.ElapsedMilliseconds}ms)";
                if (shouldSendAnalyzingMessage) onReceivedAnalyzingMessage(msg, overrideLine);
            }

            // 解析メッセージを送信するか
            if (onReceivedAnalyzingMessage != null)
            {
                shouldSendAnalyzingMessage = true;
                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }

            SusAnalyzeResult analyzeResult = new SusAnalyzeResult();
            SusNotePlaybackDatas notePlaybackDatas = new SusNotePlaybackDatas();
            notePlaybackDatas.Notes = new List<SusNotePlaybackDataBase>();

            analyzeResult.EndTiming = long.MinValue;

            // SUS解析設定の確認
            if (setting.InstantiateCycle < 1)
            {
                SusDebugger.LogWarning($"SusAnalyzeSetting: instantiateCycle cannot be set to a value less than 1 (value: {setting.InstantiateCycle})\n1 was applied instead.");
                setting.InstantiateCycle = 1;
            }

            /* パース処理 */

            sendAnalyzingMessage("パース処理を開始", false, false);
            SusParser susParser = new SusParser();
            SusObject susObject = susParser.ToSusObject(susAsset, setting, (msg, overrideLine, addProcessingTime) => sendAnalyzingMessage(msg, overrideLine, addProcessingTime));

            /* 解析処理開始 */
            sendAnalyzingMessage("解析処理を開始", false, false);
            List<SusNotePlaybackDataBase> noteDates = new List<SusNotePlaybackDataBase>();
            SusChartDatas chartDatas = susObject.ChartDatas;

            // BPM変化(mmm08)を取得
            List<SusNoteDataBase> bpmChanges = new List<SusNoteDataBase>();
            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
                if (noteData.DataType == NoteDataType.mmm08) bpmChanges.Add(noteData);

            SusCalculationUtils calculationUtils = new SusCalculationUtils(chartDatas, bpmChanges);

            // 有効タイミングを計算&ソート
            sendAnalyzingMessage("ノーツ有効タイミング計算中...", false, true);
            foreach (SusNoteDataBase noteData in chartDatas.NoteDatas)
            {
                long enabledTiming = calculationUtils.CalEnabledTiming(noteData);
                noteData.EnabledTiming = enabledTiming;
                if (enabledTiming > analyzeResult.EndTiming) analyzeResult.EndTiming = enabledTiming;
            }
            chartDatas.NoteDatas.Sort((x, y) => x.EnabledTiming.CompareTo(y.EnabledTiming));

            sendAnalyzingMessage("ハイスピード定義をセットアップ中...", false, true);
            foreach (HispeedDefinition definition in chartDatas.HispeedDefinitions)
                definition.SetUp(calculationUtils);

            sendAnalyzingMessage("ノーツ再生データ作成中...", false, true);
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
                    SusNotePlaybackDataMMM1X mmm1x = new SusNotePlaybackDataMMM1X();
                    mmm1x.CalculationUtils = calculationUtils;
                    mmm1x.Setting = setting;
                    mmm1x.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == noteData.HispeedZz);
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
                        mmm2xy.CalculationUtils = calculationUtils;
                        mmm2xy.Setting = setting;
                        mmm2xy.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == noteData.HispeedZz);
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
                                mmm2xyEnd.CalculationUtils = calculationUtils;
                                mmm2xyEnd.Setting = setting;
                                mmm2xyEnd.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
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
                        mmm3xy.CalculationUtils = calculationUtils;
                        mmm3xy.Setting = setting;
                        mmm3xy.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == noteData.HispeedZz);
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
                                    mmm3xyEnd.CalculationUtils = calculationUtils;
                                    mmm3xyEnd.Setting = setting;
                                    mmm3xyEnd.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
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
                                    mmm3xyStep.CalculationUtils = calculationUtils;
                                    mmm3xyStep.Setting = setting;
                                    mmm3xyStep.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
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
                                    mmm3xyCurveControl.CalculationUtils = calculationUtils;
                                    mmm3xyCurveControl.Setting = setting;
                                    mmm3xyCurveControl.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
                                    mmm3xyCurveControl.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm3xyCurveControl.X = nextMmm3xy.X;
                                    mmm3xyCurveControl.Size = nextMmm3xy.Size;
                                    mmm3xyCurveControl.InstantiateTiming = mmm3xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm3xy.CurveControls.Add(mmm3xyCurveControl);
                                    break;
                                case 5:
                                    SusNotePlaybackDataMMM3XYStep mmm3xyInvisibleStep = new SusNotePlaybackDataMMM3XYStep();
                                    mmm3xyInvisibleStep.CalculationUtils = calculationUtils;
                                    mmm3xyInvisibleStep.Setting = setting;
                                    mmm3xyInvisibleStep.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
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
                        SusNotePlaybackDataMMM4XY mmm4xy = new SusNotePlaybackDataMMM4XY();
                        mmm4xy.CalculationUtils = calculationUtils;
                        mmm4xy.Setting = setting;
                        mmm4xy.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == noteData.HispeedZz);
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
                                    SusNotePlaybackDataMMM4XYStep mmm4xyEnd = new SusNotePlaybackDataMMM4XYStep();
                                    mmm4xyEnd.CalculationUtils = calculationUtils;
                                    mmm4xyEnd.Setting = setting;
                                    mmm4xyEnd.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
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
                                    SusNotePlaybackDataMMM4XYStep mmm4xyStep = new SusNotePlaybackDataMMM4XYStep();
                                    mmm4xyStep.CalculationUtils = calculationUtils;
                                    mmm4xyStep.Setting = setting;
                                    mmm4xyStep.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
                                    mmm4xyStep.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm4xyStep.X = nextMmm4xy.X;
                                    mmm4xyStep.Size = nextMmm4xy.Size;
                                    mmm4xyStep.End = false;
                                    mmm4xyStep.Invisible = false;
                                    mmm4xyStep.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm4xy.Steps.Add(mmm4xyStep);
                                    break;
                                case 4:
                                    SusNotePlaybackDataMMM4XYCurveControl mmm4xyCurveControl = new SusNotePlaybackDataMMM4XYCurveControl();
                                    mmm4xyCurveControl.CalculationUtils = calculationUtils;
                                    mmm4xyCurveControl.Setting = setting;
                                    mmm4xyCurveControl.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
                                    mmm4xyCurveControl.EnabledTiming = nextNoteData.EnabledTiming;
                                    mmm4xyCurveControl.X = nextMmm4xy.X;
                                    mmm4xyCurveControl.Size = nextMmm4xy.Size;
                                    mmm4xyCurveControl.InstantiateTiming = mmm4xy.CalInstantiateTiming(setting.StartTiming);
                                    mmm4xy.CurveControls.Add(mmm4xyCurveControl);
                                    break;
                                case 5:
                                    SusNotePlaybackDataMMM4XYStep mmm4xyInvisibleStep = new SusNotePlaybackDataMMM4XYStep();
                                    mmm4xyInvisibleStep.CalculationUtils = calculationUtils;
                                    mmm4xyInvisibleStep.Setting = setting;
                                    mmm4xyInvisibleStep.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == nextNoteData.HispeedZz);
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
                    SusNotePlaybackDataMMM5X mmm5x = new SusNotePlaybackDataMMM5X();
                    mmm5x.CalculationUtils = calculationUtils;
                    mmm5x.Setting = setting;
                    mmm5x.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == noteData.HispeedZz);
                    mmm5x.EnabledTiming = noteData.EnabledTiming;
                    mmm5x.X = mmm5xData.X;
                    mmm5x.Type = mmm5xData.Type;
                    mmm5x.Size = mmm5xData.Size;
                    mmm5x.InstantiateTiming = mmm5x.CalInstantiateTiming(setting.StartTiming);
                    notePlaybackDatas.Notes.Add(mmm5x);
                }

                else if (noteData.DataType == NoteDataType.MeasureLine)
                {
                    SusNotePlaybackDataMeasureLine measureLine = new SusNotePlaybackDataMeasureLine();
                    measureLine.CalculationUtils = calculationUtils;
                    measureLine.Setting = setting;
                    measureLine.HispeedDefinition = chartDatas.HispeedDefinitions.Find((x) => x.ZZ == noteData.HispeedZz);
                    measureLine.EnabledTiming = noteData.EnabledTiming;

                    measureLine.InstantiateTiming = measureLine.CalInstantiateTiming(setting.StartTiming);
                    notePlaybackDatas.Notes.Add(measureLine);
                }

                readIndex += 1;
            }

            /*
            sendAnalyzingMessage("小節線データ作成中...", false, true);
            // 小節線データを作成
            for (int i = 0; i < measureCount; i++)
            {
                SusNotePlaybackDataMeasureLine measureLine = new SusNotePlaybackDataMeasureLine();
                measureLine.Setting = setting;
                measureLine.EnabledTiming = calculationUtils.CalMeasureEnabledTiming(i);

                measureLine.InstantiateTiming = measureLine.CalInstantiateTiming(setting.StartTiming);
                notePlaybackDatas.Notes.Add(measureLine);
            }*/

            sendAnalyzingMessage("譜面データをソート中...", false, true);
            notePlaybackDatas.Notes.Sort((x, y) => x.EnabledTiming.CompareTo(y.EnabledTiming));
            analyzeResult.NotePlaybackDatas = notePlaybackDatas;
            analyzeResult.MetaDatas = susObject.MetaDatas;

            sendAnalyzingMessage("アナライズ完了", false, false);
            return analyzeResult;
        }

        /// <summary>
        /// SusAssetのパース&解析処理を非同期で行います。
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