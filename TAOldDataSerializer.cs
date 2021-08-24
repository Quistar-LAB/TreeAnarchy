using ColossalFramework;
using System;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    public abstract class TAOldDataSerializer {
        private const int FormatVersion1TreeLimit = 1048576;
        private const int MaxSupportedTreeLimit = DefaultTreeLimit * 8;
        internal const string OldTreeUnlimiterKey = @"mabako/unlimiter";

        private enum Format : ushort {
            Version1 = 1,
            Version2 = 2,
            Version3 = 3,
        }
        [Flags]
        public enum SaveFlags : ushort {
            NONE = 0,
            PACKED = 1,
            ENCODED = 2,
        }
        private int position = 0;
#pragma warning disable IDE0044 // Add readonly modifier
        private ushort[] ushortStream; /* readonly modifier actually degrades performance */
#pragma warning restore IDE0044 // Add readonly modifier

        protected TAOldDataSerializer(byte[] data) {
            ushortStream = new ushort[data.Length >> 1];
            Buffer.BlockCopy(data, 0, ushortStream, 0, data.Length);
        }

        public abstract void AfterDeserialize();

        // These are serial..So we have to think serially
        // a bit long because of all the different formats we have to consider
        // Keeping compatibility almost broke my brain reading the old codes,
        // but was a fun challenge
        public bool Deserialize() {
            unchecked {
                const ushort fireDamageBurningMask = (ushort)~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                int treeCount = 0;
                int treeLimit;
                int maxLen;
                SaveFlags flags = 0;
                TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;

                switch ((Format)ReadUShort()) {
                case Format.Version1:
                    treeLimit = FormatVersion1TreeLimit;
                    break;
                case Format.Version2:
                    treeLimit = ReadInt();
                    break;
                case Format.Version3:
                    treeLimit = ReadInt();
                    treeCount = ReadInt();
                    ReadInt(); // Reserved for future use
                    ReadInt(); // Reserved for future use
                    flags = (SaveFlags)ReadUShort();
                    break;
                default:
                    return false;
                }
                if (treeLimit <= 0 | treeLimit > MaxSupportedTreeLimit) { return false; } /* Sanity Check */
                if (treeLimit > MaxTreeLimit) treeLimit = MaxTreeLimit; // Only load what MaxTreeLimit is limited to

                switch (flags & SaveFlags.PACKED) {
                case SaveFlags.NONE:
                    maxLen = treeLimit;
                    goto ReadData;
                case SaveFlags.PACKED:
                    if (treeCount > MaxTreeLimit - DefaultTreeLimit) maxLen = MaxTreeLimit;
                    else maxLen = treeCount + DefaultTreeLimit;
ReadData:
                    for (int i = DefaultTreeLimit; i < maxLen; i++) {
                        trees[i].m_flags = (ushort)(ReadUShort() & fireDamageBurningMask);
                        if (trees[i].m_flags != 0) {
                            trees[i].m_infoIndex = ReadUShort();
                            trees[i].m_posX = (short)ReadUShort();
                            trees[i].m_posZ = (short)ReadUShort();
                        }
                        if (position == ushortStream.Length) break;
                    }
                    return true;
                }
                return false;
            }
        }
        private ushort ReadUShort() => ushortStream[position++];
        private int ReadInt() => (ushortStream[position++] << 16) | (ushortStream[position++] & 0xffff);
    }
}
