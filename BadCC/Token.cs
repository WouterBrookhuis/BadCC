using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    abstract class Token
    {
    }

    /// <summary>
    /// Token that has a fixed key value such as "(", "{", "if", "return", etc.
    /// </summary>
    class FixedToken : Token
    {
        public enum Kind
        {
            ParOpen,
            ParClose,
            BracketOpen,
            BracketClose,
            SemiColon,
            Int,
            Return,
            Negate,
            Complement,
            LogicNegate,
            Add,
            Multiply,
            Divide,
            LogicAnd,
            LogicOr,
            Equal,
            NotEqual,
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual,
            //BinaryOr,
            //BinaryAnd,
            //BinaryXor,
            Assignment,
            If,
            Else,
            Conditional,
            Colon,
            For,
            While,
            Do,
            Break,
            Continue,
            Modulo,
            Comma,
            Increment,
            Decrement,
        }

        static private readonly BiMap<Kind, string> map = new BiMap<Kind, string>()
        {
            // TODO: Order here is VITAL for parsing correctness, ensure everything is in the correct order
            { Kind.Increment, "++" },
            { Kind.Decrement, "--" },
            { Kind.ParOpen, "(" },
            { Kind.ParClose, ")" },
            { Kind.BracketOpen, "{" },
            { Kind.BracketClose, "}" },
            { Kind.SemiColon, ";" },
            { Kind.Int, "int" },
            { Kind.Return, "return" },
            { Kind.Negate, "-" },
            { Kind.Complement, "~" },
            { Kind.Add, "+" },
            { Kind.Multiply, "*" },
            { Kind.Divide, "/" },
            { Kind.Equal, "==" },
            { Kind.NotEqual, "!=" },
            { Kind.LessThanOrEqual, "<=" },
            { Kind.LessThan, "<" },
            { Kind.GreaterThanOrEqual, ">=" },
            { Kind.GreaterThan, ">" },
            { Kind.LogicAnd, "&&" },
            { Kind.LogicOr, "||" },
            { Kind.LogicNegate, "!" },
            //{ Kind.BinaryOr, "|" },
            //{ Kind.BinaryAnd, "&" },
            //{ Kind.BinaryXor, "^" },
            { Kind.Assignment, "=" },
            { Kind.If, "if" },
            { Kind.Else, "else" },
            { Kind.Conditional, "?" },
            { Kind.Colon, ":" },
            { Kind.For, "for" },
            { Kind.While, "while" },
            { Kind.Do, "do" },
            { Kind.Break, "break" },
            { Kind.Continue, "continue" },
            { Kind.Modulo, "%" },
            { Kind.Comma, "," },
        };

        public Kind TokenKind { get; private set; }

        public FixedToken(Kind kind)
        {
            TokenKind = kind;
        }

        public static Token TryMakeToken(string str)
        {
            if(map.TryGetValueSecond(str, out Kind kind))
            {
                return new FixedToken(kind);
            }
            return null;
        }

        public override string ToString()
        {
            if(map.TryGetValueFirst(TokenKind, out string name))
            {
                return "Fixed <" + name + ">";
            }
            return "Fixed <UNKNOWN TYPE>";
        }

        public bool IsUnaryOp()
        {
            return (TokenKind == Kind.LogicNegate ||
                    TokenKind == Kind.Negate ||
                    TokenKind == Kind.Complement);
        }

        public static string GetRegexPattern()
        {
            var sb = new StringBuilder();
            foreach(var kv in map)
            {
                if(sb.Length > 0)
                {
                    sb.Append("|");
                }
                var str = kv.Value;
                // ESCAPE return with return\b !!
                // TODO: Have less nasty methods? idk, this works
                str = System.Text.RegularExpressions.Regex.Escape(str);
                if(str.Length > 1 && char.IsLetterOrDigit(str.Last()))
                {
                    str += @"\b";
                }
                //Console.WriteLine("{0} {1}", kv.Value, str);
                sb.Append(str);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Token for positive integer literals
    /// </summary>
    class LiteralIntToken : Token
    {
        public int Value { get; private set; }

        public LiteralIntToken(int value)
        {
            if(value < 0)
            {
                throw new ArgumentOutOfRangeException("value", "Value was less than 0");
            }
            Value = value;
        }

        public static Token TryMakeToken(string str)
        {
            if(int.TryParse(str, out int value))
            {
                return new LiteralIntToken(value);
            }
            return null;
        }

        public override string ToString()
        {
            return "Int <" + Value + ">";
        }
    }

    class IdentifierToken : Token
    {
        public string Name { get; private set; }

        public IdentifierToken(string name)
        {
            Name = name;
        }

        public static Token TryMakeToken(string str)
        {
            const string pattern = "[a-zA-z]\\w*";

            if(System.Text.RegularExpressions.Regex.IsMatch(str, pattern))
            {
                return new IdentifierToken(str);
            }
            return null;
        }

        public override string ToString()
        {
            return "Identifier <" + Name + ">";
        }
    }
}
