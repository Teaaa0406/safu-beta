using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tea.Safu.Analyze;
using Tea.Safu.Util;
using Tea.Safu.Models;
using Tea.Safu.SusDebug;

namespace Tea.Safu.Parse
{
    public class SusParser
    {
        struct MeasureHsApplyInfo
        {
            public MeasureHsApplyInfo(int measure, string zz)
            {
                Measure = measure;
                Zz = zz;
            }

            public int Measure { get; set; }
            public string Zz { get; set; }
        }



        /// <summary>
        /// SusAsset から SusObject を作成します。
        /// ここでは計算処理は行われません。
        /// </summary>
        /// <param name="susAsset">SusAsset</param>
        /// <returns></returns>
        public SusObject ToSusObject(SusAsset susAsset, SusAnalyzer.SusAnalyzeSetting setting, Action<string, bool, bool> sendMesssageAction = null)
        {
            SusObject susObject = new SusObject();

            sendMesssageAction("SUSファイルを読み取り中...", false, true);
            List<SusLineInfo> lineInfos = ToLineInfos(susAsset);

            // メタデータパース
            if (sendMesssageAction != null) sendMesssageAction("メタデータをパース中...", false, true);
            susObject.MetaDatas = ToSusMetaDatas(lineInfos);

            // ticks_per_beat を取得
            int tpb = 480;
            SusMetaRequest tpbReq = susObject.MetaDatas.REQUESTs.Find((x) => x.Key == "ticks_per_beat");
            if (tpbReq == null) SusDebugger.Log("\"ticks_per_beat\" was not defined, so the default \"480\" was applied.");
            else tpb = int.Parse(tpbReq.Value as string);

            // 譜面データパース
            if (sendMesssageAction != null) sendMesssageAction("譜面データをパース中...", false, true);
            susObject.ChartDatas = ToSusChartDatas(lineInfos, tpb, setting);

            return susObject;
        }

        private SusChartDatas ToSusChartDatas(List<SusLineInfo> lineInfos, int tpb, SusAnalyzer.SusAnalyzeSetting setting)
        {
            SusChartDatas chartDatas = new SusChartDatas();
            int currentMeasureBase = 0;

            // 各定義を事前取得
            List<BpmDefinition> bpmDefinitions = new List<BpmDefinition>();
            List<AttributeDefinition> attributeDefinitions = new List<AttributeDefinition>();
            List<HispeedDefinition> hispeedDefinitions = new List<HispeedDefinition>();
            List<MeasureLengthDefinition> measureLengthDefinitions = new List<MeasureLengthDefinition>();

            foreach (SusLineInfo lineInfo in lineInfos)
            {
                if (lineInfo.LineType != SusLineType.Chart) continue;

                // BPMzz (BPM定義)
                if (GetMMM(lineInfo.Header) == "BPM")
                {
                    BpmDefinition bpm = new BpmDefinition();
                    bpm.ZZ = lineInfo.Header.Substring(3, 2);
                    bpm.Bpm = float.Parse(lineInfo.Data);
                    bpmDefinitions.Add(bpm);
                }

                // ATRzz (ノーツ属性定義)
                else if (GetMMM(lineInfo.Header) == "ATR")
                {
                    AttributeDefinition atr = new AttributeDefinition();
                    atr.ZZ = lineInfo.Header.Substring(3, 2);
                    string[] splitedData = lineInfo.Data.Replace(" ", "").Split(',');
                    foreach (string data in splitedData)
                    {
                        string[] arr = data.Split(':');
                        switch (arr[0])
                        {
                            case "rh": atr.Rh.Add(float.Parse(arr[1])); break;
                            case "h": atr.H.Add(float.Parse(arr[1])); break;
                            case "pr": atr.Pr.Add(int.Parse(arr[1])); break;
                        }
                    }
                    attributeDefinitions.Add(atr);
                }

                // TILzz (スピード変化定義)
                else if (GetMMM(lineInfo.Header) == "TIL")
                {
                    HispeedDefinition hispeed = new HispeedDefinition();
                    hispeed.ZZ = lineInfo.Header.Substring(3, 2);
                    hispeed.hispeedInfos = new List<HispeedInfo>();

                    string[] splitedData = lineInfo.Data.Replace(" ", "").Split(',');
                    foreach (string data in splitedData)
                    {
                        if (string.IsNullOrEmpty(data)) continue;
                        HispeedInfo info = new HispeedInfo();
                        string[] arr = data.Split('\'');
                        info.Meas = int.Parse(arr[0]) + currentMeasureBase;
                        arr = arr[1].Split(':');
                        info.Tick = int.Parse(arr[0]);
                        info.Speed = float.Parse(arr[1]);
                        hispeed.hispeedInfos.Add(info);
                    }
                    hispeed.hispeedInfos = hispeed.hispeedInfos.OrderBy(rec => rec.Meas).ThenBy(rec => rec.Tick).ToList();
                    hispeedDefinitions.Add(hispeed);
                }

                // mmm02 (小節長)
                else if (lineInfo.Header.Substring(3, 2) == "02")
                {
                    MeasureLengthDefinition measureLength = new MeasureLengthDefinition();
                    measureLength.MeasureNumber = int.Parse(GetMMM(lineInfo.Header));
                    measureLength.MeasureLength = float.Parse(lineInfo.Data);
                    measureLengthDefinitions.Add(measureLength);
                }

                // MEASUREBS (小節番号ベース値)
                else if (lineInfo.Header == "MEASUREBS")
                {
                    currentMeasureBase = int.Parse(lineInfo.Data);
                }
            }

            List<SusNoteDataBase> noteDatas = new List<SusNoteDataBase>();
            string currentHispeedZz = null;
            currentMeasureBase = 0;
            int readIndex = 0;

            foreach (SusLineInfo lineInfo in lineInfos)
            {
                if (lineInfo.LineType != SusLineType.Chart) continue;
                if (GetMMM(lineInfo.Header) == "BPM") continue;
                if (GetMMM(lineInfo.Header) == "ATR") continue;
                if (GetMMM(lineInfo.Header) == "TIL") continue;
                if (lineInfo.Header.Substring(3, 2) == "02") continue;

                // mmm08 (BPM変化)
                else if (lineInfo.Header.Substring(3, 2) == "08")
                {
                    List<string[]> dataPartArray = ChartDataPartToArray(lineInfo.Data);
                    for (int i = 0; i < dataPartArray.Count; i++)
                    {
                        string dataStr = dataPartArray[i][0] + dataPartArray[i][1];
                        if (dataStr == "00") continue;

                        NoteDataMMM08 mmm08 = new NoteDataMMM08();
                        mmm08.MeasureNumber = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                        mmm08.LineDataCount = dataPartArray.Count;
                        mmm08.DataIndex = i;
                        mmm08.Data = dataPartArray[i];
                        noteDatas.Add(mmm08);
                    }
                }

                // HISPEED (スピード変化)
                else if (lineInfo.Header == "HISPEED")
                {
                    if (setting.ConsiderationHighSpeed) currentHispeedZz = lineInfo.Data;
                }

                // HISPEED (スピード変化適用解除)
                else if (lineInfo.Header == "NOSPEED")
                {
                    currentHispeedZz = null;
                }

                // mmm1x (タップ)
                else if (lineInfo.Header.Substring(3, 1) == "1")
                {
                    List<string[]> dataPartArray = ChartDataPartToArray(lineInfo.Data);
                    for (int i = 0; i < dataPartArray.Count; i++)
                    {
                        string dataStr = dataPartArray[i][0] + dataPartArray[i][1];
                        if (dataStr == "00") continue;

                        NoteDataMMM1X mmm1x = new NoteDataMMM1X();
                        mmm1x.MeasureNumber = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                        mmm1x.LineDataCount = dataPartArray.Count;
                        mmm1x.DataIndex = i;
                        mmm1x.Data = dataPartArray[i];
                        mmm1x.HispeedZz = currentHispeedZz;
                        mmm1x.X = Base36Util.Decode(GetX(lineInfo.Header));
                        mmm1x.Type = Base36Util.Decode(dataPartArray[i][0]);
                        mmm1x.Size = Base36Util.Decode(dataPartArray[i][1]);
                        noteDatas.Add(mmm1x);
                    }
                }

                // mmm2xy (ホールド)
                else if (lineInfo.Header.Substring(3, 1) == "2")
                {
                    List<string[]> dataPartArray = ChartDataPartToArray(lineInfo.Data);
                    for (int i = 0; i < dataPartArray.Count; i++)
                    {
                        string dataStr = dataPartArray[i][0] + dataPartArray[i][1];
                        if (dataStr == "00") continue;

                        NoteDataMMM2XY mmm2xy = new NoteDataMMM2XY();
                        mmm2xy.MeasureNumber = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                        mmm2xy.LineDataCount = dataPartArray.Count;
                        mmm2xy.DataIndex = i;
                        mmm2xy.Data = dataPartArray[i];
                        mmm2xy.HispeedZz = currentHispeedZz;
                        mmm2xy.X = Base36Util.Decode(GetX(lineInfo.Header));
                        mmm2xy.Y = GetY(lineInfo.Header);
                        mmm2xy.Type = Base36Util.Decode(dataPartArray[i][0]);
                        mmm2xy.Size = Base36Util.Decode(dataPartArray[i][1]);
                        noteDatas.Add(mmm2xy);
                    }
                }

                // mmm3xy (スライド1)
                else if (lineInfo.Header.Substring(3, 1) == "3")
                {
                    List<string[]> dataPartArray = ChartDataPartToArray(lineInfo.Data);
                    for (int i = 0; i < dataPartArray.Count; i++)
                    {
                        string dataStr = dataPartArray[i][0] + dataPartArray[i][1];
                        if (dataStr == "00") continue;

                        NoteDataMMM3XY mmm3xy = new NoteDataMMM3XY();
                        mmm3xy.MeasureNumber = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                        mmm3xy.LineDataCount = dataPartArray.Count;
                        mmm3xy.DataIndex = i;
                        mmm3xy.Data = dataPartArray[i];
                        mmm3xy.HispeedZz = currentHispeedZz;
                        mmm3xy.X = Base36Util.Decode(GetX(lineInfo.Header));
                        mmm3xy.Y = GetY(lineInfo.Header);
                        mmm3xy.Type = Base36Util.Decode(dataPartArray[i][0]);
                        mmm3xy.Size = Base36Util.Decode(dataPartArray[i][1]);
                        noteDatas.Add(mmm3xy);
                    }
                }

                // mmm4xy (スライド2)
                else if (lineInfo.Header.Substring(3, 1) == "4")
                {
                    List<string[]> dataPartArray = ChartDataPartToArray(lineInfo.Data);
                    for (int i = 0; i < dataPartArray.Count; i++)
                    {
                        string dataStr = dataPartArray[i][0] + dataPartArray[i][1];
                        if (dataStr == "00") continue;

                        NoteDataMMM4XY mmm4xy = new NoteDataMMM4XY();
                        mmm4xy.MeasureNumber = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                        mmm4xy.LineDataCount = dataPartArray.Count;
                        mmm4xy.DataIndex = i;
                        mmm4xy.Data = dataPartArray[i];
                        mmm4xy.HispeedZz = currentHispeedZz;
                        mmm4xy.X = Base36Util.Decode(GetX(lineInfo.Header));
                        mmm4xy.Y = GetY(lineInfo.Header);
                        mmm4xy.Type = Base36Util.Decode(dataPartArray[i][0]);
                        mmm4xy.Size = Base36Util.Decode(dataPartArray[i][1]);
                        noteDatas.Add(mmm4xy);
                    }
                }

                // mmm5x (ディレクショナル)
                else if (lineInfo.Header.Substring(3, 1) == "5")
                {
                    List<string[]> dataPartArray = ChartDataPartToArray(lineInfo.Data);
                    for (int i = 0; i < dataPartArray.Count; i++)
                    {
                        string dataStr = dataPartArray[i][0] + dataPartArray[i][1];
                        if (dataStr == "00") continue;

                        NoteDataMMM5X mmm5x = new NoteDataMMM5X();
                        mmm5x.MeasureNumber = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                        mmm5x.LineDataCount = dataPartArray.Count;
                        mmm5x.DataIndex = i;
                        mmm5x.Data = dataPartArray[i];
                        mmm5x.HispeedZz = currentHispeedZz;
                        mmm5x.X = Base36Util.Decode(GetX(lineInfo.Header));
                        mmm5x.Type = Base36Util.Decode(dataPartArray[i][0]);
                        mmm5x.Size = Base36Util.Decode(dataPartArray[i][1]);
                        noteDatas.Add(mmm5x);
                    }
                }

                // MEASUREBS (小節番号ベース値)
                else if (lineInfo.Header == "MEASUREBS")
                {
                    currentMeasureBase = int.Parse(lineInfo.Data);
                }

                // 例外
                else
                {
                    string mmm = GetMMM(lineInfo.Header);
                    if (mmm == "BPM" || mmm == "ATR" || mmm == "TIL" || lineInfo.Header.Substring(3, 2) == "02") continue;
                    SusDebugger.LogWarning($"Bad or unsupported header. (header: {lineInfo.Header})");
                }
                readIndex += 1;
            }

            // 小節線データ作成
            // 最大小節数を取得
            int maxMmm = 0;
            currentMeasureBase = 0;
            foreach (SusLineInfo lineInfo in lineInfos)
            {
                if (lineInfo.LineType != SusLineType.Chart) continue;
                if (GetMMM(lineInfo.Header) == "BPM") continue;
                if (GetMMM(lineInfo.Header) == "ATR") continue;
                if (GetMMM(lineInfo.Header) == "TIL") continue;
                if (lineInfo.Header.Substring(3, 2) == "02") continue;

                if (lineInfo.Header.Substring(3, 1) == "1" || lineInfo.Header.Substring(3, 1) == "2" || lineInfo.Header.Substring(3, 1) == "3" || lineInfo.Header.Substring(3, 1) == "4" || lineInfo.Header.Substring(3, 1) == "5")
                {
                    int measure = int.Parse(GetMMM(lineInfo.Header)) + currentMeasureBase;
                    if (measure > maxMmm) maxMmm = measure;
                }
                else if (lineInfo.Header == "MEASUREBS")
                {
                    currentMeasureBase = int.Parse(lineInfo.Data);
                }
            }

            List<MeasureHsApplyInfo> hsApplyInfo = new List<MeasureHsApplyInfo>();
            for(int i = 0; i < lineInfos.Count; i++)
            {
            }

            for(int i = 0; i < maxMmm; i++)
            {
                NoteDataMeasureLine measureLine = new NoteDataMeasureLine();
                measureLine.MeasureNumber = i;
                measureLine.LineDataCount = 1;
                measureLine.DataIndex = 0;
                measureLine.HispeedZz = currentHispeedZz;

                noteDatas.Add(measureLine);
            }

            measureLengthDefinitions.Sort((x, y) => x.MeasureNumber - y.MeasureNumber);

            chartDatas.TicksPerBeat = tpb;
            chartDatas.bpmDefinitions = bpmDefinitions;
            chartDatas.AttributeDefinitions = attributeDefinitions;
            chartDatas.MeasureDefinitions = measureLengthDefinitions;
            chartDatas.HispeedDefinitions = hispeedDefinitions;
            chartDatas.NoteDatas = noteDatas;
            return chartDatas;
        }

        private SusMetadatas ToSusMetaDatas(List<SusLineInfo> lineInfos)
        {
            SusMetadatas metaDatas = new SusMetadatas();
            metaDatas.REQUESTs = new List<SusMetaRequest>();

            foreach (SusLineInfo lineInfo in lineInfos)
            {
                if (lineInfo.LineType != SusLineType.Meta || string.IsNullOrEmpty(lineInfo.Data)) continue;
                switch (lineInfo.Header)
                {
                    case "TITLE":
                        metaDatas.TITLE = lineInfo.Data; break;
                    case "SUBTITLE":
                        metaDatas.SUBTITLE = lineInfo.Data; break;
                    case "ARTIST":
                        metaDatas.ARTIST = lineInfo.Data; break;
                    case "GENRE":
                        metaDatas.TITLE = lineInfo.Data; break;
                    case "DESIGNER":
                        metaDatas.DESIGNER = lineInfo.Data; break;
                    case "DIFFICULTY":
                        metaDatas.DIFFICULTY = new SusDifficulty(lineInfo.Data); break;
                    case "PLAYLEVEL":
                        metaDatas.PLAYLEVEL = new SusPlayLevel(lineInfo.Data); break;
                    case "SONGID":
                        metaDatas.SONGID = lineInfo.Data; break;
                    case "WAVE":
                        metaDatas.WAVE = lineInfo.Data; break;
                    case "WAVEOFFSET":
                        metaDatas.WAVEOFFSET = float.Parse(lineInfo.Data); break;
                    case "JACKET":
                        metaDatas.JACKET = lineInfo.Data; break;
                    case "BACKGROUND":
                        metaDatas.BACKGROUND = lineInfo.Data; break;
                    case "MOVIE":
                        metaDatas.MOVIE = lineInfo.Data; break;
                    case "MOVIEOFFSET":
                        metaDatas.MOVIEOFFSET = float.Parse(lineInfo.Data); break;
                    case "BASEBPM":
                        metaDatas.BASEBPM = lineInfo.Data; break;
                    case "REQUEST":
                        string[] dataArr = lineInfo.Data.Split(' ');
                        metaDatas.REQUESTs.Add(new SusMetaRequest(dataArr[0], dataArr[1])); break;
                    default:
                        SusDebugger.LogWarning($"Bad or unsupported Metadata. (Metadata: {lineInfo.Header})");
                        break;
                }
            }
            return metaDatas;
        }

        private List<SusLineInfo> ToLineInfos(SusAsset susAsset)
        {
            List<SusLineInfo> lineInfos = new List<SusLineInfo>();

            StringReader reader = new StringReader(susAsset.RawText);
            while (reader.Peek() != -1)
            {
                string line = reader.ReadLine();

                if (string.IsNullOrEmpty(line) || line.Substring(0, 1) != "#") continue;
                line = line.Remove(0, 1);

                if (line.Contains(":"))
                {
                    line = line.Replace(" ", "").Replace("\"", "");
                    int index = line.IndexOf(":");
                    lineInfos.Add(new SusLineInfo(line.Substring(0, index), line.Substring(index + 1, line.Length - index - 1), SusLineType.Chart));
                }
                else if (line.Contains("ATTRIBUTE") || line.Contains("NOATTRIBUTE") || line.Contains("HISPEED") || line.Contains("NOSPEED"))
                {
                    line = line.Replace("\"", "");
                    int index = line.IndexOf(" ");
                    if (index == -1) lineInfos.Add(new SusLineInfo(line, null, SusLineType.Meta));
                    else lineInfos.Add(new SusLineInfo(line.Substring(0, index), line.Substring(index + 1, line.Length - index - 1), SusLineType.Chart));
                }
                else
                {
                    line = line.Replace("\"", "");
                    int index = line.IndexOf(" ");
                    if(index == -1) lineInfos.Add(new SusLineInfo(line, null, SusLineType.Meta));
                    else lineInfos.Add(new SusLineInfo(line.Substring(0, index), line.Substring(index + 1, line.Length - index - 1), SusLineType.Meta));
                }
            }
            return lineInfos;
        }

        /// <summary>
        /// 譜面のデータパートを配列に変換する。
        /// </summary>
        /// <param name="data">データパート</param>
        /// <returns></returns>
        private List<string[]> ChartDataPartToArray(string data)
        {
            List<string[]> arr = new List<string[]>();
            for (int i = 0; i < data.Length; i += 2)
            {
                string[] addedData = new string[]
                {
                    data[i].ToString(),
                    data[i + 1].ToString()
                };
                arr.Add(addedData);
            }
            return arr;
        }

        /// <summary>
        /// ヘッダの先頭3文字(mmm)を取得する。
        /// </summary>
        /// <param name="header">ヘッダ</param>
        /// <returns></returns>
        private string GetMMM(string header)
        {
            return header.Substring(0, 3);
        }

        /// <summary>
        /// ヘッダの5文字目(X)を取得する。
        /// </summary>
        /// <param name="header">ヘッダ</param>
        /// <returns></returns>
        private string GetX(string header)
        {
            return header.Substring(4, 1);
        }

        /// <summary>
        /// ヘッダの6文字目(Y)を取得する。
        /// </summary>
        /// <param name="header">ヘッダ</param>
        /// <returns></returns>
        private string GetY(string header)
        {
            return header.Substring(5, 1);
        }
    }
}