using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    public Animator anim;

    [System.Serializable]
    public class UserSettings
    {
        public Transform rightHand;
        public Transform pistolUnequipSpot;
        public Transform rifleUnequipSpot;
    }
    [SerializeField] public UserSettings userSettings;

    [System.Serializable]
    public class AnimationParameters
    {
        public string isReloadingBool = "isReloading";
        public string isAnimingBool = "isAiming";
        public string isShootingBool = "isShooting";
        public string weaponTypeInt = "weaponType";
    }
    [SerializeField] public AnimationParameters animParams;

    public WeaponScript curr_weapon;
    public List<WeaponScript> weaponList = new List<WeaponScript>();
    public int maxWeapon = 2;
    public bool AutoReload;
    bool reloading; //For anim state
    bool shooting;  //For anim state
    bool aiming;    //For anim state
    int weaponType; //For anim state
    bool switchingWeapon;// THIS WILL PREVENT CONTINOUSLY CHANGING WEAPON, AND GLITCHING OUR IK by SWITCHING OFF IK


    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if(curr_weapon)
        {
            curr_weapon.SeEquipState(true);
            curr_weapon.SetWeaponOwner(this);
            AddWeaponToList(curr_weapon);

            if (curr_weapon.ammoSystem.currClipAmmo <= 0 && AutoReload) Reload(); //AUTO RELOAD

            if(weaponList.Count > 0)
            {
                foreach(WeaponScript theWeapon in weaponList)
                {
                    if(theWeapon != curr_weapon)
                    {// IF the weapon is not curr equiped weapon, then set its owner to me & set it as not equiped
                        theWeapon.SeEquipState(false);
                        theWeapon.SetWeaponOwner(this);
                    }
                }
            }
        }

        UpdateAnimation();
        SettingWeaponTypeNum();
    }

    void UpdateAnimation()
    {
        if (!anim) return;

        if(!aiming && weaponType == 1 && !reloading) 
            anim.SetLayerWeight(1, 0);// WHEN I'M  USING PISTOL & NOT AIMING, I DONT NEED TO USE WEIRD UPPERBODY MOVEMENT
        else
            anim.SetLayerWeight(1, 1);

        anim.SetBool(animParams.isReloadingBool, reloading);
        anim.SetBool(animParams.isAnimingBool, aiming);
        anim.SetInteger(animParams.weaponTypeInt, weaponType);

        if(curr_weapon.ammoSystem.currClipAmmo > 0)
            anim.SetBool(animParams.isShootingBool, shooting);
        else
            anim.SetBool(animParams.isShootingBool, false);
    }
    void SettingWeaponTypeNum()
    {
        if(!curr_weapon) // IF NO WEAPON
        {
            weaponType = 0;
            return;
        }

        switch(curr_weapon.weaponType)// AN ENUM
        {
            case WeaponScript.weaponTypeEnum.PISTOL:
                weaponType = 1;
                break;
            case WeaponScript.weaponTypeEnum.RIFLE:
                weaponType = 2;
                break;
        }
    }

    void AddWeaponToList(WeaponScript passed_weapon)
    {
        if (weaponList.Contains(passed_weapon)) return; // IF WEAPON ALREADY EXIST, RETURN

        weaponList.Add(passed_weapon); //ELSE ADD NEW WEAPON TO LIST
    }

    public void FingerOnTrigger(bool isPulling)
    {
        if (!curr_weapon) return;

        shooting = isPulling;

        if(!aiming || reloading)
            curr_weapon.PullTrigger(false);
        else
            curr_weapon.PullTrigger(isPulling);
    }

    public void Reload()
    {
        if (reloading || !curr_weapon) return;

        if (curr_weapon.ammoSystem.totalCarryingAmmo <= 0 || curr_weapon.ammoSystem.currClipAmmo == curr_weapon.ammoSystem.maxClipAmmo)
            return;// IF I HAVE NO AMMO LEFT TO RELOAD || MY CLIP IS FULL WITH MAX CLIP CAPACITY, then return

        reloading = true;
        curr_weapon.PlayReloadSound();
        StartCoroutine(StopReload());
    }
    IEnumerator StopReload()// AFTER RELOAD DURATION, STOPS RELOADing
    {
        yield return new WaitForSeconds(curr_weapon.weaponSettings.reloadDuration);

        curr_weapon.LoadClip();// AFTER RELOAD DURATION LOAD CLIP.
        reloading = false;
    }


    public void Aim(bool aim)// CALLS FROM USER-INPUT IF AIM BUTTON IS CLICKED
    {
        aiming = aim;
    }

    public void DropWeapon()
    {
        if (!curr_weapon) return;

        curr_weapon.SeEquipState(false);
        curr_weapon.SetWeaponOwner(null);
        weaponList.Remove(curr_weapon);
        curr_weapon = null;
    }

    public void SwitchWeapon()
    {
        if (switchingWeapon || reloading) return;

        if(curr_weapon)
        {
            int currWeaponIndex = weaponList.IndexOf(curr_weapon);// FINDS THE CURRENT WEAPON INDEX INT THE LIST
            int nextWeaponIndex = (currWeaponIndex + 1) % weaponList.Count;

            curr_weapon = weaponList[nextWeaponIndex];
        }
        else
        {
            curr_weapon = weaponList[0];
        }

        switchingWeapon = true;
        StartCoroutine(StopSwitchingWeapon());
    }
    IEnumerator StopSwitchingWeapon()
    {
        yield return new WaitForSeconds(0.7f);
        switchingWeapon = false;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!anim && !aiming) return;

        if(curr_weapon && curr_weapon.userSettings.leftHandIKTarget /*&& weaponType == 2*/ && !reloading && !switchingWeapon)
        {                                                   // IF WE ARE USING RIFLE, THEN USE LEFT HAND IK
            if (weaponType == 1 && !aiming) return; //WE DONT WANT TO USE LEFT_IK FOR IDLE IF ITS THE PISTOL.

            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);

            Transform IK_Target = curr_weapon.userSettings.leftHandIKTarget;

            anim.SetIKPosition(AvatarIKGoal.LeftHand, IK_Target.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, IK_Target.rotation);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
    }

}
