using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class Generator
    {
        public void GenerateProgram(ProgramNode program, StreamWriter writer)
        {
            GenerateFunction(program.Function, writer);
        }

        private void GenerateFunction(FunctionNode function, StreamWriter writer)
        {
            writer.WriteLine(".globl _{0}", function.Name);
            writer.WriteLine("_{0}:", function.Name);
            foreach(var statement in function.Statements)
            {
                GenerateStatement(statement, writer);
            }
        }

        private void GenerateStatement(StatementNode statement, StreamWriter writer)
        {
            if(statement is ReturnNode returnStatement)
            {
                GenerateExpression(returnStatement.Expression, writer);
                writer.WriteLine("ret");
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
        private void GenerateExpression(ExpressionNode expression, StreamWriter writer)
        {
            if(expression is ConstantNode constantNode)
            {
                writer.WriteLine("movl    ${0}, %eax", constantNode.Value);
            }
            else if(expression is UnaryNode unaryNode)
            {
                GenerateExpression(unaryNode.Expression, writer);

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
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("addl    %ebx, %eax");             // eax = ebx + eax
                        break;

                    case BinaryNode.Operation.Subtract:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ebx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("subl    %ebx, %eax");             // eax = eax - ebx
                        break;

                    case BinaryNode.Operation.Multiply:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("imul    %ebx, %eax");             // eax = ebx * eax
                        break;

                    case BinaryNode.Operation.Divide:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ebx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("movl    $0, %edx");               // Clear EDX
                        writer.WriteLine("idiv    %ebx");                   // eax = (edx:eax) / ebx, note that remainder is in edx
                        break;

                    case BinaryNode.Operation.Equal:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %ebx, %eax");             // Set ZF if eax = ebx
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("sete    %al");                    // Set al (lowest byte of eax) to 1 IF ZF is set (e.g. ebx == eax)
                        break;

                    case BinaryNode.Operation.NotEqual:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %ebx, %eax");             // Set ZF if eax = ebx
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setne   %al");                    // Set al (lowest byte of eax) to 1 IF ZF is NOT SET (e.g. ebx != eax)
                        break;

                    case BinaryNode.Operation.LessThan:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS < RHS sign is not set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setl    %al");                    // Set al (lowest byte of eax) to 1 IF SF != OF
                        break;

                    case BinaryNode.Operation.LessThanOrEqual:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS < RHS sign is not set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setle   %al");                    // Set al (lowest byte of eax) to 1 IF  SF != OF OR ZF = 1
                        break;

                    case BinaryNode.Operation.GreaterThan:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS > RHS sign is set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setg    %al");                    // Set al (lowest byte of eax) to 1 IF SF = 1
                        break;

                    case BinaryNode.Operation.GreaterThanOrEqual:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ebx");             // Do LHS - RHS: If LHS > RHS sign is set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setge   %al");                    // Set al (lowest byte of eax) to 1 IF SF = 1 OR ZF = 1
                        break;

                    case BinaryNode.Operation.LogicOr:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ebx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("orl     %ebx, %eax");             // Or eax with ebx, !(eax | ebx) == ZF
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setne   %al");                    // Set al (lowest byte of eax) to 1 IF ZF == 0
                        break;

                    case BinaryNode.Operation.LogicAnd:
                        GenerateExpression(binaryNode.FirstTerm, writer);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm, writer);  // Store the result of the right hand side in EAX
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
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
