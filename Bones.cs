
namespace AotForms
{
    // Converted OB54 -> OB55 (dump 17-09-2026). Same member names, OB55 values.
    // NOTE: OB55 dump has no calf/wrist-joint bones; mapped to nearest joints.
    internal enum Bones : uint
    {

        Head = 0x49C,
        Root = 0x4B0,
        LeftWrist = 0x4DC,
        Spine = 0x4A4,//
        Hip = 0x4A0,//
        RightCalf = 0x4BC,// mapped: RightAnkle (no calf in OB55 dump)
        LeftCalf = 0x4B8,// mapped: LeftAnkle (no calf in OB55 dump)
        RightFoot = 0x4C4,//
        LeftFoot = 0x4C0,//
        RightWrist = 0x4D8,//
        LeftHand = 0x4C8,//
        LeftShoulder = 0x4D0,//
        RightShoulder = 0x4D4,//
        RightWristJoint = 0x498,//
        LeftWristJoint = 0x4DC,// mapped: LeftWrist (no joint in OB55 dump)
        LeftElbow = 0x4E4,//
        RightElbow = 0x4E0,//
    }
}

