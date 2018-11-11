using System;
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
                if(typeof(ASTNode).IsAssignableFrom(prop.PropertyType) || 
                    (prop.PropertyType.IsGenericType)) { continue; }

                sb.Append('\t', indentLevel);
                sb.Append(prop.Name + ": ");
                sb.AppendLine(prop.GetValue(this).ToString());
            }
            return sb.ToString();
        }

        protected string ToIndentLevel(string str, int indentLevel)
        {
            return new StringBuilder().Append('\t', indentLevel).Append(str).ToString();
        }
    }

    class ProgramNode : ASTNode
    {
        public IReadOnlyList<FunctionNode> Functions { get; private set; }

        public ProgramNode(IReadOnlyList<FunctionNode> functions)
        {
            Functions = functions;
        }

        public override string ToString(int indentLevel)
        {
            var str = base.ToString(indentLevel);
            str += ToIndentLevel("Parameters:", indentLevel) + "\r\n";
            foreach (var function in Functions)
            {
                str += function.ToString(indentLevel + 1) + "\r\n";
            }
            return str;
        }
    }

    class FunctionNode : ASTNode
    {
        public string Name { get; private set; }
        public IReadOnlyList<string> Parameters { get; private set; }
        public IReadOnlyList<BlockItemNode> BodyItems { get; private set; }

        public bool IsDefinition => BodyItems != null;

        public FunctionNode(string name, IReadOnlyList<string> parameters, IReadOnlyList<BlockItemNode> bodyItems)
        {
            Name = name;
            Parameters = parameters;
            BodyItems = bodyItems;
        }

        public override string ToString(int indentLevel)
        {
            var str = base.ToString(indentLevel);
            str += ToIndentLevel("Parameters:", indentLevel) + "\r\n";
            foreach (var param in Parameters)
            {
                str += ToIndentLevel(param, indentLevel + 1) + "\r\n";
            }
            if(IsDefinition)
            {
                str += ToIndentLevel("Body:", indentLevel) + "\r\n";
                foreach (var blockItem in BodyItems)
                {
                    str += "\r\n" + blockItem.ToString(indentLevel + 1);
                }
            }
            return str;
        }
    }

    abstract class BlockItemNode : ASTNode
    {
    }

    abstract class StatementNode : BlockItemNode
    {
    }

    class DeclareNode : BlockItemNode
    {
        public VariableInfo Info { get; private set; }
        public ExpressionNode Expression { get; private set; }
        public ExpressionNode ArraySizeExpression { get; private set; }

        public DeclareNode(VariableInfo info, ExpressionNode expression)
        {
            Info = info;
            Expression = expression;
        }

        public DeclareNode(VariableInfo info, ExpressionNode expression, ExpressionNode arraySizeExpression) : this(info, expression)
        {
            if(!info.Type.IsArray && arraySizeExpression != null)
            {
                throw new Exception("Should not have an array size expression given for non array type!");
            }
            ArraySizeExpression = arraySizeExpression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Expression?.ToString(indentLevel + 1);
        }
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
    
    class IfStatmentNode : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode TrueExpression { get; private set; }
        public StatementNode FalseExpression { get; private set; }

        public IfStatmentNode(ExpressionNode condition, StatementNode trueExpression, StatementNode falseExpression)
        {
            Condition = condition;
            TrueExpression = trueExpression;
            FalseExpression = falseExpression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Condition.ToString(indentLevel + 1) + "\r\n" + TrueExpression.ToString(indentLevel + 1) + "\r\n" + FalseExpression?.ToString(indentLevel + 1);
        }
    }

    class BreakStatement : StatementNode
    {
    }

    class ContinueStatement : StatementNode
    {
    }

    class WhileStatement : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode Statement { get; private set; }

        public WhileStatement(ExpressionNode condition, StatementNode statement)
        {
            Condition = condition;
            Statement = statement;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Condition.ToString(indentLevel + 1) + "\r\n" + Statement.ToString(indentLevel + 1);
        }
    }

    class DoWhileStatement : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode Statement { get; private set; }

        public DoWhileStatement(ExpressionNode condition, StatementNode statement)
        {
            Condition = condition;
            Statement = statement;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Condition.ToString(indentLevel + 1) + "\r\n" + Statement.ToString(indentLevel + 1);
        }
    }

    class ForStatement : StatementNode
    {
        public DeclareNode InitialDeclaration { get; private set; }
        public ExpressionNode InitialExpression { get; private set; }
        public ExpressionNode Condition { get; private set; }
        public ExpressionNode Iteration { get; private set; }
        public StatementNode Statement { get; private set; }

        /// <summary>
        /// Returns true if this is an expression that has a declaration as the initial 'expression'
        /// </summary>
        public bool IsDeclarationType => InitialDeclaration != null;

        public ForStatement(ExpressionNode initialExpression, ExpressionNode condition, ExpressionNode iteration, StatementNode statement)
        {
            InitialExpression = initialExpression;
            Condition = condition;
            Iteration = iteration;
            Statement = statement;
        }

        public ForStatement(DeclareNode initialDeclaration, ExpressionNode condition, ExpressionNode iteration, StatementNode statement)
        {
            InitialDeclaration = initialDeclaration;
            Condition = condition;
            Iteration = iteration;
            Statement = statement;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n"
                + ToIndentLevel("Initial:\r\n", indentLevel)
                + (InitialDeclaration != null ? InitialDeclaration.ToString(indentLevel + 1) : InitialExpression?.ToString(indentLevel + 1)) + "\r\n"
                + ToIndentLevel("Condition:\r\n", indentLevel)
                + Condition.ToString(indentLevel + 1) + "\r\n"
                + ToIndentLevel("Iteration:\r\n", indentLevel)
                + Iteration?.ToString(indentLevel + 1) + "\r\n"
                + ToIndentLevel("Statement:\r\n", indentLevel)
                + Statement.ToString(indentLevel + 1);
        }
    }

    class BlockStatementNode : StatementNode
    {
        public IReadOnlyList<BlockItemNode> BlockItems { get; private set; }

        public BlockStatementNode(IReadOnlyList<BlockItemNode> blockItems)
        {
            BlockItems = blockItems;
        }

        public override string ToString(int indentLevel)
        {
            var str = base.ToString(indentLevel);
            foreach(var blockItem in BlockItems)
            {
                str += "\r\n" + blockItem.ToString(indentLevel + 1);
            }
            return str;
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
            return base.ToString(indentLevel) + "\r\n" + Expression?.ToString(indentLevel + 1);
        }
    }

    abstract class ExpressionNode : ASTNode
    {
    }

    class AssignmentNode : ExpressionNode
    {
        public VariableNode Variable { get; private set; }
        public ExpressionNode Expression { get; private set; }

        public AssignmentNode(VariableNode variable, ExpressionNode expression)
        {
            Variable = variable;
            Expression = expression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Variable.ToString(indentLevel + 1) + "\r\n" + Expression.ToString(indentLevel + 1);
        }
    }

    class ConditionalNode : ExpressionNode
    {
        public ExpressionNode Condition { get; private set; }
        public ExpressionNode TrueExpression { get; private set; }
        public ExpressionNode FalseExpression { get; private set; }

        public ConditionalNode(ExpressionNode condition, ExpressionNode trueExpression, ExpressionNode falseExpression)
        {
            Condition = condition;
            TrueExpression = trueExpression;
            FalseExpression = falseExpression;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Condition.ToString(indentLevel + 1) + "\r\n" + TrueExpression.ToString(indentLevel + 1) + "\r\n" + FalseExpression?.ToString(indentLevel + 1);
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
            Modulo,
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
                case FixedToken.Kind.Modulo:
                    Op = Operation.Modulo; break;
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
            Indirection,
            Address,
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
                case FixedToken.Kind.Multiply:
                    Op = Operation.Indirection; break;
                case FixedToken.Kind.BinaryAnd:
                    Op = Operation.Address; break;
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

    class PostfixNode : ExpressionNode
    {
        public enum Operation
        {
            Increment, 
            Decrement,
        }

        public Operation Op { get; private set; }
        public VariableNode Variable { get; private set; }

        public PostfixNode(Operation op, VariableNode variable)
        {
            Op = op;
            Variable = variable;
        }

        public override string ToString(int indentLevel)
        {
            return base.ToString(indentLevel) + "\r\n" + Variable.ToString(indentLevel + 1);
        }
    }

    class VariableNode : ExpressionNode
    {
        public string Name { get; private set; }
        public ExpressionNode ArrayIndexExpression { get; private set; }
        public bool UseArrayIndexer => ArrayIndexExpression != null;

        public VariableNode(string name, ExpressionNode arrayIndexExpression)
        {
            Name = name;
            ArrayIndexExpression = arrayIndexExpression;
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

    class CallNode : ExpressionNode
    {
        public string Name { get; private set; }
        public IReadOnlyList<ExpressionNode> Parameters { get; private set; }

        public CallNode(string name, IReadOnlyList<ExpressionNode> parameters)
        {
            Name = name;
            Parameters = parameters;
        }


        public override string ToString(int indentLevel)
        {
            var str = base.ToString(indentLevel);
            str += ToIndentLevel("Parameters:", indentLevel) + "\r\n";
            foreach (var param in Parameters)
            {
                str += "\r\n" + param.ToString(indentLevel + 1);
            }
            return str;
        }

    }
}
