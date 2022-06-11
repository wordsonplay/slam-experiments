using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class InitTilemap : MonoBehaviour
{
    private Tilemap tilemap;
    [SerializeField] private Tile tile;
    [SerializeField] private int width;
    [SerializeField] private int height;

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        Vector3Int p = Vector3Int.zero;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // these are switched because of grid swizzling
                // to make the hexagons flat-topped (go figure)
                p.x = y;
                p.y = x;
                tilemap.SetTile(p, tile);
                tilemap.SetTileFlags(p, TileFlags.None);
                
                float v = Random.value;
                Color c = new Color(1,v,v,1);
                tilemap.SetColor(p, c);
//                tilemap.RefreshTile(p);
            }
        }           
    }

}
