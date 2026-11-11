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

using YotsubaShader.GDScript.Annotations;
using YotsubaShader.GDScript.Diagnostics;
using YotsubaShader.GDScript.Semantics;
using YotsubaShader.GDScript.Syntax;
using YotsubaShader.IR.Intermediate;
using YotsubaShader.IR.Intermediate.FunctionBody;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.Literals;
using YotsubaShader.IR.Intermediate.Slot;
using YotsubaShader.IR.Intermediate.Slot.Types;
using YotsubaShader.IR.Intermediate.Types;
using IRStage = YotsubaShader.IR.Intermediate.Stage;

namespace YotsubaShader.GDScript.Lowering;

/// <summary>A helper or entry-point function known to the module.</summary>
internal sealed class GdFunctionInfo
{
    public GdFunctionInfo(GdFunctionDecl declaration)
    {
        Declaration = declaration;
    }

    public GdFunctionDecl Declaration { get; }
    public IRStage Stage { get; set; } = IRStage.None;
    public ComputeParams? Compute { get; set; }
    public GdType ReturnType { get; set; } = GdVoidType.Instance;
    public List<GdType> ParameterTypes { get; } = new();
    public FunctionId? Id { get; set; }
    public bool Queued { get; set; }
}

/// <summary>A global <c>const</c>, inlined where it is used.</summary>
internal sealed record GdConstant(Expression Value, GdType Type);

/// <summary>
/// Lowers a parsed GDScript file to the Yotsuba IR. This is the GDScript counterpart of the Roslyn frontend's
/// <c>ShaderProcessor</c>: it resolves types and annotations, builds the interface storage of each entry point
/// and delegates function bodies to <see cref="GdFunctionLowerer"/>. All state is per instance.
/// </summary>
public sealed class GdLowerer
{
    private readonly GdFile _file;
    private readonly AnnotationVocabulary _vocabulary;
    private readonly Dictionary<string, GdStructType> _structs = new();
    private readonly Dictionary<string, GdConstant> _constants = new();
    private readonly Dictionary<string, (Storage Storage, GdType Type)> _resources = new();
    private readonly Dictionary<string, GdFunctionInfo> _functions = new();
    private readonly List<Function> _lowered = new();
    private readonly Queue<GdFunctionInfo> _pending = new();
    private readonly List<Storage> _resourceStorages = new();
    private uint _nextFunctionId;

    internal GdLowerer(GdFile file, GdDiagnosticBag diagnostics, AnnotationVocabulary? vocabulary = null)
    {
        _file = file;
        Diagnostics = diagnostics;
        _vocabulary = vocabulary ?? AnnotationVocabulary.Default;
    }

    internal GdDiagnosticBag Diagnostics { get; }

    internal IReadOnlyDictionary<string, GdStructType> Structs => _structs;

    internal IReadOnlyDictionary<string, GdConstant> Constants => _constants;

    internal IReadOnlyDictionary<string, (Storage Storage, GdType Type)> Resources => _resources;

    internal IReadOnlyDictionary<string, GdFunctionInfo> Functions => _functions;

    /// <summary>Lowers the file. Returns null when errors were reported.</summary>
    public Shader? Lower()
    {
        ResolveStructs();
        ResolveGlobals();
        ResolveFunctions();

        var entries = _functions.Values.Where(f => f.Stage != IRStage.None).ToList();
        if (entries.Count == 0 && !Diagnostics.HasErrors)
        {
            Diagnostics.Error(new GdPosition(1, 1),
                "The script has no entry point; mark a function with @vertex, @fragment or @compute.");
        }

        if (Diagnostics.HasErrors) return null;

        // Entry points first, in declaration order, then helpers as they are reached (like ShaderProcessor).
        foreach (var entry in entries)
        {
            entry.Id = new FunctionId(_nextFunctionId++);
        }

        foreach (var entry in entries)
        {
            _lowered.Add(new GdFunctionLowerer(this, entry).Lower());
        }

        while (_pending.Count > 0)
        {
            var helper = _pending.Dequeue();
            _lowered.Add(new GdFunctionLowerer(this, helper).Lower());
        }

        return Diagnostics.HasErrors ? null : new Shader(_lowered, _resourceStorages);
    }

    /// <summary>Returns the id of a helper, assigning one and queueing it for lowering on first use.</summary>
    internal FunctionId Reference(GdFunctionInfo function)
    {
        function.Id ??= new FunctionId(_nextFunctionId++);
        if (function.Stage == IRStage.None && !function.Queued)
        {
            function.Queued = true;
            _pending.Enqueue(function);
        }

        return function.Id.Value;
    }

    // ---- Types --------------------------------------------------------------------------------------------

    internal GdType? ResolveType(string name, GdPosition position, bool allowVoid = false)
    {
        if (name == "void" && allowVoid) return GdVoidType.Instance;
        if (BuiltInType(name) is { } builtIn) return builtIn;

        if (_structs.TryGetValue(name, out var structure)) return structure;

        string? reason = name switch
        {
            "Texture2D" or "Texture3D" or "Texture2DArray" or "Cubemap" or "Texture" =>
                "textures need sampling operations that the IR does not have yet",
            "Sampler" or "SamplerState" => "samplers need sampling operations that the IR does not have yet",
            "Matrix2x2" or "Matrix3x3" or "Matrix4x4" or "Transform2D" or "Transform3D" or "Basis" or "Projection" =>
                "matrix values are not supported by the IR frontend yet",
            "Array" or "PackedFloat32Array" or "PackedInt32Array" or "Dictionary" =>
                "arrays are not supported by the IR frontend yet",
            _ => null
        };

        Diagnostics.Error(position, reason is null
            ? $"Unknown type '{name}'."
            : $"Type '{name}' is not supported: {reason}.");
        return null;
    }

    /// <summary>The scalar and vector types that exist without a declaration.</summary>
    internal static GdType? BuiltInType(string name) => name switch
    {
        "float" => GdScalarType.Float,
        "int" => GdScalarType.Int,
        "uint" => GdScalarType.UInt,
        "bool" => GdScalarType.Bool,
        "Vector2" => new GdVectorType(VectorComponentsType.Float, 2),
        "Vector3" => new GdVectorType(VectorComponentsType.Float, 3),
        "Vector4" => new GdVectorType(VectorComponentsType.Float, 4),
        "Vector2i" => new GdVectorType(VectorComponentsType.Int, 2),
        "Vector3i" => new GdVectorType(VectorComponentsType.Int, 3),
        "Vector4i" => new GdVectorType(VectorComponentsType.Int, 4),
        "Vector2u" => new GdVectorType(VectorComponentsType.Uint, 2),
        "Vector3u" => new GdVectorType(VectorComponentsType.Uint, 3),
        "Vector4u" => new GdVectorType(VectorComponentsType.Uint, 4),
        _ => null
    };

    private void ResolveStructs()
    {
        foreach (var declaration in _file.Structs)
        {
            if (_structs.ContainsKey(declaration.Name))
            {
                Diagnostics.Error(declaration.Position, $"Struct '{declaration.Name}' is declared twice.");
                continue;
            }

            var members = new List<GdStructMember>();
            var names = new HashSet<string>();
            foreach (var field in declaration.Fields)
            {
                if (!names.Add(field.Name))
                {
                    Diagnostics.Error(field.Position, $"Field '{field.Name}' is declared twice in '{declaration.Name}'.");
                    continue;
                }

                var type = ResolveType(field.TypeName, field.Position);
                if (type is null) continue;
                members.Add(new GdStructMember(field.Name, type, field));
            }

            if (members.Count == 0)
            {
                Diagnostics.Error(declaration.Position, $"Struct '{declaration.Name}' needs at least one field.");
            }

            // Registered after its fields are resolved, so a struct cannot contain itself.
            _structs[declaration.Name] = new GdStructType(declaration, members);
        }
    }

    // ---- Globals ------------------------------------------------------------------------------------------

    private void ResolveGlobals()
    {
        foreach (var global in _file.Globals)
        {
            if (_constants.ContainsKey(global.Name) || _resources.ContainsKey(global.Name))
            {
                Diagnostics.Error(global.Position, $"Global '{global.Name}' is declared twice.");
                continue;
            }

            if (global.IsConst)
            {
                ResolveConstant(global);
                continue;
            }

            ResolveResource(global);
        }
    }

    private void ResolveConstant(GdGlobalVar global)
    {
        if (global.Initializer is null)
        {
            Diagnostics.Error(global.Position, $"Constant '{global.Name}' needs a value.");
            return;
        }

        // Global constants are literals, inlined at each use.
        var scratch = new GdFunctionLowerer(this, new GdFunctionInfo(
            new GdFunctionDecl("<const>", [], null, [], [], global.Position)));
        var value = scratch.LowerConstant(global.Initializer);
        if (value is null) return;

        GdType type = value.Type;
        Expression ir = value.Ir;
        if (global.TypeName is not null)
        {
            var declared = ResolveType(global.TypeName, global.Position);
            if (declared is null) return;
            ir = scratch.Coerce(value, declared, global.Position);
            type = declared;
        }

        _constants[global.Name] = new GdConstant(ir, type);
    }

    private void ResolveResource(GdGlobalVar global)
    {
        StorageKind? kind = null;
        foreach (var annotation in global.Annotations)
        {
            if (!_vocabulary.TryGet(annotation.Name, out var definition)) continue;
            StorageKind? found = definition.Kind switch
            {
                AnnotationKind.Uniform => StorageKind.Uniform,
                AnnotationKind.StorageBuffer => StorageKind.StorageBuffer,
                AnnotationKind.PushConstant => StorageKind.PushConstant,
                _ => null
            };
            if (found is null) continue;
            if (kind is not null && kind != found)
            {
                Diagnostics.Error(annotation.Position, $"'{global.Name}' has conflicting resource annotations.");
            }

            kind = found;
        }

        if (kind is null)
        {
            Diagnostics.Error(global.Position,
                $"Global variable '{global.Name}' must be a resource (@uniform, @storage_buffer or " +
                "@push_constant); use 'const' for constants.");
            return;
        }

        if (global.TypeName is null)
        {
            Diagnostics.Error(global.Position, $"Resource '{global.Name}' needs a type.");
            return;
        }

        if (global.Initializer is not null)
        {
            Diagnostics.Error(global.Position, $"Resource '{global.Name}' cannot have an initial value.");
        }

        var type = ResolveType(global.TypeName, global.Position);
        if (type is null) return;

        InterfaceSlot? slot = ResolveSlot(global.Annotations, IRStage.None, global.Position);
        if (kind != StorageKind.PushConstant && slot is not DescriptorSlot)
        {
            Diagnostics.Error(global.Position,
                $"Resource '{global.Name}' needs a binding: add @group(n) @binding(n) or @register.");
        }

        var storage = MakeStorage(type, global.Name, kind.Value, slot, memberSlots: false, IRStage.None);
        _resources[global.Name] = (storage, type);
        _resourceStorages.Add(storage);
    }

    // ---- Functions ----------------------------------------------------------------------------------------

    private void ResolveFunctions()
    {
        foreach (var declaration in _file.Functions)
        {
            if (_functions.ContainsKey(declaration.Name))
            {
                Diagnostics.Error(declaration.Position, $"Function '{declaration.Name}' is declared twice.");
                continue;
            }

            var info = new GdFunctionInfo(declaration);
            _functions[declaration.Name] = info;

            foreach (var parameter in declaration.Parameters)
            {
                var type = ResolveType(parameter.TypeName, parameter.Position);
                info.ParameterTypes.Add(type ?? GdScalarType.Float);
            }

            if (declaration.ReturnType is not null)
            {
                info.ReturnType = ResolveType(declaration.ReturnType, declaration.Position, allowVoid: true)
                                  ?? GdVoidType.Instance;
            }

            ResolveStage(info);
        }
    }

    private void ResolveStage(GdFunctionInfo info)
    {
        var declaration = info.Declaration;
        IRStage stage = IRStage.None;
        foreach (var annotation in declaration.Annotations)
        {
            if (!_vocabulary.TryGet(annotation.Name, out var definition)) continue;
            if (definition.Kind == AnnotationKind.Stage)
            {
                if (stage != IRStage.None)
                {
                    Diagnostics.Error(annotation.Position,
                        $"'{declaration.Name}' has more than one stage annotation.");
                }

                stage = definition.Stage;
            }
            else if (definition.Kind == AnnotationKind.WorkgroupSize)
            {
                info.Compute = ReadWorkgroupSize(annotation);
            }
        }

        info.Stage = stage;
        if (stage == IRStage.Compute && info.Compute is null)
        {
            Diagnostics.Error(declaration.Position,
                $"The compute shader '{declaration.Name}' needs @workgroup_size(x, y, z) (or @numthreads).");
        }
        else if (stage != IRStage.Compute && info.Compute is not null)
        {
            Diagnostics.Error(declaration.Position,
                $"'{declaration.Name}' has a workgroup size but is not a @compute function.");
        }

        if (stage == IRStage.Compute && info.ReturnType is not GdVoidType)
        {
            Diagnostics.Error(declaration.Position, $"The compute shader '{declaration.Name}' must return void.");
        }

        if (stage is IRStage.Vertex or IRStage.Fragment && info.ReturnType is GdVoidType)
        {
            Diagnostics.Error(declaration.Position,
                $"The {stage.ToString().ToLowerInvariant()} entry point '{declaration.Name}' must declare a return type.");
        }
    }

    private ComputeParams? ReadWorkgroupSize(GdAnnotation annotation)
    {
        if (annotation.Arguments is [GdLiteralExpr { Kind: GdLiteralKind.Int } x,
                GdLiteralExpr { Kind: GdLiteralKind.Int } y, GdLiteralExpr { Kind: GdLiteralKind.Int } z] &&
            uint.TryParse(x.Text, out uint sx) && uint.TryParse(y.Text, out uint sy) &&
            uint.TryParse(z.Text, out uint sz) && sx > 0 && sy > 0 && sz > 0)
        {
            return new ComputeParams(sx, sy, sz);
        }

        Diagnostics.Error(annotation.Position,
            $"@{annotation.Name} needs three positive integer literals, for example @{annotation.Name}(8, 8, 1).");
        return null;
    }

    // ---- Slots and storage --------------------------------------------------------------------------------

    /// <summary>
    /// Resolves the interface slot described by a list of annotations. Mirrors the Roslyn frontend's
    /// <c>GetAttributeData</c>: a descriptor (group/binding) wins, then location, vertex semantic and built-in.
    /// </summary>
    internal InterfaceSlot? ResolveSlot(IReadOnlyList<GdAnnotation> annotations, IRStage stage, GdPosition owner)
    {
        int? set = null;
        int? binding = null;
        bool descriptor = false;
        InterfaceSlot? slot = null;

        foreach (var annotation in annotations)
        {
            if (!_vocabulary.TryGet(annotation.Name, out var definition))
            {
                Diagnostics.Error(annotation.Position, $"Unknown annotation '@{annotation.Name}'.");
                continue;
            }

            switch (definition.Kind)
            {
                case AnnotationKind.Group:
                    descriptor = true;
                    set = IntArgument(annotation, 0);
                    break;
                case AnnotationKind.Binding:
                    descriptor = true;
                    binding = IntArgument(annotation, 0);
                    break;
                case AnnotationKind.Register:
                    descriptor = true;
                    // @register("b", n): the register letter only classifies the resource; the index is the binding.
                    set = 0;
                    binding = IntArgument(annotation, annotation.Arguments.Count - 1);
                    break;
                case AnnotationKind.Location:
                    if (IntArgument(annotation, 0) is int location) slot ??= new LocationSlot(location);
                    break;
                case AnnotationKind.VertexSemantic:
                    slot ??= new VertexSlot(new VertexSemantic(definition.Usage!.Value,
                        annotation.Arguments.Count > 0 ? IntArgument(annotation, 0) ?? 0 : 0));
                    break;
                case AnnotationKind.VertexSemanticByName:
                    slot ??= ResolveSemanticByName(annotation);
                    break;
                case AnnotationKind.BuiltIn:
                    slot ??= BuiltInSlotFor(definition.BuiltIn!.Value, stage, annotation);
                    break;
                case AnnotationKind.BuiltInByName:
                    slot ??= ResolveBuiltInByName(annotation, stage);
                    break;
                case AnnotationKind.NotSupportedYet:
                    Diagnostics.Error(annotation.Position,
                        $"@{annotation.Name} ({definition.Note}) is not supported by the IR yet.");
                    break;
            }
        }

        if (descriptor)
        {
            if (binding is null)
            {
                Diagnostics.Error(owner, "A descriptor set was specified without a binding.");
                return null;
            }

            if (set is < 0 || binding < 0)
            {
                Diagnostics.Error(owner, "Descriptor set and binding cannot be negative.");
                return null;
            }

            return new DescriptorSlot(set ?? 0, binding.Value);
        }

        return slot;
    }

    private int? IntArgument(GdAnnotation annotation, int index)
    {
        if (index >= 0 && index < annotation.Arguments.Count &&
            annotation.Arguments[index] is GdLiteralExpr { Kind: GdLiteralKind.Int } literal &&
            int.TryParse(literal.Text, out int value))
        {
            return value;
        }

        Diagnostics.Error(annotation.Position, $"@{annotation.Name} needs an integer argument.");
        return null;
    }

    private InterfaceSlot? ResolveSemanticByName(GdAnnotation annotation)
    {
        if (annotation.Arguments is not [GdLiteralExpr { Kind: GdLiteralKind.String } name, ..])
        {
            Diagnostics.Error(annotation.Position, "@semantic needs a name, for example @semantic(\"TEXCOORD\", 1).");
            return null;
        }

        VertexUsage? usage = name.Text.ToUpperInvariant() switch
        {
            "POSITION" => VertexUsage.Position,
            "COLOR" => VertexUsage.Color,
            "TEXCOORD" => VertexUsage.TextureCoordinate,
            "NORMAL" => VertexUsage.Normal,
            "TANGENT" => VertexUsage.Tangent,
            "BINORMAL" => VertexUsage.Binormal,
            _ => null
        };
        if (usage is null)
        {
            Diagnostics.Error(annotation.Position, $"Unknown vertex semantic '{name.Text}'.");
            return null;
        }

        int index = annotation.Arguments.Count > 1 ? IntArgument(annotation, 1) ?? 0 : 0;
        return new VertexSlot(new VertexSemantic(usage.Value, index));
    }

    private InterfaceSlot? ResolveBuiltInByName(GdAnnotation annotation, IRStage stage)
    {
        string? name = annotation.Arguments switch
        {
            [GdNameExpr n] => n.Name,
            [GdLiteralExpr { Kind: GdLiteralKind.String } s] => s.Text,
            _ => null
        };
        BuiltIn? builtIn = name switch
        {
            "position" => BuiltIn.Position,
            "point_size" => BuiltIn.PointSize,
            "vertex_index" => BuiltIn.VertexIndex,
            "instance_index" => BuiltIn.InstanceIndex,
            "frag_coord" => BuiltIn.FragCoord,
            "front_facing" => BuiltIn.FrontFacing,
            "frag_depth" => BuiltIn.FragDepth,
            "global_invocation_id" => BuiltIn.GlobalInvocationId,
            "local_invocation_id" => BuiltIn.LocalInvocationId,
            "local_invocation_index" => BuiltIn.LocalInvocationIndex,
            "workgroup_id" => BuiltIn.WorkgroupId,
            "num_workgroups" => BuiltIn.NumWorkgroups,
            _ => null
        };
        if (builtIn is null)
        {
            Diagnostics.Error(annotation.Position, name is null
                ? "@builtin needs the built-in name, for example @builtin(position)."
                : $"Unknown or not yet supported built-in '{name}'.");
            return null;
        }

        return BuiltInSlotFor(builtIn.Value, stage, annotation);
    }

    private InterfaceSlot? BuiltInSlotFor(BuiltIn builtIn, IRStage stage, GdAnnotation annotation)
    {
        // As in the Roslyn frontend, the clip-space position seen by a fragment shader is the fragment coordinate.
        if (builtIn == BuiltIn.Position && stage == IRStage.Fragment) builtIn = BuiltIn.FragCoord;

        IRStage? required = builtIn switch
        {
            BuiltIn.Position or BuiltIn.PointSize or BuiltIn.VertexIndex or BuiltIn.InstanceIndex => IRStage.Vertex,
            BuiltIn.FragCoord or BuiltIn.FrontFacing or BuiltIn.FragDepth => IRStage.Fragment,
            _ => IRStage.Compute
        };

        // Stage None (resources, helpers) does not validate; entry points do.
        if (stage != IRStage.None && stage != required)
        {
            Diagnostics.Error(annotation.Position,
                $"@{annotation.Name} can only be used in a {required.ToString()!.ToLowerInvariant()} shader.");
            return null;
        }

        return new BuiltInSlot(builtIn);
    }

    /// <summary>Creates IR storage for a GDScript type. Struct members get their own slots when requested.</summary>
    internal Storage MakeStorage(
        GdType type,
        string name,
        StorageKind kind,
        InterfaceSlot? slot,
        bool memberSlots,
        IRStage stage)
    {
        switch (type)
        {
            case GdScalarType { Kind: GdScalarKind.Int }:
                return new Int(name, kind, slot) { Bits = 32, Signed = true };
            case GdScalarType { Kind: GdScalarKind.UInt }:
                return new Int(name, kind, slot) { Bits = 32, Signed = false };
            case GdScalarType { Kind: GdScalarKind.Float }:
                return new Float(name, kind, slot);
            case GdScalarType { Kind: GdScalarKind.Bool }:
                return new Bool(name, kind, slot);
            case GdVectorType vector:
                return new Vector(name, kind, slot)
                {
                    ComponentsType = vector.Component,
                    ComponentsLength = vector.Length,
                    ComponentsDefaultValues = Enumerable.Range(0, vector.Length)
                        .Select(_ => ZeroOf(vector.Component)).ToList()
                };
            case GdStructType structure:
                var properties = new List<Storage>();
                foreach (var member in structure.Members)
                {
                    InterfaceSlot? memberSlot = memberSlots
                        ? ResolveSlot(member.Field.Annotations, stage, member.Field.Position)
                        : null;
                    properties.Add(MakeStorage(member.Type, member.Name, kind, memberSlot, memberSlots, stage));
                }

                return new CustomStruct(name, kind, slot) { Properties = properties, TypeName = structure.Name };
            default:
                throw new InvalidOperationException($"Type '{type.Name}' has no storage.");
        }
    }

    internal static Expression ZeroOf(VectorComponentsType component) => component switch
    {
        VectorComponentsType.Int => new IntLiteral("0", true),
        VectorComponentsType.Uint => new IntLiteral("0", false),
        VectorComponentsType.Double => new DoubleLiteral(0d),
        _ => new FloatLiteral(0f)
    };
}
