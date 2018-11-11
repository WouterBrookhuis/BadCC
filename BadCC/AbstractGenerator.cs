using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    abstract class AbstractGenerator : IGenerator
    {
        protected class LocalVariableMap
        {
            private HashSet<string> newlyDeclaredVars;
            private int newlyDeclaredByteSize;
            private ImmutableDictionary<string, int> map;
            private int offset;

            /// <summary>
            /// The size of this map's own new scope variables in bytes
            /// </summary>
            public int ScopeByteSize => newlyDeclaredByteSize;

            public LocalVariableMap(LocalVariableMap template)
            {
                map = template.map; // We can do this since map is immutable, e.g. if we 'change it' we'll just get a copy and the original one remains the same
                offset = template.offset;
                newlyDeclaredVars = new HashSet<string>();  // Needs to be new
            }

            public LocalVariableMap()
            {
                var builder = ImmutableDictionary.CreateBuilder<string, int>();
                map = builder.ToImmutable();
                offset = -4;
                newlyDeclaredVars = new HashSet<string>();
            }

            public bool ContainsVariable(string name)
            {
                return map.ContainsKey(name);
            }

            public bool DeclaredVariable(string name)
            {
                return newlyDeclaredVars.Contains(name);
            }

            public int GetOffset(string name)
            {
                return map[name];
            }

            public bool TryGetOffset(string name, out int offset)
            {
                return map.TryGetValue(name, out offset);
            }

            public int AddInt(string name)
            {
                newlyDeclaredVars.Add(name);
                map = map.SetItem(name, offset);
                offset -= 4;
                newlyDeclaredByteSize += 4;
                return offset;
            }

            public int AddParamInt(string name, int paramIdxFromLeft)
            {
                newlyDeclaredVars.Add(name);
                map = map.SetItem(name, 8 + paramIdxFromLeft * 4);
                return offset;
            }
        }

        protected class LoopData
        {
            public string ContinueLabel { get; private set; }
            public string BreakLabel { get; private set; }
            /// <summary>
            /// The map that break and continue should exit toward
            /// </summary>
            public LocalVariableMap LoopLevelMap { get; private set; }

            public LoopData(string continueLabel, string breakLabel, LocalVariableMap loopLevelMap)
            {
                ContinueLabel = continueLabel;
                BreakLabel = breakLabel;
                LoopLevelMap = loopLevelMap;
            }
        }

        protected LocalVariableMap CurrentVariableMap => localVariableMaps.Peek();
        protected LoopData CurrentLoopData => loopDatas.Peek();

        protected StreamWriter writer;
        protected Stack<LocalVariableMap> localVariableMaps;
        protected Stack<LoopData> loopDatas;

        protected int labelCounter;
        protected FunctionNode currentFunction;

        public AbstractGenerator(StreamWriter writer)
        {
            this.writer = writer;
            localVariableMaps = new Stack<LocalVariableMap>();
            loopDatas = new Stack<LoopData>();
        }

        public abstract void GenerateProgram(ProgramNode program);

        public virtual void SetWriter(StreamWriter writer)
        {
            this.writer = writer;
        }
    }
}
