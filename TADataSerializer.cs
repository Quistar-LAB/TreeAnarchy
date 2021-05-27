﻿using System;
using System.IO;
using System.IO.Compression;
using ColossalFramework;
using ColossalFramework.IO;
using UnityEngine;
using static TreeAnarchy.TAConfig;
using static TreeInstance;

namespace TreeAnarchy
{
    [Serializable]
    public abstract class TADataSerializer : IDisposable
    {
        private const int FormatVersion1TreeLimit = 1048576;
        private enum Format : ushort
        {
            Version1 = 1,
            Version2 = 2,
            Version3 = 3,
            Version4 = 4
        }
        [Flags]
        public enum SaveFlags : ushort
        {
            NONE = 0,
            PACKED = 1,
        }

        private bool disposed = false;
        public MemoryStream m_tempBuf = null;

        public abstract void FinalizeSave();
        public abstract void AfterDeserialize(Stream s);

        private byte[] Compress(Stream s)
        {
            byte[] compressed;
            using (var outStream = new MemoryStream())
            {
                using (var gzip = new GZipStream(outStream, CompressionMode.Compress))
                    s.CopyTo(gzip);
                compressed = outStream.ToArray();
            }
            return compressed;
        }

        private MemoryStream Decompress(Stream compressed)
        {
            using (var gZip = new GZipStream(compressed, CompressionMode.Decompress))
            using (m_tempBuf = new MemoryStream())
            {
                gZip.CopyTo(m_tempBuf);
            }
            return m_tempBuf;
        }

        // These are serial..So we have to think serially
        // a bit long because of all the different formats we have to consider
        // Keeping compatibility almost broke my brain reading the old codes,
        // but was a fun challenge
        public void Deserialize(Stream s)
        {
            int treeCount = 0;
            int orgTreeCount = 0;
            int treeLimit;
            int MaxLen;
            SaveFlags flags = 0;

            TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;

            Format version = (Format)ReadUShort(s);
            switch(version)
            {
                case Format.Version1:
                    treeLimit = FormatVersion1TreeLimit;
                    break;
                case Format.Version2:
                    treeLimit = FixEndian(ReadInt32(s)); // Fixing ushort reorder
                    break;
                case Format.Version3:
                    treeLimit = FixEndian(ReadInt32(s)); // Fixing ushort reorder
                    treeCount = FixEndian(ReadInt32(s)); // Fixing ushort reorder
                    ReadInt32(s); // Reserved for future use
                    ReadInt32(s); // Reserved for future use
                    flags = (SaveFlags)ReadUShort(s);
                    break;
                case Format.Version4:
                    treeLimit = ReadInt32(s);
                    treeCount = ReadInt32(s);
                    orgTreeCount = ReadInt32(s); // used for reading posY between 0~262144 trees
                    ReadInt32(s); // Reserved for future use
                    flags = (SaveFlags)ReadUShort(s);
                    break;
                default:
                    return;
            }
            /* Sanity check */
            if (treeLimit < 1 && treeCount < 0)
            {
                Debug.Log(@"TreeAnarchy: Read Tree Capacity < 0. Invalid Data");
                return;
            }
            if(treeLimit > MaxTreeLimit) treeLimit = MaxTreeLimit; // Only load what MaxTreeLimit is limited to
            switch (version)
            {
                case Format.Version1:
                case Format.Version2:
                case Format.Version3:
                    PreviousSaveWasOldFormat = true;
                    switch (flags & SaveFlags.PACKED)
                    {
                        case SaveFlags.NONE:
                            MaxLen = treeLimit;
                            goto ReadData;
                        case SaveFlags.PACKED:
                            if (treeCount > MaxTreeLimit - DefaultTreeLimit) MaxLen = MaxTreeLimit - DefaultTreeLimit;
                            else MaxLen = DefaultTreeLimit + treeCount; // offset MaxLen from DefaultTreeLimit
                            ReadData:
                            for (uint i = DefaultTreeLimit; i < MaxLen; i++)
                            {
                                TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort(s);
                                m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                                trees[i].m_flags = (ushort)m_flags;
                                if (trees[i].m_flags != 0)
                                {
                                    trees[i].m_infoIndex = ReadUShort(s); ;
                                    trees[i].m_posX = ReadShort(s);
                                    trees[i].m_posZ = ReadShort(s);
                                    trees[i].m_posY = 0; // old format doesn't save this
                                }
                            }
                            break;
                    }
                    break;
                case Format.Version4:
                    int treeAddCount = 0;
                    for (uint i = 1; i < DefaultTreeLimit; i++)
                    {
                        if(trees[i].m_flags != 0 && treeAddCount < orgTreeCount)
                        {
                            trees[i].m_posY = ReadUShort(s);
                            treeAddCount++;
                        }
                    }
                    for (uint i = DefaultTreeLimit; i < MaxTreeLimit; i++)
                    {
                        TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort(s);
                        m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                        trees[i].m_flags = (ushort)m_flags;
                    }
                    for (uint i = DefaultTreeLimit; i < MaxTreeLimit; i++)
                    {
                        if (trees[i].m_flags != 0)
                        {
                            trees[i].m_infoIndex = ReadUShort(s);
                            trees[i].m_posX = ReadShort(s);
                            trees[i].m_posZ = ReadShort(s);
                            trees[i].m_posY = ReadUShort(s);
                            treeCount++;
                        }
                        else
                        {
                            trees[i].m_posY = 0;
                        }
                    }
                    break;
            }
        }

        private ushort UpdatePosY(Vector3 position)
        {
            position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            return (ushort)Mathf.Clamp(Mathf.RoundToInt(position.y * 64f), 0, 65535);
        }

        public void Serialize(Stream s)
        {
            int orgTreeCount = 0;
            int extraTreeCount = 0;
            int treeLimit = MaxTreeLimit;
            uint index;
            TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;

            WriteUShort(s, (ushort)Format.Version4); // 2 bytes for version
            WriteInt32(s, treeLimit);                // 4 bytes for limit
            WriteInt32(s, 0);         // Reserved for treeCount;    4 bytes
            WriteInt32(s, 0);        // treeCount for posY in 0~262144 trees    4 bytes
            WriteUInt32(s, 0);        // Reserved for future use    4 bytes
            WriteUShort(s, 0);        // flags.. this is ignore in our version 2 bytes

            /* Apparently, the trees could be located anywhere in the buffer
             * even if there's only 1 tree in the buffer. I'm assuming this is
             * due to performance concerns.
             * So have to save the entire buffer.
             */
            for (uint i = 1; i < DefaultTreeLimit; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    if (PreviousSaveWasOldFormat) buffer[i].m_posY = UpdatePosY(buffer[i].Position);
                    WriteUShort(s, buffer[i].m_posY);
                    orgTreeCount++;
                }
            }
            for(uint i = DefaultTreeLimit; i < treeLimit; i++)
            {
                WriteUShort(s, buffer[i].m_flags);
            }
            for(uint i = DefaultTreeLimit; i < treeLimit; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    WriteUShort(s, buffer[i].m_infoIndex);
                    WriteShort(s, buffer[i].m_posX);
                    WriteShort(s, buffer[i].m_posZ);
                    if (PreviousSaveWasOldFormat) buffer[i].m_posY = UpdatePosY(buffer[i].Position);
                    WriteUShort(s, buffer[i].m_posY);
                    extraTreeCount++;
                }
            }
            // Set header information now
            s.Position = 6;
            s.WriteByte((byte)extraTreeCount);
            s.WriteByte((byte)(extraTreeCount >> 8));
            s.WriteByte((byte)(extraTreeCount >> 16));
            s.WriteByte((byte)(extraTreeCount >> 24));
            s.WriteByte((byte)(orgTreeCount));
            s.WriteByte((byte)(orgTreeCount >> 8));
            s.WriteByte((byte)(orgTreeCount >> 16));
            s.WriteByte((byte)(orgTreeCount >> 24));

#if FALSE // Burning tree handled in prefix method
            // Let original codes handle the extra burning tree list. Hope it works
            WriteUInt24(s, (uint)instance.m_burningTrees.m_size);
            for (int m = 0; m < instance.m_burningTrees.m_size; m++)
            {
                WriteUInt24(s, instance.m_burningTrees.m_buffer[m].m_treeIndex);
                WriteUShort(s, instance.m_burningTrees.m_buffer[m].m_fireIntensity);
                WriteUShort(s, instance.m_burningTrees.m_buffer[m].m_fireDamage);
            }
#endif
        }

        private int FixEndian(int num)
        {
            int n = ((num & 0xffff) << 16);
            return n | ((num >> 16) & 0xffff);
        }

        private short ReadShort(Stream s)
        {
            short num = (short)s.ReadByte();
            return (short)(num |= (short)(s.ReadByte() << 8));

        }

        private int ReadInt32(Stream s)
        {
            int num = s.ReadByte();
            num |= s.ReadByte() << 8;
            num |= s.ReadByte() << 16;
            return num | s.ReadByte() << 24;
        }

        private ushort ReadUShort(Stream s)
        {
            ushort num = (ushort)(s.ReadByte() & 0xff);
            return (ushort)(num | (s.ReadByte() << 8));
        }

        public void WriteShort(Stream s, short value)
        {
            s.WriteByte((byte)(value));
            s.WriteByte((byte)(value >> 8));
        }

        private void WriteInt32(Stream s, int value)
        {
            s.WriteByte((byte)value);
            s.WriteByte((byte)(value >> 8));
            s.WriteByte((byte)(value >> 16));
            s.WriteByte((byte)(value >> 24));
        }

        private void WriteUShort(Stream s, ushort value)
        {
            s.WriteByte((byte)value);
            s.WriteByte((byte)(value >> 8));
        }

        private void WriteUInt32(Stream s, uint value)
        {
            s.WriteByte((byte)value);
            s.WriteByte((byte)(value >> 8));
            s.WriteByte((byte)(value >> 16));
            s.WriteByte((byte)(value >> 24));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (m_tempBuf != null) m_tempBuf.Dispose();
                }
                disposed = true;
            }
        }

        ~TADataSerializer()
        {
            Dispose(false);
        }
    }
}
