using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.Analyze;
using Tea.Safu.SusDebug;

namespace Tea.Safu.Models
{
    /// <summary>
    /// ここにSUSに記載されている情報がすべて格納されます。
    /// </summary>
    public class SusObject
    {
        public SusMetadatas MetaDatas { get; set; }
        public SusChartDatas ChartDatas { get; set; }
    }



    /* 譜面関係 */

    public class SusChartDatas
    {
        public int TicksPerBeat { get; set; }
        public List<BpmDefinition> bpmDefinitions { get; set; }
        public List<AttributeDefinition> AttributeDefinitions { get; set; }
        public List<MeasureLengthDefinition> MeasureDefinitions { get; set; }
        public List<SusNoteDataBase> NoteDatas { get; set; }
    }

    public class BpmDefinition
    {
        public string ZZ { get; set; }
        public float Bpm { get; set; }
    }

    public class AttributeDefinition
    {
        public string ZZ { get; set; }
        public List<float> Rh { get; set; }
        public List<float> H { get; set; }
        public List<int> Pr { get; set; }
    }

    public class HispeedDefinition
    {
        public string ZZ { get; set; }
        public List<HispeedInfo> hispeedInfos { get; set; }

        public struct HighSpeedApplyingInfo
        {
            public float Hispeed { get; set; }
            /// <summary>
            /// 終了タイミングが存在しない場合(=曲の最後まで適用)は -1
            /// </summary>
            public long EndTiming { get; set; }
        }

        public void SetUp(SusCalculationUtils utils)
        {
            foreach (HispeedInfo info in hispeedInfos) info.enabledTiming = utils.CalEnabledTiming(info.Meas, info.Tick);
        }

        public HighSpeedApplyingInfo GetHighSpeedApplyingInfoByTiming(long timing)
        {
            HighSpeedApplyingInfo applyingInfo = new HighSpeedApplyingInfo();

            // ハイスピード変更適用タイミングが指定タイミングに一番近いものを探索
            int nearest = -1;
            for (int i = 0; i < hispeedInfos.Count; i++)
            {
                long enabledTiming = hispeedInfos[i].enabledTiming;

                if (enabledTiming > timing) break;
                else if (enabledTiming <= timing) nearest = i;
            }

            // 探索されたハイスピードからデータを生成
            if(nearest == -1) applyingInfo.Hispeed = 1;
            else applyingInfo.Hispeed = hispeedInfos[nearest].Speed;

            if (nearest + 1 < hispeedInfos.Count) applyingInfo.EndTiming = hispeedInfos[nearest + 1].enabledTiming;
            else applyingInfo.EndTiming = -1;

            return applyingInfo;
        }
    }

    public class HispeedInfo
    {
        public int Meas { get; set; }
        public int Tick { get; set; }
        public float Speed { get; set; }
        public long enabledTiming { get; set; }
    }

    public class MeasureLengthDefinition
    {
        public int MeasureNumber { get; set; }
        public float MeasureLength { get; set; }
    }



    /* メタデータ関係 */

    /// <summary>
    /// SUSのメタデータに関する情報が格納されます。
    /// </summary>
    public class SusMetadatas
    {
        public string TITLE { get; set; }
        public string SUBTITLE { get; set; }
        public string ARTIST { get; set; }
        public string GENRE { get; set; }
        public string DESIGNER { get; set; }
        public SusDifficulty DIFFICULTY { get; set; }
        public SusPlayLevel PLAYLEVEL { get; set; }
        public string SONGID { get; set; }
        public string WAVE { get; set; }
        public float WAVEOFFSET { get; set; }
        public string JACKET { get; set; }
        public string BACKGROUND { get; set; }
        public string MOVIE { get; set; }
        public float MOVIEOFFSET { get; set; }
        public string BASEBPM { get; set; }
        public List<SusMetaRequest> REQUESTs { get; set; }
    }

    public class SusMetaRequest
    {
        public SusMetaRequest(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public object Value { get; set; }
    }

    /// <summary>
    /// メタデータ DIFFICULTY の情報です。
    /// Int、Stringに対応しています。
    /// </summary>
    public class SusDifficulty
    {
        private string difficulty;

        public SusDifficulty(string difficulty)
        {
            this.difficulty = difficulty;
        }

        public string GetDifficultyString()
        {
            return difficulty;
        }

        public int GetDifficultyInt()
        {
            int difficultyInt;
            if (!int.TryParse(difficulty, out difficultyInt)) SusDebugger.LogError($"Failed to convert DifficultyStr to Int. (DifficultyStr: \"{difficulty}\")");
            return difficultyInt;
        }
    }

    /// <summary>
    /// メタデータ PLAYLEVEL の情報です。
    /// Int、Stringに対応しています。
    /// </summary>
    public class SusPlayLevel
    {
        private string playLevel;

        public SusPlayLevel(string playLevel)
        {
            this.playLevel = playLevel;
        }

        public string GetPlayLevelString()
        {
            return playLevel;
        }

        public int GetPlayLevelInt()
        {
            int playLevelInt;
            if (!int.TryParse(playLevel, out playLevelInt)) SusDebugger.LogError($"Failed to convert PlayLevelStr to Int. (PlayLevelStr: \"{playLevel}\")");
            return playLevelInt;
        }
    }



    /// <summary>
    /// SUS の各行の情報をヘッダとデータに分けて格納します。
    /// </summary>
    public class SusLineInfo
    {
        public string Header { get; set; }
        public string Data { get; set; }
        public SusLineType LineType { get; set; }

        public SusLineInfo(string header, string data, SusLineType lineType)
        {
            Header = header;
            Data = data;
            LineType = lineType;
        }
    }

    public enum SusLineType
    {
        /// <summary>
        /// メタデータに関する情報です。
        /// </summary>
        Meta,
        /// <summary>
        /// 譜面情報です。
        /// </summary>
        Chart
    }
}
