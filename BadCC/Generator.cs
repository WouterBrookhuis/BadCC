using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace BadCC
{
    class Generator
    {
        private StreamWriter writer;
        private ImmutableDictionary<string, int> localVariableMap;
        private int localVariableOffset;

        private static readonly ConstantNode s_constZeroNode = new ConstantNode(0);

        public Generator(StreamWriter writer)
        {
            this.writer = writer;
            var builder = ImmutableDictionary.CreateBuilder<string, int>();
            localVariableMap = builder.ToImmutable();
            localVariableOffset = -4;   // One after saved EPB
        }

        public void GenerateProgram(ProgramNode program)
        {
            GenerateFunction(program.Function);
        }

        private void GenerateFunction(FunctionNode function)
        {
            writer.WriteLine(".globl _{0}", function.Name);
            writer.WriteLine("_{0}:", function.Name);
            foreach(var statement in function.Statements)
            {
                GenerateStatement(statement);
            }
        }

        private void GenerateStatement(StatementNode statement)
        {
            if(statement is ReturnNode returnStatement)
            {
                // Return statement
                GenerateExpression(returnStatement.Expression);
                // TODO: Frame cleanup stuff
                writer.WriteLine("ret");
            }
            else if(statement is DeclareNode declareNode)
            {
                // Variable declaration
                if(localVariableMap.ContainsKey(declareNode.Name))
                {
                    throw new GeneratorException("Duplicate variable declaration!", statement);
                }

                // Execute the expression or use default initializer
                if(declareNode.Expression != null)
                {
                    GenerateExpression(declareNode.Expression);
                }
                else
                {
                    GenerateExpression(Generator.s_constZeroNode);
                }
                // Save initial value on stack
                writer.WriteLine("push    %eax");

                // Keep track of where it is
                localVariableMap = localVariableMap.Add(declareNode.Name, localVariableOffset);
                localVariableOffset -= 4;
            }
            else if(statement is ExpressionStatementNode expressionStatement)
            {
                // Just an expression
                GenerateExpression(expressionStatement.Expression);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates an expression for which the result gets stored in EAX
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="writer"></param>
        private void GenerateExpression(ExpressionNode expression)
        {
            if(expression is ConstantNode constantNode)
            {
                writer.WriteLine("movl    ${0}, %eax", constantNode.Value);
            }
            else if(expression is UnaryNode unaryNode)
            {
                GenerateExpression(unaryNode.Expression);

                switch(unaryNode.Op)
                {
                    case UnaryNode.Operation.Complement:
                        writer.WriteLine("not     %eax");
                        break;
                    case UnaryNode.Operation.Negate:
                        writer.WriteLine("neg     %eax");
                        break;
                    case UnaryNode.Operation.LogicNegate:
                        writer.WriteLine("cmpl    $0, %eax");   // Set ZF if eax = 0
                        writer.WriteLine("movl    $0, %eax");   // Zero eax
                        writer.WriteLine("sete    %al");        // Set al (lowest byte of eax) to 1 IF ZF is set
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if(expression is BinaryNode binaryNode)
            {
                switch(binaryNode.Op)
                {
                    case BinaryNode.Operation.Add:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("addl    %ebx, %eax");             // eax = ebx + eax
                        break;

                    case BinaryNode.Operation.Subtract:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ebx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("subl    %ebx, %eax");             // eax = eax - ebx
                        break;

                    case BinaryNode.Operation.Multiply:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("imul    %ebx, %eax");             // eax = ebx * eax
                        break;

                    case BinaryNode.Operation.Divide:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ebx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("movl    $0, %edx");               // Clear EDX
                        writer.WriteLine("idiv    %ebx");                   // eax = (edx:eax) / ebx, note that remainder is in edx
                        break;

                    case BinaryNode.Operation.Equal:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %ebx, %eax");             // Set ZF if eax = ebx
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("sete    %al");                    // Set al (lowest byte of eax) to 1 IF ZF is set (e.g. ebx == eax)
                        break;

                    case BinaryNode.Operation.NotEqual:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %ebx, %eax");             // Set ZF if eax = ebx
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setne   %al");                    // Set al (lowest byte of eax) to 1 IF ZF is NOT SET (e.g. ebx != eax)
                        break;

                    case BinaryNode.Operation.LessThan:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS < RHS sign is not set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setl    %al");                    // Set al (lowest byte of eax) to 1 IF SF != OF
                        break;

                    case BinaryNode.Operation.LessThanOrEqual:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS < RHS sign is not set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setle   %al");                    // Set al (lowest byte of eax) to 1 IF  SF != OF OR ZF = 1
                        break;

                    case BinaryNode.Operation.GreaterThan:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS > RHS sign is set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setg    %al");                    // Set al (lowest byte of eax) to 1 IF SF = 1
                        break;

                    case BinaryNode.Operation.GreaterThanOrEqual:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS > RHS sign is set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setge   %al");                    // Set al (lowest byte of eax) to 1 IF SF = 1 OR ZF = 1
                        break;

                    case BinaryNode.Operation.LogicOr:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("orl     %ebx, %eax");             // Or eax with ebx, !(eax | ebx) == ZF
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setne   %al");                    // Set al (lowest byte of eax) to 1 IF ZF == 0
                        break;

                    case BinaryNode.Operation.LogicAnd:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    $0, %ebx");               // Set ZF iff ebx == 0
                        writer.WriteLine("setne   %bl");                    // Set bl = 1 iff ebx != 0
                        writer.WriteLine("cmpl    $0, %eax");               // Set ZF iff eax == 0
                        writer.WriteLine("movl    $0, %eax");               // Clear eax
                        writer.WriteLine("setne   %al");                    // Set al = 1 iff eax != 0
                        writer.WriteLine("andb    %bl, %al");               // And bl & al, al = 1 iif bl = 1 and al = 1
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if(expression is AssignmentNode assignmentNode)
            {
                // Variable assignment
                if(localVariableMap.TryGetValue(assignmentNode.Name, out int variableOffset))
                {
                    writer.WriteLine("movl    %eax, {0}(%ebp)", variableOffset);    // Store eax in memory at ebp + variableOffset
                }
                else
                {
                    throw new GeneratorException("Reference to undeclared variable", assignmentNode);
                }
            }
            else if(expression is VariableNode variableNode)
            {
                // Reference to a variable
                if(localVariableMap.TryGetValue(variableNode.Name, out int variableOffset))
                {
                    writer.WriteLine("movl    {0}(%ebp), %eax", variableOffset);    // eax = mem(ebp + variableOffset)
                }
                else
                {
                    throw new GeneratorException("Reference to undeclared variable", variableNode);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
