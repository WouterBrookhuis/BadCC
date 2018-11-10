using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class Parser
    {
        private static readonly ConstantNode s_constZeroNode = new ConstantNode(0);
        private static readonly ConstantNode s_constOneNode = new ConstantNode(1);

        private Queue<Token> tokens;
        private Dictionary<string, FunctionNode> functionDeclarations;
        private Dictionary<string, FunctionNode> functionDefinitions;

        /// <summary>
        /// Takes a fixed token from the queue, throws an exception if it can't.
        /// </summary>
        /// <param name="tokens">The token queue</param>
        /// <param name="kind">The kind of token to dequeue</param>
        /// <exception cref="UnexpectedTokenException">Thrown when the given token kind is not first in the queue</exception>
        private void TakeFixedToken(FixedToken.Kind kind)
        {
            var typeToken = tokens.Dequeue() as FixedToken;
            if(typeToken == null || typeToken.TokenKind != kind)
            {
                throw new UnexpectedTokenException("Did not get token of kind " + kind.ToString(), typeToken);
            }
        }

        /// <summary>
        /// Tries to take a fixed token from the queue and returns if it could. Does not change the queue if the given token kind is not on top.
        /// </summary>
        /// <param name="tokens">Queue of tokens</param>
        /// <param name="kind">The kind of token we want to dequeue</param>
        /// <returns>True if the token was dequeued, false if it wasn't</returns>
        private bool TryTakeFixedToken(FixedToken.Kind kind)
        {
            var typeToken = tokens.Peek() as FixedToken;
            if(typeToken == null || typeToken.TokenKind != kind)
            {
                return false;
            }
            tokens.Dequeue();
            return true;
        }

        /// <summary>
        /// Peeks the next token in the queue and checks if it's a fixed token of the given kind.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="kind"></param>
        /// <returns></returns>
        private bool PeekFixedToken(FixedToken.Kind kind)
        {
            var typeToken = tokens.Peek() as FixedToken;
            if(typeToken == null || typeToken.TokenKind != kind)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Takes an identifier token from the queue, throws exception if it can't.
        /// </summary>
        /// <returns>IdentifierToken that was dequeued</returns>
        /// <exception cref="UnexpectedTokenException">Thrown if the first token in the queue is not an IdentifierToken</exception>
        private IdentifierToken TakeIdentifierToken()
        {
            var token = tokens.Dequeue() as IdentifierToken;
            if(token == null)
            {
                throw new UnexpectedTokenException("Did not get IdentifierToken", token);
            }
            return token;
        }

        public ProgramNode ParseProgram(Queue<Token> tokens)
        {
            this.tokens = tokens;
            functionDeclarations = new Dictionary<string, FunctionNode>();
            functionDefinitions = new Dictionary<string, FunctionNode>();

            var functions = new List<FunctionNode>();
            while(tokens.Count > 0)
            {
                functions.Add(ParseFunction());
            }

            tokens = null;
            return new ProgramNode(functions);
        }

        private FunctionNode ParseFunction()
        {
            // Return type of the function
            var typeToken = tokens.Dequeue() as FixedToken;
            if(typeToken == null || typeToken.TokenKind != FixedToken.Kind.Int)
            {
                throw new UnexpectedTokenException("Did not get Int token", typeToken);
            }

            // Identifier of the function
            var nameToken = TakeIdentifierToken();

            // Opening parenthesis
            TakeFixedToken(FixedToken.Kind.ParOpen);
            // Parse parameters until we hit and take the closing parenthesis
            var parameters = new List<string>();
            while(!TryTakeFixedToken(FixedToken.Kind.ParClose))
            {
                // TODO: Other types...
                if(parameters.Count > 0)
                {
                    // Not the first parameter, so we should have a comma
                    TakeFixedToken(FixedToken.Kind.Comma);
                }
                TakeFixedToken(FixedToken.Kind.Int);        // int
                var name = TakeIdentifierToken().Name;      // name
                if(parameters.Contains(name))
                {
                    throw new ParsingException(string.Format("Duplicate parameter name {0} in function {1}", name, nameToken.Name));
                }
                parameters.Add(name);
            }

            // We should get either a semi or a opening bracket
            if (TryTakeFixedToken(FixedToken.Kind.SemiColon))
            {
                // This is just a declaration
                var node = new FunctionNode(nameToken.Name, parameters, null);
                if(functionDeclarations.ContainsKey(nameToken.Name) &&
                    functionDeclarations[nameToken.Name].Parameters.Count != parameters.Count)
                {
                    throw new ParsingException("Conflicting function declarations for " + nameToken.Name);
                }
                functionDeclarations[node.Name] = node;
                return node;
            }
            else
            {
                // This should be a definition, but register declaration first to allow recursion
                var tmpDefNode = new FunctionNode(nameToken.Name, parameters, null);
                if (functionDeclarations.ContainsKey(nameToken.Name) &&
                    functionDeclarations[nameToken.Name].Parameters.Count != parameters.Count)
                {
                    throw new ParsingException("Conflicting function declarations for " + nameToken.Name);
                }
                functionDeclarations[tmpDefNode.Name] = tmpDefNode;

                // Opening bracket
                TakeFixedToken(FixedToken.Kind.BracketOpen);

                // Parse statements until we find (and take!) a closing bracket after one
                var blockItems = new List<BlockItemNode>();
                while (!TryTakeFixedToken(FixedToken.Kind.BracketClose))
                {
                    var statement = ParseBlockItem();
                    blockItems.Add(statement);
                };

                // Check if we are missing a return statement. If so add a return 0;
                // TODO: Update this to handle void and other return types when we get to it
                if (blockItems.Count == 0 || !(blockItems[blockItems.Count - 1] is ReturnNode))
                {
                    blockItems.Add(new ReturnNode(new ConstantNode(0)));
                }

                // Create definition node
                var node = new FunctionNode(nameToken.Name, parameters, blockItems);
                if (functionDeclarations.ContainsKey(nameToken.Name) &&
                    functionDeclarations[nameToken.Name].Parameters.Count != parameters.Count)
                {
                    throw new ParsingException("Conflicting function declarations for " + nameToken.Name);
                }
                functionDeclarations[node.Name] = node;
                if(functionDefinitions.ContainsKey(node.Name))
                {
                    throw new ParsingException("More than 1 function definition for" + nameToken.Name);
                }
                functionDefinitions.Add(node.Name, node);
                return node;
            }
        }

        private BlockItemNode ParseBlockItem()
        {
            // TODO: Multiple types
            if(PeekFixedToken(FixedToken.Kind.Int))
            {
                return ParseDeclaration();
            }

            // It's a statement
            return ParseStatement();
        }

        private DeclareNode ParseDeclaration()
        {
            // Integer variable declaration
            if(TryTakeFixedToken(FixedToken.Kind.Int))
            {
                // Name of the variable
                var idToken = TakeIdentifierToken();

                // Check for assignment (=) to see if there is an expression here
                ExpressionNode expression = null;
                if(TryTakeFixedToken(FixedToken.Kind.Assignment))
                {
                    // The initialization expression
                    expression = ParseExpression();
                }
                else
                {
                    // Default initializer
                    expression = s_constZeroNode;
                }

                // Semi colon
                TakeFixedToken(FixedToken.Kind.SemiColon);

                return new DeclareNode(idToken.Name, expression);
            }
            throw new UnexpectedTokenException("Expected int token", tokens.Peek());
        }

        private StatementNode ParseStatement()
        {
            // Return statement
            if(TryTakeFixedToken(FixedToken.Kind.Return))
            {
                // Expression to return
                var expression = ParseExpression();

                // Semi colon
                TakeFixedToken(FixedToken.Kind.SemiColon);

                return new ReturnNode(expression);
            }
            // If statement
            else if(TryTakeFixedToken(FixedToken.Kind.If))
            {
                // ( expr )
                TakeFixedToken(FixedToken.Kind.ParOpen);
                var conditional = ParseExpression();
                TakeFixedToken(FixedToken.Kind.ParClose);

                // True statement
                var trueStatement = ParseStatement();

                // Optional else + statement
                StatementNode falseStatement = null;
                if(TryTakeFixedToken(FixedToken.Kind.Else))
                {
                    falseStatement = ParseStatement();
                }

                return new IfStatmentNode(conditional, trueStatement, falseStatement);
            }
            // Block statement
            else if(TryTakeFixedToken(FixedToken.Kind.BracketOpen))
            {
                // Parse statements until we find (and take!) a closing bracket after one
                var blockItems = new List<BlockItemNode>();
                while(!TryTakeFixedToken(FixedToken.Kind.BracketClose))
                {
                    var statement = ParseBlockItem();
                    blockItems.Add(statement);
                };

                return new BlockStatementNode(blockItems);
            }
            // Break and continue
            else if(TryTakeFixedToken(FixedToken.Kind.Break))
            {
                TakeFixedToken(FixedToken.Kind.SemiColon);
                return new BreakStatement();
            }
            else if(TryTakeFixedToken(FixedToken.Kind.Continue))
            {
                TakeFixedToken(FixedToken.Kind.SemiColon);
                return new ContinueStatement();
            }
            // While statement
            else if(TryTakeFixedToken(FixedToken.Kind.While))
            {
                // while ( expr ) statement
                TakeFixedToken(FixedToken.Kind.ParOpen);
                var expr = ParseExpression();
                TakeFixedToken(FixedToken.Kind.ParClose);
                var statement = ParseStatement();
                return new WhileStatement(expr, statement);
            }
            // Do while statement
            else if(TryTakeFixedToken(FixedToken.Kind.Do))
            {
                // do statement while expr ;
                var statement = ParseStatement();
                TakeFixedToken(FixedToken.Kind.While);
                var expr = ParseExpression();
                TakeFixedToken(FixedToken.Kind.SemiColon);
                return new DoWhileStatement(expr, statement);
            }
            // For statement
            else if(TryTakeFixedToken(FixedToken.Kind.For))
            {
                // We have two options
                // for ( decl opt-exp ; opt-exp ) statement
                // for ( opt-exp ; opt-exp ; opt-exp ) statement
                DeclareNode initialDeclare = null;
                ExpressionNode initialExpr = null;
                TakeFixedToken(FixedToken.Kind.ParOpen);
                // TODO: Support for other types
                if(PeekFixedToken(FixedToken.Kind.Int))
                {
                    // decl
                    initialDeclare = ParseDeclaration();
                }
                else
                {
                    // opt-exp ;
                    initialExpr = ParseOptionalExpression();
                    TakeFixedToken(FixedToken.Kind.SemiColon);
                }

                var conditionExpr = ParseOptionalExpression();
                TakeFixedToken(FixedToken.Kind.SemiColon);
                var iterationExpr = ParseOptionalExpression();
                TakeFixedToken(FixedToken.Kind.ParClose);
                var statement = ParseStatement();

                // Condition expression must be replaced with a non-zero constant if it's missing
                if(conditionExpr == null)
                {
                    conditionExpr = s_constOneNode;
                }

                if(initialDeclare != null)
                {
                    return new ForStatement(initialDeclare, conditionExpr, iterationExpr, statement);
                }
                return new ForStatement(initialExpr, conditionExpr, iterationExpr, statement);
            }

            // Treat it as an expression statement
            return ParseExpressionStatement();
        }

        private ExpressionStatementNode ParseExpressionStatement()
        {
            // Check for empty statement
            if(TryTakeFixedToken(FixedToken.Kind.SemiColon))
            {
                // TODO: It might be worth filtering these out later
                return new ExpressionStatementNode(null);
            }
            // expr ;
            var expr = ParseExpression();
            TakeFixedToken(FixedToken.Kind.SemiColon);
            return new ExpressionStatementNode(expr);
        }

        /// <summary>
        /// Parses an optional expression followed by a ; or ) BUT NOT the ; or ) at the end.
        /// Returns null of no expression is found.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>Null if no expression was found, the expression node otherwise.</returns>
        private ExpressionNode ParseOptionalExpression()
        {
            // [expr] ; | [expr] )
            if(PeekFixedToken(FixedToken.Kind.SemiColon)
                || PeekFixedToken(FixedToken.Kind.ParClose))
            {
                // No expression found
                return null;
            }

            return ParseExpression();
        }

        private ExpressionNode ParseExpression()
        {
            // Assignment or variable expression
            if(tokens.Peek() is IdentifierToken idToken)
            {
                // = token?
                var token = tokens.ElementAt(1);
                if(token is FixedToken equalsToken && equalsToken.TokenKind == FixedToken.Kind.Assignment)
                {
                    // Assignment expression, remove both tokens
                    tokens.Dequeue();
                    tokens.Dequeue();

                    // Expression
                    var expression = ParseExpression();

                    return new AssignmentNode(idToken.Name, expression);
                }

                // Variable expression probably, fall trough
            }

            // Conditional expression
            return ParseConditionalExpression();
        }

        private ExpressionNode ParseConditionalExpression()
        {
            ExpressionNode expr = ParseLogicalOrExpression();

            if(TryTakeFixedToken(FixedToken.Kind.Conditional))
            {
                var trueExpression = ParseExpression();
                TakeFixedToken(FixedToken.Kind.Colon);
                var falseExpression = ParseConditionalExpression();
                return new ConditionalNode(expr, trueExpression, falseExpression);
            }

            return expr;
        }

        private ExpressionNode ParseLogicalOrExpression()
        {
            ExpressionNode expr = ParseLogicalAndExpression();

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                nextToken.TokenKind == FixedToken.Kind.LogicOr)  // Check if there is a ||
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseLogicalAndExpression();
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseLogicalAndExpression()
        {
            ExpressionNode expr = ParseEqualityExpression();

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                nextToken.TokenKind == FixedToken.Kind.LogicAnd)  // Check if there is a &&
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseEqualityExpression();
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseEqualityExpression()
        {
            ExpressionNode expr = ParseRelationalExpression();

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                (nextToken.TokenKind == FixedToken.Kind.NotEqual ||
                nextToken.TokenKind == FixedToken.Kind.Equal))  // Check if there is a == or !=
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseRelationalExpression();
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseRelationalExpression()
        {
            ExpressionNode expr = ParseAdditiveExpression();

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                (nextToken.TokenKind == FixedToken.Kind.LessThan ||
                nextToken.TokenKind == FixedToken.Kind.LessThanOrEqual ||
                nextToken.TokenKind == FixedToken.Kind.GreaterThan ||
                nextToken.TokenKind == FixedToken.Kind.GreaterThanOrEqual))  // Check if there is a <, >, <= or => after this
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseAdditiveExpression();
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseAdditiveExpression()
        {
            ExpressionNode term = ParseTerm();

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                (nextToken.TokenKind == FixedToken.Kind.Add || nextToken.TokenKind == FixedToken.Kind.Negate))  // Check if there is a + or - after this
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the + or - token

                var nextTerm = ParseTerm();
                term = new BinaryNode(nextToken.TokenKind, term, nextTerm);

                nextToken = tokens.Peek() as FixedToken;
            }

            return term;
        }

        private ExpressionNode ParseTerm()
        {
            ExpressionNode factor = ParseFactor();

            var nextToken = tokens.Peek() as FixedToken;  
            while(nextToken != null && 
                (nextToken.TokenKind == FixedToken.Kind.Multiply ||
                nextToken.TokenKind == FixedToken.Kind.Divide || 
                nextToken.TokenKind == FixedToken.Kind.Modulo))  // Check if there is a * or / after this
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the * or / token

                var nextFactor = ParseFactor();
                factor = new BinaryNode(nextToken.TokenKind, factor, nextFactor);

                nextToken = tokens.Peek() as FixedToken;
            }

            return factor;
        }

        private ExpressionNode ParseFactor()
        {
            var token = tokens.Dequeue();
            if(token is LiteralIntToken literalIntToken)
            {
                // Constant int
                return new ConstantNode(literalIntToken.Value);
            }
            else if(token is FixedToken fixedToken)
            {
                // Unary op
                if(fixedToken.IsUnaryOp())
                {
                    var expression = ParseFactor();
                    return new UnaryNode(fixedToken.TokenKind, expression);
                }
                else if(fixedToken.TokenKind == FixedToken.Kind.ParOpen)
                {
                    var expression = ParseExpression();
                    token = tokens.Dequeue();
                    if(token is FixedToken closingToken && closingToken.TokenKind == FixedToken.Kind.ParClose)
                    {
                        return expression;
                    }
                }
            }
            else if(token is IdentifierToken idToken)
            {
                // Variable reference or function call
                if(TryTakeFixedToken(FixedToken.Kind.ParOpen))
                {
                    // Function call: id ( [expr][,expr] )
                    var expressions = new List<ExpressionNode>();
                    while(!TryTakeFixedToken(FixedToken.Kind.ParClose))
                    {
                        if(expressions.Count > 0)
                        {
                            TakeFixedToken(FixedToken.Kind.Comma);
                        }
                        expressions.Add(ParseExpression());
                    }
                    if(functionDeclarations.TryGetValue(idToken.Name, out FunctionNode calledNode))
                    {
                        if(calledNode.Parameters.Count != expressions.Count)
                        {
                            throw new ParsingException(string.Format("Function {0} does not take {1} arguments", idToken.Name, expressions.Count));
                        }
                    }
                    else
                    {
                        throw new ParsingException("Missing function declaration for " + idToken.Name);
                    }
                    return new CallNode(idToken.Name, expressions);
                }

                return new VariableNode(idToken.Name);
            }

            throw new UnexpectedTokenException("Did not get a valid start of expression token", token);
        }
    }
}
