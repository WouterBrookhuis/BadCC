using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    interface IGenerator
    {
        void SetWriter(StreamWriter writer);
        void GenerateProgram(ProgramNode program);
    }
}
