using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class GeneratorException : Exception
    {
        public ASTNode Node { get; private set; }

        public GeneratorException(string message, ASTNode node) : base(message)
        {
            Node = node;
        }
    }
}
