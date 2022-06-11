using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WordsOnPlay.Utils;

public class PlayerSense : MonoBehaviour
{
    [SerializeField] private int nRays = 12;
    [SerializeField] private float maxDistance = 10;
    [SerializeField] private LayerMask mapLayer;
    [SerializeField] private OccupancyGrid occGrid;

    private float[] rayDistance;
    private bool[] rayHit;

    void Start()
    {
        rayDistance = new float[nRays];
        rayHit = new bool[nRays];
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < nRays; i++) {
            Vector2 dir = transform.up;
            dir = dir.Rotate(i * 360 / nRays);

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, maxDistance, mapLayer);
            if (hit.collider != null) {
                rayHit[i] = true;
                rayDistance[i] = hit.distance;            
            }
            else {
                rayHit[i] = false;
                rayDistance[i] = maxDistance;            
            }

            occGrid.LineOfSight(transform.position, dir * rayDistance[i]);
        }        
    }

    void OnDrawGizmos() {
        if (!Application.isPlaying) {
            return;
        }

        for (int i = 0; i < nRays; i++) {
            Vector2 v = rayDistance[i] * transform.up;
            v = v.Rotate(i * 360 / nRays);
            Gizmos.color = rayHit[i] ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)v);
        }
    }
}
