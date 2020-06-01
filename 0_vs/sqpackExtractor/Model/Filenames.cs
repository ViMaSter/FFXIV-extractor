using System;
using System.Collections.Generic;

namespace sqpackExtractor.Model
{
    public partial class Filenames
    {
        public long Hash { get; set; }
        public byte[] Name { get; set; }
        public long Used { get; set; }
        public byte[] Archive { get; set; }
        public long? Version { get; set; }
    }
}
