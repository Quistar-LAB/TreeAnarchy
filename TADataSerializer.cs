using System;
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
        [Flags]
        public enum ErrorFlags : ushort
        {
            NONE = 0,
            SUCCESS = 1,
            FAILED = 2,
            OLDFORMAT = 4,
        }

        private bool disposed = false;
        private readonly MemoryStream m_Stream = null;
        public MemoryStream m_tempBuf = null;

        public TADataSerializer()
        {
            m_Stream = new MemoryStream();
        }

        protected TADataSerializer(byte[] data)
        {
            m_Stream = new MemoryStream(data);
        }

        public TADataSerializer(int size)
        {
            m_Stream = new MemoryStream(size);
        }

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
        public ErrorFlags Deserialize(TreeInstance[] trees)
        {
            int treeCount = 0;
            int orgTreeCount = 0;
            int treeLimit;
            int MaxLen;
            SaveFlags flags = 0;

            Format version = (Format)ReadUShort();
            switch(version)
            {
                case Format.Version1:
                    treeLimit = FormatVersion1TreeLimit;
                    break;
                case Format.Version2:
                    treeLimit = FixEndian(ReadInt32()); // Fixing ushort reorder
                    break;
                case Format.Version3:
                    treeLimit = FixEndian(ReadInt32()); // Fixing ushort reorder
                    treeCount = FixEndian(ReadInt32()); // Fixing ushort reorder
                    ReadInt32(); // Reserved for future use
                    ReadInt32(); // Reserved for future use
                    flags = (SaveFlags)ReadUShort();
                    break;
                case Format.Version4:
                    treeLimit = ReadInt32();
                    treeCount = ReadInt32();
                    orgTreeCount = ReadInt32(); // used for reading posY between 0~262144 trees
                    ReadInt32(); // Reserved for future use
                    flags = (SaveFlags)ReadUShort();
                    break;
                default:
                    return ErrorFlags.FAILED;
            }
            /* Sanity check */
            if (treeLimit < 1 && treeCount < 0)
            {
                Debug.Log(@"TreeAnarchy: Read Tree Capacity < 0. Invalid Data");
                return ErrorFlags.FAILED;
            }
            if(treeLimit > MaxTreeLimit) treeLimit = MaxTreeLimit; // Only load what MaxTreeLimit is limited to
            switch (version)
            {
                case Format.Version1:
                case Format.Version2:
                case Format.Version3:
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
                                TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort();
                                m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                                trees[i].m_flags = (ushort)m_flags;
                                if (trees[i].m_flags != 0)
                                {
                                    trees[i].m_infoIndex = ReadUShort(); 
                                    trees[i].m_posX = ReadShort();
                                    trees[i].m_posZ = ReadShort();
                                    trees[i].m_posY = 0; // old format doesn't save this
                                }
                            }
                            break;
                    }
                    return ErrorFlags.SUCCESS | ErrorFlags.OLDFORMAT;
                case Format.Version4:
                    int treeAddCount = 0;
                    for (uint i = 1; i < DefaultTreeLimit; i++)
                    {
                        if(trees[i].m_flags != 0 && treeAddCount < orgTreeCount)
                        {
                            trees[i].m_posY = ReadUShort();
                            treeAddCount++;
                        }
                    }
                    for (uint i = DefaultTreeLimit; i < MaxTreeLimit; i++)
                    {
                        TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort();
                        m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                        trees[i].m_flags = (ushort)m_flags;
                    }
                    for (uint i = DefaultTreeLimit; i < MaxTreeLimit; i++)
                    {
                        if (trees[i].m_flags != 0)
                        {
                            trees[i].m_infoIndex = ReadUShort();
                            trees[i].m_posX = ReadShort();
                            trees[i].m_posZ = ReadShort();
                            trees[i].m_posY = ReadUShort();
                            treeCount++;
                        }
                        else
                        {
                            trees[i].m_posY = 0;
                        }
                    }
                    return ErrorFlags.SUCCESS;
            }
            return ErrorFlags.FAILED;
        }

        private ushort UpdatePosY(Vector3 position)
        {
            position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            return (ushort)Mathf.Clamp(Mathf.RoundToInt(position.y * 64f), 0, 65535);
        }

        public byte[] Serialize()
        {
            int orgTreeCount = 0;
            int extraTreeCount = 0;
            int treeLimit = MaxTreeLimit;
            TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;

            WriteUShort((ushort)Format.Version4); // 2 bytes for version
            WriteInt32(treeLimit);                // 4 bytes for limit
            WriteInt32(0);         // Reserved for treeCount;    4 bytes
            WriteInt32(0);        // treeCount for posY in 0~262144 trees    4 bytes
            WriteUInt32(0);        // Reserved for future use    4 bytes
            WriteUShort(0);        // flags.. this is ignore in our version 2 bytes

            /* Apparently, the trees could be located anywhere in the buffer
             * even if there's only 1 tree in the buffer. I'm assuming this is
             * due to performance concerns.
             * So have to save the entire buffer.
             */
            for (uint i = 1; i < DefaultTreeLimit; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    WriteUShort(buffer[i].m_posY);
                    orgTreeCount++;
                }
            }
            for(uint i = DefaultTreeLimit; i < treeLimit; i++)
            {
                WriteUShort(buffer[i].m_flags);
            }
            for(uint i = DefaultTreeLimit; i < treeLimit; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    WriteUShort(buffer[i].m_infoIndex);
                    WriteShort(buffer[i].m_posX);
                    WriteShort(buffer[i].m_posZ);
                    WriteUShort(buffer[i].m_posY);
                    extraTreeCount++;
                }
            }
            // Set header information now
            m_Stream.Position = 6;
            m_Stream.WriteByte((byte)extraTreeCount);
            m_Stream.WriteByte((byte)(extraTreeCount >> 8));
            m_Stream.WriteByte((byte)(extraTreeCount >> 16));
            m_Stream.WriteByte((byte)(extraTreeCount >> 24));
            m_Stream.WriteByte((byte)(orgTreeCount));
            m_Stream.WriteByte((byte)(orgTreeCount >> 8));
            m_Stream.WriteByte((byte)(orgTreeCount >> 16));
            m_Stream.WriteByte((byte)(orgTreeCount >> 24));

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
            return m_Stream.ToArray();
        }

        private int FixEndian(int num)
        {
            int n = ((num & 0xffff) << 16);
            return n | ((num >> 16) & 0xffff);
        }

        private short ReadShort()
        {
            short num = (short)m_Stream.ReadByte();
            return (short)(num |= (short)(m_Stream.ReadByte() << 8));

        }

        private int ReadInt32()
        {
            int num = m_Stream.ReadByte();
            num |= m_Stream.ReadByte() << 8;
            num |= m_Stream.ReadByte() << 16;
            return num | m_Stream.ReadByte() << 24;
        }

        private ushort ReadUShort()
        {
            ushort num = (ushort)(m_Stream.ReadByte() & 0xff);
            return (ushort)(num | (m_Stream.ReadByte() << 8));
        }

        public void WriteShort(short value)
        {
            m_Stream.WriteByte((byte)(value));
            m_Stream.WriteByte((byte)(value >> 8));
        }

        private void WriteInt32(int value)
        {
            m_Stream.WriteByte((byte)value);
            m_Stream.WriteByte((byte)(value >> 8));
            m_Stream.WriteByte((byte)(value >> 16));
            m_Stream.WriteByte((byte)(value >> 24));
        }

        private void WriteUShort(ushort value)
        {
            m_Stream.WriteByte((byte)value);
            m_Stream.WriteByte((byte)(value >> 8));
        }

        private void WriteUInt32(uint value)
        {
            m_Stream.WriteByte((byte)value);
            m_Stream.WriteByte((byte)(value >> 8));
            m_Stream.WriteByte((byte)(value >> 16));
            m_Stream.WriteByte((byte)(value >> 24));
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
                    m_Stream.Dispose();
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
