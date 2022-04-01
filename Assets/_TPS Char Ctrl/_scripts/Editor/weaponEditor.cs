using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WeaponScript))]
public class weaponEditor : Editor
{
    WeaponScript weaponScript;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        weaponScript = (WeaponScript)target;

        EditorGUILayout.LabelField("[-WEAPON-HELPER-]");
        
        if(GUILayout.Button("Save Gun Equip Location"))
        {
            Transform weaponT = weaponScript.transform;
            Vector3 weaponPos = weaponT.localPosition;
            Vector3 weaponRot = weaponT.localEulerAngles;

            weaponScript.weaponSettings.equipPos = weaponPos;
            weaponScript.weaponSettings.equipRot = weaponRot;
        }

        if(GUILayout.Button("Save Gun UnEquip Location"))
        {
            Transform weaponT = weaponScript.transform;
            Vector3 weaponPos = weaponT.localPosition;
            Vector3 weaponRot = weaponT.localEulerAngles;

            weaponScript.weaponSettings.unequipPos = weaponPos;
            weaponScript.weaponSettings.unequipRot = weaponRot;
        }

        EditorGUILayout.LabelField("[-DEBUG-POSITIONING-]");

        if(GUILayout.Button("Move Gun to Equip Location"))
        {
            Transform weaponT = weaponScript.transform;
            weaponT.localPosition = weaponScript.weaponSettings.equipPos;
            weaponT.localRotation = Quaternion.Euler(weaponScript.weaponSettings.equipRot);
        }
        if(GUILayout.Button("Move Gun to UnEquip Location"))
        {
            Transform weaponT = weaponScript.transform;
            weaponT.localPosition = weaponScript.weaponSettings.unequipPos;
            weaponT.localRotation = Quaternion.Euler(weaponScript.weaponSettings.unequipRot);
        }
    }
}
