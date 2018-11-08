using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class UnexpectedTokenException : Exception
    {
        public Token Token { get; private set; }

        public UnexpectedTokenException(string message, Token foundToken) : base(message)
        {
            Token = foundToken;
        }

        public override string ToString()
        {
            return Message + "\r\n" + "Found Token " + Token;
        }
    }
}
