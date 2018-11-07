﻿/*
 * IndexPool.cs - Pool of indices which handles index reusing
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Freeserf.Renderer.OpenTK
{
    internal class IndexPool
    {
        Dictionary<int, bool> assignedIndices = new Dictionary<int, bool>();
        int firstFree = 0;

        public int AssignNextFreeIndex()
        {
            bool firstRun = true;

            while (assignedIndices.ContainsKey(firstFree) && assignedIndices[firstFree])
            {
                if (++firstFree == int.MaxValue)
                {
                    if (!firstRun)
                        throw new Exception("Now free index available.");

                    firstFree = 0;
                    firstRun = false;
                }
            }

            assignedIndices[firstFree] = true;

            return firstFree;
        }

        public void UnassignIndex(int index)
        {
            assignedIndices[index] = false;
        }
    }
}
