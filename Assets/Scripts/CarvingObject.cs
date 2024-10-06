using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu (fileName = "Carving", menuName = "Carving Objects/Create Carvable")]
public class CarvingObject : ScriptableObject
{
    [SerializeField] private TileBase tileBase;

    public TileBase GetTileBase { get { return tileBase; } } 
}
