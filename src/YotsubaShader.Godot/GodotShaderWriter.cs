#region MS-PL License - Copyright (c) 2026 Memphis Yomael Núñez Cruz - Dominican Republic
/*
╔════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                    ║
║  ███╗   ███╗███████╗      ██████╗ ██╗                                              ║
║  ████╗ ████║██╔════╝      ██╔══██╗██║                                              ║
║  ██╔████╔██║███████╗█████╗██████╔╝██║                                              ║
║  ██║╚██╔╝██║╚════██║╚════╝██╔═══╝ ██║                                              ║
║  ██║ ╚═╝ ██║███████║      ██║     ███████╗                                         ║
║  ╚═╝     ╚═╝╚══════╝      ╚═╝     ╚══════╝                                         ║
║                                                                                    ║
╠════════════════════════════════════════════════════════════════════════════════════╣
║  Copyright (c) 2026 Memphis Yomael Núñez Cruz - Dominican Republic                 ║
╠════════════════════════════════════════════════════════════════════════════════════╣
║                                                                                    ║
║  Microsoft Public License (MS-PL)                                                  ║
║                                                                                    ║
║  This license governs use of the accompanying software. If you use the software,   ║
║  you accept this license. If you do not accept the license, do not use the         ║
║  software.                                                                         ║
║                                                                                    ║
║  1. Definitions                                                                    ║
║  The terms "reproduce," "reproduction," "derivative works," and "distribution"     ║
║  have the same meaning here as under U.S. copyright law.                           ║
║  A "contribution" is the original software, to which copyright notices are         ║
║  attached, and any additions or changes to the software.                           ║
║  A "contributor" is any person that distributes its contribution under this        ║
║  license.                                                                          ║
║  "Licensed patents" are a contributor's patent claims that read directly on its    ║
║  contribution.                                                                     ║
║                                                                                    ║
║  2. Grant of Rights                                                                ║
║  (A) Copyright Grant- Subject to the terms of this license, including the license  ║
║  conditions and limitations in section 3, each contributor grants you a non-       ║
║  exclusive, worldwide, royalty-free copyright license to reproduce its             ║
║  contribution, prepare derivative works of its contribution, and distribute its    ║
║  contribution or any derivative works that you create.                             ║
║  (B) Patent Grant- Subject to the terms of this license, including the license     ║
║  conditions and limitations in section 3, each contributor grants you a non-       ║
║  exclusive, worldwide, royalty-free license under its licensed patents to make,    ║
║  have made, use, sell, offer for sale, import, and/or otherwise dispose of its     ║
║  contribution in the software or derivative works of the contribution in the       ║
║  software.                                                                         ║
║                                                                                    ║
║  3. Conditions and Limitations                                                     ║
║  (A) No Trademark License- This license does not grant you rights to use any       ║
║  contributors' name, logo, or trademarks.                                          ║
║  (B) If you bring a patent claim against any contributor over patents that you     ║
║  claim are infringed by the software, your patent license from such contributor    ║
║  to the software ends automatically.                                               ║
║  (C) If you distribute any portion of the software, you must retain all copyright, ║
║  patent, trademark, and attribution notices that are present in the software.      ║
║  (D) If you distribute any portion of the software in source code form, you may do ║
║  so only under this license by including a complete copy of this license with      ║
║  your distribution. If you distribute any portion of the software in compiled or   ║
║  object code form, you may only do so under a license that complies with this      ║
║  license.                                                                          ║
║  (E) The software is licensed "as-is." You bear the risk of using it. The          ║
║  contributors give no express warranties, guarantees or conditions. You may have   ║
║  additional consumer rights under your local laws which this license cannot        ║
║  change. To the extent permitted under your local laws, the contributors exclude   ║
║  the implied warranties of merchantability, fitness for a particular purpose and   ║
║  non-infringement.                                                                 ║
║                                                                                    ║
╚════════════════════════════════════════════════════════════════════════════════════╝
*/
#endregion

using System.Globalization;
using System.Text;
using YotsubaShader.IR.Intermediate;
using YotsubaShader.IR.Intermediate.FunctionBody;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.AssignmentTargets;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.FunctionCalls;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.Literals;
using YotsubaShader.IR.Intermediate.FunctionBody.Statements;
using YotsubaShader.IR.Intermediate.Slot;
using YotsubaShader.IR.Intermediate.Slot.Types;
using YotsubaShader.IR.Intermediate.ValueConvertions;
using IRTypes = YotsubaShader.IR.Intermediate.Types;

namespace YotsubaShader.Godot;

/// <summary>Raised when the IR cannot be expressed in Godot's shading language.</summary>
public sealed class GodotBackendException : Exception
{
    public GodotBackendException(string message) : base(message)
    {
    }
}

/// <summary>
/// Writes Godot shading language (a <c>spatial</c> shader) from the Yotsuba IR. It only reads the IR.
/// </summary>
/// <remarks>
/// Godot shaders have no stages with free-form interfaces, so the IR's entry points are mapped to Godot's
/// built-ins: the vertex entry point becomes <c>vertex()</c>, the fragment one <c>fragment()</c>, vertex outputs
/// with a location or semantic become <c>varying</c> variables, and uniform resources become <c>uniform</c>
/// variables (a struct is flattened into one uniform per field, named <c>structname_field</c>).
/// Compute entry points have no equivalent and are rejected.
/// </remarks>
public static class GodotShaderWriter
{
    public static string Write(Shader shader, string shaderName) => new Writer(shader, shaderName).Write();
}

internal sealed class Writer
{
    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "if", "else", "for", "while", "do", "switch", "case", "default", "break", "continue", "return", "discard",
        "const", "uniform", "varying", "in", "out", "inout", "flat", "smooth", "lowp", "mediump", "highp", "struct",
        "true", "false", "void", "bool", "int", "uint", "float", "vec2", "vec3", "vec4", "ivec2", "ivec3", "ivec4",
        "uvec2", "uvec3", "uvec4", "bvec2", "bvec3", "bvec4", "mat2", "mat3", "mat4", "sampler2D", "shader_type",
        "render_mode", "group_uniforms", "global", "instance", "input", "output", "precision", "layout",
        "radians", "degrees", "sin", "cos", "tan", "asin", "acos", "atan", "sinh", "cosh", "tanh", "asinh", "acosh",
        "atanh", "pow", "exp", "exp2", "log", "log2", "sqrt", "inversesqrt", "abs", "sign", "floor", "trunc",
        "round", "roundEven", "ceil", "fract", "mod", "modf", "min", "max", "clamp", "mix", "step", "smoothstep",
        "isnan", "isinf", "length", "distance", "dot", "cross", "normalize", "reflect", "refract", "faceforward",
        "transpose", "determinant", "inverse", "any", "all", "not", "texture", "textureLod", "textureSize",
        "texelFetch", "dFdx", "dFdy", "fwidth", "vertex", "fragment", "light", "start", "process", "sky", "fog"
    };

    private readonly Shader _shader;
    private readonly string _name;
    private readonly StringBuilder _out = new();
    private int _indent;

    private readonly Dictionary<uint, Function> _functions = new();
    private readonly Dictionary<uint, string> _functionNames = new();
    private readonly HashSet<string> _globalNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IRTypes.CustomStruct> _structs = new(StringComparer.Ordinal);
    private readonly List<string> _structOrder = new();

    // Interface mapping.
    private readonly Dictionary<InterfaceSlot, Varying> _varyings = new();
    private readonly Dictionary<Storage, UniformNode> _uniforms = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Storage, InterfaceNode<string>> _inputs = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Storage, InterfaceNode<OutputTarget>> _outputs = new(ReferenceEqualityComparer.Instance);

    // Per function.
    private Dictionary<Storage, string> _locals = new(ReferenceEqualityComparer.Instance);
    private HashSet<string> _localNames = new(StringComparer.Ordinal);
    private Function _current;
    private readonly Stack<LoopContext> _loops = new();
    private int _counter;

    // Godot forbids `return` inside vertex() and fragment(). An entry point whose `return` is not its final
    // statement runs inside a single-pass loop instead, and a `return` becomes "set the flag, break".
    private bool _wrapped;
    private string _doneFlag = "";

    public Writer(Shader shader, string name)
    {
        _shader = shader;
        _name = name;
    }

    private sealed record Varying(string Name, string Type, bool Flat);

    private sealed record OutputTarget(string Type, Func<string, string> Assign);

    private sealed class InterfaceNode<T>
    {
        public T? Value;
        public Dictionary<string, T> Members { get; } = new(StringComparer.Ordinal);
    }

    private sealed class UniformNode
    {
        public string? Name;
        public Dictionary<string, UniformNode> Children { get; } = new(StringComparer.Ordinal);
    }

    private sealed record LoopContext(Block Continuing, Expression? BottomCondition);

    // ---- Entry ---------------------------------------------------------------------------------------------

    public string Write()
    {
        foreach (var function in _shader.Functions) _functions[function.Id.Value] = function;

        var vertex = Entry(Stage.Vertex);
        var fragment = Entry(Stage.Fragment);
        if (vertex is null && fragment is null)
        {
            throw new GodotBackendException(_shader.Functions.Any(f => f.Stage == Stage.Compute)
                ? "Godot's shading language has no compute stage. A compute shader runs through the RenderingDevice " +
                  "with SPIR-V; use the SPIR-V output instead."
                : "The shader has no vertex or fragment entry point.");
        }

        ReserveGlobalNames();
        CollectStructs();
        var uniformLines = BuildUniforms();
        if (vertex is { } v) BuildVertexInterface(v);
        if (fragment is { } f) BuildFragmentInterface(f);

        Line($"// Generated by Yotsuba Shader (GDScript frontend) from '{_name}'. Do not edit.");
        Line("shader_type spatial;");
        Line("render_mode unshaded;");
        Line("");

        foreach (string line in uniformLines) Line(line);
        if (uniformLines.Count > 0) Line("");

        foreach (var varying in _varyings.Values)
        {
            Line($"varying {(varying.Flat ? "flat " : "")}{varying.Type} {varying.Name};");
        }

        if (_varyings.Count > 0) Line("");

        foreach (string typeName in _structOrder)
        {
            var structure = _structs[typeName];
            Line($"struct {typeName} {{");
            _indent++;
            foreach (var member in structure.Properties) Line($"{TypeOf(member)} {Field(member.HumanName)};");
            _indent--;
            Line("};");
            Line("");
        }

        foreach (var helper in HelpersInCallOrder())
        {
            EmitHelper(helper);
            Line("");
        }

        if (vertex is { } vertexFunction)
        {
            EmitEntry(vertexFunction, "vertex");
            Line("");
        }

        if (fragment is { } fragmentFunction) EmitEntry(fragmentFunction, "fragment");

        return _out.ToString().TrimEnd() + "\n";
    }

    private Function? Entry(Stage stage)
    {
        var entries = _shader.Functions.Where(f => f.Stage == stage).ToList();
        if (entries.Count > 1)
        {
            throw new GodotBackendException(
                $"Godot shaders have a single {stage.ToString().ToLowerInvariant()}() function, but the shader has {entries.Count} entry points.");
        }

        return entries.Count == 1 ? entries[0] : null;
    }

    // ---- Names ----------------------------------------------------------------------------------------------

    private static string Safe(string name)
    {
        // Double underscores are reserved in GLSL, and the frontends name their temporaries `__select` and so on.
        string result = name.StartsWith("__", StringComparison.Ordinal) ? "yb_" + name.TrimStart('_') : name;
        result = result.Replace("__", "_");
        result = result.TrimStart('_');
        if (result.Length == 0 || char.IsDigit(result[0])) result = "yb_" + result;
        if (Reserved.Contains(result) || result.StartsWith("gl_", StringComparison.Ordinal) ||
            result.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '_'))
        {
            result += "_";
        }

        return result;
    }

    private static string Field(string name) => Safe(name);

    private void ReserveGlobalNames()
    {
        foreach (var function in _shader.Functions)
        {
            string name = function.Stage switch
            {
                Stage.Vertex => "vertex",
                Stage.Fragment => "fragment",
                _ => UniqueGlobal(Safe(function.HumanName))
            };
            _functionNames[function.Id.Value] = name;
        }

        foreach (var resource in _shader.GlobalResources) _globalNames.Add(Safe(resource.HumanName));
    }

    private string UniqueGlobal(string preferred)
    {
        string name = preferred;
        for (int i = 1; !_globalNames.Add(name); i++) name = $"{preferred}_{i}";
        return name;
    }

    private string LocalName(Storage storage)
    {
        if (_locals.TryGetValue(storage, out string? existing)) return existing;

        string preferred = Safe(storage.HumanName);
        string name = preferred;
        for (int i = 1; _globalNames.Contains(name) || !_localNames.Add(name); i++) name = $"{preferred}_{i}";
        _locals[storage] = name;
        return name;
    }

    private string Temporary(string prefix)
    {
        string name;
        do name = $"yb_{prefix}{_counter++}";
        while (_globalNames.Contains(name) || !_localNames.Add(name));
        return name;
    }

    // ---- Types ----------------------------------------------------------------------------------------------

    private string TypeOf(Storage storage)
    {
        switch (storage)
        {
            case IRTypes.Float:
                return "float";
            case IRTypes.Bool:
                return "bool";
            case IRTypes.Int { Bits: 32 } integer:
                return integer.Signed ? "int" : "uint";
            case IRTypes.Vector vector:
                string prefix = vector.ComponentsType switch
                {
                    IRTypes.VectorComponentsType.Float => "vec",
                    IRTypes.VectorComponentsType.Int => "ivec",
                    IRTypes.VectorComponentsType.Uint => "uvec",
                    _ => throw new GodotBackendException("Double precision vectors do not exist in Godot shaders.")
                };
                return $"{prefix}{vector.ComponentsLength}";
            case IRTypes.CustomStruct structure:
                return Safe(structure.TypeName);
            case IRTypes.Double:
                throw new GodotBackendException("Godot shaders have no double precision type.");
            case IRTypes.Matrix:
            case IRTypes.Array:
                throw new GodotBackendException($"'{storage.HumanName}': matrices and arrays are not supported by the Godot backend yet.");
            default:
                throw new GodotBackendException($"'{storage.HumanName}': {storage.GetType().Name} has no Godot shading language type.");
        }
    }

    private string ZeroOf(Storage storage) => storage switch
    {
        IRTypes.Float => "0.0",
        IRTypes.Bool => "false",
        IRTypes.Int { Signed: true } => "0",
        IRTypes.Int => "0u",
        IRTypes.Vector { ComponentsType: IRTypes.VectorComponentsType.Float } v => $"{TypeOf(v)}(0.0)",
        IRTypes.Vector v => $"{TypeOf(v)}(0)",
        IRTypes.CustomStruct s => $"{TypeOf(s)}({string.Join(", ", s.Properties.Select(ZeroOf))})",
        _ => throw new GodotBackendException($"'{storage.HumanName}' has no default value in Godot shaders.")
    };

    private void CollectStructs()
    {
        void Register(Storage storage)
        {
            if (storage is not IRTypes.CustomStruct structure) return;
            string name = Safe(structure.TypeName);
            if (_structs.ContainsKey(name)) return;
            foreach (var member in structure.Properties) Register(member);
            _structs[name] = structure;
            _structOrder.Add(name);
        }

        var reachable = HelpersInCallOrder().ToList();
        foreach (var function in _shader.Functions.Where(f => f.Stage is Stage.Vertex or Stage.Fragment).Concat(reachable))
        {
            if (function.Stage == Stage.None)
            {
                foreach (var input in function.Inputs) Register(input);
                foreach (var output in function.Outputs) Register(output);
            }

            WalkStatements(function.Body, Register);
        }
    }

    private static void WalkStatements(IEnumerable<Statement> statements, Action<Storage> onStorage)
    {
        foreach (var statement in statements) WalkStatement(statement, onStorage);
    }

    private static void WalkStatement(Statement statement, Action<Storage> onStorage)
    {
        switch (statement)
        {
            case VariableDeclaration declaration:
                onStorage(declaration.Storage);
                if (declaration.Initializer is not null) WalkExpression(declaration.Initializer, onStorage);
                break;
            case Assignment assignment:
                WalkExpression(assignment.Target, onStorage);
                WalkExpression(assignment.Value, onStorage);
                break;
            case Block block:
                WalkStatements(block.Statements, onStorage);
                break;
            case If branch:
                WalkExpression(branch.Condition, onStorage);
                WalkStatement(branch.Then, onStorage);
                if (branch.Else is not null) WalkStatement(branch.Else, onStorage);
                break;
            case Loop loop:
                if (loop.Condition is not null) WalkExpression(loop.Condition, onStorage);
                if (loop.BottomCondition is not null) WalkExpression(loop.BottomCondition, onStorage);
                WalkStatement(loop.Body, onStorage);
                WalkStatement(loop.Continuing, onStorage);
                break;
            case Return { Value: not null } ret:
                WalkExpression(ret.Value, onStorage);
                break;
            case FunctionCallStatement call:
                foreach (var argument in call.Arguments) WalkExpression(argument, onStorage);
                break;
        }
    }

    private static void WalkExpression(Expression expression, Action<Storage> onStorage)
    {
        switch (expression)
        {
            case CustomStructExpression structure:
                onStorage(structure.StructType);
                foreach (var property in structure.Properties) WalkExpression(property, onStorage);
                break;
            case SequenceExpression sequence:
                WalkStatements(sequence.Prelude, onStorage);
                WalkExpression(sequence.Value, onStorage);
                break;
            case BinaryExpression binary:
                WalkExpression(binary.Left, onStorage);
                WalkExpression(binary.Right, onStorage);
                break;
            case UnaryExpression unary:
                WalkExpression(unary.Operand, onStorage);
                break;
            case NumericConversion conversion:
                WalkExpression(conversion.Operand, onStorage);
                break;
            case VectorExpression vector:
                foreach (var value in vector.Values) WalkExpression(value, onStorage);
                break;
            case FunctionCallExpression call:
                foreach (var argument in call.Arguments) WalkExpression(argument, onStorage);
                break;
            case StorageStructPropertyReference property:
                WalkExpression(property.Reference, onStorage);
                break;
        }
    }

    // ---- Uniforms ---------------------------------------------------------------------------------------------

    private List<string> BuildUniforms()
    {
        var lines = new List<string>();

        void Flatten(Storage storage, string name, UniformNode node)
        {
            if (storage is IRTypes.CustomStruct structure)
            {
                foreach (var member in structure.Properties)
                {
                    var child = new UniformNode();
                    node.Children[member.HumanName] = child;
                    Flatten(member, $"{name}_{member.HumanName}", child);
                }

                return;
            }

            string uniformName = UniqueGlobal(Safe(name));
            node.Name = uniformName;
            lines.Add($"uniform {TypeOf(storage)} {uniformName};");
        }

        foreach (var resource in _shader.GlobalResources)
        {
            if (resource.Kind is not (StorageKind.Uniform or StorageKind.PushConstant))
            {
                throw new GodotBackendException(
                    $"'{resource.HumanName}' is a {resource.Kind} resource, which Godot shaders cannot express " +
                    "(only uniforms and push constants map to Godot uniforms).");
            }

            var node = new UniformNode();
            _uniforms[resource] = node;
            // The resource name was reserved already; the first uniform of a scalar resource takes it.
            _globalNames.Remove(Safe(resource.HumanName));
            Flatten(resource, resource.HumanName, node);
        }

        return lines;
    }

    // ---- Interface -----------------------------------------------------------------------------------------

    private static string SlotName(InterfaceSlot slot) => slot switch
    {
        LocationSlot l => $"loc{l.Location}",
        VertexSlot v => $"{v.Semantic.Usage.ToString().ToLowerInvariant()}{v.Semantic.Index}",
        _ => throw new GodotBackendException($"Cannot name a varying for slot {slot}.")
    };

    private static IEnumerable<(string? Member, Storage Storage)> Parts(Storage storage) =>
        storage is IRTypes.CustomStruct structure
            ? structure.Properties.Select(p => ((string?)p.HumanName, p))
            : [((string?)null, storage)];

    private static string Adapt(string expression, string from, string to, string what)
    {
        if (from == to) return expression;
        switch (from, to)
        {
            case ("vec3", "vec2"):
            case ("vec4", "vec2"):
                return $"{expression}.xy";
            case ("vec4", "vec3"):
                return $"{expression}.xyz";
            case ("vec2", "vec3"):
                return $"vec3({expression}, 0.0)";
            case ("vec2", "vec4"):
                return $"vec4({expression}, 0.0, 1.0)";
            case ("vec3", "vec4"):
                return $"vec4({expression}, 1.0)";
            case ("int", "uint"):
                return $"uint({expression})";
            case ("int", "float"):
                return $"float({expression})";
        }

        throw new GodotBackendException($"{what} has type '{to}', but Godot provides it as '{from}'.");
    }

    private void BuildVertexInterface(Function function)
    {
        foreach (var input in function.Inputs)
        {
            var node = new InterfaceNode<string>();
            foreach (var (member, storage) in Parts(input))
            {
                string what = $"'{input.HumanName}{(member is null ? "" : "." + member)}'";
                (string source, string type) = VertexInputSource(storage.Slot, what);
                string expression = Adapt(source, type, TypeOf(storage), what);
                if (member is null) node.Value = expression;
                else node.Members[member] = expression;
            }

            _inputs[input] = node;
        }

        foreach (var output in function.Outputs)
        {
            var node = new InterfaceNode<OutputTarget>();
            foreach (var (member, storage) in Parts(output))
            {
                string what = $"'{(member ?? output.HumanName)}'";
                var target = VertexOutputTarget(storage, what);
                if (member is null) node.Value = target;
                else node.Members[member] = target;
            }

            _outputs[output] = node;
        }
    }

    private static (string Expression, string Type) VertexInputSource(InterfaceSlot? slot, string what) => slot switch
    {
        VertexSlot { Semantic: { Usage: VertexUsage.Position } } => ("VERTEX", "vec3"),
        VertexSlot { Semantic: { Usage: VertexUsage.Color } } => ("COLOR", "vec4"),
        VertexSlot { Semantic: { Usage: VertexUsage.TextureCoordinate, Index: 0 } } => ("UV", "vec2"),
        VertexSlot { Semantic: { Usage: VertexUsage.TextureCoordinate, Index: 1 } } => ("UV2", "vec2"),
        VertexSlot { Semantic: { Usage: VertexUsage.Normal } } => ("NORMAL", "vec3"),
        VertexSlot { Semantic: { Usage: VertexUsage.Tangent } } => ("TANGENT", "vec3"),
        VertexSlot { Semantic: { Usage: VertexUsage.Binormal } } => ("BINORMAL", "vec3"),
        BuiltInSlot { BuiltIn: BuiltIn.VertexIndex } => ("VERTEX_ID", "int"),
        BuiltInSlot { BuiltIn: BuiltIn.InstanceIndex } => ("INSTANCE_ID", "int"),
        LocationSlot { Location: >= 0 and <= 3 } l => ($"CUSTOM{l.Location}", "vec4"),
        null => throw new GodotBackendException($"{what} is a vertex input without a semantic, location or built-in."),
        _ => throw new GodotBackendException($"{what}: the vertex input {slot} has no Godot equivalent.")
    };

    private OutputTarget VertexOutputTarget(Storage storage, string what)
    {
        string type = TypeOf(storage);
        switch (storage.Slot)
        {
            case BuiltInSlot { BuiltIn: BuiltIn.Position }:
                if (type != "vec4") throw new GodotBackendException($"{what}: the position must be a Vector4.");
                return new OutputTarget(type, v => $"POSITION = {v};");
            case BuiltInSlot { BuiltIn: BuiltIn.PointSize }:
                return new OutputTarget(type, v => $"POINT_SIZE = {v};");
            case LocationSlot or VertexSlot:
                var slot = storage.Slot;
                if (!_varyings.TryGetValue(slot, out var varying))
                {
                    varying = new Varying("v_" + SlotName(slot), type, storage is IRTypes.Int);
                    _varyings[slot] = varying;
                }

                if (varying.Type != type)
                {
                    throw new GodotBackendException($"{what}: two vertex outputs share {slot} with different types.");
                }

                return new OutputTarget(type, v => $"{varying.Name} = {v};");
            case null:
                throw new GodotBackendException($"{what} is a vertex output without a location, semantic or built-in.");
            default:
                throw new GodotBackendException($"{what}: the vertex output {storage.Slot} has no Godot equivalent.");
        }
    }

    private void BuildFragmentInterface(Function function)
    {
        foreach (var input in function.Inputs)
        {
            var node = new InterfaceNode<string>();
            foreach (var (member, storage) in Parts(input))
            {
                string what = $"'{input.HumanName}{(member is null ? "" : "." + member)}'";
                string expression;
                switch (storage.Slot)
                {
                    case BuiltInSlot { BuiltIn: BuiltIn.FragCoord }:
                        expression = Adapt("FRAGCOORD", "vec4", TypeOf(storage), what);
                        break;
                    case BuiltInSlot { BuiltIn: BuiltIn.FrontFacing }:
                        expression = Adapt("FRONT_FACING", "bool", TypeOf(storage), what);
                        break;
                    case LocationSlot or VertexSlot:
                        if (!_varyings.TryGetValue(storage.Slot, out var varying))
                        {
                            throw new GodotBackendException(
                                $"{what}: no vertex output provides {storage.Slot}, so the fragment shader cannot read it.");
                        }

                        if (varying.Type != TypeOf(storage))
                        {
                            throw new GodotBackendException(
                                $"{what}: has type '{TypeOf(storage)}' but the vertex output is '{varying.Type}'.");
                        }

                        expression = varying.Name;
                        break;
                    default:
                        throw new GodotBackendException($"{what}: the fragment input {storage.Slot} has no Godot equivalent.");
                }

                if (member is null) node.Value = expression;
                else node.Members[member] = expression;
            }

            _inputs[input] = node;
        }

        foreach (var output in function.Outputs)
        {
            var node = new InterfaceNode<OutputTarget>();
            foreach (var (member, storage) in Parts(output))
            {
                string what = $"'{(member ?? output.HumanName)}'";
                var target = FragmentOutputTarget(storage, what);
                if (member is null) node.Value = target;
                else node.Members[member] = target;
            }

            _outputs[output] = node;
        }
    }

    private OutputTarget FragmentOutputTarget(Storage storage, string what)
    {
        string type = TypeOf(storage);
        switch (storage.Slot)
        {
            case LocationSlot { Location: 0 }:
                return type switch
                {
                    "vec4" => new OutputTarget(type, v => $"ALBEDO = {v}.rgb; ALPHA = {v}.a;"),
                    "vec3" => new OutputTarget(type, v => $"ALBEDO = {v};"),
                    _ => throw new GodotBackendException($"{what}: the color output must be a Vector3 or a Vector4.")
                };
            case BuiltInSlot { BuiltIn: BuiltIn.FragDepth }:
                return new OutputTarget(type, v => $"DEPTH = {v};");
            case LocationSlot l:
                throw new GodotBackendException($"{what}: Godot shaders have one color output; location {l.Location} is not supported.");
            case null:
                throw new GodotBackendException($"{what} is a fragment output without a location or built-in.");
            default:
                throw new GodotBackendException($"{what}: the fragment output {storage.Slot} has no Godot equivalent.");
        }
    }

    // ---- Functions -------------------------------------------------------------------------------------------

    private IEnumerable<Function> HelpersInCallOrder()
    {
        var visited = new HashSet<uint>();
        var order = new List<Function>();

        void Visit(Function function, HashSet<uint> path)
        {
            if (!visited.Add(function.Id.Value)) return;
            path.Add(function.Id.Value);
            foreach (uint callee in CalleesOf(function))
            {
                if (path.Contains(callee)) throw new GodotBackendException("Recursion is not supported in Godot shaders.");
                if (_functions.TryGetValue(callee, out var target)) Visit(target, path);
            }

            path.Remove(function.Id.Value);
            if (function.Stage == Stage.None) order.Add(function);
        }

        // Compute entry points have no Godot equivalent, so what only they call is not written.
        foreach (var function in _shader.Functions.Where(f => f.Stage is Stage.Vertex or Stage.Fragment))
        {
            Visit(function, new HashSet<uint>());
        }

        return order;
    }

    private static IEnumerable<uint> CalleesOf(Function function)
    {
        var ids = new List<uint>();

        void Statements(IEnumerable<Statement> statements)
        {
            foreach (var s in statements) Statement(s);
        }

        void Statement(Statement statement)
        {
            switch (statement)
            {
                case FunctionCallStatement call:
                    ids.Add(call.Target.Value);
                    foreach (var a in call.Arguments) Expr(a);
                    break;
                case VariableDeclaration { Initializer: not null } d:
                    Expr(d.Initializer);
                    break;
                case Assignment a:
                    Expr(a.Target);
                    Expr(a.Value);
                    break;
                case Block b:
                    Statements(b.Statements);
                    break;
                case If i:
                    Expr(i.Condition);
                    Statement(i.Then);
                    if (i.Else is not null) Statement(i.Else);
                    break;
                case Loop l:
                    if (l.Condition is not null) Expr(l.Condition);
                    if (l.BottomCondition is not null) Expr(l.BottomCondition);
                    Statement(l.Body);
                    Statement(l.Continuing);
                    break;
                case Return { Value: not null } r:
                    Expr(r.Value);
                    break;
            }
        }

        void Expr(Expression expression)
        {
            switch (expression)
            {
                case FunctionCallExpression call:
                    ids.Add(call.Target.Value);
                    foreach (var a in call.Arguments) Expr(a);
                    break;
                case SequenceExpression s:
                    Statements(s.Prelude);
                    Expr(s.Value);
                    break;
                case BinaryExpression b:
                    Expr(b.Left);
                    Expr(b.Right);
                    break;
                case UnaryExpression u:
                    Expr(u.Operand);
                    break;
                case NumericConversion c:
                    Expr(c.Operand);
                    break;
                case VectorExpression v:
                    foreach (var x in v.Values) Expr(x);
                    break;
                case CustomStructExpression s:
                    foreach (var x in s.Properties) Expr(x);
                    break;
                case StorageStructPropertyReference p:
                    Expr(p.Reference);
                    break;
            }
        }

        Statements(function.Body);
        return ids;
    }

    private void BeginFunction(Function function)
    {
        _current = function;
        _locals = new Dictionary<Storage, string>(ReferenceEqualityComparer.Instance);
        _localNames = new HashSet<string>(StringComparer.Ordinal);
        _loops.Clear();
    }

    private void EmitHelper(Function function)
    {
        BeginFunction(function);
        _wrapped = false;
        string returnType = function.Outputs.Count == 0 ? "void" : TypeOf(function.Outputs[0]);
        string parameters = string.Join(", ", function.Inputs.Select(i => $"{TypeOf(i)} {LocalName(i)}"));
        Line($"{returnType} {_functionNames[function.Id.Value]}({parameters}) {{");
        _indent++;
        EmitStatements(function.Body);
        _indent--;
        Line("}");
    }

    private void EmitEntry(Function function, string name)
    {
        BeginFunction(function);
        int returns = function.Body.Sum(CountReturns);
        _wrapped = !(returns == 0 || (returns == 1 && function.Body.Count > 0 && function.Body[^1] is Return));

        Line($"void {name}() {{");
        _indent++;
        if (_wrapped)
        {
            _doneFlag = Temporary("done");
            Line($"bool {_doneFlag} = false;");
            string once = Temporary("once");
            Line($"for (int {once} = 0; {once} < 1; {once}++) {{");
            _indent++;
        }

        EmitStatements(function.Body);
        if (_wrapped)
        {
            _indent--;
            Line("}");
        }

        _indent--;
        Line("}");
        _wrapped = false;
    }

    private static int CountReturns(Statement statement) => statement switch
    {
        Return => 1,
        Block block => block.Statements.Sum(CountReturns),
        If branch => CountReturns(branch.Then) + (branch.Else is null ? 0 : CountReturns(branch.Else)),
        Loop loop => CountReturns(loop.Body) + CountReturns(loop.Continuing),
        _ => 0
    };

    // ---- Statements ------------------------------------------------------------------------------------------

    private void Line(string text) => _out.Append(' ', _indent * 4).Append(text).Append('\n');

    private void EmitStatements(IEnumerable<Statement> statements)
    {
        foreach (var statement in statements) EmitStatement(statement);
    }

    private void EmitBlockBody(Statement statement)
    {
        _indent++;
        if (statement is Block block) EmitStatements(block.Statements);
        else EmitStatement(statement);
        _indent--;
    }

    private void EmitStatement(Statement statement)
    {
        switch (statement)
        {
            case Block block:
                // A nested IR block is a plain scope; frontends use it to group declarations.
                EmitStatements(block.Statements);
                break;
            case VariableDeclaration declaration:
                string init = declaration.Initializer is null ? ZeroOf(declaration.Storage) : Bare(Expr(declaration.Initializer));
                Line($"{TypeOf(declaration.Storage)} {LocalName(declaration.Storage)} = {init};");
                break;
            case Assignment assignment:
                EmitAssignment(assignment);
                break;
            case If branch:
                string condition = Bare(Expr(branch.Condition));
                Line($"if ({condition}) {{");
                EmitBlockBody(branch.Then);
                if (branch.Else is not null)
                {
                    Line("} else {");
                    EmitBlockBody(branch.Else);
                }

                Line("}");
                break;
            case Loop loop:
                EmitLoop(loop);
                break;
            case Break:
                Line("break;");
                break;
            case Continue:
                EmitContinue();
                break;
            case Return ret:
                EmitReturn(ret);
                break;
            case FunctionCallStatement call:
                Line($"{CallText(call.Target, call.Arguments)};");
                break;
            default:
                throw new GodotBackendException($"The statement '{statement.GetType().Name}' is not supported by the Godot backend.");
        }
    }

    private void EmitAssignment(Assignment assignment)
    {
        if (ResolveOutput(assignment.Target) is { } output)
        {
            string value = Bare(Expr(assignment.Value));
            string temp = Temporary("out");
            Line("{");
            _indent++;
            Line($"{output.Type} {temp} = {value};");
            Line(output.Assign(temp));
            _indent--;
            Line("}");
            return;
        }

        string target = LValue(assignment.Target);
        Line($"{target} = {Bare(Expr(assignment.Value))};");
    }

    private void EmitReturn(Return ret)
    {
        if (_current.Stage == Stage.None)
        {
            Line(ret.Value is null ? "return;" : $"return {Bare(Expr(ret.Value))};");
            return;
        }

        if (ret.Value is not null && _current.Outputs.Count > 0)
        {
            var output = _current.Outputs[0];
            var node = _outputs[output];
            string value = Bare(Expr(ret.Value));
            string temp = Temporary("ret");
            Line("{");
            _indent++;
            Line($"{TypeOf(output)} {temp} = {value};");
            if (output is IRTypes.CustomStruct)
            {
                foreach (var (member, target) in node.Members) Line(target.Assign($"{temp}.{Field(member)}"));
            }
            else
            {
                Line(node.Value!.Assign(temp));
            }

            if (_wrapped) EmitLeave();
            _indent--;
            Line("}");
            return;
        }

        if (_wrapped) EmitLeave();
    }

    private void EmitLeave()
    {
        Line($"{_doneFlag} = true;");
        Line("break;");
    }

    /// <summary>After a loop inside a wrapped entry point: a `return` inside the loop must leave the entry point too.</summary>
    private void EmitLeaveCheck()
    {
        if (!_wrapped) return;
        Line($"if ({_doneFlag}) {{");
        _indent++;
        Line("break;");
        _indent--;
        Line("}");
    }

    private void EmitLoop(Loop loop)
    {
        _loops.Push(new LoopContext(loop.Continuing, loop.BottomCondition));
        try
        {
            bool simple = loop.Condition is not null && loop.BottomCondition is null && !HasSequence(loop.Condition);
            if (simple)
            {
                Line($"while ({Bare(Expr(loop.Condition!))}) {{");
                EmitBlockBody(loop.Body);
                _indent++;
                EmitStatements(loop.Continuing.Statements);
                _indent--;
                Line("}");
                EmitLeaveCheck();
                return;
            }

            Line("while (true) {");
            _indent++;
            if (loop.Condition is not null)
            {
                // The condition may need statements of its own (a conditional expression), so it lives inside the loop.
                string condition = Bare(Expr(loop.Condition));
                Line($"if (!({condition})) {{");
                _indent++;
                Line("break;");
                _indent--;
                Line("}");
            }

            if (loop.Body is Block body) EmitStatements(body.Statements);
            else EmitStatement(loop.Body);
            EmitStatements(loop.Continuing.Statements);
            if (loop.BottomCondition is not null) EmitBottomCheck(loop.BottomCondition);
            _indent--;
            Line("}");
            EmitLeaveCheck();
        }
        finally
        {
            _loops.Pop();
        }
    }

    private void EmitBottomCheck(Expression bottom)
    {
        string condition = Bare(Expr(bottom));
        Line($"if (!({condition})) {{");
        _indent++;
        Line("break;");
        _indent--;
        Line("}");
    }

    /// <summary>`continue` must run the loop's "continuing" statements (and a do-while's condition) first.</summary>
    private void EmitContinue()
    {
        var context = _loops.Peek();
        if (context.Continuing.Statements.Count == 0 && context.BottomCondition is null)
        {
            Line("continue;");
            return;
        }

        Line("{");
        _indent++;
        EmitStatements(context.Continuing.Statements);
        if (context.BottomCondition is not null) EmitBottomCheck(context.BottomCondition);
        Line("continue;");
        _indent--;
        Line("}");
    }

    private static bool HasSequence(Expression expression) => expression switch
    {
        SequenceExpression => true,
        BinaryExpression b => HasSequence(b.Left) || HasSequence(b.Right),
        UnaryExpression u => HasSequence(u.Operand),
        NumericConversion c => HasSequence(c.Operand),
        StorageStructPropertyReference p => HasSequence(p.Reference),
        FunctionCallExpression call => call.Arguments.Any(HasSequence),
        VectorExpression v => v.Values.Any(HasSequence),
        CustomStructExpression s => s.Properties.Any(HasSequence),
        _ => false
    };

    // ---- Expressions -----------------------------------------------------------------------------------------

    private string CallText(FunctionId target, IReadOnlyList<Expression> arguments)
    {
        if (!_functionNames.TryGetValue(target.Value, out string? name))
        {
            throw new GodotBackendException($"Call to an unknown function id {target.Value}.");
        }

        return $"{name}({string.Join(", ", arguments.Select(a => Bare(Expr(a))))})";
    }

    /// <summary>Returns the Godot expression for an IR expression. Statements it needs are written first.</summary>
    private string Expr(Expression expression)
    {
        switch (expression)
        {
            case FloatLiteral f:
                return FloatText(f.Value);
            case IntLiteral i:
                if (i.Bits != 32) throw new GodotBackendException("Only 32-bit integers exist in Godot shaders.");
                return i.Signed ? (i.Value.StartsWith('-') ? $"({i.Value})" : i.Value) : $"{i.Value}u";
            case BoolLiteral b:
                return b.Value ? "true" : "false";
            case DoubleLiteral:
                throw new GodotBackendException("Godot shaders have no double precision type.");
            case VectorExpression v:
                string vectorType = v.ComponentsType switch
                {
                    IRTypes.VectorComponentsType.Float => "vec",
                    IRTypes.VectorComponentsType.Int => "ivec",
                    IRTypes.VectorComponentsType.Uint => "uvec",
                    _ => throw new GodotBackendException("Double precision vectors do not exist in Godot shaders.")
                };
                return $"{vectorType}{v.Values.Count}({string.Join(", ", v.Values.Select(Expr))})";
            case CustomStructExpression s:
                return s.Properties.Count == 0
                    ? ZeroOf(s.StructType)
                    : $"{TypeOf(s.StructType)}({string.Join(", ", s.Properties.Select(Expr))})";
            case NumericConversion c:
                string target = c.TargetType switch
                {
                    ScalarType.Int32 => "int",
                    ScalarType.UInt32 => "uint",
                    ScalarType.Float32 => "float",
                    _ => throw new GodotBackendException("Godot shaders have no double precision type.")
                };
                return $"{target}({Expr(c.Operand)})";
            case UnaryExpression u:
                string op = u.Operator switch
                {
                    UnaryOperator.Negate => "-",
                    UnaryOperator.LogicalNot => "!",
                    _ => "~"
                };
                return $"({op}{Expr(u.Operand)})";
            case BinaryExpression b:
                return BinaryText(b);
            case FunctionCallExpression call:
                return CallText(call.Target, call.Arguments);
            case SequenceExpression sequence:
                // Statements run before the statement that uses the value; the frontends only build
                // sequences whose statements affect temporaries.
                EmitStatements(sequence.Prelude);
                return Expr(sequence.Value);
            case StorageReference or StorageStructPropertyReference:
                return ReadText(expression);
            case StorageArrayElementReference:
                throw new GodotBackendException("Array elements are not supported by the Godot backend yet.");
            default:
                throw new GodotBackendException($"The expression '{expression.GetType().Name}' is not supported by the Godot backend.");
        }
    }

    private string BinaryText(BinaryExpression binary)
    {
        string left = Expr(binary.Left);
        string right = Expr(binary.Right);
        if (binary.Operator == BinaryOperator.Remainder && KindOf(binary.Left) == ScalarKind.Float)
        {
            // GLSL has no % for floats; C#'s % is a truncated remainder.
            return $"({left} - {right} * trunc({left} / {right}))";
        }

        string op = binary.Operator switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Remainder => "%",
            BinaryOperator.LogicalAnd => "&&",
            BinaryOperator.LogicalOr => "||",
            BinaryOperator.Equal => "==",
            BinaryOperator.NotEqual => "!=",
            BinaryOperator.Less => "<",
            BinaryOperator.LessEqual => "<=",
            BinaryOperator.Greater => ">",
            BinaryOperator.GreaterEqual => ">=",
            BinaryOperator.BitwiseAnd => "&",
            BinaryOperator.BitwiseOr => "|",
            BinaryOperator.BitwiseXor => "^",
            BinaryOperator.ShiftLeft => "<<",
            BinaryOperator.ShiftRight => ">>",
            _ => throw new GodotBackendException($"The operator '{binary.Operator}' is not supported by the Godot backend.")
        };
        return $"({left} {op} {right})";
    }

    /// <summary>Removes parentheses that wrap a whole expression; statements already delimit it.</summary>
    private static string Bare(string expression)
    {
        while (expression.Length >= 2 && expression[0] == '(' && expression[^1] == ')' && MatchingClose(expression) == expression.Length - 1)
        {
            expression = expression[1..^1];
        }

        return expression;
    }

    private static int MatchingClose(string text)
    {
        int depth = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')' && --depth == 0) return i;
        }

        return -1;
    }

    private static string FloatText(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new GodotBackendException("NaN and infinity literals cannot be written in Godot shaders.");
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture).Replace('E', 'e');
        int exponent = text.IndexOf('e');
        string mantissa = exponent < 0 ? text : text[..exponent];
        string rest = exponent < 0 ? "" : text[exponent..];
        if (!mantissa.Contains('.')) mantissa += ".0";
        text = mantissa + rest;
        return value < 0 ? $"({text})" : text;
    }

    // ---- References ----------------------------------------------------------------------------------------------

    private string ReadText(Expression expression)
    {
        if (TryResolveUniform(expression, out var uniform))
        {
            return uniform.Name ?? throw new GodotBackendException("A whole uniform struct cannot be used as a value; read its fields.");
        }

        if (TryResolveInput(expression, out string? input)) return input;

        switch (expression)
        {
            case StorageReference reference:
                if (_outputs.ContainsKey(reference.Storage))
                {
                    throw new GodotBackendException($"'{reference.Storage.HumanName}' is a stage output and cannot be read.");
                }

                return LocalName(reference.Storage);
            case StorageStructPropertyReference property:
                return $"{ReadText(property.Reference)}.{MemberName(property)}";
        }

        throw new GodotBackendException("Unsupported reference.");
    }

    private string MemberName(StorageStructPropertyReference property)
    {
        // Vector components keep their letter; struct fields go through the same sanitizer as their declaration.
        return IsVector(property.Reference) ? property.PropertyName.ToLowerInvariant() : Field(property.PropertyName);
    }

    private bool IsVector(Expression expression) => StorageOf(expression) is IRTypes.Vector;

    private Storage? StorageOf(Expression expression) => expression switch
    {
        StorageReference r => r.Storage,
        StorageStructPropertyReference p => StorageOf(p.Reference) is IRTypes.CustomStruct s
            ? s.Properties.FirstOrDefault(m => m.HumanName == p.PropertyName)
            : null,
        FunctionCallExpression call => _functions.TryGetValue(call.Target.Value, out var f) && f.Outputs.Count > 0
            ? f.Outputs[0]
            : null,
        SequenceExpression s => StorageOf(s.Value),
        _ => null
    };

    private string LValue(Expression target)
    {
        switch (target)
        {
            case StorageReference reference:
                if (_inputs.ContainsKey(reference.Storage) || _uniforms.ContainsKey(reference.Storage))
                {
                    throw new GodotBackendException($"'{reference.Storage.HumanName}' is read-only.");
                }

                return LocalName(reference.Storage);
            case StorageStructPropertyReference property:
                if (IsVector(property.Reference))
                {
                    throw new GodotBackendException("Writing to a vector component is not supported.");
                }

                return $"{LValue(property.Reference)}.{Field(property.PropertyName)}";
            default:
                throw new GodotBackendException("Unsupported assignment target.");
        }
    }

    private OutputTarget? ResolveOutput(Expression target)
    {
        switch (target)
        {
            case StorageReference reference when _outputs.TryGetValue(reference.Storage, out var node):
                return node.Value ?? throw new GodotBackendException(
                    $"'{reference.Storage.HumanName}' is a struct output: assign its fields.");
            case StorageStructPropertyReference { Reference: StorageReference reference } property
                when _outputs.TryGetValue(reference.Storage, out var node):
                return node.Members.TryGetValue(property.PropertyName, out var member)
                    ? member
                    : throw new GodotBackendException($"'{reference.Storage.HumanName}' has no output '{property.PropertyName}'.");
            default:
                return null;
        }
    }

    private bool TryResolveInput(Expression expression, out string text)
    {
        text = "";
        switch (expression)
        {
            case StorageReference reference when _inputs.TryGetValue(reference.Storage, out var node):
                if (node.Value is null)
                {
                    throw new GodotBackendException(
                        $"'{reference.Storage.HumanName}' is a stage input struct; read its fields instead of using it as a value.");
                }

                text = node.Value;
                return true;
            case StorageStructPropertyReference { Reference: StorageReference reference } property
                when _inputs.TryGetValue(reference.Storage, out var node):
                // A vector input (not a struct) read by component: `coord.x`.
                if (node.Value is not null && node.Members.Count == 0) return false;
                if (!node.Members.TryGetValue(property.PropertyName, out string? member))
                {
                    throw new GodotBackendException($"'{reference.Storage.HumanName}' has no input '{property.PropertyName}'.");
                }

                text = member;
                return true;
            default:
                return false;
        }
    }

    private bool TryResolveUniform(Expression expression, out UniformNode node)
    {
        node = null!;
        switch (expression)
        {
            case StorageReference reference when _uniforms.TryGetValue(reference.Storage, out var found):
                node = found;
                return true;
            case StorageStructPropertyReference property when TryResolveUniform(property.Reference, out var parent):
                if (!parent.Children.TryGetValue(property.PropertyName, out var child))
                {
                    throw new GodotBackendException($"The uniform has no field '{property.PropertyName}'.");
                }

                node = child;
                return true;
            default:
                return false;
        }
    }

    // ---- Light type inference (only what the backend needs) ------------------------------------------------------

    private enum ScalarKind { Float, Int, UInt, Bool, Other }

    private ScalarKind KindOf(Expression expression)
    {
        switch (expression)
        {
            case FloatLiteral:
                return ScalarKind.Float;
            case IntLiteral i:
                return i.Signed ? ScalarKind.Int : ScalarKind.UInt;
            case BoolLiteral:
                return ScalarKind.Bool;
            case NumericConversion c:
                return c.TargetType switch
                {
                    ScalarType.Float32 => ScalarKind.Float,
                    ScalarType.Int32 => ScalarKind.Int,
                    ScalarType.UInt32 => ScalarKind.UInt,
                    _ => ScalarKind.Other
                };
            case UnaryExpression u:
                return u.Operator == UnaryOperator.LogicalNot ? ScalarKind.Bool : KindOf(u.Operand);
            case BinaryExpression b:
                return b.Operator is BinaryOperator.Equal or BinaryOperator.NotEqual or BinaryOperator.Less
                    or BinaryOperator.LessEqual or BinaryOperator.Greater or BinaryOperator.GreaterEqual
                    or BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr
                    ? ScalarKind.Bool
                    : KindOf(b.Left);
            case SequenceExpression s:
                return KindOf(s.Value);
            case StorageReference or StorageStructPropertyReference or FunctionCallExpression:
                var storage = StorageOf(expression);
                if (expression is StorageStructPropertyReference { Reference: var owner } p &&
                    StorageOf(owner) is IRTypes.Vector vector)
                {
                    _ = p;
                    return vector.ComponentsType switch
                    {
                        IRTypes.VectorComponentsType.Float => ScalarKind.Float,
                        IRTypes.VectorComponentsType.Int => ScalarKind.Int,
                        IRTypes.VectorComponentsType.Uint => ScalarKind.UInt,
                        _ => ScalarKind.Other
                    };
                }

                return storage switch
                {
                    IRTypes.Float => ScalarKind.Float,
                    IRTypes.Bool => ScalarKind.Bool,
                    IRTypes.Int { Signed: true } => ScalarKind.Int,
                    IRTypes.Int => ScalarKind.UInt,
                    _ => ScalarKind.Other
                };
            default:
                return ScalarKind.Other;
        }
    }
}
