using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BadCC
{
    class Lexer
    {
        private Dictionary<string, Type> regexes;

        public Lexer()
        {
            var fixedPattern = FixedToken.GetRegexPattern();
            regexes = new Dictionary<string, Type>
            {
                { fixedPattern, typeof(FixedToken) },
                { @"([a-zA-Z]\w*)", typeof(IdentifierToken) },
                { @"(\d+)", typeof(LiteralIntToken) },
            };
        }

        public Queue<Token> GetTokens(StreamReader reader)
        {
            int charValue;
            StringBuilder stringBuilder = new StringBuilder();
            var tokens = new Queue<Token>();

            // Loop until we run out of text, make sure we empty the string builder as well!
            while((charValue = reader.Read()) != -1 || stringBuilder.Length > 0)
            {
                char readChar = (char)charValue;

                if(charValue == -1 || char.IsWhiteSpace(readChar))
                {
                    var str = stringBuilder.ToString();
                    stringBuilder.Clear();

                    if(str.Length == 0)
                    {
                        continue;
                    }

                    int strIdx = 0;
                    int tokenizedChars = 0;
                    bool foundAnything = false;
                    do
                    {
                        foundAnything = false;

                        for(int i = 0; i < regexes.Count; i++)
                        {
                            var kv = regexes.ElementAt(i);

                            var regex = new Regex(kv.Key);
                            var match = regex.Match(str, strIdx);
                            while(match.Success && match.Index == strIdx)
                            {
                                strIdx = match.Index + match.Length;
                                tokenizedChars += match.Length;
                                foundAnything = true;

                                int groupIdx = match.Groups.Count - 1;
                                var method = kv.Value.GetMethod("TryMakeToken", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                                if(method == null)
                                {
                                    throw new Exception("Missing static method TryMakeToken on " + kv.Value.FullName);
                                }
                                if(method.Invoke(null, new string[] { match.Value }) is Token result)
                                {
                                    tokens.Enqueue(result);
                                }
                                else
                                {
                                    throw new Exception("Regex matched but token could not be created");
                                }

                                match = match.NextMatch();
                            }
                        }
                    } while(foundAnything);

                    if(tokenizedChars != str.Length)
                    {
                        throw new Exception("Unexpected character in " + str);
                    }
                   
                }
                else
                {
                    stringBuilder.Append(readChar);
                }
            }

            return tokens;
        }
    }
}
