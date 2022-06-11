using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class OccupancyGrid : MonoBehaviour
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

                SetOccupancy(x,y,0);                
            }
        }           
    }

    private float ToLogOdds(float probability) {
        if (probability == 0) {
            return float.NegativeInfinity;
        } else if (probability == 1) {
            return float.PositiveInfinity;
        }
        return Mathf.Log(probability / (1-probability));
    }

    private float ToProbability(float logOdds) {
        if (logOdds == float.NegativeInfinity) {
            return 0;
        } else if (logOdds == float.PositiveInfinity) {
            return 1;
        }
        float odds = Mathf.Exp(logOdds);
        return odds / (1+odds);
    }

    private void SetOccupancy(int x, int y, float logOdds) {
        Vector3Int pos = new Vector3Int(y,x,0); // swizzled
        float prob = ToProbability(logOdds);

        // 1 = black, 0 = white
        Color c = new Color(1-prob,1-prob,1-prob,1);
        tilemap.SetColor(pos, c);
    }

}
