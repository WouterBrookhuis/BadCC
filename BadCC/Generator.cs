using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace BadCC
{
    class Generator : AbstractGenerator
    {
        public Generator(StreamWriter writer) : base(writer)
        {
        }

        public override void GenerateProgram(ProgramNode program)
        {
            writer.WriteLine(".text");
            foreach (var function in program.Functions)
            {
                GenerateFunction(function);
            }
        }

        private string GetUniqueLabel()
        {
            return string.Format("_{0}_{1}", currentFunction.Name, labelCounter++);
        }

        private string GetFunctionLabel(string functionName)
        {
            return string.Format("_{0}", functionName);
        }

        public void GenerateProgram(ProgramNode program)
        {
            writer.WriteLine(".text");
            foreach(var function in program.Functions)
            {
                GenerateFunction(function);
            }
        }

        private void GenerateFunction(FunctionNode function)
        {
            // No need to generate non-definitions
            if(!function.IsDefinition) { return; }

            currentFunction = function;
            labelCounter = 0;

            localVariableMaps.Push(new LocalVariableMap());
            // Add local variables to the map
            int i = 0;
            foreach(var var in function.Parameters)
            {
                CurrentVariableMap.AddParamInt(var, i++);
            }

            // Function label
            writer.WriteLine(".globl {0}", GetFunctionLabel(function.Name));
            writer.WriteLine(GetFunctionLabel(function.Name) + ":");

            // Function prologue, epiloge is included in return statement generation
            writer.WriteLine("push    %ebp");           // Store ebp on the stack
            writer.WriteLine("movl    %esp, %ebp");     // Use the current esp as our ebp
                                                        // TODO: Add more prologue/epilogue depending on calling conventions?

            // Process all block items
            foreach(var item in function.BodyItems)
            {
                GenerateBlockItem(item);
            }

            currentFunction = null;
        }

        private void GenerateBlockItem(BlockItemNode blockItem)
        {
            if(blockItem is DeclareNode declareNode)
            {
                // Variable declaration
                if(CurrentVariableMap.DeclaredVariable(declareNode.Name))
                {
                    throw new GeneratorException("Duplicate variable declaration!", blockItem);
                }

                // Execute the expression or use default initializer
                if(declareNode.Expression != null)
                {
                    GenerateExpression(declareNode.Expression);
                }
                // Save initial value on stack
                writer.WriteLine("push    %eax");

                // Keep track of where it is
                CurrentVariableMap.AddInt(declareNode.Name);
            }
            else if(blockItem is StatementNode statement)
            {
                GenerateStatement(statement);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void GenerateBlockEntry()
        {
            // TODO: ENSURE we allways have a matching call to GenerateBlockExit !
            // Entering a block means we need a new local variable map
            localVariableMaps.Push(new LocalVariableMap(CurrentVariableMap));
        }

        /// <summary>
        /// Pops a variable map and generates the exit block code for it.
        /// </summary>
        /// <returns></returns>
        private void GenerateBlockExit()
        {
            // Exiting the block means we should ditch the variable map
            var scopeMap = localVariableMaps.Pop();
            // Clean up stack if needed
            GenerateBlockExit(scopeMap);
        }

        /// <summary>
        /// Generates the code for exiting the given variable map
        /// </summary>
        /// <param name="map"></param>
        private void GenerateBlockExit(LocalVariableMap map)
        {
            if(map.ScopeByteSize != 0)
            {
                writer.WriteLine("addl    ${0}, %esp", map.ScopeByteSize); // Move the stack pointer 'back' (up) to where it was before entering this block
            }
        }

        private void EnterLoop(string continueLabel, string breakLabel, LocalVariableMap variableMap)
        {
            loopDatas.Push(new LoopData(continueLabel, breakLabel, variableMap));
        }

        private void ExitLoop()
        {
            loopDatas.Pop();
        }

        private void GenerateStatement(StatementNode statement)
        {
            if(statement is ReturnNode returnStatement)
            {
                // Return statement
                GenerateExpression(returnStatement.Expression);
                writer.WriteLine("movl    %ebp, %esp");     // Restore esp to point to the old ebp
                writer.WriteLine("pop     %ebp");           // Restore ebp by popping it off the stack
                writer.WriteLine("ret");
            }
            else if(statement is ExpressionStatementNode expressionStatement)
            {
                // Just an (optional) expression
                if(expressionStatement.Expression != null)
                {
                    GenerateExpression(expressionStatement.Expression);
                }
            }
            else if(statement is IfStatmentNode ifStatment)
            {
                // If statement
                // Evaluate the conditional expression and jump to the correct code segment if we have to.
                // We need a unique name for each label, so use the function name and a counter
                //     Format
                // conditional expression
                // branch
                // true statement
                // jmp end
                // false statement

                var startOfElseLabel = GetUniqueLabel();
                var endOfElseLabel = GetUniqueLabel();

                // Condition
                GenerateExpression(ifStatment.Condition);
                // Branch, jump to else label if false
                writer.WriteLine("cmpl    $0, %eax");                   // ZF = (0 == eax)
                writer.WriteLine("je      {0}", startOfElseLabel);      // Jump to else if ZF == 0
                GenerateStatement(ifStatment.TrueExpression);           // Put in the true expression
                if(ifStatment.FalseExpression != null)
                {
                    writer.WriteLine("jmp     {0}", endOfElseLabel);    // Jump to end
                    writer.WriteLine("{0}:", startOfElseLabel);         // Put in start of else label
                    GenerateStatement(ifStatment.FalseExpression);      // Else statement
                }
                else
                {
                    writer.WriteLine("{0}:", startOfElseLabel);         // Put in start of else label, will point to the same place as end label
                }
                writer.WriteLine("{0}:", endOfElseLabel);               // Put in end of else label last

            }
            // Block statements
            else if(statement is BlockStatementNode block)
            {
                GenerateBlockEntry();
                // Process all block items
                foreach(var item in block.BlockItems)
                {
                    GenerateBlockItem(item);
                }
                GenerateBlockExit();
            }
            // For statement
            else if(statement is ForStatement forStatement)
            {
                // Initial declaration / expression
                if(forStatement.IsDeclarationType)
                {
                    GenerateBlockEntry();                               // Need to consider this declaration as inside a seperate block
                    GenerateBlockItem(forStatement.InitialDeclaration);
                }
                else if(forStatement.InitialExpression != null)
                {
                    GenerateExpression(forStatement.InitialExpression);
                }

                // Loop start
                var loopStartLabel = GetUniqueLabel();
                var loopEndLabel = GetUniqueLabel();
                var continueLabel = GetUniqueLabel();                   // For requires a seperate continue label because we must execute the iteration expression

                EnterLoop(forStatement.Iteration != null ? continueLabel : loopStartLabel, loopEndLabel, CurrentVariableMap);

                writer.WriteLine("{0}:", loopStartLabel);               // Loop start label
                GenerateExpression(forStatement.Condition);             // The checking condition.
                writer.WriteLine("cmpl    $0, %eax");                   // See if eax is 0
                writer.WriteLine("je      {0}", loopEndLabel);          // Jump to end if condition was 0
                GenerateStatement(forStatement.Statement);              // Generate the statement
                if(forStatement.Iteration != null)
                {
                    writer.WriteLine("{0}:", continueLabel);            // Continue label
                    GenerateExpression(forStatement.Iteration);         // Do the loop iteration expression after the statement
                }
                writer.WriteLine("jmp     {0}", loopStartLabel);        // Jump back to start of loop
                writer.WriteLine("{0}:", loopEndLabel);                 // Insert loop end label at the end

                ExitLoop();

                if(forStatement.IsDeclarationType)
                {
                    GenerateBlockExit();                                // Clean up the 'block' we made for the declaration
                }
            }
            else if(statement is WhileStatement whileStatement)
            {
                // Loop start
                var loopStartLabel = GetUniqueLabel();
                var loopEndLabel = GetUniqueLabel();

                EnterLoop(loopStartLabel, loopEndLabel, CurrentVariableMap);

                writer.WriteLine("{0}:", loopStartLabel);               // Loop start label
                GenerateExpression(whileStatement.Condition);           // The checking condition.
                writer.WriteLine("cmpl    $0, %eax");                   // See if eax is 0
                writer.WriteLine("je      {0}", loopEndLabel);          // Jump to end if condition was 0
                GenerateStatement(whileStatement.Statement);            // Generate the statement
                writer.WriteLine("jmp     {0}", loopStartLabel);        // Jump back to start of loop
                writer.WriteLine("{0}:", loopEndLabel);                 // Insert loop end label at the end

                ExitLoop();
            }
            else if(statement is DoWhileStatement doWhileStatement)
            {
                // Loop start
                var loopStartLabel = GetUniqueLabel();
                var loopEndLabel = GetUniqueLabel();

                EnterLoop(loopStartLabel, loopEndLabel, CurrentVariableMap);

                writer.WriteLine("{0}:", loopStartLabel);               // Loop start label
                GenerateStatement(doWhileStatement.Statement);          // Generate the statement
                GenerateExpression(doWhileStatement.Condition);         // The checking condition.
                writer.WriteLine("cmpl    $0, %eax");                   // See if eax is 0
                writer.WriteLine("jne      {0}", loopStartLabel);       // Jump to start if condition was true
                writer.WriteLine("{0}:", loopEndLabel);                 // Insert loop end label at the end

                ExitLoop();
            }
            else if(statement is BreakStatement)
            {
                // Exit blocks we are in until we reach the stored loop map
                var removedMaps = new Stack<LocalVariableMap>();
                while(localVariableMaps.Peek() != CurrentLoopData.LoopLevelMap)
                {
                    var map = localVariableMaps.Pop();
                    removedMaps.Push(map);

                    GenerateBlockExit(map);
                }
                // Restore the map stack
                while(removedMaps.Count > 0)
                {
                    localVariableMaps.Push(removedMaps.Pop());
                }
                writer.WriteLine("jmp     {0}", CurrentLoopData.BreakLabel);        // Jump to end of current loop
            }
            else if(statement is ContinueStatement)
            {
                // Exit blocks we are in until we reach the stored loop map
                var removedMaps = new Stack<LocalVariableMap>();
                while(localVariableMaps.Peek() != CurrentLoopData.LoopLevelMap)
                {
                    var map = localVariableMaps.Pop();
                    removedMaps.Push(map);

                    GenerateBlockExit(map);
                }
                // Restore the map stack
                while(removedMaps.Count > 0)
                {
                    localVariableMaps.Push(removedMaps.Pop());
                }
                writer.WriteLine("jmp     {0}", CurrentLoopData.ContinueLabel);     // Jump to the correct label
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
                switch(unaryNode.Op)
                {
                    case UnaryNode.Operation.Complement:
                        GenerateExpression(unaryNode.Expression);
                        writer.WriteLine("not     %eax");
                        break;
                    case UnaryNode.Operation.Negate:
                        GenerateExpression(unaryNode.Expression);
                        writer.WriteLine("neg     %eax");
                        break;
                    case UnaryNode.Operation.LogicNegate:
                        GenerateExpression(unaryNode.Expression);
                        writer.WriteLine("cmpl    $0, %eax");   // Set ZF if eax = 0
                        writer.WriteLine("movl    $0, %eax");   // Zero eax
                        writer.WriteLine("sete    %al");        // Set al (lowest byte of eax) to 1 IF ZF is set
                        break;
                    case UnaryNode.Operation.Address:
                        // Store the absolute variable address in eax
                        var name = ((VariableNode)unaryNode.Expression).Name;
                        // TODO: Function addresses
                        if(!CurrentVariableMap.TryGetOffset(name, out int offset))
                        {
                            throw new GeneratorException("Reference to undeclared variable", unaryNode);
                        }

                        // Store the value of ebp + offset in eax
                        writer.WriteLine("movl    %ebp, %eax");         // Get EBP in EAX
                        writer.WriteLine("addl    ${0}, %eax", offset); // Add offset to EAX, getting the absolute variable address

                        break;
                    case UnaryNode.Operation.Indirection:
                        // Interpret eax as an address and get the value in memory at that address
                        GenerateExpression(unaryNode.Expression);
                        writer.WriteLine("movl    (%eax), %eax");
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
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("addl    %ecx, %eax");             // eax = ebx + eax
                        break;

                    case BinaryNode.Operation.Subtract:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ecx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("subl    %ecx, %eax");             // eax = eax - ebx
                        break;

                    case BinaryNode.Operation.Multiply:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("imul    %ecx, %eax");             // eax = ebx * eax
                        break;

                    case BinaryNode.Operation.Divide:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ecx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("movl    $0, %edx");               // Clear EDX
                        writer.WriteLine("idiv    %ecx");                   // eax = (edx:eax) / ebx, note that remainder is in edx
                        break;

                    case BinaryNode.Operation.Modulo:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("movl    %eax, %ecx");             // Move result of right hand side into EBX
                        writer.WriteLine("pop     %eax");                   // Pop left hand side result off the stack into EAX
                        writer.WriteLine("movl    $0, %edx");               // Clear EDX
                        writer.WriteLine("idiv    %ecx");                   // eax = (edx:eax) / ebx, the remainder is in edx
                        writer.WriteLine("movl    %edx, %eax");             // Move the remainder to EAX
                        break;

                    case BinaryNode.Operation.Equal:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %ecx, %eax");             // Set ZF if eax = ebx
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("sete    %al");                    // Set al (lowest byte of eax) to 1 IF ZF is set (e.g. ebx == eax)
                        break;

                    case BinaryNode.Operation.NotEqual:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %ecx, %eax");             // Set ZF if eax = ebx
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setne   %al");                    // Set al (lowest byte of eax) to 1 IF ZF is NOT SET (e.g. ebx != eax)
                        break;

                    case BinaryNode.Operation.LessThan:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ecx");             // Do LHS - RHS: If LHS < RHS sign is not set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setl    %al");                    // Set al (lowest byte of eax) to 1 IF SF != OF
                        break;

                    case BinaryNode.Operation.LessThanOrEqual:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ecx");             // Do LHS - RHS: If LHS < RHS sign is not set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setle   %al");                    // Set al (lowest byte of eax) to 1 IF  SF != OF OR ZF = 1
                        break;

                    case BinaryNode.Operation.GreaterThan:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ecx");             // Do LHS - RHS: If LHS > RHS sign is set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setg    %al");                    // Set al (lowest byte of eax) to 1 IF SF = 1
                        break;

                    case BinaryNode.Operation.GreaterThanOrEqual:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    %eax, %ecx");             // Do LHS - RHS: If LHS > RHS sign is set
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setge   %al");                    // Set al (lowest byte of eax) to 1 IF SF = 1 OR ZF = 1
                        break;

                    case BinaryNode.Operation.LogicOr:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("orl     %ecx, %eax");             // Or eax with ebx, !(eax | ebx) == ZF
                        writer.WriteLine("movl    $0, %eax");               // Zero eax
                        writer.WriteLine("setne   %al");                    // Set al (lowest byte of eax) to 1 IF ZF == 0
                        break;

                    case BinaryNode.Operation.LogicAnd:
                        GenerateExpression(binaryNode.FirstTerm);   // Store the result of the left hand side in EAX
                        writer.WriteLine("push    %eax");                   // Push left hand side result on stack
                        GenerateExpression(binaryNode.SecondTerm);  // Store the result of the right hand side in EAX
                        writer.WriteLine("pop     %ecx");                   // Pop left hand side result off the stack into EBX
                        writer.WriteLine("cmpl    $0, %ecx");               // Set ZF iff ebx == 0
                        writer.WriteLine("setne   %cl");                    // Set bl = 1 iff ebx != 0
                        writer.WriteLine("cmpl    $0, %eax");               // Set ZF iff eax == 0
                        writer.WriteLine("movl    $0, %eax");               // Clear eax
                        writer.WriteLine("setne   %al");                    // Set al = 1 iff eax != 0
                        writer.WriteLine("andb    %cl, %al");               // And bl & al, al = 1 iif bl = 1 and al = 1
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if(expression is AssignmentNode assignmentNode)
            {
                // Variable assignment
                if(CurrentVariableMap.TryGetOffset(assignmentNode.Name, out int variableOffset))
                {
                    GenerateExpression(assignmentNode.Expression);
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
                if(CurrentVariableMap.TryGetOffset(variableNode.Name, out int variableOffset))
                {
                    writer.WriteLine("movl    {0}(%ebp), %eax", variableOffset);    // eax = mem(ebp + variableOffset)
                }
                else
                {
                    throw new GeneratorException("Reference to undeclared variable", variableNode);
                }
            }
            else if(expression is ConditionalNode conditional)
            {
                // Conditional expression a ? b : c
                var startOfElseLabel = GetUniqueLabel();
                var endOfElseLabel = GetUniqueLabel();

                // Condition
                GenerateExpression(conditional.Condition);
                // Branch, jump to else label if false
                writer.WriteLine("cmpl    $0, %eax");                   // ZF = (0 == eax)
                writer.WriteLine("je      {0}", startOfElseLabel);      // Jump to else if ZF == 0
                GenerateExpression(conditional.TrueExpression);         // Put in the true expression
                writer.WriteLine("jmp     {0}", endOfElseLabel);        // Jump to end
                writer.WriteLine("{0}:", startOfElseLabel);             // Put in start of else label
                GenerateExpression(conditional.FalseExpression);        // Else statement
                writer.WriteLine("{0}:", endOfElseLabel);               // Put in end of else label last
            }
            else if(expression is CallNode call)
            {
                // Function call a(params)
                var funcLabel = GetFunctionLabel(call.Name);
                // Push params on stack in reverse order
                foreach(var expr in call.Parameters.Reverse())
                {
                    GenerateExpression(expr);                           // Generate expression for the param
                    writer.WriteLine("push    %eax");                   // Push param on the stack
                }
                writer.WriteLine("call    {0}", funcLabel);             // Call the function
                writer.WriteLine("addl    ${0}, %esp", call.Parameters.Count * 4);  // Restore stack pointer after returning
            }
            else if(expression is PostfixNode postfix)
            {
                // a-- or a++
                // Increment or decrement a but return the value of a before this happened
                GenerateExpression(postfix.Variable);                       // Get the value of a
                writer.WriteLine("push    %eax");                           // Save a on stack
                switch(postfix.Op)
                {
                    case PostfixNode.Operation.Increment:
                        writer.WriteLine("addl    $1, %eax");               // Add 1 to a
                        break;
                    case PostfixNode.Operation.Decrement:
                        writer.WriteLine("subl    $1, %eax");               // Subtract 1 from a
                        break;
                    default:
                        throw new NotImplementedException();
                }
                writer.WriteLine("movl    %eax, {0}(%ebp)",
                    CurrentVariableMap.GetOffset(postfix.Variable.Name));   // Store a back in memory
                writer.WriteLine("pop     %eax");                           // Return old value of a in EAX
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
