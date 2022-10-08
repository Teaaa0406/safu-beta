using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickNoteController : NoteMoverBase, INoteMover
{
    [SerializeField] private SpriteRenderer flickRenderer;
    [SerializeField] private GameObject flickObj;

    public void ConfigureFlickNote(float size, float noteHeight, int sortingOrder)
    {
        flickObj.transform.localPosition = new Vector3(size / 2, 0, 0);
        if (size == 1) flickRenderer.size = new Vector2(0.5f, noteHeight);
        else if (size == 2) flickRenderer.size = new Vector2(1f, noteHeight);
        else flickRenderer.size = new Vector2(size - 2f, noteHeight);
        flickRenderer.sortingOrder = sortingOrder + 1;
    }
}
