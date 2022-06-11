using System;
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
    private float[,] occupancy;

    void Start()
    {
        InitTilemap();
    }

    void Update() {
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

    private Vector3Int LocalToCell(Vector2 p) {
        return Swizzle(tilemap.layoutGrid.LocalToCell(p));
    }

    private Vector2 CellToLocal(Vector3Int p) {
        return tilemap.layoutGrid.GetCellCenterLocal(Swizzle(p));
    }


    private void SetColour(Vector3Int p, Color c) {
        tilemap.SetColor(Swizzle(p), c);
    }

    private void InitTilemap() {
        tilemap = GetComponent<Tilemap>();
        Vector3Int p = Vector3Int.zero;
        occupancy = new float[width,height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                p.x = x;
                p.y = y;
                tilemap.SetTile(Swizzle(p), tile);
                tilemap.SetTileFlags(Swizzle(p), TileFlags.None);
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
        occupancy[pos.x,pos.y] = logOdds;
        float prob = ToProbability(logOdds);

        // 1 = black, 0 = white
        Color c = new Color(1-prob,1-prob,1-prob,1);
        SetColour(pos, c);
    }

    private void AddOccupancy(Vector3Int pos, float increment) {
        occupancy[pos.x,pos.y] += increment;
        float prob = ToProbability(occupancy[pos.x,pos.y]);

        // 1 = black, 0 = white
        Color c = new Color(1-prob,1-prob,1-prob,1);
        SetColour(pos, c);
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
        // calculate each corner from a single reference point to reduce fp errors
        // Only corners 1 and 4 belong to this hex

        i = (i % 6 + 6) % 6;

        Vector2 corner;

        switch (i) {
            case 1: 
                corner = CellToLocal(p) + Vector2.left * tilemap.layoutGrid.cellSize.y / 2;
                return corner;
            case 4: 
                corner = CellToLocal(p) + Vector2.right * tilemap.layoutGrid.cellSize.y / 2;
                return corner;
            case 0:
                return Corner(Neighbour(p, 1), 4);
            case 2:
                return Corner(Neighbour(p, 2), 4);
            case 3:
                return Corner(Neighbour(p, 4), 1);
            case 5:
                return Corner(Neighbour(p, 5), 1);
        }

        throw new InvalidOperationException("Unreachable code");
    }

	public float Side(Vector2 v1, Vector2 v2) {
		return v1.x * v2.y - v1.y * v2.x;
	}

    private int NextLineOfSight(Vector2 origin, Vector2 dir, Vector3Int p, int lastNeighbour) {
        int i = lastNeighbour;
        Vector2 corner = Corner(p, i);
        // Debug.Log(string.Format("corner[{0}] = ({1:G9},{2:G9})", i, corner.x, corner.y));
        Vector2 vOC = corner - origin;
        // Debug.Log(string.Format("vOC = ({0:G9},{1:G9})", vOC.x, vOC.y));

        float side = Side(vOC, dir);
        // Debug.Log(string.Format("side = {0:G9}", side));

        if (side < 0) { // left
            // circle right until you find a corner on the right of the ray
            int maxIt = 6;
            while (side < 0 && maxIt > 0) {
                maxIt--;
                i--;
                corner = Corner(p, i);
                // Debug.Log(string.Format("corner[{0}] = ({1:G9},{2:G9})", i, corner.x, corner.y));
                vOC = corner - origin;
                // Debug.Log(string.Format("vOC = ({0:G9},{1:G9})", vOC.x, vOC.y));
                side = Side(vOC, dir);
                // Debug.Log(string.Format("side = {0:G9}", side));
            } 
            if (side == 0) {
                Debug.Log("side == 0");
            }
            if (maxIt == 0) {
                throw new InvalidOperationException(
                    string.Format("All corners are on the left.\n" +
                    "\tVector2 origin=new Vector2({0:G9}f,{1:G9}f);\n" +
                    "\tVector2 dir=new Vector2({2:G9}f,{3:G9}f);\n" + 
                    "p={4}\nlast={5}",
                        origin.x, origin.y, 
                        dir.x, dir.y,  
                        p, lastNeighbour
                    )
                );
            }

            return (i+1 + 6) % 6;
        }
        else if (side > 0) {
            // circle left until you find a corner on the left of the ray
            int maxIt = 6;
            while (side > 0 && maxIt > 0) {
                maxIt--;
                i++;
                corner = Corner(p, i);
                // Debug.Log(string.Format("corner[{0}] = ({1:G9},{2:G9})", i, corner.x, corner.y));
                vOC = corner - origin;
                // Debug.Log(string.Format("vOC = ({0:G9},{1:G9})", vOC.x, vOC.y));
                side = Side(vOC, dir);
                // Debug.Log(string.Format("side = {0:G9}", side));
            } 
            if (side == 0) {
                Debug.Log("side == 0");
            }

            if (maxIt == 0) {
                throw new InvalidOperationException(
                    string.Format("All corners are on the right.\n" +
                    "\tVector2 origin=new Vector2({0:G9}f,{1:G9}f);\n" +
                    "\tVector2 dir=new Vector2({2:G9}f,{3:G9}f);\n" + 
                    "p={4}\nlast={5}",
                        origin.x, origin.y, 
                        dir.x, dir.y,  
                        p, lastNeighbour
                    )
                );
            }

            return (i + 6) % 6;
        }
        else {
            Debug.Log("side == 0");
            return i;
        }
    }

    public void LineOfSight(Vector2 origin, Vector2 dir, bool hit) {
        origin = transform.InverseTransformPoint(origin);
        dir = transform.InverseTransformVector(dir);
        Vector3Int p = LocalToCell(origin);
        Vector3Int last = LocalToCell(origin + dir);

        int maxIt = 100;

        int neighbour = 0; 
        while (p != last && maxIt > 0) {
            // Debug.Log($"p = {p}; neighbour = {neighbour}");
            maxIt--;
            AddOccupancy(p, -0.01f);
            neighbour = NextLineOfSight(origin, dir, p, neighbour);
            p = Neighbour(p, neighbour);
        }
        AddOccupancy(last, hit ? 0.01f : -0.01f);
    }


}
