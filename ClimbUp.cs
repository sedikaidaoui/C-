using System;
using System.Numerics;
using System.Threading;

namespace AotForms
{
    internal static class ClimbUp
    {
        internal static void Start()
        {
            while (true)
            {
                if (!Config.ClimbUpEnabled)
                {
                    Thread.Sleep(5);
                    continue;
                }

                if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                {
                    Thread.Sleep(5);
                    continue;
                }

                try
                {

                    if (!InternalMemory.Read(Core.LocalPlayer + (uint)Bones.Root, out uint rootBonePtr)) continue;
                    if (!InternalMemory.Read(rootBonePtr + 0x8, out uint transformValue)) continue;
                    if (!InternalMemory.Read(transformValue + 0x8, out uint transformObjPtr)) continue;
                    if (!InternalMemory.Read(transformObjPtr + 0x20, out uint matrixPtr)) continue;

                    if (!Transform.GetNodePosition(rootBonePtr, out var currentPos)) continue;


                    Vector3 lowerPos = new Vector3(currentPos.X, currentPos.Y - 0.2f, currentPos.Z);
                    for (int d = 0; d < 20; d++)
                    {
                        InternalMemory.Write(matrixPtr + 0x60, lowerPos);
                        Thread.Sleep(5);
                    }


                    if (!Transform.GetNodePosition(rootBonePtr, out currentPos)) continue;


                    float stepSize = 1.5f;
                    int safetyLimit = 50;
                    Vector3 climbPos = currentPos;

                    for (int i = 0; i < safetyLimit && Config.ClimbUpEnabled; i++)
                    {
                        Vector3 nextPos = new Vector3(climbPos.X, climbPos.Y + stepSize, climbPos.Z);
                        InternalMemory.Write(matrixPtr + 0x60, nextPos);
                        Thread.Sleep(3);

                        if (!Transform.GetNodePosition(rootBonePtr, out var updatedPos)) break;
                        climbPos = updatedPos;
                    }


                    Vector3 finalPos = new Vector3(climbPos.X, climbPos.Y + 1.0f, climbPos.Z);
                    InternalMemory.Write(matrixPtr + 0x60, finalPos);
                }
                catch
                {

                }
            }
        }
    }
}
