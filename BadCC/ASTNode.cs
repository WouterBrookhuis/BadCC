﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    abstract class ASTNode
    {
        public override string ToString()
        {
            return ToString(0);
        }

        public virtual string ToString(int indentLevel)
        {
            var sb = new StringBuilder();
            sb.Append('\t', indentLevel);
            sb.AppendLine(GetType().Name);
            var properties = GetType().GetProperties();
            foreach(var prop in properties)
            {
                if(typeof(ASTNode).IsAssignableFrom(prop.PropertyType)) { continue; }

                sb.Append('\t', indentLevel);
                sb.Append(prop.Name + ": ");
                sb.AppendLine(prop.GetValue(this).ToString());
            }
            return sb.ToString();
        }
    }

    class ProgramNode : ASTNode
    {
        public FunctionNode Function { get; private set; }

        public ProgramNode(FunctionNode function)
        {
            Function = function;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Function.ToString(indentLevel + 1);
        }
    }

    class FunctionNode : ASTNode
    {
        public string Name { get; private set; }
        public List<StatementNode> Statements { get; private set; }

        public FunctionNode(string name, List<StatementNode> statements)
        {
            Name = name;
            Statements = statements;
        }

        public override string ToString(int indentLevel)
        {
            var str = base.ToString(indentLevel);
            foreach(var statement in Statements)
            {
                str += "\r\n" + statement.ToString(indentLevel + 1);
            }
            return str;
        }
    }

    abstract class StatementNode : ASTNode
    {
    }

    class ReturnNode : StatementNode
    {
        public ExpressionNode Expression { get; private set; }

        public ReturnNode(ExpressionNode expression)
        {
            Expression = expression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Expression.ToString(indentLevel + 1);
        }
    }

    class DeclareNode : StatementNode
    {
        public string Name { get; private set; }
        public ExpressionNode Expression { get; private set; }

        public DeclareNode(string name, ExpressionNode expression)
        {
            Name = name;
            Expression = expression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Expression?.ToString(indentLevel + 1);
        }
    }
    
    class ExpressionStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; private set; }

        public ExpressionStatementNode(ExpressionNode expression)
        {
            Expression = expression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Expression.ToString(indentLevel + 1);
        }
    }

    abstract class ExpressionNode : ASTNode
    {
    }

    class AssignmentNode : ExpressionNode
    {
        public string Name { get; private set; }
        public ExpressionNode Expression { get; private set; }

        public AssignmentNode(string name, ExpressionNode expression)
        {
            Name = name;
            Expression = expression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Expression.ToString(indentLevel + 1);
        }
    }

    class BinaryNode : ExpressionNode
    {
        public enum Operation
        {
            Add,
            Subtract,
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
        }

        public Operation Op { get; private set; }
        public ExpressionNode FirstTerm { get; private set; }
        public ExpressionNode SecondTerm { get; private set; }

        public BinaryNode(FixedToken.Kind op, ExpressionNode firstTerm, ExpressionNode secondTerm)
        {
            switch(op)
            {
                case FixedToken.Kind.Add:
                    Op = Operation.Add; break;
                case FixedToken.Kind.Negate:
                    Op = Operation.Subtract; break;
                case FixedToken.Kind.Multiply:
                    Op = Operation.Multiply; break;
                case FixedToken.Kind.Divide:
                    Op = Operation.Divide; break;
                case FixedToken.Kind.LogicAnd:
                    Op = Operation.LogicAnd; break;
                case FixedToken.Kind.LogicOr:
                    Op = Operation.LogicOr; break;
                case FixedToken.Kind.Equal:
                    Op = Operation.Equal; break;
                case FixedToken.Kind.NotEqual:
                    Op = Operation.NotEqual; break;
                case FixedToken.Kind.LessThan:
                    Op = Operation.LessThan; break;
                case FixedToken.Kind.LessThanOrEqual:
                    Op = Operation.LessThanOrEqual; break;
                case FixedToken.Kind.GreaterThan:
                    Op = Operation.GreaterThan; break;
                case FixedToken.Kind.GreaterThanOrEqual:
                    Op = Operation.GreaterThanOrEqual; break;
                default:
                    throw new ArgumentException("Token kind is not one for Binary operators!");
            }
            FirstTerm = firstTerm;
            SecondTerm = secondTerm;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + FirstTerm.ToString(indentLevel + 1) + "\r\n" + SecondTerm.ToString(indentLevel + 1);
        }
    }

    class UnaryNode : ExpressionNode
    {
        public enum Operation
        {
            Negate,
            Complement,
            LogicNegate,
        }

        public Operation Op { get; private set; }
        public ExpressionNode Expression { get; private set; }

        public UnaryNode(FixedToken.Kind op, ExpressionNode expression)
        {
            switch(op)
            {
                case FixedToken.Kind.Negate:
                    Op = Operation.Negate; break;
                case FixedToken.Kind.Complement:
                    Op = Operation.Complement; break;
                case FixedToken.Kind.LogicNegate:
                    Op = Operation.LogicNegate; break;
                default:
                    throw new ArgumentException("Token kind is not one for Unary operators!");
            }
            Expression = expression;
        }

        public UnaryNode(Operation op, ExpressionNode expression)
        {
            Op = op;
            Expression = expression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Expression.ToString(indentLevel + 1);
        }
    }

    class ConstantNode : ExpressionNode
    {
        public int Value { get; private set; }

        public ConstantNode(int value)
        {
            Value = value;
        }
    }
}