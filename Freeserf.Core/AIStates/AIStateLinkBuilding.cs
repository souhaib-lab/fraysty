﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateLinkBuilding : AIState
    {
        uint buildingPos = Global.BadMapPos;

        public AIStateLinkBuilding(uint buildingPos)
        {
            this.buildingPos = buildingPos;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            var flagPos = game.Map.MoveDownRight(buildingPos);
            uint bestFlagPos = Global.BadMapPos;
            int minDist = int.MaxValue;
            Dictionary<uint, int> flags = new Dictionary<uint, int>();

            foreach (var flag in game.GetPlayerFlags(player))
            {
                if (flagPos == flag.Position)
                    continue; // not link to self

                int distX = game.Map.DistX(flagPos, flag.Position);
                int distY = game.Map.DistY(flagPos, flag.Position);
                int dist = Misc.Round(Math.Sqrt(distX * distX + distY * distY));

                if (dist < minDist)
                {
                    bestFlagPos = flag.Position;
                    minDist = dist;

                    if (dist == 2)
                        break;
                }

                flags.Add(flag.Position, dist);
            }

            if (bestFlagPos == Global.BadMapPos)
            {
                // could not find a valid flag to link to
                // return to idle state and decide there what to do
                Kill(ai);
                return;
            }

            while (flags.Count > 0)
            {
                var road = Pathfinder.Map(game.Map, flagPos, bestFlagPos);

                if (road.Length > minDist * 2) // maybe the nearest flag is behind the border and the way is much longer as thought
                {
                    flags.Remove(bestFlagPos);
                    bestFlagPos = flags.OrderBy(f => f.Value).First().Key;
                    continue;
                }

                if (game.BuildRoad(road, player))
                    break;
            }

            // could not link the flags
            // return to idle state and decide there what to do
            Kill(ai);
        }
    }
}