using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sinn.TPS_CTRL;
using UnityStandardAssets.CrossPlatformInput;

public class CoverSystem : MonoBehaviour
{
    Animator anim;
    CapsuleCollider capCol;
    Rigidbody RB;

    [Header("Basic Cover")]
    float coverMovement;

    public bool takeCover;
    public bool isCovering;
    public float lowCoverAmount;
    public LayerMask coverWallLayer;
    public float coverCheckDist = 5f;
    public float minDistanceFromCover = 0.25f;
    public Transform coverObj;

    float coverBtnPressCooldown; // SO WE CONTINIOUSLY DONT COVER-UNCOVER;


    [Header("Cover Edge")]
    public bool cornerred;
    public int numCheckCoverRays = 8;
    public float cornerCheckRayOriginX = 0.6f;

    [Header("Cover Shoot")]
    public bool isAim;

    [Header("Others")]
    Vector3 shift;
    Vector3 coverPoint;
    RaycastHit closestHit;

    [System.Serializable]
    public class InputSettings
    {
        public string COVER = "Cover";
        public string COVER_MOVE = "Horizontal";
    }
    [SerializeField] public InputSettings INPUT;

    void Start()
    {
        anim = GetComponent<Animator>();
        capCol = GetComponent<CapsuleCollider>();
        RB = GetComponent<Rigidbody>();
    }

    void Update()
    {
        isAim = anim.GetBool("isAiming");
    }

    void FixedUpdate()
    {
        coverBtnPressCooldown--;
        if (CrossPlatformInputManager.GetButtonDown(INPUT.COVER) && coverBtnPressCooldown <= 0)
        {
            coverBtnPressCooldown = 10;

            closestHit = new RaycastHit();
            closestHit.distance = Mathf.Infinity;
            FindClosestCover();

            if(coverObj || isCovering)
                takeCover = !takeCover;
        }


        GetInCover();
        GetOutOfCover();
        CheckForEdge();
        CheckLowCover();
        UpdateAnimation();

        //TPS_UserCtrl.covering = isCovering;
        //TPS_UserCtrl.cornerred = cornerred;

        if((isCovering || takeCover) && !cornerred) // Force player against cover (deal with irregular horizontal movements).
            RB.AddForce(-10 * closestHit.normal, ForceMode.Acceleration);

        coverMovement = CrossPlatformInputManager.GetAxis(INPUT.COVER_MOVE);


    }

    void UpdateAnimation()
    {
        anim.SetBool("isCovering", isCovering);
        anim.SetBool("isCornered", cornerred);
        anim.SetFloat("lowCover", lowCoverAmount);
        anim.SetFloat("coverMovement", coverMovement * 0.6f);
    }

    void GetInCover()
    {
        if (!coverObj || isCovering || !takeCover) return;

        //coverPoint = transform.position;
        //coverPoint.z = coverObj.position.z;
        //transform.position = Vector3.Lerp(transform.position, coverPoint, 0.1f);
        //transform.rotation = Quaternion.Lerp(transform.rotation, coverObj.rotation, 0.5f);

        //float distanceFromCover = Mathf.Abs(transform.position.z - closestHit.point.z);
        float distanceFromCover = Vector3.Distance(transform.position, closestHit.point);
        //print(distanceFromCover);

        if (distanceFromCover <= minDistanceFromCover)
        {
            StartCoroutine(StartTakeCover());

            //Vector3 wallLerpPos = transform.position;
            //wallLerpPos.x = closestHit.point.x;
            //wallLerpPos.z = closestHit.point.z;
            //transform.position = Vector3.Lerp(transform.position, wallLerpPos, 1f);
            //print(wallLerpPos);


            //anim.SetFloat("forward", 0);
        }
        else
        {
            Vector3 moveDir = (coverPoint - transform.position).normalized;
            GetComponent<TPS_Ctrl>().Move(moveDir, false, false, false);
            //Rotating((coverPoint - transform.position).normalized);
            //anim.SetFloat("forward", 0.7f);
        }
    }
    IEnumerator StartTakeCover()
    {
        Quaternion targetRotation = Quaternion.LookRotation(closestHit.normal);
        targetRotation.x = 0;
        targetRotation.z = 0;
        Quaternion newRotation = Quaternion.Slerp(RB.rotation, targetRotation, 0.9f);
        RB.MoveRotation(newRotation);

        yield return new WaitForSeconds(0.02f);
        if(newRotation.y == transform.rotation.y)
           isCovering = true;
    }
    void GetOutOfCover()
    {
        if (!isCovering || takeCover) return;
        takeCover = false;
        coverObj = null;
        isCovering = false;
    }
    void CheckForEdge()
    {
        if (!isCovering) return;

        int moveDirection = 0;
        //moveDirection = (int)Mathf.Sign(anim.GetFloat("coverMovement"));
        if (coverMovement > 0)      moveDirection = 1;
        else if (coverMovement < 0) moveDirection = -1;
        else                        moveDirection = 0;
        //print(moveDirection);

        //shift = Vector3.zero;
        shift.x = -closestHit.normal.z;
        shift.z = closestHit.normal.x;
        shift.y = 0;
        shift *= -moveDirection * cornerCheckRayOriginX;
        shift *= capCol.radius;

        //Vector3 RayOrigin = (transform.up * 1.2f) - (capCol.radius * transform.forward * -1.1f) + transform.position;
        //Vector3 RayOrigin = (transform.up * 0.2f) + transform.position - shift;
        //Vector3 RayDir = transform.forward * -1;
        Vector3 RayOrigin = (0.3f * Vector3.up) - (capCol.radius * 0.9f * transform.forward) + transform.position - shift;
        Vector3 RayDir = -closestHit.normal;

        Ray ray = new Ray(RayOrigin, RayDir);
        RaycastHit hit;

        //cornerFound = !Physics.Raycast(ray, out hit, 1, coverWallLayer);
        if(Physics.Raycast(ray, out hit, 1, coverWallLayer))
        {
            StartCoroutine(MakeCornerFoundFalse());
            IEnumerator MakeCornerFoundFalse()
            {
                yield return new WaitForSeconds(0.2f);
                cornerred = false;
            }
        }
        else
        {
            cornerred = true;
        }
        Debug.DrawRay(RayOrigin, RayDir, cornerred ? Color.red: Color.green);
    }

    void CheckLowCover()
    {
        Vector3 RayOrigin = (transform.up * 1.5f) + transform.position;
        Vector3 RayDir = transform.forward * -1;

        Ray ray = new Ray(RayOrigin, RayDir);
        RaycastHit hit;

        bool getLow = !Physics.Raycast(ray, out hit, 1, coverWallLayer);

        if (!getLow || (!getLow && isAim))
        {   //Get Down
            lowCoverAmount = Mathf.Lerp(lowCoverAmount, 0, 0.1f);
            Debug.DrawRay(RayOrigin, RayDir, Color.red);
        }
        else
        {   // Get Up
            lowCoverAmount = Mathf.Lerp(lowCoverAmount, 1, 0.1f);
            Debug.DrawRay(RayOrigin, RayDir, Color.green);
        }

    }

    void FindClosestCover()
    {
        float angleStep = 360 / numCheckCoverRays;
        for(int i=0; i < numCheckCoverRays; i++)
        {
            Quaternion angle = Quaternion.AngleAxis(i * angleStep, transform.up);

            Vector3 RayOrigin = (transform.up * 0.1f) + transform.position;
            Vector3 RayDir = angle * transform.forward * 5;
            Ray ray = new Ray(RayOrigin, RayDir);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, coverCheckDist, coverWallLayer))
            {
                if (hit.distance < closestHit.distance)
                {
                    closestHit = hit;
                    Debug.DrawRay(RayOrigin, RayDir, Color.red, 3f);
                    Debug.DrawRay(hit.point, closestHit.normal, Color.blue, 3f);
                }
            }
            else
            {
                Debug.DrawRay(RayOrigin, RayDir, Color.cyan, 2f);
            }
        }

        coverObj = closestHit.transform;
        coverPoint = closestHit.point;
    }

}
