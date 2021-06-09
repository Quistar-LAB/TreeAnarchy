using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ColossalFramework;
using ColossalFramework.IO;
using static TreeAnarchy.TAConfig;
using UnityEngine;

namespace TreeAnarchy {
    [Serializable]
    public unsafe abstract class TADataSerializer : IDisposable {
        private const int FormatVersion1TreeLimit = 1048576;
        private readonly byte[] _buffer;
        private byte* pBuffer;
        private readonly byte* pOrigin;
        private readonly GCHandle gcHandle;
        private bool disposed = false;

        public virtual long Length => pBuffer - pOrigin;
        public virtual long Position {
            get => pBuffer - pOrigin;
            set => pBuffer = pOrigin + value;
        }
        private enum Format : ushort {
            Version1 = 1,
            Version2 = 2,
            Version3 = 3,
            Version4 = 4
        }
        [Flags]
        public enum SaveFlags : ushort {
            NONE = 0,
            PACKED = 1,
        }
        [Flags]
        public enum ErrorFlags : ushort {
            NONE = 0,
            SUCCESS = 1,
            FAILED = 2,
            OLDFORMAT = 4,
        }

        public MemoryStream m_tempBuf = null;

        protected TADataSerializer() {
            const int headerSize = 10 * sizeof(ushort);
            const int bodySize = sizeof(ushort) // m_infoIndex
                               + sizeof(short)  // m_posX
                               + sizeof(short)  // m_posZ
                               + sizeof(ushort); // m_posY
            int size = sizeof(ushort) + MaxTreeLimit;
            size += headerSize + (bodySize * MaxTreeLimit) + 100 /* extra safety space */;
            _buffer = new byte[size];
            gcHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            pOrigin = pBuffer = (byte*)gcHandle.AddrOfPinnedObject();
        }

        protected TADataSerializer(byte[] data) {
            gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            _buffer = data;
            pOrigin = pBuffer = (byte*)gcHandle.AddrOfPinnedObject();
        }

        protected TADataSerializer(int size) {
            _buffer = new byte[size];
            gcHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            pOrigin = pBuffer = (byte*)gcHandle.AddrOfPinnedObject();
        }

        public abstract void FinalizeSave();
        public abstract void AfterDeserialize(DataSerializer s);

        private byte[] Compress(Stream s) {
            byte[] compressed;
            using (var outStream = new MemoryStream()) {
                using (var gzip = new GZipStream(outStream, CompressionMode.Compress))
                    s.CopyTo(gzip);
                compressed = outStream.ToArray();
            }
            return compressed;
        }

        private MemoryStream Decompress(Stream compressed) {
            using (var gZip = new GZipStream(compressed, CompressionMode.Decompress))
            using (m_tempBuf = new MemoryStream()) {
                gZip.CopyTo(m_tempBuf);
            }
            return m_tempBuf;
        }

        // These are serial..So we have to think serially
        // a bit long because of all the different formats we have to consider
        // Keeping compatibility almost broke my brain reading the old codes,
        // but was a fun challenge
        public bool Deserialize(TreeInstance[] trees, out ErrorFlags errorFlags) {
            int treeCount = 0;
            int orgTreeCount = 0;
            int treeLimit;
            int MaxLen;
            SaveFlags flags = 0;

            errorFlags = ErrorFlags.NONE;
            Format version = (Format)ReadUShort();
            switch (version) {
                case Format.Version1:
                treeLimit = FormatVersion1TreeLimit;
                break;
                case Format.Version2:
                treeLimit = FixEndian(ReadInt()); // Fixing ushort reorder
                break;
                case Format.Version3:
                treeLimit = FixEndian(ReadInt()); // Fixing ushort reorder
                treeCount = FixEndian(ReadInt()); // Fixing ushort reorder
                ReadInt(); // Reserved for future use
                ReadInt(); // Reserved for future use
                flags = (SaveFlags)ReadUShort();
                break;
                case Format.Version4:
                treeLimit = ReadInt();
                treeCount = ReadInt();
                orgTreeCount = ReadInt(); // used for reading posY between 0~262144 trees
                ReadInt(); // Reserved for future use
                flags = (SaveFlags)ReadUShort();
                break;
                default:
                errorFlags |= ErrorFlags.FAILED;
                return false;
            }
            /* Sanity check */
            if (treeLimit < 1 && treeCount < 0) {
                errorFlags |= ErrorFlags.FAILED;
                return false;
            }
            if (treeLimit > MaxTreeLimit)
                treeLimit = MaxTreeLimit; // Only load what MaxTreeLimit is limited to
            switch (version) {
                case Format.Version1:
                case Format.Version2:
                case Format.Version3:
                switch (flags & SaveFlags.PACKED) {
                    case SaveFlags.NONE:
                    MaxLen = treeLimit;
                    goto ReadData;
                    case SaveFlags.PACKED:
                    if (treeCount > MaxTreeLimit - DefaultTreeLimit)
                        MaxLen = MaxTreeLimit - DefaultTreeLimit;
                    else
                        MaxLen = DefaultTreeLimit + treeCount; // offset MaxLen from DefaultTreeLimit
ReadData:
                    for (uint i = DefaultTreeLimit; i < MaxLen; i++) {
                        TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort();
                        m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                        trees[i].m_flags = (ushort)m_flags;
                        if (trees[i].m_flags != 0) {
                            trees[i].m_infoIndex = ReadUShort();
                            trees[i].m_posX = ReadShort();
                            trees[i].m_posZ = ReadShort();
                            trees[i].m_posY = 0; // old format doesn't save this
                        }
                    }
                    break;
                }
                errorFlags |= (ErrorFlags.SUCCESS | ErrorFlags.OLDFORMAT);
                return true;
                case Format.Version4:
                int treeAddCount = 0;
                for (uint i = 1; i < DefaultTreeLimit; i++) {
                    if (trees[i].m_flags != 0) {
                        trees[i].m_posY = ReadUShort();
                        treeAddCount++;
                    }
                }
                for (uint i = DefaultTreeLimit; i < MaxTreeLimit; i++) {
                    TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort();
                    m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                    trees[i].m_flags = (ushort)m_flags;
                }
                for (uint i = DefaultTreeLimit; i < MaxTreeLimit; i++) {
                    if (trees[i].m_flags != 0) {
                        trees[i].m_infoIndex = ReadUShort();
                        trees[i].m_posX = ReadShort();
                        trees[i].m_posZ = ReadShort();
                        trees[i].m_posY = ReadUShort();
                        treeCount++;
                    } else {
                        trees[i].m_posY = 0;
                    }
                }
                errorFlags = ErrorFlags.SUCCESS;
                return true;
            }
            return false;
        }

        public byte[] Serialize() {
            int orgTreeCount = 0;
            int extraTreeCount = 0;
            int treeLimit = MaxTreeLimit;
            TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;

            WriteUShort((ushort)Format.Version4); // 2 bytes for version
            WriteInt(treeLimit);                // 4 bytes for limit
            WriteInt(0);         // Reserved for treeCount;    4 bytes
            WriteInt(0);        // treeCount for posY in 0~262144 trees    4 bytes
            WriteUInt(0);        // Reserved for future use    4 bytes
            WriteUShort(0);        // flags.. this is ignore in our version 2 bytes

            /* Apparently, the trees could be located anywhere in the buffer
             * even if there's only 1 tree in the buffer. I'm assuming this is
             * due to performance concerns.
             * So have to save the entire buffer.
             */
            for (uint i = 1; i < DefaultTreeLimit; i++) {
                if (buffer[i].m_flags != 0) {
                    WriteUShort(buffer[i].m_posY);
                    orgTreeCount++;
                }
            }
            for (uint i = DefaultTreeLimit; i < treeLimit; i++) {
                WriteUShort(buffer[i].m_flags);
            }
            for (uint i = DefaultTreeLimit; i < treeLimit; i++) {
                if (buffer[i].m_flags != 0) {
                    WriteUShort(buffer[i].m_infoIndex);
                    WriteShort(buffer[i].m_posX);
                    WriteShort(buffer[i].m_posZ);
                    WriteUShort(buffer[i].m_posY);
                    extraTreeCount++;
                }
            }
            // Set header information now
            WriteIntAt(6, extraTreeCount);
            WriteIntAt(10, orgTreeCount);

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
            byte[] array = new byte[Length];
            Debug.Log($"TreeAnarchy: Created {Length} bytes");
            Buffer.BlockCopy(_buffer, 0, array, 0, (int)Length);
            Debug.Log("TreeAnarchy: Success");
            return array;
        }

        private int FixEndian(int num) {
            int n = ((num & 0xffff) << 16);
            return n | ((num >> 16) & 0xffff);
        }

        private short ReadShort() => (short)(*pBuffer++ | *pBuffer++ << 8);

        private ushort ReadUShort() => (ushort)(*pBuffer++ | *pBuffer++ << 8);

        private int ReadInt() => *pBuffer++ | *pBuffer++ << 8 | *pBuffer++ << 16 | *pBuffer++ << 24;

        private void WriteShort(short val) {
            unchecked {
                *pBuffer++ = (byte)val;
                *pBuffer++ = (byte)(val >> 8);
            }
        }

        private void WriteUShort(ushort val) {
            unchecked {
                *pBuffer++ = (byte)val;
                *pBuffer++ = (byte)(val >> 8);
            }
        }

        private void WriteInt(int val) {
            unchecked {
                *pBuffer++ = (byte)val;
                *pBuffer++ = (byte)(val >> 8);
                *pBuffer++ = (byte)(val >> 16);
                *pBuffer++ = (byte)(val >> 24);
            }
        }

        private void WriteIntAt(int pos, int val) {
            unchecked {
                *(pOrigin + pos++) = (byte)val;
                *(pOrigin + pos++) = (byte)(val >> 8);
                *(pOrigin + pos++) = (byte)(val >> 16);
                *(pOrigin + pos) = (byte)(val >> 24);
            }
        }


        private void WriteUInt(uint val) {
            unchecked {
                *pBuffer++ = (byte)val;
                *pBuffer++ = (byte)(val >> 8);
                *pBuffer++ = (byte)(val >> 16);
                *pBuffer++ = (byte)(val >> 24);
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    if(m_tempBuf != null)
                        m_tempBuf.Dispose();
                }
                gcHandle.Free();
                disposed = true;
            }
        }

        ~TADataSerializer() {
            Dispose(false);
        }
    }
}
