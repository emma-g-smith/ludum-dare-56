using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CarvingCreator : MonoBehaviour
{
    [SerializeField] private Tilemap previewMap, carvingMap;

    [SerializeField] private CarvingObject carving;

    [SerializeField] private Camera _camera;

    private Vector2 mousePos;
    private Vector3Int currentGridPosition;
    private Vector3Int lastGridPosition;

    private void Update()
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
        carvingMap.SetTile(currentGridPosition, carving.GetTileBase);
    }

}
