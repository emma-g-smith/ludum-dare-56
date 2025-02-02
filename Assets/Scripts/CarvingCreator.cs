using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CarvingCreator : MonoBehaviour
{
    [SerializeField] private Tilemap previewMap, carvingMap;

    [SerializeField] private CarvingObject carving;

    [SerializeField] private Camera _camera;

    [SerializeField] private Button button;

    [SerializeField] private int carvingMinimum = 20;

    private int carvingsMade;
    private bool pumpkinCarvable;

    private Vector2 mousePos;
    private Vector3Int currentGridPosition;
    private Vector3Int lastGridPosition;

    public delegate void OnCarvingStart();
    public static OnCarvingStart onCarvingStart;

    public delegate void OnCarvingEnd();
    public static event OnCarvingEnd onCarvingEnd;

    private void Start()
    {
        carvingsMade = 0;
        pumpkinCarvable = false;
        button.gameObject.SetActive(false);

        onCarvingStart += CarvingStart;
        onCarvingEnd += CarvingEnd;
    }

    private void CarvingStart()
    {
        pumpkinCarvable = true;
    }

    public void CarvingEndInvoke()
    {
        onCarvingEnd?.Invoke();
    }

    private void CarvingEnd()
    {
        pumpkinCarvable = false;
        button.gameObject.SetActive(false);
    }

    private void Update()
    {        
        if(pumpkinCarvable)
        {
            mousePos = Input.mousePosition;

            Vector3 pos = _camera.ScreenToWorldPoint(mousePos);
            Vector3Int gridPos = previewMap.WorldToCell(pos);

            if (gridPos != currentGridPosition)
            {
                lastGridPosition = currentGridPosition;
                currentGridPosition = gridPos;

                UpdatePreview();
            }

            if (Input.GetMouseButton(0))
            {
                HandleDrawing();
            }

            if (carvingsMade > carvingMinimum)
            {
                button.gameObject.SetActive(true);
            }
        }
    }

    private void UpdatePreview()
    {
        previewMap.SetTile(lastGridPosition, null);
        previewMap.SetTile(currentGridPosition, carving.GetTileBase);
    }

    private void HandleDrawing()
    {
        DrawItem();
    }

    private void DrawItem()
    {
        TileBase current_tile = carvingMap.GetTile(currentGridPosition);
        if(current_tile != carving.GetTileBase)
        {
            carvingsMade += 1;
        }

        carvingMap.SetTile(currentGridPosition, carving.GetTileBase);
    }

}
