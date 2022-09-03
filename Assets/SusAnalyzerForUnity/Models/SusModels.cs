using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tea.Safu.SusDebug;

namespace Tea.Safu.Models
{
    /// <summary>
    /// ������SUS�ɋL�ڂ���Ă����񂪂��ׂĊi�[����܂��B
    /// </summary>
    public class SusObject
    {
        public SusMetadatas MetaDatas { get; set; }
        public SusChartDatas ChartDatas { get; set; }
    }



    /* ���ʊ֌W */

    public class SusChartDatas
    {
        public int TicksPerBeat { get; set; }
        public List<BpmDefinition> BpmDefinitions { get; set; }
        public List<AttributeDefinition> AttributeDefinitions { get; set; }
        public List<HispeedDefinition> HispeedDefinitions { get; set; }
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
    }

    public class HispeedInfo
    {
        public int Meas { get; set; }
        public int Tick { get; set; }
        public float Speed { get; set; }
    }

    public class MeasureLengthDefinition
    {
        public int MeasureNumber { get; set; }
        public float MeasureLength { get; set; }
    }



    /* ���^�f�[�^�֌W */

    /// <summary>
    /// SUS�̃��^�f�[�^�Ɋւ����񂪊i�[����܂��B
    /// </summary>
    public class SusMetadatas
    {
        public string TITLE { get; set; }
        public string SUBTITLE { get; set; }
        public string ARTIST { get; set; }
        public string GENRE { get; set; }
        public string DESIGER { get; set; }
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
    /// ���^�f�[�^ DIFFICULTY �̏��ł��B
    /// Int�AString�ɑΉ����Ă��܂��B
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
    /// ���^�f�[�^ PLAYLEVEL �̏��ł��B
    /// Int�AString�ɑΉ����Ă��܂��B
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
    /// SUS �̊e�s�̏����w�b�_�ƃf�[�^�ɕ����Ċi�[���܂��B
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
        /// ���^�f�[�^�Ɋւ�����ł��B
        /// </summary>
        Meta,
        /// <summary>
        /// ���ʏ��ł��B
        /// </summary>
        Chart
    }
}
