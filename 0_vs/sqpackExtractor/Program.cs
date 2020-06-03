using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
        public Type type;
        public byte[] hash;

        public SQPack(BinaryReader reader)
        {
            signature = reader.ReadBytes(0x000c).Select(value => (char)value).ToArray();
            headerLength = reader.ReadInt32();
            reader.ReadInt32();
            type = (Type)reader.ReadInt32();
            reader.ReadInt32();
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
        public Int32 segmentOffset;
        public Int32 segmentSize;
        public byte[] hash;

        public Segment(BinaryReader reader)
        {
            reader.ReadInt32();
            segmentOffset = reader.ReadInt32();
            segmentSize = reader.ReadInt32();
            hash = reader.ReadBytes(0x0014);
        }
    }

    public class FileSegment
    {
        public Int32 idHash1;
        public Int32 idHash2 = -1;
        public Int32 dataOffset;

        public FileSegment(BinaryReader reader, bool isIndex2)
        {
            idHash1 = reader.ReadInt32();
            if (!isIndex2)
            {
                idHash2 = reader.ReadInt32();
            }
            dataOffset = reader.ReadInt32();
            
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

        public Int32 fileCount => folderSize / 0x10;

        private bool _isIndex2 = false;
        public bool IsIndex2 => _isIndex2;

        public FolderSegment() { }

        public FolderSegment(BinaryReader reader)
        {
            idHash = reader.ReadInt32();
            filesIndexOffset = reader.ReadInt32();
            folderSize = reader.ReadInt32();
            reader.ReadInt32();
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

    public class DataHeader
    {
        public Int32 headerLength;
        public Int32 dataSize;
        public Int32 spannedDAT;
        public string DATFile
        {
            get => ".dat" + (spannedDAT - 1);
        }
        public Int32 maxFileSize;
        public Int32 sha1Data;
        public Int32 sha1Header;

        public DataHeader(BinaryReader reader)
        {
            headerLength = reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            dataSize = reader.ReadInt32();
            spannedDAT = reader.ReadInt32();
            reader.ReadInt32();
            maxFileSize = reader.ReadInt32();
            reader.ReadInt32();
            sha1Data = reader.ReadInt32();
            sha1Header = reader.ReadInt32();
            reader.ReadBytes(0x3E4);
        }
    }

    public class DataEntryHeader
    {
        public Int32 headerLength;
        public enum ContentType : Int32
        {
            Binary = 2,
            Model = 3,
            Texture = 4
        }
        public ContentType contentType;
        public Int32 uncompressedSize;
        public Int32 blockBufferSize;
        public Int32 numBlocks;

        public DataEntryHeader(BinaryReader reader)
        {
            headerLength = reader.ReadInt32();
            contentType = (ContentType)reader.ReadInt32();
            uncompressedSize = reader.ReadInt32();
            reader.ReadInt32();
            blockBufferSize = reader.ReadInt32();
            numBlocks = reader.ReadInt32();
        }
    }

    public class BlockTable { }

    public class BinaryBlockTable : BlockTable
    {
        public Int32 offset;
        public short blockSize;
        public short decompressedDataSize;

        public BinaryBlockTable(BinaryReader reader)
        {
            offset = reader.ReadInt32();
            blockSize = reader.ReadInt16();
            decompressedDataSize = reader.ReadInt16();
        }
    }

    public class BlockHeader
    {
        public Int32 headerSize;
        public Int32 compressedLength;
        public Int32 decompressedLength;

        public BlockHeader(BinaryReader reader)
        {
            headerSize = reader.ReadInt32();
            reader.ReadInt32();
            compressedLength = reader.ReadInt32();
            decompressedLength = reader.ReadInt32();
        }

        public static uint SwapBytes(uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public byte[] Read(BinaryReader reader)
        {
            if (compressedLength == 32000)
            {
                return reader.ReadBytes(decompressedLength);
            }
            else
            {
                var compressedContent = reader.ReadBytes(compressedLength);


                byte[] gzipedData = new byte[compressedLength + 6];
                                                                 
                gzipedData[0] = (byte)0x78;
                gzipedData[1] = (byte)0x9C;

                // Actual Data
                compressedContent.CopyTo(gzipedData, 2);

                var thr = new ICSharpCode.SharpZipLib.Checksum.Adler32();
                thr.Update(compressedContent);
                
                BitConverter.GetBytes(BitConverter.IsLittleEndian ? (int)SwapBytes((uint)thr.Value) : (int)thr.Value).CopyTo(gzipedData, 2 + compressedLength);

                byte[] decompressedData = new byte[10000];
                int decompressedLength = 0;

                using (MemoryStream memory = new MemoryStream(gzipedData))
                using (ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream inflater = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(memory))
                    decompressedLength = inflater.Read(decompressedData, 0, decompressedData.Length);

                var f = 2;
                return decompressedData;
            }
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

            FileSegment completionSegment;

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

                List<FolderSegment> folderSegments = new List<FolderSegment>();


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

                var context = new Model.hashlistContext();
                var filenameMapping = context.Filenames.Where(row => row.Name != null).ToDictionary(row => UTF8Encoding.UTF8.GetString(row.Name), row => row);

                Dictionary<FolderSegment, List<FileSegment>> filesByFolder = folderSegments.ToDictionary(item => item, item => item.ReadFiles(b));

                completionSegment = filesByFolder.Values.SelectMany(a => a).First(item => item.idHash1 == filenameMapping["completion_100_en.exd"].Hash);
            }
            using (BinaryReader b = new BinaryReader(File.Open(pathToIndex.Replace(".index", ".dat0"), FileMode.Open)))
            {
                var a = new SQPack(b);
                var c = new DataHeader(b);
                b.BaseStream.Seek(completionSegment.dataOffset*0x08, SeekOrigin.Begin);
                long startPointer = b.BaseStream.Position;
                var d = new DataEntryHeader(b);
                BlockTable[] blockTables = new BlockTable[d.numBlocks];
                for (int i = 0; i < d.numBlocks; ++i)
                {
                    switch (d.contentType)
                    {
                        case DataEntryHeader.ContentType.Binary:
                            blockTables[i] = new BinaryBlockTable(b);
                            break;
                    }
                }
                long offsetRemaining = 0x80 - (b.BaseStream.Position - startPointer);
                b.ReadBytes((int)offsetRemaining);
                startPointer = b.BaseStream.Position;
                var f = new BlockHeader(b);
                byte[] g = f.Read(b);

                var e = 3;
            }
        }
    }
}
