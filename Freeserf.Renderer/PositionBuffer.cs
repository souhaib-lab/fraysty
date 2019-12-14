﻿/*
 * PositionBuffer.cs - Buffer for shader position data
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Silk.NET.OpenGL;

namespace Freeserf.Renderer
{
    internal class PositionBuffer : BufferObject<short>
    {
        uint index = 0;
        bool disposed = false;
        short[] buffer = null;
        readonly object bufferLock = new object();
        int size;
        readonly IndexPool indices = new IndexPool();
        bool changedSinceLastCreation = true;
        readonly BufferUsageARB usageHint = BufferUsageARB.DynamicDraw;

        public override int Size => size;

        public override VertexAttribPointerType Type => VertexAttribPointerType.Short;

        public override int Dimension => 2;

        public PositionBuffer(bool staticData)
        {
            index = State.Gl.GenBuffer();

            if (staticData)
                usageHint = BufferUsageARB.StaticDraw;
        }

        public bool IsPositionValid(int index)
        {
            index *= Dimension; // 2 coords each

            if (index < 0 || index >= buffer.Length)
                return false;

            return buffer[index] != short.MaxValue;
        }

        public int Add(short x, short y, int index = -1)
        {
            bool reused;

            if (index == -1)
                index = indices.AssignNextFreeIndex(out reused);
            else
                reused = indices.AssignIndex(index);

            if (buffer == null)
            {
                buffer = new short[256];
                buffer[0] = x;
                buffer[1] = y;
                size = 2;
                changedSinceLastCreation = true;
            }
            else
            {
                if (index == buffer.Length / 2) // we need to recreate the buffer
                {
                    if (buffer.Length < 1024)
                        Array.Resize(ref buffer, buffer.Length + 256);
                    else if (buffer.Length < 4096)
                        Array.Resize(ref buffer, buffer.Length + 512);
                    else
                        Array.Resize(ref buffer, buffer.Length + 1024);

                    changedSinceLastCreation = true;
                }

                if (!reused)
                    size += 2;

                int bufferIndex = index * 2;

                if (buffer[bufferIndex + 0] != x ||
                    buffer[bufferIndex + 1] != y)
                {
                    buffer[bufferIndex + 0] = x;
                    buffer[bufferIndex + 1] = y;

                    changedSinceLastCreation = true;
                }
            }

            return index;
        }

        public void Update(int index, short x, short y)
        {
            int bufferIndex = index * 2;

            if (buffer[bufferIndex + 0] != x ||
                buffer[bufferIndex + 1] != y)
            {
                buffer[bufferIndex + 0] = x;
                buffer[bufferIndex + 1] = y;

                changedSinceLastCreation = true;
            }
        }

        public void Remove(int index)
        {
            indices.UnassignIndex(index);
        }

        public void ReduceSizeTo(int size)
        {
            this.size = size;
        }

        public override void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    State.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

                    if (index != 0)
                    {
                        State.Gl.DeleteBuffer(index);

                        if (buffer != null)
                        {
                            lock (bufferLock)
                            {
                                buffer = null;
                            }
                        }

                        size = 0;
                        index = 0;
                    }

                    disposed = true;
                }
            }
        }

        public override void Bind()
        {
            if (disposed)
                throw new Exception("Tried to bind a disposed buffer.");

            State.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, index);

            Recreate(); // ensure that the data is up to date
        }

        void Recreate() // is only called when the buffer is bound (see Bind())
        {
            if (!changedSinceLastCreation || buffer == null)
                return;

            lock (bufferLock)
            {
                unsafe
                {
                    fixed (short* ptr = &buffer[0])
                    {
                        State.Gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(Size * sizeof(short)),
                            ptr, usageHint);
                    }
                }
            }

            changedSinceLastCreation = false;
        }

        internal override bool RecreateUnbound()
        {
            if (!changedSinceLastCreation || buffer == null)
                return false;

            if (disposed)
                throw new Exception("Tried to recreate a disposed buffer.");

            State.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, index);

            lock (bufferLock)
            {
                unsafe
                {
                    fixed (short* ptr = &buffer[0])
                    {
                        State.Gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(Size * sizeof(short)),
                            ptr, usageHint);
                    }
                }
            }

            changedSinceLastCreation = false;

            return true;
        }
    }
}