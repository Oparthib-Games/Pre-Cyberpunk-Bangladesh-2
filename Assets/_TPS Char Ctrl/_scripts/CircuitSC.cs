using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitSC : MonoBehaviour
{
    public GameObject[] waypoints;
    public float sphereRadius = 0.5f;
    private void OnDrawGizmos()
    {
        if (waypoints.Length > 1)
            for (int i = 0; i < waypoints.Length; i++)
            {
                Gizmos.DrawLine(waypoints[i].transform.position, i != waypoints.Length - 1 ? waypoints[i + 1].transform.position : waypoints[0].transform.position);
                Gizmos.DrawWireSphere(waypoints[i].transform.position, sphereRadius);
            }
    }
}
