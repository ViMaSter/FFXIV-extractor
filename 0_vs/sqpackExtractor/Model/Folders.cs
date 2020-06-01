using System;
using System.Collections.Generic;

namespace sqpackExtractor.Model
{
    public partial class Folders
    {
        public long Hash { get; set; }
        public byte[] Path { get; set; }
        public long Used { get; set; }
        public byte[] Archive { get; set; }
        public long? Version { get; set; }
    }
}
