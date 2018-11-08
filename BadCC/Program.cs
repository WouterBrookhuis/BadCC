using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class Program
    {
        static int Main(string[] args)
        {
            var fileName = "../../program_2";

            if(args.Length == 1)
            {
                fileName = args[0];
                if(fileName.EndsWith(".c"))
                {
                    fileName = Path.GetFullPath(fileName);
                    var extension = Path.GetExtension(fileName);
                    fileName = fileName.Substring(0, fileName.Length - extension.Length);
                }
            }

            var filePath = fileName + ".c";
            var outPath = fileName + ".s";

            int errorcode = 0;

            Queue<Token> tokens = null;
            try
            {
                using(var reader = new StreamReader(filePath))
                {
                    var lexer = new Lexer();
                    tokens = lexer.GetTokens(reader);
                }

                if(tokens != null)
                {
                    Console.WriteLine("Token list:\r\n");
                    foreach(var token in tokens)
                    {
                        Console.WriteLine(token);
                    }


                    var parser = new Parser();
                    var program = parser.ParseProgram(tokens);

                    Console.WriteLine("\r\nProgram AST:\r\n");
                    Console.WriteLine(program.ToString());

                    using(var writer = new StreamWriter(outPath))
                    {
                        var generator = new Generator(writer);
                        generator.GenerateProgram(program);
                    }
                }
            }
            // Catch and log exceptions when running tests
            catch(Exception e) when (args.Length == 1)
            {
                Console.WriteLine(e.ToString());

                errorcode = 1;
            }

            if(args.Length == 0)
            {
                Console.ReadKey();
            }

            return errorcode;
        }
    }
}
