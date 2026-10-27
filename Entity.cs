
using System.Numerics;

namespace AotForms
{
    internal class Entity
    {
        internal ulong BaseAddress;
        internal bool IsKnown;
        internal Bool3 IsTeam;
        internal Vector3 Head;
        internal Vector3 Neck;
        internal Vector3 LeftWrist;
        internal Vector3 RightWrist;
        internal Vector3 Spine;
        internal Vector3 Root;
        internal Vector3 Hip;
        internal Vector3 RightCalf;
        internal Vector3 LeftCalf;
        internal Vector3 RightFoot;
        internal Vector3 LeftFoot;
        internal Vector3 LeftHand;
        internal Vector3 LeftShoulder;
        internal Vector3 RightShoulder;
        internal Vector3 RightWristJoint;
        internal Vector3 LeftWristJoint;
        internal Vector3 RightElbow;
        internal Vector3 LeftElbow;
        internal short Health;
        internal bool IsDead;
        internal bool IsKnocked;
        internal string Name;
        internal float Distance;
        internal bool isVisible;
        internal string WeaponName;
        internal uint Address;
        internal bool IsEnemy;
        internal DateTime LastSeen;

        // ESP screen smoothing state
        internal Vector2 EspSmHead;
        internal Vector2 EspSmFoot;
        internal bool EspSmValid;
        internal byte EspSmMiss;

    }

    /// <summary>Ground-loot candidate: world position + identified weapon. Read-only probe, no new offsets.</summary>
    internal class LootItem
    {
        internal uint Address;
        internal Vector3 Position;
        internal int WeaponId;
        internal string Name = "";
        internal DateTime Seen;
    }

}
