using System;
using System.IO;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy {
    public abstract class TAOldDataSerializer : IDisposable {
        private const int FormatVersion1TreeLimit = 1048576;
        public const string OldTreeUnlimiterKey = @"mabako/unlimiter";

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
            ENCODED = 2,
        }
        [Flags]
        public enum ErrorFlags : ushort {
            NONE = 0,
            SUCCESS = 1,
            FAILED = 2,
            OLDFORMAT = 4,
        }

        private bool disposed = false;
        private readonly MemoryStream m_Stream = null;
        public MemoryStream m_tempBuf = null;

        protected TAOldDataSerializer(byte[] data) {
            m_Stream = new MemoryStream(data);
        }

        public abstract void AfterDeserialize();

        private int DeserializePrefab(ushort index) {
            int count = PrefabCollection<TreeInfo>.PrefabCount();
            if (index > count) {
                // Most likely the tree asset doesn't exist
                return -1;
            }
            try {
                TreeInfo info = PrefabCollection<TreeInfo>.GetPrefab(index);
            } catch {
                return -1;
            }
            return index;
        }

        // These are serial..So we have to think serially
        // a bit long because of all the different formats we have to consider
        // Keeping compatibility almost broke my brain reading the old codes,
        // but was a fun challenge
        public bool Deserialize(TreeInstance[] trees, out ErrorFlags errorFlags) {
            int treeCount = 0;
            int treeLimit;
            int maxLen;
            SaveFlags flags = 0;

            errorFlags = ErrorFlags.NONE;
            Format version = (Format)ReadUShort();
            switch (version) {
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
                    maxLen = treeLimit;
                    goto ReadData;
                    case SaveFlags.PACKED:
                    if (treeCount > MaxTreeLimit - DefaultTreeLimit)
                        maxLen = MaxTreeLimit;
                    else
                        maxLen = DefaultTreeLimit + treeCount; // offset MaxLen from DefaultTreeLimit
ReadData:
                    for (uint i = DefaultTreeLimit; i < maxLen; i++) {
                        TreeInstance.Flags m_flags = (TreeInstance.Flags)ReadUShort();
                        m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                        trees[i].m_flags = (ushort)m_flags;
                        if (trees[i].m_flags != 0) {
                            int infoIndex = DeserializePrefab(ReadUShort());
                            short x = ReadShort();
                            short y = ReadShort();
                            if (infoIndex >= 0) {
                                trees[i].m_infoIndex = (ushort)infoIndex;
                                trees[i].m_posX = x;
                                trees[i].m_posZ = y;
                                trees[i].m_posY = 0;
                            } else {
                                trees[i].m_infoIndex = 0;
                                trees[i].m_posX = 0;
                                trees[i].m_posZ = 0;
                                trees[i].m_posY = 0;
                                trees[i].m_flags = 0;
                            }
                        }
                    }
                    break;
                }
                errorFlags |= (ErrorFlags.SUCCESS | ErrorFlags.OLDFORMAT);
                return true;
            }
            return false;
        }

        private int FixEndian(int num) {
            int n = ((num & 0xffff) << 16);
            return n | ((num >> 16) & 0xffff);
        }

        private short ReadShort() {
            short num = (short)m_Stream.ReadByte();
            return (short)(num |= (short)(m_Stream.ReadByte() << 8));

        }

        private int ReadInt32() {
            int num = m_Stream.ReadByte();
            num |= m_Stream.ReadByte() << 8;
            num |= m_Stream.ReadByte() << 16;
            return num | m_Stream.ReadByte() << 24;
        }

        private ushort ReadUShort() {
            ushort num = (ushort)(m_Stream.ReadByte() & 0xff);
            return (ushort)(num | (m_Stream.ReadByte() << 8));
        }

        public void WriteShort(short value) {
            m_Stream.WriteByte((byte)(value));
            m_Stream.WriteByte((byte)(value >> 8));
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    m_Stream.Dispose();
                    if (m_tempBuf != null)
                        m_tempBuf.Dispose();
                }
                disposed = true;
            }
        }

        ~TAOldDataSerializer() {
            Dispose(false);
        }
    }
}
