using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraRig))]
public class CameraRigEditor : Editor
{
    CameraRig cameraRig;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        cameraRig = (CameraRig)target;

        EditorGUILayout.LabelField("[-Camera Helper-]");

        if(GUILayout.Button("[-Save Camera's Current Position-]"))
        {
            Camera cam = Camera.main;
            Transform pivot = cameraRig.transform.GetChild(0);

            if (!cam) return;

            Vector3 camPos = cam.transform.localPosition;
            camPos.y = pivot.position.y;

            Vector3 camRight = camPos;
            Vector3 camLeft = camPos;

            camLeft.x = -camPos.x; // CAMERA LEFT WILL ALAWYS BE ON THE (-)negetive to camera pos

            cameraRig.camSettings.camPosOffsetLeft = camLeft;
            cameraRig.camSettings.camPosOffsetRight = camRight;
        }
    }
}
