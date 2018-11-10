using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class Parser
    {
        /// <summary>
        /// Takes a fixed token from the queue, throws an exception if it can't.
        /// </summary>
        /// <param name="tokens">The token queue</param>
        /// <param name="kind">The kind of token to dequeue</param>
        /// <exception cref="UnexpectedTokenException">Thrown when the given token kind is not first in the queue</exception>
        private void TakeFixedToken(Queue<Token> tokens, FixedToken.Kind kind)
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
        private bool TryTakeFixedToken(Queue<Token> tokens, FixedToken.Kind kind)
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
        /// Takes an identifier token from the queue, throws exception if it can't.
        /// </summary>
        /// <param name="tokens">The queue of tokens</param>
        /// <returns>IdentifierToken that was dequeued</returns>
        /// <exception cref="UnexpectedTokenException">Thrown if the first token in the queue is not an IdentifierToken</exception>
        private IdentifierToken TakeIdentifierToken(Queue<Token> tokens)
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
            var function = ParseFunction(tokens);
            if(tokens.Count > 0)
            {
                throw new UnexpectedTokenException("Reached end of program but found a token", tokens.Peek());
            }
            return new ProgramNode(function);
        }

        private FunctionNode ParseFunction(Queue<Token> tokens)
        {
            // Return type of the function
            var typeToken = tokens.Dequeue() as FixedToken;
            if(typeToken == null || typeToken.TokenKind != FixedToken.Kind.Int)
            {
                throw new UnexpectedTokenException("Did not get Int token", typeToken);
            }

            // Identifier of the function
            var nameToken = TakeIdentifierToken(tokens);

            // Opening parenthesis
            TakeFixedToken(tokens, FixedToken.Kind.ParOpen);

            // Closing parenthesis
            TakeFixedToken(tokens, FixedToken.Kind.ParClose);

            // Opening bracket
            TakeFixedToken(tokens, FixedToken.Kind.BracketOpen);

            // Parse statements until we find (and take!) a closing bracket after one
            var blockItems = new List<BlockItemNode>();
            while(!TryTakeFixedToken(tokens, FixedToken.Kind.BracketClose))
            {
                var statement = ParseBlockItem(tokens);
                blockItems.Add(statement);
            };


            // Check if we are missing a return statement. If so add a return 0;
            // TODO: Update this to handle void and other return types when we get to it
            if(blockItems.Count == 0 || !(blockItems[blockItems.Count - 1] is ReturnNode))
            {
                blockItems.Add(new ReturnNode(new ConstantNode(0)));
            }

            return new FunctionNode(nameToken.Name, blockItems);
        }

        private BlockItemNode ParseBlockItem(Queue<Token> tokens)
        {
            // Integer variable declaration
            if(TryTakeFixedToken(tokens, FixedToken.Kind.Int))
            {
                // Name of the variable
                var idToken = TakeIdentifierToken(tokens);

                // Check for assignment (=) to see if there is an expression here
                ExpressionNode expression = null;
                if(TryTakeFixedToken(tokens, FixedToken.Kind.Assignment))
                {
                    // The initialization expression
                    expression = ParseExpression(tokens);
                }

                // Semi colon
                TakeFixedToken(tokens, FixedToken.Kind.SemiColon);

                return new DeclareNode(idToken.Name, expression);
            }

            // It's a statement
            return ParseStatement(tokens);
        }

        private StatementNode ParseStatement(Queue<Token> tokens)
        {
            // Return statement
            if(TryTakeFixedToken(tokens, FixedToken.Kind.Return))
            {
                // Expression to return
                var expression = ParseExpression(tokens);

                // Semi colon
                TakeFixedToken(tokens, FixedToken.Kind.SemiColon);

                return new ReturnNode(expression);
            }
            // If statement
            else if(TryTakeFixedToken(tokens, FixedToken.Kind.If))
            {
                // ( expr )
                TakeFixedToken(tokens, FixedToken.Kind.ParOpen);
                var conditional = ParseExpression(tokens);
                TakeFixedToken(tokens, FixedToken.Kind.ParClose);

                // True statement
                var trueStatement = ParseStatement(tokens);

                // Optional else + statement
                StatementNode falseStatement = null;
                if(TryTakeFixedToken(tokens, FixedToken.Kind.Else))
                {
                    falseStatement = ParseStatement(tokens);
                }

                return new IfStatmentNode(conditional, trueStatement, falseStatement);
            }
            // Block statement
            else if(TryTakeFixedToken(tokens, FixedToken.Kind.BracketOpen))
            {
                // Parse statements until we find (and take!) a closing bracket after one
                var blockItems = new List<BlockItemNode>();
                while(!TryTakeFixedToken(tokens, FixedToken.Kind.BracketClose))
                {
                    var statement = ParseBlockItem(tokens);
                    blockItems.Add(statement);
                };

                return new BlockStatementNode(blockItems);
            }

            // Try to parse an expression statement, if it isn't valid it will crash here
            var expr = ParseExpression(tokens);
            // Semi colon
            TakeFixedToken(tokens, FixedToken.Kind.SemiColon);
            return new ExpressionStatementNode(expr);
        }

        private ExpressionNode ParseExpression(Queue<Token> tokens)
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
                    var expression = ParseExpression(tokens);

                    return new AssignmentNode(idToken.Name, expression);
                }

                // Variable expression probably, fall trough
            }

            // Conditional expression
            return ParseConditionalExpression(tokens);
        }

        private ExpressionNode ParseConditionalExpression(Queue<Token> tokens)
        {
            ExpressionNode expr = ParseLogicalOrExpression(tokens);

            if(TryTakeFixedToken(tokens, FixedToken.Kind.Conditional))
            {
                var trueExpression = ParseExpression(tokens);
                TakeFixedToken(tokens, FixedToken.Kind.Colon);
                var falseExpression = ParseConditionalExpression(tokens);
                return new ConditionalNode(expr, trueExpression, falseExpression);
            }

            return expr;
        }

        private ExpressionNode ParseLogicalOrExpression(Queue<Token> tokens)
        {
            ExpressionNode expr = ParseLogicalAndExpression(tokens);

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                nextToken.TokenKind == FixedToken.Kind.LogicOr)  // Check if there is a ||
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseLogicalAndExpression(tokens);
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseLogicalAndExpression(Queue<Token> tokens)
        {
            ExpressionNode expr = ParseEqualityExpression(tokens);

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                nextToken.TokenKind == FixedToken.Kind.LogicAnd)  // Check if there is a &&
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseEqualityExpression(tokens);
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseEqualityExpression(Queue<Token> tokens)
        {
            ExpressionNode expr = ParseRelationalExpression(tokens);

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                (nextToken.TokenKind == FixedToken.Kind.NotEqual ||
                nextToken.TokenKind == FixedToken.Kind.Equal))  // Check if there is a == or !=
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseRelationalExpression(tokens);
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseRelationalExpression(Queue<Token> tokens)
        {
            ExpressionNode expr = ParseAdditiveExpression(tokens);

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                (nextToken.TokenKind == FixedToken.Kind.LessThan ||
                nextToken.TokenKind == FixedToken.Kind.LessThanOrEqual ||
                nextToken.TokenKind == FixedToken.Kind.GreaterThan ||
                nextToken.TokenKind == FixedToken.Kind.GreaterThanOrEqual))  // Check if there is a <, >, <= or => after this
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the token

                var nextExpr = ParseAdditiveExpression(tokens);
                expr = new BinaryNode(nextToken.TokenKind, expr, nextExpr);

                nextToken = tokens.Peek() as FixedToken;
            }

            return expr;
        }

        private ExpressionNode ParseAdditiveExpression(Queue<Token> tokens)
        {
            ExpressionNode term = ParseTerm(tokens);

            var nextToken = tokens.Peek() as FixedToken;
            while(nextToken != null &&
                (nextToken.TokenKind == FixedToken.Kind.Add || nextToken.TokenKind == FixedToken.Kind.Negate))  // Check if there is a + or - after this
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the + or - token

                var nextTerm = ParseTerm(tokens);
                term = new BinaryNode(nextToken.TokenKind, term, nextTerm);

                nextToken = tokens.Peek() as FixedToken;
            }

            return term;
        }

        private ExpressionNode ParseTerm(Queue<Token> tokens)
        {
            ExpressionNode factor = ParseFactor(tokens);

            var nextToken = tokens.Peek() as FixedToken;  
            while(nextToken != null && 
                (nextToken.TokenKind == FixedToken.Kind.Multiply || nextToken.TokenKind == FixedToken.Kind.Divide))  // Check if there is a * or / after this
            {
                nextToken = tokens.Dequeue() as FixedToken; // Remove the * or / token

                var nextFactor = ParseFactor(tokens);
                factor = new BinaryNode(nextToken.TokenKind, factor, nextFactor);

                nextToken = tokens.Peek() as FixedToken;
            }

            return factor;
        }

        private ExpressionNode ParseFactor(Queue<Token> tokens)
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
                    var expression = ParseFactor(tokens);
                    return new UnaryNode(fixedToken.TokenKind, expression);
                }
                else if(fixedToken.TokenKind == FixedToken.Kind.ParOpen)
                {
                    var expression = ParseExpression(tokens);
                    token = tokens.Dequeue();
                    if(token is FixedToken closingToken && closingToken.TokenKind == FixedToken.Kind.ParClose)
                    {
                        return expression;
                    }
                }
            }
            else if(token is IdentifierToken idToken)
            {
                // Variable reference
                return new VariableNode(idToken.Name);
            }

            throw new UnexpectedTokenException("Did not get a valid start of expression token", token);
        }
    }
}
