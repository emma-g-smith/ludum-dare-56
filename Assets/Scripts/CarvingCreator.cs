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

    [SerializeField] private StateSaveObject state;

    private int carvingsMade;

    private Vector2 mousePos;
    private Vector3Int currentGridPosition;
    private Vector3Int lastGridPosition;

    private void Start()
    {
        carvingsMade = 0;
        button.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(state.PumpkinCarved)
        {
            button.gameObject.SetActive(false);
        }
        
        if(state.CanCarve)
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
                state.PumpkinCarved = true;
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
