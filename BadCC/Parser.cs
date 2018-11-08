using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class Parser
    {
        public ProgramNode ParseProgram(Queue<Token> tokens)
        {
            var function = ParseFunction(tokens);
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
            var nameToken = tokens.Dequeue() as IdentifierToken;
            if(nameToken == null)
            {
                throw new UnexpectedTokenException("Did not get ID token", nameToken);
            }

            // Opening parenthesis
            var parToken = tokens.Dequeue() as FixedToken;
            if(parToken == null || parToken.TokenKind != FixedToken.Kind.ParOpen)
            {
                throw new UnexpectedTokenException("Did not get ( token", parToken);
            }

            // Closing parenthesis
            parToken = tokens.Dequeue() as FixedToken;
            if(parToken == null || parToken.TokenKind != FixedToken.Kind.ParClose)
            {
                throw new UnexpectedTokenException("Did not get ) token", parToken);
            }

            // Opening bracket
            var brackToken = tokens.Dequeue() as FixedToken;
            if(brackToken == null || brackToken.TokenKind != FixedToken.Kind.BracketOpen)
            {
                throw new UnexpectedTokenException("Did not get { token", brackToken);
            }

            // Parse statements until we find a closing bracket after one
            var statements = new List<StatementNode>();
            do
            {
                var statement = ParseStatement(tokens);
                statements.Add(statement);
            } while( !(tokens.Peek() is FixedToken potentialBracket && potentialBracket.TokenKind == FixedToken.Kind.BracketClose));
            
            // Eat the closing bracket
            brackToken = tokens.Dequeue() as FixedToken;
            if(brackToken == null || brackToken.TokenKind != FixedToken.Kind.BracketClose)
            {
                throw new UnexpectedTokenException("Did not get } token", brackToken);
            }

            return new FunctionNode(nameToken.Name, statements);
        }

        private StatementNode ParseStatement(Queue<Token> tokens)
        {
            // PEEK The next token, don't take it yet!
            var token = tokens.Peek();

            if(token is FixedToken fixedToken)
            {
                // Return statement
                if(fixedToken.TokenKind == FixedToken.Kind.Return)
                {
                    tokens.Dequeue();

                    // Expression to return
                    var expression = ParseExpression(tokens);

                    // Semi colon
                    var semiToken = tokens.Dequeue() as FixedToken;
                    if(semiToken == null || semiToken.TokenKind != FixedToken.Kind.SemiColon)
                    {
                        throw new UnexpectedTokenException("Did not get ; token", semiToken);
                    }

                    return new ReturnNode(expression);
                }
                // Integer variable declaration
                else if(fixedToken.TokenKind == FixedToken.Kind.Int)
                {
                    tokens.Dequeue();

                    // Name of the variable
                    var idToken = tokens.Dequeue() as IdentifierToken;
                    if(idToken == null)
                    {
                        throw new UnexpectedTokenException("Did not get an identifier token", idToken);
                    }

                    // Check for assignment (=) to see if there is an expression here
                    ExpressionNode expression = null;
                    if(tokens.Peek() is FixedToken assignToken && assignToken.TokenKind == FixedToken.Kind.Assignment)
                    {
                        tokens.Dequeue();

                        // The initialization expression
                        expression = ParseExpression(tokens);
                    }

                    // Semi colon
                    var semiToken2 = tokens.Dequeue() as FixedToken;
                    if(semiToken2 == null || semiToken2.TokenKind != FixedToken.Kind.SemiColon)
                    {
                        throw new UnexpectedTokenException("Did not get ; token", semiToken2);
                    }

                    return new DeclareNode(idToken.Name, expression);
                }
            }

            // Try to parse an expression statement, if it isn't valid it will crash here
            var expr = ParseExpression(tokens);
            // Semi colon
            var semi = tokens.Dequeue() as FixedToken;
            if(semi == null || semi.TokenKind != FixedToken.Kind.SemiColon)
            {
                throw new UnexpectedTokenException("Did not get ; token", semi);
            }
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

            // Or expression
            return ParseLogicalOrExpression(tokens);
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
