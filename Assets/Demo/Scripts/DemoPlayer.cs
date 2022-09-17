using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tea.Safu;
using Tea.Safu.Analyze;
using Tea.Safu.Models;
using Cysharp.Threading.Tasks;

public class DemoPlayer : MonoBehaviour
{
    [SerializeField] private SusAnalyzer.SusAnalyzeSetting analyzeSetting;
    [SerializeField] private SusAsset susAsset;

    [SerializeField] private Text metaDataText;
    [SerializeField] private Text timingText;
    [SerializeField] private Text fpsText;

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

    private long timing;
    private float time;
    private bool playedGuideSe = false;

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
    public void Analyze()
    {
        SusAnalyzer susAnalyzer = new SusAnalyzer(analyzeSetting);
        analyzeResult = susAnalyzer.Analyze(susAsset);

        SetMetadataText(analyzeResult.MetaDatas);
    }

    public void Playback()
    {
        PlaybackChart().Forget();
    }

    /// <summary>
    /// 解析されたデータの再生を行います。
    /// </summary>
    /// <returns></returns>
    private async UniTask PlaybackChart()
    {
        INoteMover settingMover(INoteMover mover, bool playGuideSe, SusNotePlaybackDataBase note)
        {
            mover.Initialize(playGuideSe, () => PlayGuideSe());
            mover.NotePlaybackData = note;
            return mover;
        }

        SusNotePlaybackDatas playbackDatas = analyzeResult.NotePlaybackDatas;
        List<SusNotePlaybackDataBase> notes = playbackDatas.Notes;

        /*
         * 譜面の生成＆移動を行います。
         */
        bool gotNextNotes = false;
        bool instantiated = false;
        int readIndex = 0;
        long nextInstantiateTiming = analyzeSetting.StartTiming;
        List<SusNotePlaybackDataBase> nextNotes = new List<SusNotePlaybackDataBase>();
        List<INoteMover> noteMovers = new List<INoteMover>();

        MoveTiming(analyzeSetting.StartTiming);

        while (true)
        {

            playedGuideSe = false;

            // 次に生成されるノートをすべて取得
            if (readIndex < notes.Count && !gotNextNotes)
            {
                nextInstantiateTiming = notes[readIndex].InstantiateTiming;
                while (readIndex < notes.Count && notes[readIndex].InstantiateTiming == nextInstantiateTiming)
                {
                    nextNotes.Add(notes[readIndex]);
                    readIndex += 1;
                }
                gotNextNotes = true;
                instantiated = false;
            }

            // 次のノートの生成タイミングになったら生成する
            if(timing >= nextInstantiateTiming && !instantiated)
            {
                foreach(SusNotePlaybackDataBase note in nextNotes)
                {
                    INoteMover mover = null;
                    bool playGuideSe = true;

                    // mmm1x
                    if (note.NoteDataType == NoteDataType.mmm1x)
                    {
                        SusNotePlaybackDataMMM1X mmm1x = note.NoteData as SusNotePlaybackDataMMM1X;

                        // タップノートの種類で分岐
                        switch (mmm1x.Type)
                        {
                            // Tap
                            case 1:
                                {
                                    GameObject instantiatedNote = Instantiate(tapNoteObject, NotesParent, true);
                                    instantiatedNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);
                                    SpriteRenderer spriteRenderer = instantiatedNote.GetComponent<SpriteRenderer>();
                                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                                    TapNoteController controller = instantiatedNote.GetComponent<TapNoteController>();
                                    mover = controller;

                                    spriteRenderer.sortingOrder = tapNoteSortingOrder;
                                }
                                break;

                            // ExTap
                            case 2:
                                {
                                    GameObject instantiatedNote = Instantiate(exTapNoteObject, NotesParent, true);
                                    instantiatedNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);
                                    SpriteRenderer spriteRenderer = instantiatedNote.GetComponent<SpriteRenderer>();
                                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                                    ExTapNoteController controller = instantiatedNote.GetComponent<ExTapNoteController>();
                                    mover = controller;

                                    spriteRenderer.sortingOrder = exTapNoteSortingOrder;
                                }
                                break;

                            // Flick
                            case 3:
                                {
                                    GameObject instantiatedNote = Instantiate(flickNoteObject, NotesParent, true);
                                    instantiatedNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);
                                    SpriteRenderer spriteRenderer = instantiatedNote.GetComponent<SpriteRenderer>();
                                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                                    FlickNoteController controller = instantiatedNote.GetComponent<FlickNoteController>();
                                    controller.SetFlickNote(mmm1x.Size, noteHeight, flickNoteSortingOrder);
                                    mover = controller;

                                    spriteRenderer.sortingOrder = flickNoteSortingOrder;
                                }
                                break;

                            // 仕様書には記載されていませんでしたが、4 5 6 はおそらくエフェクトの異なるEXタップノートです。
                            // ここでは通常のEXタップノートとして生成します。
                            case 4:
                            case 5:
                            case 6:
                                {
                                    GameObject instantiatedNote = Instantiate(exTapNoteObject, NotesParent, true);
                                    instantiatedNote.transform.localPosition = new Vector3(mmm1x.X, 0, analyzeSetting.InstantiatePosition);
                                    SpriteRenderer spriteRenderer = instantiatedNote.GetComponent<SpriteRenderer>();
                                    spriteRenderer.size = new Vector2(mmm1x.Size, noteHeight);
                                    ExTapNoteController controller = instantiatedNote.GetComponent<ExTapNoteController>();
                                    mover = controller;

                                    spriteRenderer.sortingOrder = exTapNoteSortingOrder;
                                }
                                break;
                        }
                    }

                    // mmm2xy
                    else if (note.NoteDataType == NoteDataType.mmm2xy)
                    {
                        SusNotePlaybackDataMMM2XY mmm2xy = note.NoteData as SusNotePlaybackDataMMM2XY;

                        GameObject instantiatedHoldNote = Instantiate(holdNoteObject, NotesParent, true);
                        instantiatedHoldNote.transform.localPosition = new Vector3(mmm2xy.X, 0, analyzeSetting.InstantiatePosition);
                        SpriteRenderer startSpriteRenderer = instantiatedHoldNote.GetComponent<SpriteRenderer>();
                        startSpriteRenderer.size = new Vector2(mmm2xy.Size, noteHeight);
                        HoldNoteController controller = instantiatedHoldNote.GetComponent<HoldNoteController>();

                        startSpriteRenderer.sortingOrder = holdNoteSortingOrder;

                        // 終了点をインスタンス化
                        GameObject instantiatedHoldEndNote = Instantiate(holdStepNoteObject, NotesParent, true);
                        instantiatedHoldEndNote.transform.localPosition = new Vector3(mmm2xy.X, 0, analyzeSetting.InstantiatePosition);
                        SpriteRenderer endSpriteRenderer = instantiatedHoldEndNote.GetComponent<SpriteRenderer>();
                        endSpriteRenderer.size = new Vector2(mmm2xy.Size, noteHeight);
                        instantiatedHoldEndNote.transform.SetParent(instantiatedHoldNote.transform);

                        endSpriteRenderer.sortingOrder = holdNoteSortingOrder;

                        controller.EndNoteMover = settingMover(instantiatedHoldEndNote.GetComponent<HoldEndNoteController>(), true, mmm2xy.EndNote.NoteData as SusNotePlaybackDataBase);
                        mover = controller;
                    }

                    // mmm3xy
                    else if (note.NoteDataType == NoteDataType.mmm3xy)
                    {
                        SusNotePlaybackDataMMM3XY mmm3xy = note.NoteData as SusNotePlaybackDataMMM3XY;

                        GameObject instantiatedNote = Instantiate(slideNoteObject, NotesParent, true);
                        instantiatedNote.transform.localPosition = new Vector3(mmm3xy.X, 0, analyzeSetting.InstantiatePosition);
                        SpriteRenderer startSpriteRenderer = instantiatedNote.GetComponent<SpriteRenderer>();
                        startSpriteRenderer.size = new Vector2(mmm3xy.Size, noteHeight);
                        SlideNoteController controller = instantiatedNote.GetComponent<SlideNoteController>();

                        startSpriteRenderer.sortingOrder = slideNoteSortingOrder;

                        controller.StepNoteMovers = new List<INoteMover>();
                        controller.CurveControlMovers = new List<SusNotePlaybackDataMMM3XYCurveControl>();

                        // すべての中継点をインスタンス化
                        foreach (SusNotePlaybackDataMMM3XYStep step in mmm3xy.Steps)
                        {
                            GameObject instantiatedStepNote;
                            if (!step.Invisible)
                            {
                                instantiatedStepNote = Instantiate(slideStepNoteObject, NotesParent, true);
                                SpriteRenderer stepSpriteRenderer = instantiatedStepNote.GetComponent<SpriteRenderer>();
                                stepSpriteRenderer.size = new Vector2(step.Size, noteHeight);
                                instantiatedStepNote.transform.localPosition = new Vector3(step.X, 0, analyzeSetting.InstantiatePosition);
                                instantiatedStepNote.transform.SetParent(instantiatedNote.transform);
                                controller.StepNoteMovers.Add(settingMover(instantiatedStepNote.GetComponent<SlideStepNoteController>(), true, step.NoteData as SusNotePlaybackDataBase));

                                stepSpriteRenderer.sortingOrder = slideNoteSortingOrder;
                            }
                            else
                            {
                                instantiatedStepNote = Instantiate(slideInvisibleStepNoteObject, NotesParent, true);
                                instantiatedStepNote.transform.SetParent(instantiatedNote.transform);
                                controller.StepNoteMovers.Add(settingMover(instantiatedStepNote.GetComponent<SlideInvisibleStepNoteController>(), false, step.NoteData as SusNotePlaybackDataBase));
                            }
                            mover = controller;
                        }

                        controller.CurveControlMovers = mmm3xy.CurveControls;
                        mover = controller;
                    }

                    // mmm4xy (AirHold)
                    else if (note.NoteDataType == NoteDataType.mmm4xy)
                    {
                        SusNotePlaybackDataMMM4XY mmm4xy = note.NoteData as SusNotePlaybackDataMMM4XY;

                        GameObject instantiatedNote = Instantiate(AirHoldNoteObject, NotesParent, true);
                        instantiatedNote.transform.localPosition = new Vector3(mmm4xy.X + mmm4xy.Size / 2f, 0, analyzeSetting.InstantiatePosition);
                        AirHoldNoteController controller = instantiatedNote.GetComponent<AirHoldNoteController>();
                        controller.AirHoldNoteSortingOrder = airHoldNoteSortingOrder;

                        instantiatedNote.transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = airHoldNoteSortingOrder;

                        controller.StepNoteMovers = new List<INoteMover>();

                        // すべての中継点をインスタンス化
                        foreach (SusNotePlaybackDataMMM4XYStep step in mmm4xy.Steps)
                        {
                            GameObject instantiatedStepNote = Instantiate(AirActionNoteObject, NotesParent, true);
                            instantiatedStepNote.transform.localScale = new Vector3(step.Size, instantiatedStepNote.transform.localScale.y, instantiatedStepNote.transform.localScale.z);
                            instantiatedStepNote.transform.localPosition = new Vector3(step.X + instantiatedStepNote.transform.localScale.x / 2f, instantiatedStepNote.transform.localPosition.y, analyzeSetting.InstantiatePosition);
                            instantiatedStepNote.transform.SetParent(instantiatedNote.transform);
                            controller.StepNoteMovers.Add(settingMover(instantiatedStepNote.GetComponent<AirActionNoteController>(), true, step.NoteData as SusNotePlaybackDataBase));

                            instantiatedStepNote.GetComponent<MeshRenderer>().sortingOrder = airHoldNoteSortingOrder;

                            // 影を生成
                            GameObject instantiatedShadow = Instantiate(AirActionShadowObject, NotesParent, true);
                            SpriteRenderer shadowSpriteRenderer = instantiatedShadow.GetComponent<SpriteRenderer>();
                            shadowSpriteRenderer.size = new Vector2(step.Size, noteHeight);
                            instantiatedShadow.transform.localPosition = new Vector3(step.X, 0, analyzeSetting.InstantiatePosition);
                            AirActionShadowController shadowController = instantiatedShadow.GetComponent<AirActionShadowController>();
                            controller.StepNoteMovers.Add(settingMover(shadowController, false, step.NoteData as SusNotePlaybackDataBase));

                            instantiatedShadow.transform.SetParent(instantiatedStepNote.transform);
                            shadowSpriteRenderer.sortingOrder = airActionShadowSortingOrder;
                        }

                        mover = controller;
                    }

                    // Air
                    else if (note.NoteDataType == NoteDataType.mmm5x)
                    {
                        SusNotePlaybackDataMMM5X mmm5x = note.NoteData as SusNotePlaybackDataMMM5X;

                        GameObject instantiatedNote = Instantiate(AirNoteObject, NotesParent, true);
                        instantiatedNote.transform.localPosition = new Vector3(mmm5x.X, 0, analyzeSetting.InstantiatePosition);
                        AirNoteController controller = instantiatedNote.GetComponent<AirNoteController>();
                        controller.AirType = mmm5x.Type;
                        mover = controller;

                        instantiatedNote.GetComponent<MeshRenderer>().sortingOrder = airNoteSortingOrder;
                    }

                    // MeasureLine
                    else if(note.NoteDataType == NoteDataType.MeasureLine)
                    {
                        GameObject measureLine = Instantiate(measureLineObject, NotesParent, true);
                        measureLine.transform.localPosition = new Vector3(8, 0, analyzeSetting.InstantiatePosition);
                        MeasureLineController controller = measureLine.GetComponent<MeasureLineController>();
                        mover = controller;
                        playGuideSe = false;

                        measureLine.GetComponent<SpriteRenderer>().sortingOrder = measureLineSortingOrder;
                    }

                    if(mover != null)
                    {
                        noteMovers.Add(settingMover(mover, playGuideSe, note));
                    }
                }
                nextNotes.Clear();
                instantiated = true;
                gotNextNotes = false;
            }

            // ノートの移動
            foreach(NoteMoverBase mover in noteMovers)
            {
                if (!mover.Destroyed)
                {
                    mover.MoveClock(timing);
                    fpsText.text = $"FPS: {((float)1f / (float)Time.deltaTime).ToString("F1")}";
                }
            }

            // 仮BGM
            if(timing > 0 + bgmOffset && !bgmPlayed && playBgm)
            {
                audioSourceBgm.PlayOneShot(bgm);
                bgmPlayed = true;
            }

            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        }
    }

    private void MoveTiming(long startTiming)
    {
        timing = startTiming;
        time = startTiming / 1000f;
        movingTiming = true;
    }

    private void StopTiming()
    {
        movingTiming = false;
    }

    private void PlayGuideSe()
    {
        if (playedGuideSe) return;
        playedGuideSe = true;
        audioSource.PlayOneShot(guideSe);
    }



    private void SetMetadataText(SusMetadatas metadatas)
    {
        metaDataText.text =
            $"[Metadatas]\n" +
            $"TITLE: {metadatas.TITLE}\n" +
            $"SUBTITLE: {metadatas.SUBTITLE}\n" +
            $"ARTIST: {metadatas.ARTIST}\n" +
            $"GENRE: {metadatas.GENRE}\n" +
            $"DESIGNER: {metadatas.DESIGNER}\n" +
            $"DIFFICULTY: {metadatas.DIFFICULTY.GetDifficultyString()}\n" +
            $"PLAYLEVEL: {metadatas.PLAYLEVEL.GetPlayLevelString()}\n" +
            $"SONGID: {metadatas.SONGID}\n" +
            $"WAVE: {metadatas.WAVE}\n" +
            $"WAVEOFFSET: {metadatas.WAVEOFFSET}\n" +
            $"JACKET: {metadatas.JACKET}\n" +
            $"BACKGROUND: {metadatas.BACKGROUND}\n" +
            $"MOVIE: {metadatas.MOVIE}\n" +
            $"MOVIEOFFSET: {metadatas.MOVIEOFFSET}\n" +
            $"BASEBPM: {metadatas.BASEBPM}";

        foreach(SusMetaRequest request in metadatas.REQUESTs)
            metaDataText.text += $"\nREQUEST {request.Key}: {request.Value}";
    }
}
