using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sinn.TPS_CTRL;


[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
[RequireComponent(typeof(TPS_Ctrl))]
public class AI_Ctrl : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent agent;
    private TPS_Ctrl tps_ctrl { get { return GetComponent<TPS_Ctrl>(); } set { tps_ctrl = value; } }

    public enum AI_State_Enum { Patrol, Attack, FindCover};
    public AI_State_Enum AI_State;

    [System.Serializable]
    public class PatrolSystem
    {
        public WaypointBase[] waypoints;
    }
    public PatrolSystem patrolSystem;

    private float currentWaitTime;
    private int waypointIndex;
    private Transform currLookTransform;

    private float forward;

    void Start()
    {
        agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();//NAVMESH AGENT MUST BE A CHILE OF CHARACTER

        agent.speed = 0;
        agent.acceleration = 0;
    }

    void Update()
    {
        Vector3 moveDir = forward * transform.forward; ///TODOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
        //tps_ctrl.Move(moveDir, false, false, false); //TODOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO

        agent.transform.position = transform.position;

        switch(AI_State)
        {
            case AI_State_Enum.Patrol:
                Patrol();
                break;
        }
    }

    void Patrol()
    {
        if (!agent.isOnNavMesh) return;


    }
}

[System.Serializable]
public class WaypointBase
{
    public Transform destination;
    public float waitTime;
    public Transform LookAtTarget;
}