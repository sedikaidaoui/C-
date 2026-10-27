using System;
using System.Numerics;
using System.Threading;

namespace AotForms
{
    internal static class ShurikenMultiDamage
    {
        private const float FakeDamageValue = 9014280192.0f;
        private const float NormalDamageValue = 1.0f;

        internal static void Work()
        {
            while (true)
            {
                try
                {
                    if (Config.KXCShurikenMultiDamage)
                        ApplyDamage();
                }
                catch { }

                Thread.Sleep(10);
            }
        }

        private static void ApplyDamage()
        {
            ulong localPlayer = Core.LocalPlayer;
            if (localPlayer == 0) return;

            if (!InternalMemory.Read<uint>(localPlayer + Offsets.PlayerAttributes_Alt, out uint attributes))
                return;

            if (attributes == 0) return;

            // Max 200m: only engage while an enemy is within range, else keep normal value.
            bool enemyInRange = false;
            try
            {
                foreach (var e in Core.Entities.Values)
                {
                    if (e == null || !e.IsKnown || e.IsDead) continue;
                    if (Vector3.Distance(Core.LocalMainCamera, e.Head) <= 200f) { enemyInRange = true; break; }
                }
            }
            catch { }

            if (enemyInRange && Config.KXCFakeDame)
                InternalMemory.Write<float>(attributes + Offsets.Attr_BuffDamage, FakeDamageValue);
            else
                InternalMemory.Write<float>(attributes + Offsets.Attr_BuffDamage, NormalDamageValue);
        }
    }
}