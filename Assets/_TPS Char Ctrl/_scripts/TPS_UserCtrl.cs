using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace sinn.TPS_CTRL
{
    [RequireComponent(typeof(TPS_Ctrl))]
    public class TPS_UserCtrl : MonoBehaviour
    {
        Animator anim;

        #region move-jump-crouch
        [Header("[-Move, Jump, Crouch-]")]
        TPS_Ctrl TPS_Ctrl;
        Transform cam;
        Vector3 camForward;
        Vector3 moveDir;
        bool doJump;
        bool crouch;
        #endregion


        #region weapon
        [System.Serializable]
        public class InputSettings
        {
            public string HORZ = "Horizontal";
            public string VERT = "Vertical";
            public string CROUCH = "Crouch";
            public string SPRINT = "Sprint";

            public string RELOAD = "Reload";
            public string FIRE = "Fire1";
            public string AIM = "Fire2";
            public string DROP = "DropWeapon";
            public string SWITCHWEAPON = "SwitchWeapon";
        }
        [SerializeField] public InputSettings INPUT;

        [Header("[-WEAPON-]")]
        WeaponHandler weaponHandler;

        bool aiming;
        public Transform mySpine;
        public Vector3 crouchSpineRotOffset;
        #endregion

        [Header("[-COVER-]")]
        public bool covering;
        public bool cornerred;
        public float lowCover;

        void Start()
        {
            #region move-jump-crouch
            TPS_Ctrl = GetComponent<TPS_Ctrl>();
            anim = GetComponent<Animator>();
            cam = Camera.main.transform;
            #endregion

            #region weapon
            weaponHandler = GetComponent<WeaponHandler>();

            #endregion
        }

        void Update()
        {
            #region move-jump-crouch
            if (!doJump) 
                doJump = CrossPlatformInputManager.GetButtonDown("Jump");
            #endregion


            //Get Some Anim State
            covering = anim.GetBool("isCovering");
            cornerred = anim.GetBool("isCornered");
            lowCover = anim.GetFloat("lowCover");
        }
        void FixedUpdate()
        {
            if (!covering)
                MovementLogic();

            if((covering && cornerred) || !covering || (covering && lowCover > 0.7f))
                WeaponLogic();
        }

        private void MovementLogic()
        {
            #region move-jump-crouch
            float H = CrossPlatformInputManager.GetAxis(INPUT.HORZ);
            float V = CrossPlatformInputManager.GetAxis(INPUT.VERT);
            crouch = CrossPlatformInputManager.GetButton(INPUT.CROUCH);
            bool sprint = CrossPlatformInputManager.GetButton(INPUT.SPRINT);

            //              [- calculate camera relative direction to move:-]
            camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized; // makes y axis 0, & normalize

            moveDir = V * camForward + H * cam.right;

            if (!sprint) moveDir *= 0.5f;
            if (aiming) moveDir *= 0.7f;

            TPS_Ctrl.Move(moveDir, crouch, doJump, aiming);
            doJump = false; // [-IN EVERY FIXED-UPDATE MAKE JUMP FALSE-]
            #endregion
        }

        void LateUpdate()
        {
            #region Spine Positioning with aim
            if (weaponHandler.curr_weapon && aiming && ((covering && (cornerred || lowCover > 0.7f)) || !covering))
            {
                PositionSpine();
            }
            #endregion
        }

        void PositionSpine()
        {
            if (!mySpine || !weaponHandler.curr_weapon || !cam) return;

            //[OLD] - WORKED ONLY FOR CYBERPUNK CHARACTER
            //Ray ray = new Ray(cam.position, cam.up);
            //mySpine.LookAt(ray.GetPoint(500), transform.right); // LOOK AT 500 UNIT AWAY ALONG THE RAY, at LOCAL UPWARD DIRECTION.
            //[NEW] - :
            Ray ray = new Ray(cam.position, cam.forward);
            mySpine.LookAt(ray.GetPoint(500), Vector3.up);

            Vector3 eularAngleOffset = weaponHandler.curr_weapon.userSettings.spineRotOffset;

            if(!crouch)
                mySpine.Rotate(eularAngleOffset);
            else
                mySpine.Rotate(eularAngleOffset + crouchSpineRotOffset);


            //Character Look y rotation
            if (!covering || !cornerred)
            {
                float lookDistance = 30;
                Transform cam_pivot = cam.transform.parent;
                Vector3 lookTarget = cam_pivot.position + (cam_pivot.forward * lookDistance);
                Vector3 lookDir = lookTarget - transform.position;
                Quaternion lookRot = Quaternion.LookRotation(lookDir);
                lookRot.x = 0;
                lookRot.z = 0;

                Quaternion newRotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * 50); // GOTTA ROTATE FAST TO KEEP  UP WITH SPINE
                transform.rotation = newRotation;
            }
        }

        void WeaponLogic()
        {
            if (!weaponHandler) return;

            aiming = CrossPlatformInputManager.GetButton(INPUT.AIM);

            weaponHandler.Aim(aiming);

            weaponHandler.FingerOnTrigger(CrossPlatformInputManager.GetButton(INPUT.FIRE));

            if (CrossPlatformInputManager.GetButtonDown(INPUT.RELOAD))
                weaponHandler.Reload();
            if (CrossPlatformInputManager.GetButtonDown(INPUT.DROP))
                weaponHandler.DropWeapon();
            if (CrossPlatformInputManager.GetButtonDown(INPUT.SWITCHWEAPON))
                weaponHandler.SwitchWeapon();
        }
    }
}
