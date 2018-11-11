using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class ARCGenerator : AbstractGenerator
    {
        public ARCGenerator(StreamWriter writer) : base(writer)
        {
        }

        public override void GenerateProgram(ProgramNode program)
        {
            throw new NotImplementedException();
        }
    }
}
