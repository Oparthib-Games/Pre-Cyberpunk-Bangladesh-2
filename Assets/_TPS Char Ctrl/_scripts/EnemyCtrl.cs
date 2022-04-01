using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sinn.TPS_CTRL;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
[RequireComponent(typeof(TPS_Ctrl))]
public class EnemyCtrl : MonoBehaviour
{
    public enum ActionEnum { PATROL, ALERT, FIND_PLAYER, ATTACK}
    public ActionEnum ActionType;



    [Header("Player Ditection")]
    public GameObject Player;
    public LayerMask IgnoreLayer;
    public float rayCastHight = 1.5f;
    public float PlayerCheckDist = 50f;


    #region Navigation AI
    public UnityEngine.AI.NavMeshAgent agent { get; private set; }             // the navmesh agent required for the path finding
    public TPS_Ctrl tps_ctrl { get; private set; } // the character we are controlling
    public Transform target;                                 // target to aim for

    #endregion

    #region Patrol
    public Transform myWaypoints;
    CircuitSC circuitSC;
    int curr_waypoint_index;
    public float way_point_tolerance = 1f;
    #endregion

    void Start()
    {
        SetUpNavmesh();

        circuitSC = myWaypoints.GetComponent<CircuitSC>();
    }

    void Update()
    {
        Patrol();
        PlayerDetection();
    }

    void SetUpNavmesh()
    {
        agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
        tps_ctrl = GetComponent<TPS_Ctrl>();

        agent.updateRotation = false;
        agent.updatePosition = true;
    }

    void NavigateToTarget(float speed)
    {
        if (target != null)
            agent.SetDestination(target.position);

        agent.speed = speed;

        if (agent.remainingDistance > agent.stoppingDistance)
            tps_ctrl.Move(agent.desiredVelocity, false, false, false);
        else
            tps_ctrl.Move(Vector3.zero, false, false, false);
    }

    void PlayerDetection()
    {
        Vector3 RayOrigin = (transform.up * rayCastHight) + transform.position;
        Vector3 RayDir = Player.transform.position - RayOrigin;
        Ray ray = new Ray(RayOrigin, RayDir);
        RaycastHit hit;

        

        if (Physics.Raycast(ray, out hit, PlayerCheckDist))
        {
            if(hit.transform.tag == "Player")
            {
                Debug.DrawRay(RayOrigin, RayDir, Color.red);

                #region Counting Player Angle
                Vector3 player_XZ_T = new Vector3(Player.transform.position.x, 0, Player.transform.position.z);
                Vector3 playerDir_XZ = player_XZ_T - transform.position;
                float angle = Vector3.Angle(playerDir_XZ, transform.forward);

                if(angle <= 60)
                {
                    print("Attack!!!!!!!!!!!!!!!");
                    //TODO ATTACK
                }
                #endregion
            }
            else
            {
                Debug.DrawRay(RayOrigin, RayDir, Color.green);
            }
        }

    }


    #region Patroling
    void Patrol()
    {
        if (isReachedWaypoint())
            curr_waypoint_index = Next_Waypoint_Pos(curr_waypoint_index);

        target = getWaypointPos(curr_waypoint_index);
        NavigateToTarget(0.5f);
    }
    bool isReachedWaypoint()
    {
        float distance_to_waypoint = Vector3.Distance(transform.position, getWaypointPos(curr_waypoint_index).position);
        return distance_to_waypoint <= way_point_tolerance;
    }
    Transform getWaypointPos(int i)
    {
        return myWaypoints.GetChild(i).transform;
    }
    int Next_Waypoint_Pos(int i)
    {
        if (i + 1 == myWaypoints.childCount) return 0;
        return i + 1;
    }
    #endregion
}
