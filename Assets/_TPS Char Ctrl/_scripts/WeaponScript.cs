using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WeaponScript : MonoBehaviour
{
    Collider col;
    Rigidbody RB;
    Animator anim;

    public enum weaponTypeEnum { PISTOL, RIFLE}
    public weaponTypeEnum weaponType;

    [System.Serializable]
    public class UserSettings
    {
        public Transform leftHandIKTarget;
        public Vector3 spineRotOffset; // Spine Rotation Offset According to Specific Weapon Type
    }
    [SerializeField] public UserSettings userSettings;

    [System.Serializable]
    public class WeaponSettings
    {
        [Header("[-Bullet Options-]")]
        public Transform bulletSpawnPos;
        public float damage = 5.0f;
        public float bulletSpread = 0.1f;
        public float fireRate = 0.2f;
        public LayerMask bulletLayer;
        public float rayRange = 200.0f;

        [Header("[-Effects-]")]
        public GameObject muzzleFlash;
        public GameObject decal;
        public GameObject shell;
        public GameObject clip;

        [Header("[-Others-]")]
        public float reloadDuration = 2.0f;
        public Transform shellEjectSpot;
        public float shellEjectSpeed = 7.5f;
        public Transform clipEjectPos;
        public GameObject clipPrefab;

        [Header("[-Positioning-]")]
        public Vector3 equipPos;
        public Vector3 equipRot;
        public Vector3 unequipPos;
        public Vector3 unequipRot;

        [Header("[-Animation-]")]
        public bool useAnimation;
        public int fireAnimLayer; // THE ANIMATION LAYER THAT HAS THE FIRE ANIMATIONS
        public string fireAnimName = "Pistol-Firing";
    }
    [SerializeField] public WeaponSettings weaponSettings;

    [System.Serializable]
    public class AmmoSystem
    {
        public int totalCarryingAmmo; // Total Ammo I Got
        public int currClipAmmo;      // Current One Clip Ammo
        public int maxClipAmmo;       // Max Number of Ammo One Clip Can Hold
    }
    [SerializeField] public AmmoSystem ammoSystem;
    
    [System.Serializable]
    public class SoundSystem
    {
        public AudioClip[] FiringSound;
        public AudioClip BlankFireSound;
        public AudioClip ReloadSound;
        [Range(0, 3)] public float PitchMin = 1f;
        [Range(0, 3)] public float PitchMax = 1.4f;
        [Range(0, 1)] public float Volume = 0.5f;
    }
    [SerializeField] public SoundSystem soundSystem;


    WeaponHandler weaponOwner;
    bool equipped;
    bool pullingTrigger;
    bool resetingCartridge;
    Camera mainCam;

    void Start()
    {
        col = GetComponent<Collider>();
        RB = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        mainCam = Camera.main;
    }

    void Update()
    {
        if(weaponOwner)
        {
            DisableEnableComponent(false);// IF I HAVE OWNER, DISABLE PHYSICS SYSTEM

            if(equipped)
            {
                if (weaponOwner.userSettings.rightHand)
                {
                    Equip();
                    if (pullingTrigger) Fire();
                }
            }
            else
            {
                UnEquip(weaponType);
            }
        }
        else
        {   // NO OWNER ENABLE PHYSICS, AND SET PARENT NULL
            DisableEnableComponent(true);
            transform.SetParent(null);
        }
    }

    void Fire()
    {
        if (resetingCartridge || !weaponSettings.bulletSpawnPos) return;

        #region Firing-Sound
        if (ammoSystem.currClipAmmo <= 0)
        {
            PlayFiringSound(soundSystem.BlankFireSound);
            resetingCartridge = true;// WHEN PLAYING BLANK SOUND, THIS IS REQUIRED TO CONTINIOUSLY PLAYING BLANK SOUND
            StartCoroutine(LoadNextBullet()); 
            return;
        }
        else
        {
            AudioClip RandomClip = soundSystem.FiringSound[Random.Range(0, soundSystem.FiringSound.Length - 1)];
            PlayFiringSound(RandomClip);
        }
        #endregion

        Ray camera_ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        RaycastHit cam_hitInfo;
        Vector3 camHitPoint = new Vector3();
        if(Physics.Raycast(camera_ray, out cam_hitInfo, 1000)) camHitPoint = cam_hitInfo.point;

        Vector3 spawnPoint = weaponSettings.bulletSpawnPos.position;
        Vector3 dir = camHitPoint - spawnPoint;
        //.......THIS CREATES A RAY DIR WHICHS END-POINT SAME AS CAMERA RAY, BUT STARTS FROM GUN-POINT

        dir += Random.insideUnitSphere * weaponSettings.bulletSpread;

        RaycastHit gun_hitInfo;
        Ray gun_ray = new Ray(spawnPoint, dir);

        Debug.DrawRay(spawnPoint, dir, Color.red);

        if (Physics.Raycast(gun_ray, out gun_hitInfo, weaponSettings.rayRange, weaponSettings.bulletLayer, QueryTriggerInteraction.Ignore))
        {
            #region decal-region
            if (gun_hitInfo.collider.gameObject.isStatic && weaponSettings.decal)// IF THE HIT-OBJ IS STATIC
            {
                Quaternion lookRot = Quaternion.LookRotation(gun_hitInfo.normal);// gives 90" of the hit obj
                GameObject decalGO = Instantiate(weaponSettings.decal, gun_hitInfo.point, lookRot) as GameObject;

                decalGO.transform.SetParent(gun_hitInfo.transform);
                Destroy(decalGO, Random.Range(30f, 40f));
            }
            #endregion
        }
        #region muzzle-flash-region
        if(weaponSettings.muzzleFlash)
        {
            GameObject muzzleFlashGO = Instantiate(weaponSettings.muzzleFlash, weaponSettings.bulletSpawnPos.position, Quaternion.identity) as GameObject;
            muzzleFlashGO.transform.SetParent(weaponSettings.bulletSpawnPos);
            Destroy(muzzleFlashGO, 1.0f);
        }
        #endregion

        #region Shell-Region
        if(weaponSettings.shell)
        {
            if(weaponSettings.shellEjectSpot)
            {
                GameObject shellGO = Instantiate(weaponSettings.shell, weaponSettings.shellEjectSpot.position, weaponSettings.shellEjectSpot.rotation);
                if(shellGO.GetComponent<Rigidbody>())
                {
                    Rigidbody shell_RB = shellGO.GetComponent<Rigidbody>();
                    shell_RB.AddForce(weaponSettings.shellEjectSpot.forward * Random.RandomRange(0.5f, 1f) * weaponSettings.shellEjectSpeed, ForceMode.Impulse);
                }
                Destroy(shellGO, Random.Range(30f, 40f));
            }
        }
        #endregion

        if (weaponSettings.useAnimation) // WEAPON ANIMATION
            anim.Play(weaponSettings.fireAnimName, weaponSettings.fireAnimLayer);

        ammoSystem.currClipAmmo--;
        resetingCartridge = true; // AFTER EVERY FIRE FIRING NEEDS TO STOP FOR NEXT BULLET SETTINGs(which is called resetingCartridge)

        StartCoroutine(LoadNextBullet());
    }
    IEnumerator LoadNextBullet()
    {
        yield return new WaitForSeconds(weaponSettings.fireRate);
        resetingCartridge = false; // IN, AFTER FIRE-RATE resetingCartridge FINISHES;
    }

    void DisableEnableComponent(bool enable)
    {
        if(!enable)
        {
            RB.isKinematic = true;
            col.enabled = false;
        }
        else
        {
            RB.isKinematic = false;
            col.enabled = true;
        }
    }

    void Equip() // EQUIPs THIS WEAPON to RIGHT HAND
    {
        if (!weaponOwner)
            return;
        else if (!weaponOwner.userSettings.rightHand)
            return;

        transform.SetParent(weaponOwner.userSettings.rightHand);
        transform.localPosition = weaponSettings.equipPos;
        transform.localRotation = Quaternion.Euler(weaponSettings.equipRot);
    }
    void UnEquip(weaponTypeEnum wp_type)
    {
        if (!weaponOwner) return;

        switch(wp_type)
        {
            case weaponTypeEnum.PISTOL:
                transform.SetParent(weaponOwner.userSettings.pistolUnequipSpot);
                break;
            case weaponTypeEnum.RIFLE:
                transform.SetParent(weaponOwner.userSettings.rifleUnequipSpot);
                break;
        }
        transform.localPosition = weaponSettings.unequipPos;
        transform.localRotation = Quaternion.Euler(weaponSettings.unequipRot);
    }

    public void LoadClip()// LOADS CLIP & CALCULATE AMMO
    {
        int ammoNeeded = ammoSystem.maxClipAmmo - ammoSystem.currClipAmmo;

        if(ammoNeeded >= ammoSystem.totalCarryingAmmo)// THEN WE CAN LOAD
        {
            ammoSystem.currClipAmmo = ammoSystem.totalCarryingAmmo;
            ammoSystem.totalCarryingAmmo = 0;
        }
        else
        {
            ammoSystem.totalCarryingAmmo -= ammoNeeded;
            ammoSystem.currClipAmmo = ammoSystem.maxClipAmmo;
        }
    }

    public void SeEquipState(bool equip)// SETs IF I'm EQUIPED OF NOT
    {
        equipped = equip;
    }
    public void PullTrigger(bool isPulling)// PULLS THE TRIGGER
    {
        pullingTrigger = isPulling;
    }
    public void SetWeaponOwner(WeaponHandler passed_weaponHandler)// SETs THE WeaponHandler OF THIS WEAPON
    {
        weaponOwner = passed_weaponHandler;
    }

    void PlayFiringSound(AudioClip Clip)
    {
        GameObject OneShotAudio = new GameObject("OneShotAudio");
        AudioSource newAudioSRS = OneShotAudio.AddComponent<AudioSource>();
        newAudioSRS.clip = Clip;
        newAudioSRS.pitch = Random.Range(soundSystem.PitchMin, soundSystem.PitchMax);
        OneShotAudio.transform.position = mainCam.transform.position;
        OneShotAudio.transform.SetParent(mainCam.transform);
        newAudioSRS.volume = soundSystem.Volume;
        newAudioSRS.Play();
        Destroy(OneShotAudio, Clip.length);
    }
    public void PlayReloadSound()
    {
        GameObject OneShotAudio = new GameObject("OneShotAudio");
        AudioSource newAudioSRS = OneShotAudio.AddComponent<AudioSource>();
        newAudioSRS.clip = soundSystem.ReloadSound;
        OneShotAudio.transform.position = mainCam.transform.position;
        OneShotAudio.transform.SetParent(mainCam.transform);
        newAudioSRS.volume = soundSystem.Volume;
        newAudioSRS.Play();
        Destroy(OneShotAudio, soundSystem.ReloadSound.length);
    }

}
