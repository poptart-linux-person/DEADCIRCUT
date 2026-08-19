using System.Collections.Generic;
using UnityEngine;

namespace DeadCircuit.AI
{
    public static class DeadCircuitNoiseDirector
    {
        static readonly List<DeadCircuitMonster> Monsters = new();

        public static void Register(DeadCircuitMonster monster)
        {
            if (monster != null && !Monsters.Contains(monster)) Monsters.Add(monster);
        }

        public static void Unregister(DeadCircuitMonster monster)
        {
            Monsters.Remove(monster);
        }

        public static void Emit(NoiseEvent noise)
        {
            for (int i = Monsters.Count - 1; i >= 0; i--)
            {
                if (Monsters[i] == null) { Monsters.RemoveAt(i); continue; }
                Monsters[i].HearNoise(noise.Position, noise.Loudness, noise.Type);
            }
        }
    }
}
