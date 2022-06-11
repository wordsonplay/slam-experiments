using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WordsOnPlay.Utils;

[RequireComponent(typeof(Tilemap))]
public class OccupancyGrid : MonoBehaviour
{
    [SerializeField] private Tile tile;
    [SerializeField] private int width;
    [SerializeField] private int height;

    private Tilemap tilemap;
    private HashSet<Vector3Int> visible;

    void Start()
    {
        visible = new HashSet<Vector3Int>();
        InitTilemap();
    }

    private Vector3Int Swizzle(Vector3Int p) {
        // these are switched because of grid swizzling
        // to make the hexagons flat-topped (go figure)
        p.z = p.x;
        p.x = p.y;
        p.y = p.z;
        p.z = 0;
        return p;
    }

    private void InitTilemap() {
        tilemap = GetComponent<Tilemap>();
        Vector3Int p = Vector3Int.zero;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                tilemap.SetTile(Swizzle(p), tile);
                tilemap.SetTileFlags(Swizzle(p), TileFlags.None);

                p.x = x;
                p.y = y;
                SetOccupancy(p,0);                
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

    private void SetOccupancy(Vector3Int pos, float logOdds) {
        pos = Swizzle(pos);
        float prob = ToProbability(logOdds);

        // 1 = black, 0 = white
        Color c = new Color(1-prob,1-prob,1-prob,1);
        tilemap.SetColor(pos, c);
    }

    //       __
    //    __/0 \__
    //   /1 \__/5 \
    //   \__/p \__/
    //   /2 \__/4 \
    //   \__/3 \__/
    //      \__/
    
    private Vector3Int Neighbour(Vector3Int p, int i) {
        i = (i % 6 + 6) % 6;
        bool isEven = (p.x % 2 + 2) % 2 == 0;

        switch (i) {
            case 0:
                return new Vector3Int(p.x, p.y+1, 0);
            case 1:
                return (isEven ? new Vector3Int(p.x-1, p.y, 0) : new Vector3Int(p.x-1, p.y+1, 0));
            case 2:
                return (isEven ? new Vector3Int(p.x-1, p.y-1, 0) : new Vector3Int(p.x-1, p.y, 0));
            case 3:
                return new Vector3Int(p.x, p.y-1, 0);
            case 4:
                return (isEven ? new Vector3Int(p.x+1, p.y-1, 0) : new Vector3Int(p.x+1, p.y, 0));
            case 5:
                return (isEven ? new Vector3Int(p.x+1, p.y, 0) : new Vector3Int(p.x+1, p.y+1, 0));
             
        }
        // should never happen
        return p;
    }

    //      0__5 
    //     1/  \4
    //      \__/ 
    //      2  3

    private Vector2 Corner(Vector3Int p, int i) {
        i = (i % 6 + 6) % 6;

        // Note: assuming tiles are regular hexagons (not stretched)
        float r = tilemap.layoutGrid.cellSize.y / 2;

        Vector2 corner = r * Vector3.up;
        float angle = 30 + i * 60;
        corner = corner.Rotate(angle);
        corner.x += p.x;
        corner.y += p.y;

        return corner;
    }

    private Vector3Int NextLineOfSight(Vector2 origin, Vector2 dir, Vector3Int p) {
        int i = 0;
        Vector2 corner = Corner(p, i);
        Vector2 vOC = corner - origin;

        if (vOC.IsOnLeft(dir)) {
            // circle right until you find a corner on the right of the ray
            while (vOC.IsOnLeft(dir)) {
                i--;
                corner = Corner(p, i);
                vOC = corner - origin;
            } 

            return Neighbour(p, i+1);
        }
        else {
            // circle left until you find a corner on the left of the ray
            while (!vOC.IsOnLeft(dir)) {
                i++;
                corner = Corner(p, i);
                vOC = corner - origin;
            } 

            return Neighbour(p, i);
        }

    }

    public void ClearVisible() {
        foreach (Vector3Int p in visible) {
            tilemap.SetColor(Swizzle(p), Color.grey);
        }
        visible.Clear();
    }

    public void LineOfSight(Vector2 origin, Vector2 dir) {
        Vector3Int p = Swizzle(tilemap.layoutGrid.LocalToCell(origin));
        Vector3Int last = Swizzle(tilemap.layoutGrid.LocalToCell(origin + dir));

        while (p != last) {
            visible.Add(p);
            p = NextLineOfSight(origin, dir, p);
        }
        visible.Add(last);
    }

    public void DrawVisible() {
        foreach (Vector3Int p in visible) {
            tilemap.SetColor(Swizzle(p), Color.white);
        }
    }

}
