﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class ParsingException : Exception
    {
        public ParsingException(string message) : base(message)
        {
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
