using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace sqpackExtractor
{
    #region PDO
    public class SQPack
    {
        public enum Type : Int32
        {
            SQDB = 0,
            Data = 1,
            Index = 2
        }

        public char[] signature;
        public Int32 headerLength;
        public Int32 unknown;
        public Type type;
        public Int32 unknown2;
        public byte[] hash;

        public SQPack(BinaryReader reader)
        {
            signature = reader.ReadBytes(0x000c).Select(value => (char)value).ToArray();
            headerLength = reader.ReadInt32();
            unknown = reader.ReadInt32();
            type = (Type)reader.ReadInt32();
            unknown2 = reader.ReadInt32();
            reader.ReadBytes(0x3c0 - (0xc + 4 + 4 + 4 + 4));
            hash = reader.ReadBytes(0x0008);
            reader.ReadBytes(0x38);
        }
    }
    
    public class SegmentHeader
    {
        public Int32 headerLength;

        public SegmentHeader(BinaryReader reader)
        {
            headerLength = reader.ReadInt32();
        }
    }

    public class Segment
    {
        public Int32 numDatsUnknown;
        public Int32 segmentOffset;
        public Int32 segmentSize;
        public byte[] hash;

        public Segment(BinaryReader reader)
        {
            numDatsUnknown = reader.ReadInt32();
            segmentOffset = reader.ReadInt32();
            segmentSize = reader.ReadInt32();
            hash = reader.ReadBytes(0x0014);
        }
    }

    public class FileSegment
    {
        public Int32 idHash1;
        public Int32 idHash2 = -1;
        public Int32 dateOffset;

        public FileSegment(BinaryReader reader, bool isIndex2)
        {
            idHash1 = reader.ReadInt32();
            if (!isIndex2)
            {
                idHash2 = reader.ReadInt32();
            }
            dateOffset = reader.ReadInt32();
            
            if (isIndex2)
            {
                reader.ReadInt32();
            }
            reader.ReadInt32();
        }
    }

    public class FolderSegment
    {
        public Int32 idHash;
        public Int32 filesIndexOffset;
        public Int32 folderSize;
        public Int32 padding;

        public Int32 fileCount => folderSize / 0x10;

        private bool _isIndex2 = false;
        public bool IsIndex2 => _isIndex2;

        public FolderSegment() { }

        public FolderSegment(BinaryReader reader)
        {
            idHash = reader.ReadInt32();
            filesIndexOffset = reader.ReadInt32();
            folderSize = reader.ReadInt32();
            padding = reader.ReadInt32();
        }

        public void SetIndex2()
        {
            _isIndex2 = true;
        }

        public List<FileSegment> ReadFiles(BinaryReader reader)
        {
            reader.BaseStream.Seek(this.filesIndexOffset, SeekOrigin.Begin);

            List<FileSegment> files = new List<FileSegment>();
            for (int i = 0; i < fileCount; i++)
            {
                files.Add(new FileSegment(reader, IsIndex2));
            }

            return files;
        }
    }
    #endregion

    class Program
    {
        public static T ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        static void Main(string[] args)
        {
            string pathToIndex = "C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\sqpack\\ffxiv\\0a0000.win32.index";
            bool isIndex2 = pathToIndex.Contains("index2");
            List<FolderSegment> folderSegments = new List<FolderSegment>();
            using (BinaryReader b = new BinaryReader(File.Open(pathToIndex, FileMode.Open)))
            {
                var a = new SQPack(b);
                var c = new SegmentHeader(b);
                var files = new Segment(b);
                b.ReadBytes(0x4);
                b.ReadBytes(0x28);
                var segment1 = new Segment(b);
                b.ReadBytes(0x28);
                var segment2 = new Segment(b);
                b.ReadBytes(0x28);
                var folders = new Segment(b);

                if (folders.segmentOffset != 0)
                {
                    b.BaseStream.Seek(folders.segmentOffset, SeekOrigin.Begin);
                    int folderCount = folders.segmentSize / 0x10;
                    for (int i = 0; i < folderCount; i++)
                    {
                        folderSegments.Add(new FolderSegment(b));
                    }
                }

                FolderSegment filesSegment = new FolderSegment()
                {
                    idHash = 0,
                    folderSize = (isIndex2 ? 2 : 1) * files.segmentSize,
                    filesIndexOffset = files.segmentOffset
                };

                if (isIndex2)
                {
                    filesSegment.SetIndex2();
                }

                folderSegments.Add(filesSegment);

                Dictionary<FolderSegment, List<FileSegment>> filesByFolder = folderSegments.ToDictionary(item => item, item => item.ReadFiles(b));
            }
        }
    }
}
