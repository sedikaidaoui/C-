using System;

namespace AotForms
{
    // Synced OB55 (dump 17-09-2026 - KRISHU X DUMPER). Full dump, same member names.
    internal static class Offsets
    {
        internal static ulong Il2Cpp;
        internal static uint InitBase = 0xA342EFC;
        internal static uint GameFacade_Script = 0xA5BC344;
        internal static uint StaticClass = 0x5C;

        internal static uint CurrentMatchGame = 0x4;
        internal static uint CurrentMatch = 0x50;
        internal static uint MatchState = 0x8C;
        internal static uint MatchStatus = 0x8C;
        internal static uint GameTimer = 0x10;
        internal static uint FixedDeltaTime = 0x24;
        internal static uint Level = 0x14;
        internal static uint DictionaryEntities = 0x68;
        internal static uint LevelObjectManager = 0x60;
        internal static uint LevelObjectManagerAlt = 0x64;
        internal static uint DicPlayer = 0xC8;

        internal static uint LocalPlayer = 0x94;
        internal static uint PlayerAttributes = 0x500;
        internal static uint PlayerAttributes_Alt = 0x500;
        internal static uint LocalPlayerAttributes = 0x500;
        internal static uint Player_Data = 0x48;
        internal static uint Player_Name = 0x31C;
        internal static uint Player_OriginalNick = 0x320;
        internal static uint Player_IsDead = 0x50;
        internal static uint Player_ShadowBase = 0x1A60;
        internal static uint Player_BaseProfile = 0x1A74;
        internal static uint Player_IsFrozenKnock = 0x6C;
        internal static uint Player_UserID = 0x298;
        internal static uint Player_CharacterCtrl = 0x348;
        internal static uint Player_ActiveWeapon = 0x434;
        internal static uint Player_AimRotation = 0x440;
        internal static uint Player_FollowCamera = 0x494;
        internal static uint Player_HeadCollider = 0x4E8;
        internal static uint Player_AvatarManager = 0x504;
        internal static uint Player_HitObjectInfo = 0x9D0;
        internal static uint Player_PhysXData = 0x14E8;
        internal static uint Player_GroundHit = 0x1554;
        internal static uint Player_SecondaryWeapon = 0x3F8;
        internal static uint Player_FastSwitch = 0x564;
        internal static uint Player_UGCRequestJump = 0x17C;
        internal static uint Player_HitFlyInfo = 0x55C;
        internal static uint Player_CurrentAimRot = 0x12C4;
        internal static uint Player_Profile = 0x18CC;
        internal static uint Player_LevelPtr = 0x18CC;
        internal static uint IsClientBot = 0x324;
        internal static uint IsBotOffs = 0x254;
        internal static uint TeamModeID = 0x294;
        internal static uint PlayerID = 0x268;
        internal static uint XPose = 0x78;
        internal static uint IsFiring = 0x544;
        internal static uint IS_FIRING = 0x544;
        internal static uint pomba = 0x544;
        internal static uint LocalPlayerIsFiring = 0x540;
        internal static uint InSnowSlideWayDashing = 0x1480;
        internal static uint UnlimitedAmmo = 0xE0;

        internal static uint Profile_AccountID = 0x8;
        internal static uint Profile_Region = 0x10;
        internal static uint Profile_Level = 0x14;
        internal static uint Profile_NickName = 0x18;
        internal static uint Profile_ClanName = 0x30;
        internal static uint Profile_ClanID = 0x38;
        internal static uint Profile_Rank = 0x58;
        internal static uint Profile_RankPoint = 0x5C;
        internal static uint Profile_BRPeakRankPos = 0x60;
        internal static uint Profile_CSRank = 0x64;
        internal static uint Profile_CSRankPoint = 0x68;
        internal static uint Profile_CSPeakRankPos = 0x6C;
        internal static uint Profile_GroupMemberCnt = 0x98;
        internal static uint Profile_GroupMode = 0x9C;
        internal static uint Profile_WorkshopMap = 0xAC;
        internal static uint Profile_MatchMode = 0xE8;
        internal static uint Profile_GameMode = 0xEC;
        internal static uint Profile_MapID = 0xF0;
        internal static uint Profile_HasPveTalent = 0xF4;
        internal static uint Profile_RoomID = 0xF8;
        internal static uint Profile_RoomType = 0x100;
        internal static uint Profile_RoomMember = 0x104;
        internal static uint Profile_RoomMaxMember = 0x108;
        internal static uint Profile_GroupID = 0x190;
        internal static uint Profile_StreakWins = 0x1A4;

        internal static uint Attr_NoReload = 0x99;
        internal static uint Attr_NoReloadPtr = 0xC1;
        internal static uint Attr_InfiniteAmmo = 0xE0;
        internal static uint Attr_DamageAdditionScale = 0xE8;
        internal static uint Attr_ExecuteDamageScale = 0xF0;
        internal static uint Attr_BuffWeaponDamageScale = 0xF8;
        internal static uint Attr_BuffDamage = 0xF8;
        internal static uint Attr_BuffWeaponAmmoClip = 0x104;
        internal static uint Attr_PetSkillCDReduction = 0x15C;
        internal static uint Attr_ActiveSkillCDReduction = 0x160;
        internal static uint Attr_FireIntervalScale1 = 0x1C4;
        internal static uint Attr_FireIntervalScale2 = 0x1D4;
        internal static uint RapidfirePtr = 0x1C8;
        internal static uint EnableAccelerationOnFalling = 0x28F3;
        internal static uint EnableLowFallingSwapWeapon = 0x2C3D;
        internal static uint Shadow_TargetTickCount = 0x8;
        internal static uint Shadow_SimTick = 0xC;
        internal static uint Shadow_Pos = 0x1C;
        internal static uint Shadow_TargetPhysXPose = 0x74;
        internal static uint GameVar_RotSensMin = 0xF0C;
        internal static uint GameVar_RotSensMax = 0xF10;
        internal static uint GameVar_AimRotSensMin = 0xF14;
        internal static uint GameVar_AimRotSensMax = 0xF18;
        internal static uint Avatar = 0xA8;
        internal static uint AvatarManager = 0x504;
        internal static uint Avatar_Data = 0x14;
        internal static uint Avatar_Data_IsTeam = 0x59;
        internal static uint Avatar_IsVisible = 0x95;

        internal static uint Camera = 0x18;
        internal static uint Camera_Intptr = 0x8;
        internal static uint MainCameraTransform = 0x28C;
        internal static uint FollowCamera = 0x494;
        internal static uint AimRotation = 0x440; // reverted: 0x1A8C test failed
        internal static uint ViewMatrix = 0xE8;
        internal static uint FollowCam_RightOffset = 0x64;
        internal static uint FollowCam_UpOffset = 0x68;
        internal static uint FollowCam_VisionSpeed = 0x48;

        internal static uint CurrentObserver = 0xB4;
        internal static uint ObserverPlayer = 0x28;

        internal static uint Weapon = 0x434;
        internal static uint WeaponData = 0x58;
        internal static uint IntWeaponType = 0x64;
        internal static uint WeaponRecoil = 0xC;
        internal static uint Weapon_ItemId = 0x494;
        internal static uint WeaponInfo = 0x978;
        internal static uint WeaponInfoClass = 0x4A8;
        internal static uint IsCombineWeapon = 0xD8;
        internal static uint WeaponOnHand = 0x54;
        internal static uint CombineWeaponOnHand = 0x58;
        internal static uint WeaponID = 0x14;
        internal static uint Weapon_FireDelay = 0x10;
        internal static uint Weapon_IsSighting = 0x5CC;
        internal static uint LastAimingInfo = 0x978;
        internal static uint GunTipPosition = 0x38;
        internal static uint BulletHit = 0x2C;
        internal static uint HBIB_WeaponData = 0x64;
        internal static uint IOKEAL_WeaponDataFromWpId = 0x2C;
        internal static uint IEFJGC_ItemUniqueId = 0x8;

        internal static uint AimbotVisible = 0x4E8;
        internal static uint LockedAimingCollider = 0x54;
        internal static uint Collider = 0x4E8;
        internal static uint sAim1 = 0x58C;
        internal static uint sAim2 = 0x9D0;
        internal static uint sAim3 = 0x38;
        internal static uint sAim4 = 0x2C;
        internal static uint aimlock1 = 0x448;
        internal static uint aimlock3 = 0x46C;

        internal static uint SilentFiring = 0x540;
        internal static uint SilentHit = 0x7D0;
        internal static uint Silents = 0x38;
        internal static uint Silentr = 0x2C;

        internal static uint Hit_GameObject = 0xC;
        internal static uint Hit_HeadCollider = 0x10;
        internal static uint Hit_Loc = 0x14;
        internal static uint Hit_Normal = 0x20;
        internal static uint Hit_RayDir = 0x2C;
        internal static uint Hit_StartPos = 0x38;
        internal static uint Hit_Part = 0x50;
        internal static uint Hit_Ignore = 0x58;
        internal static uint Hit_OrgStartPos = 0x5C;
        internal static uint Hit_SpecialType = 0x68;

        internal static uint Dmg_BaseDamage = 0x8;
        internal static uint Dmg_ColliderType = 0xC;
        internal static uint Dmg_PlayerID = 0x18;
        internal static uint Dmg_WeaponOnHand = 0x30;
        internal static uint Dmg_WeaponDataId = 0x34;
        internal static uint Dmg_FirePos = 0x38;
        internal static uint Dmg_HitPos = 0x44;

        internal static uint uiinGameScene = 0x8;
        internal static uint BigMap = 0x218;
        internal static uint BigMapCtrl = 0x240;
        internal static uint JumpBtnCtrl = 0x28C;
        internal static uint CreepCtrl = 0x288;
        internal static uint AuxfireCtrl = 0x3B4;
        internal static uint MapContent = 0x54;
        internal static uint MapContentCtrl = 0x5C;
        internal static uint MapMarkController = 0x90;
        internal static uint LocalMapMarkCtrl = 0x94;
        internal static uint marked = 0x58;
        internal static uint MarkPos = 0x60;

        internal static uint HeadCollider = 0x360;
        internal static uint ReplaceCollider = 0x54;
        internal static uint WeaponHeadCollider = 0x4A4;

        internal static uint EnableAccelOnFalling = 0x28F3;
        internal static uint EnableLowFallingSwap = 0x2C3D;

        internal static uint krishuxcheats = 0x54;
        internal static uint cax = 0x54;
        internal static uint setposition = 0x60;
        internal static uint ShadowState = 0x18B8;

        internal static uint Msg_SwapWeapon_PlayerID = 0xC;
        internal static uint Msg_SwapWeapon_ItemUID = 0x10;
        internal static uint Msg_Execute_PlayerID = 0xC;
        internal static uint Msg_Execute_TargetSlot = 0x10;
        internal static uint Msg_Rescue_PlayerID = 0xC;
        internal static uint Msg_Rescue_TargetSlot = 0x10;
        internal static uint Msg_Rescue_ActionType = 0x14;

        // OB54 compat aliases (old names used across the project)
        internal static uint FXNINZA = 0x54;
        internal static uint NoReload = 0xC1;
        internal static uint NoReloadPtr = 0xC1;

        internal static class Methods
        {
            internal static uint HitFly_ClientHitFlyToCenterTrigger = 0x15A3828;
            internal static uint MatchGame_CanFire = 0x67DD914;
            internal static uint Player_TakeDamage = 0x635884C;
            internal static uint PlayerNetwork_TakeDamage = 0x678CBC0;

            internal static uint CurrentLocalPlayer = 0x661BCDC;
            internal static uint CurrentMatch = 0x661B780;
            internal static uint CurrentInGameUIScene = 0x6618688;
            internal static uint GameFacade_Send = 0x6628714;
            internal static uint GameSimulationTimer = 0x6629458;
            internal static uint IsLocalTeammate = 0x62F48B0;
            internal static uint GetHeadTF = 0x637D01C;
            internal static uint GetHipTF = 0x637D0A8;
            internal static uint GetHeadCollider = 0x62D1758;
            internal static uint InSwapWeaponCD = 0x62D3668;
            internal static uint ResetSwapWeaponTime = 0x62D3604;
            internal static uint UpdateBehavior_Player = 0x6332708;
            internal static uint UpdateBehavior_PNet = 0x6785600;
            internal static uint Vector3MsgCreator = 0x3D9A3D0;
            internal static uint QuatMsgCreator = 0x3D9A7E0;
            internal static uint ShuffleCheckParams = 0x6994118;
            internal static uint Transform_get_pos = 0x9CFA0A0;
            internal static uint Transform_set_pos = 0x9CFA1CC;
            internal static uint Camera_WorldToScreen = 0x9C8FE7C;
            internal static uint Camera_get_main = 0x9C90410;

            internal static uint Player_GetWeaponOnHand = 0x62AF0D0;
            internal static uint Player_GetActiveWeapon = 0x62AECC4;
            internal static uint Player_get_PlayerID = 0x6292740;
            internal static uint Player_get_MyPhsXData = 0x62D7190;
            internal static uint Player_set_aim = 0x62CF8CC;
            internal static uint Player_SwapWeapon = 0x6365028;
            internal static uint Player_get_CurHP = 0x635AAC8;
            internal static uint Player_get_IsDieing = 0x62B6334;
            internal static uint Player_get_IsSighting = 0x62C5618;
            internal static uint Player_get_IsFiringFromPRI = 0x647BDE8;
            internal static uint Player_IsFiring = 0x62BA2C4;
            internal static uint Player_get_IsWearingFoldWing = 0x641D1C4;
            internal static uint Player_IsFoldWingGliding = 0x63D2E58;
            internal static uint Player_GetCurrentDashSpeed = 0x6450D7C;
            internal static uint Player_IsWeaponInAutoCharge = 0x6387490;
            internal static uint Player_get_CanDoubleJump = 0x640BB00;
            internal static uint Player_get_IsWearingValidJumpShoes = 0x640BB74;
            internal static uint Player_GetDoubleJumpData = 0x640BFA8;
            internal static uint Player_CanHumanTireJump = 0x62D87DC;
            internal static uint Player_get_CurrentHumanTireIAmIn = 0x62D86C0;
            internal static uint Player_get_ShowDamageNum = 0x64C29AC;
            internal static uint Player_get_IsShowDamageInfo = 0x501AB6C;
            internal static uint Player_GetPhysXState = 0x62D33BC;
            internal static uint Player_IsMoving = 0x62C51A0;

            internal static uint Attr_get_FireIntervalScale = 0x73266E8;
            internal static uint Attr_set_FireIntervalScale = 0x732673C;
            internal static uint Attr_GetMultiplyFireIntervalScaleByWeaponType = 0x7323088;
            internal static uint Attr_GetSpeedScaleBySpeedType = 0x732103C;
            internal static uint Attr_get_FireIntervalScaleSkill = 0x732679C;
            internal static uint Attr_get_FireIntervalScaleTwo = 0x732691C;

            internal static uint HBIB_WeaponFire = 0x699B7D4;
            internal static uint HBIB_SetStartDamage = 0x6989B48;
            internal static uint HBIB_CGKCJLOLNKC = 0x696C5C8;
            internal static uint HBIB_NPLMKLBKJPE = 0x698D4C4;
            internal static uint HBIB_GetDamage = 0x69572F8;
            internal static uint HBIB_GetRange = 0x694F49C;
            internal static uint HBIB_JBHDCDGJDPK = 0x6957;
            internal static uint HBIB_RefreshWeaponPrm = 0x6955050;
            internal static uint HBIB_DoubleGun = 0x694F014;

            internal static uint PlayerNetwork_RequestRescureTarget = 0x6791A8C;
            internal static uint PlayerNetwork_RequestExecuteTarget = 0x6790DE0;

            internal static uint Raycast_BKIAMGKDCLD_4arg = 0x721D9B8;
            internal static uint Raycast_BKIAMGKDCLD_7arg = 0x722CC04;
            internal static uint Raycast_FFLLOGJDGOJ_5arg = 0x721D53C;
            internal static uint Raycast_FFLLOGJDGOJ_7arg = 0x721F294;
            internal static uint Raycast_LBIKIHBPLKC_7arg = 0x721DBF4;
            internal static uint Raycast_LBIKIHBPLKC_6arg = 0x721E690;

            internal static uint GhostMode_AGDFODEDCCP = 0x6F5B400;
            internal static uint PhyXShield_DamageS2C = 0x6E86244;
            internal static uint BEADLMGGGGL_uint = 0x7330E80;

            internal static uint Screen_SetResolution = 0x9CA0C94;
            internal static uint Time_get_time = 0x9CF60AC;
            internal static uint RenderSettings_set_skybox = 0x9CAD668;
            internal static uint RenderSettings_get_skybox = 0x9CAD628;
            internal static uint RenderSettings_set_ambientMode = 0x9CACF38;
            internal static uint TransformNode_get_transform = 0x7AF5AC8;
            internal static uint String_CreateString = 0x98D9DCC;
            internal static uint String_get_Chars = 0x98DF994;
            internal static uint CharacterController_get_isGrounded = 0x9D6D138;
            internal static uint GameConfig_get_ResetGuest = 0x5C301E4;
            internal static uint AnimationSystem_GetCurrentRunningHandler = 0x7AF48A8;
            internal static uint Component_get_transform = 0x9CE7F98;
            internal static uint Transform_get_forward = 0x9CFAA30;
        }

        internal static class Opcodes
        {
            internal const int RUDP_START_RESCURE = 142;
            internal const int RUDP_TAKE_DAMAGE = 106;
            internal const int RUDP_CHANGE_INVENTORY_ON_HAND = 108;
            internal const int RUDP_START_EXECUTE = 951;
            internal const int RUDP_TAKE_DAMAGE_PENETRATED = 1322;
        }
    }
}
