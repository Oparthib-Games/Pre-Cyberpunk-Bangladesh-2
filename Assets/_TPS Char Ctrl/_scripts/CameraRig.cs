using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;


[ExecuteInEditMode]
public class CameraRig : MonoBehaviour
{
    public Transform target;

    public bool autoTargetPlayer;
    public LayerMask WallLayer;

    public enum shoulder_enum { Left, Right }
    public shoulder_enum shoulder;

    [System.Serializable]
    public class CameraSettings
    {
        [Header("[-Positioning-]")]
        public Vector3 camPosOffsetLeft = new Vector3(-0.35f, 0.0f, -2f);
        public Vector3 camPosOffsetRight = new Vector3(0.35f, 0.0f, -2f);

        [Header("[-Camera Option-]")]
        public float mouseXsensitivity = 5.0f;
        public float mouseYsensitivity = 5.0f;
        public float minAngle = -30.0f;
        public float maxAngle = 70.0f;
        public float rotLerpSpeed = 5.0f;

        [Header("[-Zoom-]")]
        public float FOV = 70.0f;
        public float zoomFOV = 30.0f;
        public float zoomSpeed = 7.0f;

        [Header("[-Visual Option-]")]
        public float hideMeshDist = 0.5f; // WHEN CAMERA IS TOO CLOSE
    }
    [SerializeField] public CameraSettings camSettings;

    [System.Serializable]
    public class InputSettings
    {
        public string MouseY = "Mouse Y";
        public string MouseX = "Mouse X";
        public string AimBtn = "Fire2";
        public string switchShoulderBtn = "Shoulder";
    }
    [SerializeField] public InputSettings INPUT;

    [System.Serializable]
    public class MovementSettings
    {
        public float movementLerpSpeed = 5.0f;
    }
    [SerializeField] public MovementSettings movementSettings;

    Transform pivot;
    Camera mainCam;
    float newX = 0.0f;
    float newY = 0.0f;

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        mainCam = Camera.main;
        pivot = transform.GetChild(0);

        if (!target) target = GameObject.FindGameObjectWithTag("Player").transform;
        shoulder = shoulder_enum.Right;
    }

    void Update()
    {
        if(target)
        {
            if (Application.isPlaying)
            {
                transform.position = target.position;

                RotateCamera();
                ProtectCameraFromWall();
                CheckMeshRend();
                Zoom(CrossPlatformInputManager.GetButton(INPUT.AimBtn));

                if (CrossPlatformInputManager.GetButtonDown(INPUT.switchShoulderBtn))
                    SwitchShoulder();
            }
        }
    }
    void LateUpdate()
    {
        if (Application.isPlaying)
        {
            transform.position = target.position;
        }
    }


    void RotateCamera()
    {
        if (!pivot) return;

        newX += CrossPlatformInputManager.GetAxis(INPUT.MouseX) * camSettings.mouseXsensitivity;
        newY -= CrossPlatformInputManager.GetAxis(INPUT.MouseY) * camSettings.mouseYsensitivity;

        Vector3 euler_angle_axis = new Vector3();
        euler_angle_axis.x = newY; // [-CROSS CONNECTION-] \/
        euler_angle_axis.y = newX; // [-CROSS CONNECTION-] /\

        newX = Mathf.Repeat(newX, 360);
        newY = Mathf.Clamp(newY, camSettings.minAngle, camSettings.maxAngle);

        Quaternion newRot = Quaternion.Slerp(pivot.localRotation, Quaternion.Euler(euler_angle_axis), camSettings.rotLerpSpeed);
        pivot.localRotation = newRot;
    }

    void ProtectCameraFromWall() // CHECKS THE WALL & MOVES THE CAMERA UP IF WE HIT
    {
        if (!pivot || !mainCam) return;

        

        Transform camTransform = mainCam.transform;
        Vector3 camPos = mainCam.transform.position;
        Vector3 pivotPos = pivot.position;

        Vector3 dir = camPos - pivotPos;

        float dist = Mathf.Abs( shoulder == shoulder_enum.Left? camSettings.camPosOffsetLeft.z: camSettings.camPosOffsetRight.z);
        
        RaycastHit hitInfo;
        Ray ray = new Ray(pivotPos, dir);

        if(Physics.SphereCast(ray, 0.1f, out hitInfo, dist, WallLayer))
        {
            float hitDist = hitInfo.distance;
            Vector3 sphereCastCenter = pivotPos + (dir.normalized * hitDist);
            mainCam.transform.position = sphereCastCenter;
        }
        else  // IF NO SPHERE CAST THEN, POSTION THE CAMERA TO LEFT OR RIGHT
        {
            switch(shoulder)
            {
                case shoulder_enum.Left:
                    ShoulderPositionCamera(camSettings.camPosOffsetLeft);
                    break;
                case shoulder_enum.Right:
                    ShoulderPositionCamera(camSettings.camPosOffsetRight);
                    break;
            }
        }
    }

    void ShoulderPositionCamera(Vector3 cam_LEFT_RIGHT_pos)// POSITIONS CAMERAs LOCAL POS LEFT OR RIGHT
    {
        if (!mainCam) return;

        Vector3 camLocalPos = mainCam.transform.localPosition;
        Vector3 newPos = Vector3.Lerp(camLocalPos, cam_LEFT_RIGHT_pos, Time.deltaTime * movementSettings.movementLerpSpeed);
        mainCam.transform.localPosition = newPos;
    }

    void CheckMeshRend() // HIDES MESH RENDER WHEN TOO CLOSE TO WALL
    {
        if (!mainCam || !target) return;

        SkinnedMeshRenderer[] skinned_meshes = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        MeshRenderer[] meshes = target.GetComponentsInChildren<MeshRenderer>();

        Vector3 camPos = mainCam.transform.position;

        float cam_nd_target_dist = Vector3.Distance(camPos, pivot.position);

        foreach(SkinnedMeshRenderer i in skinned_meshes)
        {
            if (cam_nd_target_dist <= camSettings.hideMeshDist)
                i.enabled = false;
            else
                i.enabled = true;
        }
        foreach(MeshRenderer i in meshes)
        {
            if (cam_nd_target_dist <= camSettings.hideMeshDist)
                i.enabled = false;
            else
                i.enabled = true;
        }
    }

    void Zoom(bool isZooming)
    {
        if (!mainCam) return;

        if(isZooming)
        {
            float newFOV = Mathf.Lerp(mainCam.fieldOfView, camSettings.zoomFOV, camSettings.zoomSpeed * Time.deltaTime);
            mainCam.fieldOfView = newFOV;
        }
        else
        {
            float originalFOV = Mathf.Lerp(mainCam.fieldOfView, camSettings.FOV, camSettings.zoomSpeed * Time.deltaTime);
            mainCam.fieldOfView = originalFOV;
        }
    }

    public void SwitchShoulder()
    {
        if (shoulder == shoulder_enum.Right)
            shoulder = shoulder_enum.Left;
        else
            shoulder = shoulder_enum.Right;
    }
}
