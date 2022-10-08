using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tea.Safu;
using Tea.Safu.Analyze;
using Tea.Safu.Models;
using Tea.Safu.Util;
using Cysharp.Threading.Tasks;

public class DemoPlayer : MonoBehaviour
{
    [SerializeField] private SusAnalyzer.SusAnalyzeSetting analyzeSetting;
    [SerializeField] private SusAsset susAsset;

    [SerializeField] private Text metaDataText;
    [SerializeField] private Text timingText;

    [SerializeField] private AudioClip guideSe;
    [SerializeField] private AudioClip bgm;
    [SerializeField] private float bgmOffset;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource audioSourceBgm;

    [SerializeField] private float noteHeight;

    private SusAnalyzer.SusAnalyzeResult analyzeResult;
    private bool movingTiming = false;
    private bool bgmPlayed = false;
    public bool playBgm;

    // 譜面の再生用
    [SerializeField] private GameObject tapNoteObject;
    [SerializeField] private GameObject exTapNoteObject;
    [SerializeField] private GameObject flickNoteObject;
    [SerializeField] private GameObject holdNoteObject;
    [SerializeField] private GameObject holdStepNoteObject;
    [SerializeField] private GameObject slideNoteObject;
    [SerializeField] private GameObject slideStepNoteObject;
    [SerializeField] private GameObject slideInvisibleStepNoteObject;
    [SerializeField] private GameObject AirNoteObject;
    [SerializeField] private GameObject AirHoldNoteObject;
    [SerializeField] private GameObject AirActionNoteObject;
    [SerializeField] private GameObject AirActionShadowObject;
    [SerializeField] private GameObject measureLineObject;
    [SerializeField] private Transform NotesParent;

    [SerializeField] private int tapNoteSortingOrder;
    [SerializeField] private int exTapNoteSortingOrder;
    [SerializeField] private int flickNoteSortingOrder;
    [SerializeField] private int holdNoteSortingOrder;
    [SerializeField] private int slideNoteSortingOrder;
    [SerializeField] private int airNoteSortingOrder;
    [SerializeField] private int airHoldNoteSortingOrder;
    [SerializeField] private int airActionShadowSortingOrder;
    [SerializeField] private int measureLineSortingOrder;

    // デモ関係
    [SerializeField] private AnalysisScreen analysisScreen;
    private SynchronizationContext SyncContext;
    private bool playing = false;

    private List<INoteMover> noteMovers = new List<INoteMover>();
    private long timing;
    private float time;
    private bool playedGuideSe = false;

    private void Start()
    {
        SyncContext = SynchronizationContext.Current;
    }

    private void FixedUpdate()
    {
        if (movingTiming)
        {
            time += Time.deltaTime;
            timing = (long)Math.Round(time * 1000f);
            timingText.text = $"Timing: {timing}";
        }
    }

    /// <summary>
    /// Susの解析を行います。
    /// </summary>
    public async void Analyze()
    {
        // SusAnalyzerを作成
        SusAnalyzer susAnalyzer = new SusAnalyzer(analyzeSetting);

        // 解析メッセージ受信時のイベントを登録(デバッグ用)
        susAnalyzer.OnReceivedAnalyzingMessage += OnReceivedAnalyzingMessage;

        // Susの解析を行う
        analysisScreen.gameObject.SetActive(true);

        // WebGL上で再生する場合は同期的に解析するかUniTaskを使用します。
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            analyzeResult = susAnalyzer.Analyze(susAsset);
        else
            analyzeResult = await susAnalyzer.AnalyzeAsync(susAsset);

        analysisScreen.gameObject.SetActive(false);

        // メタデータテキストの設定(デモ)
        SetMetadataText(analyzeResult.MetaDatas);
    }

    /// <summary>
    /// 譜面の再生を開始します
    /// </summary>
    public void Playback()
    {
        if (playing) return;
        PlaybackChart().Forget();
    }

    /// <summary>
    /// 解析されたデータの再生を行います。
    /// </summary>
    private async UniTask PlaybackChart()
    {
        /*
         * 実際に譜面の再生を行います。
         * 
         * 譜面の再生には SusPlaybackUtility を使用します。
         * 
         * SusPlaybackUtility.OnInstantiateNotesReceived にインスタンス化すべき
         * ノーツ情報が送られてくるのでそれらノーツの生成を実装する必要があります。
         * ここでは InstantiateNotes() で上記を実装しています。
         * 
         * SusPlaybackUtility には現在のTimingを送信する必要があります。
         * Timingの移動を実装する必要があります。
         * 
         * 各ノートは生成後に SusNotePlaybackData.CalNotePositionByTiming() を
         * もとに継続的に座標を更新することで、ノートの移動を実装します。
         * ここでは InstantiateNotes() でノーツの生成と同時に、送られてきた
         * SusNotePlaybackDatas を移動対象リストに含めます。
         * このリストに含まれる SusNotePlaybackData すべてを別の場所から参照し、
         * ノートの移動処理を行います。
         */

        playing = true;
        noteMovers = new List<INoteMover>();

        // Sus再生用ユーティリティを作成
        SusPlaybackUtility player = new SusPlaybackUtility(analyzeSetting);

        // インスタンス化するノーツを受け取った時のイベントを登録
        player.OnInstantiateNotesReceived += InstantiateNotes;

        // ノート情報を送信して初期化
        player.Initialize(analyzeResult.NotePlaybackDatas.Notes);

        // 再生開始
        MoveTiming(analyzeSetting.StartTiming);
        bgmPlayed = false;

        // Taimingが譜面の終了一秒後を超えるまでループ
        while (timing < analyzeResult.EndTiming + 1000)
        {
            // タイミングの更新
            player.UpdateTiming(timing);

            // 登録されたノーツの移動
            foreach (NoteMoverBase mover in noteMovers)
            {
                if (!mover.Destroyed)
                {
                    mover.MoveClock(timing);
                }
            }

            // 仮BGM
            if (timing > 0 + bgmOffset && !bgmPlayed && playBgm)
            {
                audioSourceBgm.PlayOneShot(bgm);
                bgmPlayed = true;
            }

            playedGuideSe = false;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        }

        StopTiming();
        bgmPlayed = false;
        playing = false;
    }

    /// <summary>
    /// 受け取ったノート情報をもとに各ノートをインスタンス化し、移動対象ノートリストに追加します。
    /// </summary>
    /// <param name="notes">インスタンス化するノート情報</param>
    private void InstantiateNotes(List<SusNotePlaybackDataBase> notes)
    {
        foreach (SusNotePlaybackDataBase note in notes)
        {
            // ノートタイプごとに生成します
            switch (note.NoteDataType)
            {
                // TapNote, ExTapNote, FlickNote を生成
                case NoteDataType.mmm1x:

                    INoteMover mmm1xMover = InstantiateMmm1x(note.NoteData as SusNotePlaybackDataMMM1X);
                    noteMovers.Add(mmm1xMover);

                    break;

                // HoldNote を生成
                case NoteDataType.mmm2xy:

                    List<INoteMover> mmm2xyMovers = InstantiateMmm2xy(note.NoteData as SusNotePlaybackDataMMM2XY);
                    noteMovers.AddRange(mmm2xyMovers);

                    break;

                // SlideNote を生成
                case NoteDataType.mmm3xy:

                    List<INoteMover> mmm3xyMovers = InstantiateMmm3xy(note.NoteData as SusNotePlaybackDataMMM3XY);
                    noteMovers.AddRange(mmm3xyMovers);

                    break;

                // AirHoldHoldNote を生成
                case NoteDataType.mmm4xy:

                    List<INoteMover> mmm4xyMovers = InstantiateMmm4xy(note.NoteData as SusNotePlaybackDataMMM4XY);
                    noteMovers.AddRange(mmm4xyMovers);

                    break;

                // AirNote を生成
                case NoteDataType.mmm5x:

                    INoteMover mmm5xMover = InstantiateMmm5x(note.NoteData as SusNotePlaybackDataMMM5X);
                    noteMovers.Add(mmm5xMover);

                    break;

                // MeasureLine を生成
                case NoteDataType.MeasureLine:

                    INoteMover measureLineMover = InstantiateMeasureLine(note.NoteData as SusNotePlaybackDataMeasureLine);
                    noteMovers.Add(measureLineMover);

                    break;
            }
        }
    }



    /* 各ノートの生成 */

    /// <summary>
    /// mmm1x をインスタンス化します
    /// </summary>
    /// <param name="mmm1x"> mmm1x再生情報</param>
    /// <returns>生成されたノート</returns>
    private INoteMover InstantiateMmm1x(SusNotePlaybackDataMMM1X mmm1x)
    {
        INoteMover noteMover = null;

        // mmm1xのタイプで分岐
        switch (mmm1x.Type)
        {
            // Tap
            case 1:
                {
                    GameObject tapNote = Instantiate(tapNoteObject, NotesParent, true);
                    tapNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);

                    SpriteRenderer spriteRenderer = tapNote.GetComponent<SpriteRenderer>();
                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                    spriteRenderer.sortingOrder = tapNoteSortingOrder;

                    TapNoteController tapController = tapNote.GetComponent<TapNoteController>();
                    tapController.NotePlaybackData = mmm1x;
                    tapController.Initialize(() => PlayGuideSe());

                    noteMover = tapController;
                }
                break;

            // ExTap
            // 仕様書には記載されていませんでしたが、4 5 6 はおそらくエフェクトの異なるEXタップノートです。
            // ここでは通常のEXタップノートとして生成します。
            case 2:
            case 4:
            case 5:
            case 6:
                {
                    GameObject exTapNote = Instantiate(exTapNoteObject, NotesParent, true);
                    exTapNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);

                    SpriteRenderer spriteRenderer = exTapNote.GetComponent<SpriteRenderer>();
                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                    spriteRenderer.sortingOrder = exTapNoteSortingOrder;

                    ExTapNoteController exTapController = exTapNote.GetComponent<ExTapNoteController>();
                    exTapController.NotePlaybackData = mmm1x;
                    exTapController.Initialize(() => PlayGuideSe());

                    noteMover = exTapController;
                }
                break;

            // Flick
            case 3:
                {
                    GameObject flickNote = Instantiate(flickNoteObject, NotesParent, true);
                    flickNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);

                    SpriteRenderer spriteRenderer = flickNote.GetComponent<SpriteRenderer>();
                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                    spriteRenderer.sortingOrder = flickNoteSortingOrder;

                    FlickNoteController flickController = flickNote.GetComponent<FlickNoteController>();
                    flickController.NotePlaybackData = mmm1x;
                    flickController.Initialize(() => PlayGuideSe());
                    flickController.ConfigureFlickNote(mmm1x.Size, noteHeight, flickNoteSortingOrder);

                    noteMover = flickController;
                }
                break;
        }

        return noteMover;
    }

    /// <summary>
    /// mmm2xy をインスタンス化します
    /// </summary>
    /// <param name="mmm2xy"> mmm2xy再生情報</param>
    /// <returns>生成されたノート</returns>
    private List<INoteMover> InstantiateMmm2xy(SusNotePlaybackDataMMM2XY mmm2xy)
    {
        List<INoteMover> noteMovers = new List<INoteMover>();

        // 始点ノートを生成
        GameObject holdStartNote = Instantiate(holdNoteObject, NotesParent, true);
        holdStartNote.transform.localPosition = new Vector3(mmm2xy.X, 0, analyzeSetting.InstantiatePosition);

        SpriteRenderer startSpriteRenderer = holdStartNote.GetComponent<SpriteRenderer>();
        startSpriteRenderer.size = new Vector2(mmm2xy.Size, noteHeight);
        startSpriteRenderer.sortingOrder = holdNoteSortingOrder;

        HoldNoteController startController = holdStartNote.GetComponent<HoldNoteController>();
        startController.NotePlaybackData = mmm2xy;
        startController.Initialize(() => PlayGuideSe());
        noteMovers.Add(startController);

        // 終了点ノートを生成
        GameObject holdStepNote = Instantiate(holdStepNoteObject, NotesParent, true);
        holdStepNote.transform.localPosition = new Vector3(mmm2xy.X, 0, analyzeSetting.InstantiatePosition);
        holdStepNote.transform.SetParent(holdStartNote.transform);

        SpriteRenderer endSpriteRenderer = holdStepNote.GetComponent<SpriteRenderer>();
        endSpriteRenderer.size = new Vector2(mmm2xy.Size, noteHeight);
        endSpriteRenderer.sortingOrder = holdNoteSortingOrder;

        HoldEndNoteController endController = holdStepNote.GetComponent<HoldEndNoteController>();
        endController.NotePlaybackData = mmm2xy.EndNote.NoteData as SusNotePlaybackDataBase;
        endController.Initialize(() => PlayGuideSe());
        startController.EndNoteMover = endController;
        noteMovers.Add(endController);

        return noteMovers;
    }

    /// <summary>
    /// mmm3xy をインスタンス化します
    /// </summary>
    /// <param name="mmm3xy"> mmm3xy再生情報</param>
    /// <returns>生成されたノート</returns>
    private List<INoteMover> InstantiateMmm3xy(SusNotePlaybackDataMMM3XY mmm3xy)
    {
        List<INoteMover> noteMovers = new List<INoteMover>();

        // 始点ノートを生成
        GameObject slideStartNote = Instantiate(slideNoteObject, NotesParent, true);
        slideStartNote.transform.localPosition = new Vector3(mmm3xy.X, 0, analyzeSetting.InstantiatePosition);

        SpriteRenderer startSpriteRenderer = slideStartNote.GetComponent<SpriteRenderer>();
        startSpriteRenderer.size = new Vector2(mmm3xy.Size, noteHeight);
        startSpriteRenderer.sortingOrder = holdNoteSortingOrder;

        SlideNoteController startController = slideStartNote.GetComponent<SlideNoteController>();
        startController.NotePlaybackData = mmm3xy;
        startController.Initialize(() => PlayGuideSe());
        noteMovers.Add(startController);

        // すべての中継点をインスタンス化
        startController.StepNoteMovers = new List<INoteMover>();
        startController.CurveControlMovers = mmm3xy.CurveControls;

        foreach (SusNotePlaybackDataMMM3XYStep step in mmm3xy.Steps)
        {
            if (step.Invisible)
            {
                GameObject invisibleStepNote = Instantiate(slideInvisibleStepNoteObject, NotesParent, true);
                invisibleStepNote.transform.localPosition = new Vector3(step.X, 0, analyzeSetting.InstantiatePosition);
                invisibleStepNote.transform.SetParent(slideStartNote.transform);

                SlideInvisibleStepNoteController invisibleStepController = invisibleStepNote.GetComponent<SlideInvisibleStepNoteController>();
                invisibleStepController.NotePlaybackData = step;
                invisibleStepController.Initialize(null);
                startController.StepNoteMovers.Add(invisibleStepController);
                noteMovers.Add(invisibleStepController);
            }
            else
            {
                GameObject slideStepNote = Instantiate(slideStepNoteObject, NotesParent, true);
                slideStepNote.transform.localPosition = new Vector3(step.X, 0, analyzeSetting.InstantiatePosition);
                slideStepNote.transform.SetParent(slideStartNote.transform);

                SpriteRenderer stepSpriteRenderer = slideStepNote.GetComponent<SpriteRenderer>();
                stepSpriteRenderer.size = new Vector2(step.Size, noteHeight);

                SlideStepNoteController stepController = slideStepNote.GetComponent<SlideStepNoteController>();
                stepController.NotePlaybackData = step;
                stepController.Initialize(() => PlayGuideSe());

                startController.StepNoteMovers.Add(stepController);
                noteMovers.Add(stepController);
            }
        }

        return noteMovers;
    }

    /// <summary>
    /// mmm4xy をインスタンス化します
    /// </summary>
    /// <param name="mmm4xy"> mmm4xy再生情報</param>
    /// <returns>生成されたノート</returns>
    private List<INoteMover> InstantiateMmm4xy(SusNotePlaybackDataMMM4XY mmm4xy)
    {
        List<INoteMover> noteMovers = new List<INoteMover>();

        // 始点ノートを生成
        GameObject airHoldStartNote = Instantiate(AirHoldNoteObject, NotesParent, true);
        airHoldStartNote.transform.localPosition = new Vector3(mmm4xy.X + mmm4xy.Size / 2f, 0, analyzeSetting.InstantiatePosition);

        SpriteRenderer startSpriteRenderer = airHoldStartNote.transform.GetChild(0).GetComponent<SpriteRenderer>();
        startSpriteRenderer.sortingOrder = airHoldNoteSortingOrder;

        AirHoldNoteController startController = airHoldStartNote.GetComponent<AirHoldNoteController>();
        startController.NotePlaybackData = mmm4xy;
        startController.AirHoldNoteSortingOrder = airHoldNoteSortingOrder;
        startController.Initialize(() => PlayGuideSe());
        noteMovers.Add(startController);

        // すべての中継点(エアーアクション)を生成
        startController.StepNoteMovers = new List<INoteMover>();

        foreach (SusNotePlaybackDataMMM4XYStep step in mmm4xy.Steps)
        {
            GameObject airHoldStepNote = Instantiate(AirActionNoteObject, NotesParent, true);
            airHoldStepNote.transform.localScale = new Vector3(step.Size, airHoldStepNote.transform.localScale.y, airHoldStepNote.transform.localScale.z);
            airHoldStepNote.transform.localPosition = new Vector3(step.X + airHoldStepNote.transform.localScale.x / 2f, airHoldStepNote.transform.localPosition.y, analyzeSetting.InstantiatePosition);
            airHoldStepNote.transform.SetParent(airHoldStartNote.transform);

            MeshRenderer stepMeshRenderer = airHoldStepNote.GetComponent<MeshRenderer>();
            stepMeshRenderer.sortingOrder = airHoldNoteSortingOrder;

            AirActionNoteController stepController = airHoldStepNote.GetComponent<AirActionNoteController>();
            stepController.NotePlaybackData = step;
            stepController.Initialize(() => PlayGuideSe());

            startController.StepNoteMovers.Add(stepController);
            noteMovers.Add(stepController);

            // 影を生成
            GameObject shadow = Instantiate(AirActionShadowObject, NotesParent, true);
            shadow.transform.localPosition = new Vector3(step.X, 0, analyzeSetting.InstantiatePosition);
            shadow.transform.SetParent(airHoldStepNote.transform);

            SpriteRenderer shadowSpriteRenderer = shadow.GetComponent<SpriteRenderer>();
            shadowSpriteRenderer.size = new Vector2(step.Size, noteHeight);
            shadowSpriteRenderer.sortingOrder = airActionShadowSortingOrder;

            AirActionShadowController shadowController = shadow.GetComponent<AirActionShadowController>();
            stepController.NotePlaybackData = step;
            stepController.Initialize(null);
            noteMovers.Add(stepController);
        }

        return noteMovers;
    }

    /// <summary>
    /// mmm5xy をインスタンス化します
    /// </summary>
    /// <param name="mmm5x"> mmm5xy再生情報</param>
    /// <returns>生成されたノート</returns>
    private INoteMover InstantiateMmm5x(SusNotePlaybackDataMMM5X mmm5x)
    {
        GameObject airNote = Instantiate(AirNoteObject, NotesParent, true);
        airNote.transform.localPosition = new Vector3(mmm5x.X, 0, analyzeSetting.InstantiatePosition);

        MeshRenderer airMeshRenderer = airNote.GetComponent<MeshRenderer>();
        airMeshRenderer.sortingOrder = airNoteSortingOrder;

        AirNoteController airController = airNote.GetComponent<AirNoteController>();
        airController.NotePlaybackData = mmm5x;
        airController.AirType = mmm5x.Type;
        airController.Initialize(() => PlayGuideSe());
        noteMovers.Add(airController);

        return airController;
    }

    /// <summary>
    /// MeasureLine をインスタンス化します
    /// </summary>
    /// <param name="measureLine"> mmm5xy再生情報</param>
    /// <returns>生成されたノート</returns>
    private INoteMover InstantiateMeasureLine(SusNotePlaybackDataMeasureLine measureLine)
    {
        GameObject measureLineNote = Instantiate(measureLineObject, NotesParent, true);
        measureLineNote.transform.localPosition = new Vector3(8, 0, analyzeSetting.InstantiatePosition);

        SpriteRenderer measureLineSpriteRenderer = measureLineNote.GetComponent<SpriteRenderer>();
        measureLineSpriteRenderer.sortingOrder = holdNoteSortingOrder;

        MeasureLineController airController = measureLineNote.GetComponent<MeasureLineController>();
        airController.NotePlaybackData = measureLine;
        airController.Initialize(null);
        noteMovers.Add(airController);

        return airController;
    }



    /// <summary>
    ///  タイミングの移動を開始します
    /// </summary>
    /// <param name="startTiming">開始タイミング</param>
    private void MoveTiming(long startTiming)
    {
        timing = startTiming;
        time = startTiming / 1000f;
        movingTiming = true;
    }

    /// <summary>
    ///  タイミングの移動を停止します
    /// </summary>
    private void StopTiming()
    {
        movingTiming = false;
    }

    /// <summary>
    /// ガイド音を再生します
    /// </summary>
    private void PlayGuideSe()
    {
        if (playedGuideSe) return;
        playedGuideSe = true;
        audioSource.PlayOneShot(guideSe);
    }



    /// <summary>
    /// メタデータ情報をテキストとして表示します
    /// </summary>
    /// <param name="metadatas">メタデータ情報</param>
    private void SetMetadataText(SusMetadatas metadatas)
    {
        metaDataText.text = $"[Metadatas]\n";
        if (metadatas.TITLE != null) metaDataText.text += $"TITLE: {metadatas.TITLE}\n";
        if (metadatas.SUBTITLE != null) metaDataText.text += $"SUBTITLE: {metadatas.SUBTITLE}\n";
        if (metadatas.ARTIST != null) metaDataText.text += $"ARTIST: {metadatas.ARTIST}\n";
        if (metadatas.GENRE != null) metaDataText.text += $"GENRE: {metadatas.GENRE}\n";
        if (metadatas.DESIGNER != null) metaDataText.text += $"DESIGNER: {metadatas.DESIGNER}\n";
        if (metadatas.DIFFICULTY != null) metaDataText.text += $"DIFFICULTY: {metadatas.DIFFICULTY.GetDifficultyString()}\n";
        if (metadatas.PLAYLEVEL != null) metaDataText.text += $"PLAYLEVEL: {metadatas.PLAYLEVEL.GetPlayLevelString()}\n";
        if (metadatas.SONGID != null) metaDataText.text += $"SONGID: {metadatas.SONGID}\n";
        if (metadatas.WAVE != null) metaDataText.text += $"WAVE: {metadatas.WAVE}\n";
        if (metadatas.WAVEOFFSET != null) metaDataText.text += $"WAVEOFFSET: {metadatas.WAVEOFFSET}\n";
        if (metadatas.JACKET != null) metaDataText.text += $"JACKET: {metadatas.JACKET}\n";
        if (metadatas.BACKGROUND != null) metaDataText.text += $"BACKGROUND: {metadatas.BACKGROUND}\n";
        if (metadatas.MOVIE != null) metaDataText.text += $"MOVIE: {metadatas.MOVIE}\n";
        if (metadatas.MOVIEOFFSET != null) metaDataText.text += $"MOVIEOFFSET: {metadatas.MOVIEOFFSET}\n";
        if (metadatas.BASEBPM != null) metaDataText.text += $"BASEBPM: {metadatas.BASEBPM}\n";
            

        foreach(SusMetaRequest request in metadatas.REQUESTs)
            metaDataText.text += $"\nREQUEST {request.Key}: {request.Value}";
    }



    /// <summary>
    /// 解析メッセージを受け取った際に、それを表示します
    /// </summary>
    /// <param name="msg">メッセージ</param>
    /// <param name="overrideLine">改行するか</param>
    private void OnReceivedAnalyzingMessage(string msg, bool overrideLine)
    {
        SyncContext.Post(_ =>
        {
            analysisScreen.AddAnalyzingMessage(msg, overrideLine);
        }, null);
    }
}
