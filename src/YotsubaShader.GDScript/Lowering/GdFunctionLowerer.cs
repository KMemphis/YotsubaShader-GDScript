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
using YotsubaShader.GDScript.Diagnostics;
using YotsubaShader.GDScript.Semantics;
using YotsubaShader.GDScript.Syntax;
using YotsubaShader.IR.Intermediate;
using YotsubaShader.IR.Intermediate.FunctionBody;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.AssignmentTargets;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.FunctionCalls;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.Literals;
using YotsubaShader.IR.Intermediate.FunctionBody.Statements;
using YotsubaShader.IR.Intermediate.Types;
using YotsubaShader.IR.Intermediate.ValueConvertions;

namespace YotsubaShader.GDScript.Lowering;

/// <summary>An IR expression together with its GDScript type.</summary>
internal sealed record Typed(Expression Ir, GdType Type);

/// <summary>
/// Lowers the body of one function to IR statements, typing every expression on the way. It plays the role of
/// <c>StatementOperationProcessor</c> in the Roslyn frontend.
/// </summary>
internal sealed class GdFunctionLowerer
{
    private static readonly HashSet<string> Intrinsics =
    [
        "sin", "cos", "tan", "asin", "acos", "atan", "atan2", "sinh", "cosh", "tanh", "sqrt", "inversesqrt", "pow",
        "exp", "exp2", "log", "log2", "abs", "sign", "floor", "ceil", "round", "fract", "min", "max", "clamp", "mix",
        "lerp", "step", "smoothstep", "dot", "cross", "normalize", "length", "distance", "reflect", "refract", "mod",
        "fmod", "fposmod", "texture", "texelFetch", "sample", "ddx", "ddy", "fwidth", "discard", "saturate"
    ];

    private sealed record LocalVariable(Storage Storage, GdType Type, bool IsConst);

    private readonly GdLowerer _module;
    private readonly GdFunctionInfo _info;
    private readonly List<Dictionary<string, LocalVariable>> _scopes = new();
    private readonly HashSet<string> _usedNames = new();
    private int _loopDepth;

    public GdFunctionLowerer(GdLowerer module, GdFunctionInfo info)
    {
        _module = module;
        _info = info;
    }

    private GdDiagnosticBag Diagnostics => _module.Diagnostics;

    private bool IsEntry => _info.Stage != Stage.None;

    // ---- Function -----------------------------------------------------------------------------------------

    public Function Lower()
    {
        var declaration = _info.Declaration;
        var inputs = new List<Storage>();
        var outputs = new List<Storage>();
        _scopes.Add(new Dictionary<string, LocalVariable>());

        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            var parameter = declaration.Parameters[i];
            var type = _info.ParameterTypes[i];
            if (type is GdVoidType) continue;

            InterfaceSlotOrNull(out var slot, parameter.Annotations, parameter.Position);
            var storage = _module.MakeStorage(type, parameter.Name, StorageKind.Input, slot, IsEntry, _info.Stage);
            inputs.Add(storage);
            Declare(parameter.Name, storage, type, false, parameter.Position);
        }

        if (_info.ReturnType is not GdVoidType)
        {
            InterfaceSlotOrNull(out var slot, declaration.Annotations, declaration.Position);
            outputs.Add(_module.MakeStorage(_info.ReturnType, "__return", StorageKind.Output, slot, IsEntry,
                _info.Stage));
        }

        var body = new List<Statement>();
        foreach (var statement in declaration.Body)
        {
            var lowered = LowerStatement(statement);
            if (lowered is not null) body.Add(lowered);
        }

        if (_info.ReturnType is not GdVoidType && !AlwaysReturns(declaration.Body))
        {
            Diagnostics.Error(declaration.Position,
                $"Not all code paths of '{declaration.Name}' return a value.");
        }

        return new Function(_info.Id!.Value, declaration.Name, _info.Stage, inputs, outputs, body, _info.Compute);
    }

    /// <summary>Only entry points carry interface slots; for helpers the annotations are ignored.</summary>
    private void InterfaceSlotOrNull(
        out YotsubaShader.IR.Intermediate.Slot.InterfaceSlot? slot,
        IReadOnlyList<GdAnnotation> annotations,
        GdPosition position)
    {
        slot = IsEntry ? _module.ResolveSlot(annotations, _info.Stage, position) : null;
    }

    private static bool AlwaysReturns(IReadOnlyList<GdStmt> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case GdReturnStmt:
                    return true;
                case GdIfStmt { Else: not null } branch when AlwaysReturns(branch.Then) && AlwaysReturns(branch.Else):
                    return true;
            }
        }

        return false;
    }

    // ---- Scopes -------------------------------------------------------------------------------------------

    private string UniqueName(string preferred)
    {
        string name = preferred;
        for (int suffix = 1; !_usedNames.Add(name); suffix++)
        {
            name = $"{preferred}_{suffix}";
        }

        return name;
    }

    private void Declare(string name, Storage storage, GdType type, bool isConst, GdPosition position)
    {
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                Diagnostics.Error(position, $"'{name}' is already declared in this function.");
                return;
            }
        }

        _usedNames.Add(storage.HumanName);
        _scopes[^1][name] = new LocalVariable(storage, type, isConst);
    }

    private LocalVariable? Find(string name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].TryGetValue(name, out var variable)) return variable;
        }

        return null;
    }

    private Storage NewLocal(GdType type, string preferredName) =>
        _module.MakeStorage(type, UniqueName(preferredName), StorageKind.Local, null, false, Stage.None);

    // ---- Statements ---------------------------------------------------------------------------------------

    private Block LowerBlock(IReadOnlyList<GdStmt> statements)
    {
        _scopes.Add(new Dictionary<string, LocalVariable>());
        var result = new List<Statement>();
        foreach (var statement in statements)
        {
            var lowered = LowerStatement(statement);
            if (lowered is not null) result.Add(lowered);
        }

        _scopes.RemoveAt(_scopes.Count - 1);
        return new Block(result);
    }

    private Statement? LowerStatement(GdStmt statement)
    {
        switch (statement)
        {
            case GdPassStmt:
                return null;
            case GdVarStmt local:
                return LowerLocal(local);
            case GdAssignStmt assign:
                return LowerAssignment(assign);
            case GdExprStmt expression:
                return LowerExpressionStatement(expression);
            case GdIfStmt branch:
                return LowerIf(branch);
            case GdWhileStmt loop:
                return LowerWhile(loop);
            case GdForStmt loop:
                return LowerFor(loop);
            case GdReturnStmt ret:
                return LowerReturn(ret);
            case GdBreakStmt or GdContinueStmt:
                if (_loopDepth == 0)
                {
                    Diagnostics.Error(statement.Position, "'break' and 'continue' are only valid inside a loop.");
                    return null;
                }

                return statement is GdBreakStmt ? new Break() : new Continue();
            default:
                Diagnostics.Error(statement.Position, "Unsupported statement.");
                return null;
        }
    }

    private Statement? LowerLocal(GdVarStmt local)
    {
        Typed? initializer = local.Initializer is null ? null : LowerExpression(local.Initializer);
        GdType? type = null;
        if (local.TypeName is not null) type = _module.ResolveType(local.TypeName, local.Position);
        else if (initializer is not null) type = initializer.Type;
        else if (local.Initializer is not null) return null; // the initializer already reported its error
        else Diagnostics.Error(local.Position, $"'{local.Name}' needs a type or an initial value.");

        if (type is null) return null;
        if (type is GdVoidType)
        {
            Diagnostics.Error(local.Position, $"'{local.Name}' cannot be void.");
            return null;
        }

        Expression? ir = null;
        if (initializer is not null) ir = Coerce(initializer, type, local.Position);

        var storage = NewLocal(type, local.Name);
        Declare(local.Name, storage, type, local.IsConst, local.Position);
        return new VariableDeclaration(storage, ir);
    }

    private Statement? LowerAssignment(GdAssignStmt assign)
    {
        var target = LowerExpression(assign.Target);
        var value = LowerExpression(assign.Value);
        if (target is null || value is null) return null;
        if (!CheckAssignable(assign.Target, target)) return null;

        if (assign.Operator == "=")
        {
            return new Assignment(target.Ir, Coerce(value, target.Type, assign.Position));
        }

        string op = assign.Operator[..^1];
        var computed = LowerBinaryOperation(op, target, value, assign.Position, assign.Target, assign.Value);
        if (computed is null) return null;
        return new Assignment(target.Ir, Coerce(computed, target.Type, assign.Position));
    }

    private bool CheckAssignable(GdExpr source, Typed target)
    {
        if (target.Ir is StorageStructPropertyReference { Reference: var owner } &&
            Resolve(owner) is GdVectorType)
        {
            Diagnostics.Error(source.Position,
                "Writing to a vector component is not supported by the IR yet; assign a whole vector instead.");
            return false;
        }

        var root = RootOf(target.Ir);
        if (root is null)
        {
            Diagnostics.Error(source.Position, "The left side of an assignment must be a variable or a field.");
            return false;
        }

        if (root.Kind is StorageKind.Uniform or StorageKind.PushConstant)
        {
            Diagnostics.Error(source.Position, $"'{root.HumanName}' is read-only.");
            return false;
        }

        if (FindByStorage(root) is { IsConst: true })
        {
            Diagnostics.Error(source.Position, $"Cannot assign to constant '{root.HumanName}'.");
            return false;
        }

        return true;
    }

    private LocalVariable? FindByStorage(Storage storage)
    {
        foreach (var scope in _scopes)
        {
            foreach (var variable in scope.Values)
            {
                if (ReferenceEquals(variable.Storage, storage)) return variable;
            }
        }

        return null;
    }

    private static Storage? RootOf(Expression expression) => expression switch
    {
        StorageReference reference => reference.Storage,
        StorageStructPropertyReference property => RootOf(property.Reference),
        _ => null
    };

    /// <summary>The type of a storage expression built by this lowerer (struct member chains).</summary>
    private GdType? Resolve(Expression expression) => expression switch
    {
        StorageReference reference => FindByStorage(reference.Storage)?.Type ??
                                      _module.Resources.Values.FirstOrDefault(r =>
                                          ReferenceEquals(r.Storage, reference.Storage)).Type,
        StorageStructPropertyReference property => Resolve(property.Reference) switch
        {
            GdStructType structure => structure.Members.FirstOrDefault(m => m.Name == property.PropertyName)?.Type,
            _ => null
        },
        _ => null
    };

    private Statement? LowerExpressionStatement(GdExprStmt statement)
    {
        if (statement.Expression is GdCallExpr call && ResolveFunctionCall(call, out var function, out var args))
        {
            return new FunctionCallStatement(_module.Reference(function!), args!);
        }

        // Errors were already reported for calls that failed to resolve.
        if (statement.Expression is not GdCallExpr)
        {
            Diagnostics.Error(statement.Position, "This expression has no effect; use it in an assignment or call.");
        }
        else if (!Diagnostics.HasErrors)
        {
            Diagnostics.Error(statement.Position, "Only calls to helper functions can be used as statements.");
        }

        return null;
    }

    private Statement? LowerIf(GdIfStmt branch)
    {
        var condition = LowerCondition(branch.Condition);
        var then = LowerBlock(branch.Then);
        Statement? otherwise = branch.Else is null ? null : LowerBlock(branch.Else);
        return condition is null ? null : new If(condition, then, otherwise);
    }

    private Statement? LowerWhile(GdWhileStmt loop)
    {
        var condition = LowerCondition(loop.Condition);
        _loopDepth++;
        var body = LowerBlock(loop.Body);
        _loopDepth--;
        return condition is null ? null : new Loop(condition, body, new Block([]));
    }

    private Statement? LowerFor(GdForStmt loop)
    {
        if (loop.Iterable is not GdCallExpr { Callee: GdNameExpr { Name: "range" }, Arguments.Count: >= 1 and <= 3 } range)
        {
            Diagnostics.Error(loop.Position, "Only 'for i in range(...)' loops are supported.");
            return null;
        }

        var arguments = range.Arguments.Select(LowerExpression).ToList();
        if (arguments.Any(a => a is null)) return null;
        var ints = new List<Expression>();
        foreach (var (argument, source) in arguments.Zip(range.Arguments))
        {
            ints.Add(Coerce(argument!, GdScalarType.Int, source.Position));
        }

        Expression start = ints.Count == 1 ? new IntLiteral("0", true) : ints[0];
        Expression end = ints.Count == 1 ? ints[0] : ints[1];
        int direction = 1;
        Expression step = new IntLiteral("1", true);
        if (ints.Count == 3)
        {
            step = ints[2];
            direction = step switch
            {
                IntLiteral { Value: var v } when v.StartsWith('-') => -1,
                IntLiteral { Value: var v } when v != "0" => 1,
                _ => 0
            };
            if (direction == 0)
            {
                Diagnostics.Error(range.Arguments[2].Position, "The step of range() must be a non-zero integer literal.");
                return null;
            }
        }

        var statements = new List<Statement>();
        if (end is not IntLiteral and not StorageReference)
        {
            var endStorage = NewLocal(GdScalarType.Int, "__range_end");
            statements.Add(new VariableDeclaration(endStorage, end));
            end = new StorageReference(endStorage);
        }

        _scopes.Add(new Dictionary<string, LocalVariable>());
        var counter = NewLocal(GdScalarType.Int, loop.Variable);
        Declare(loop.Variable, counter, GdScalarType.Int, false, loop.Position);
        var counterReference = new StorageReference(counter);
        statements.Add(new VariableDeclaration(counter, start));

        _loopDepth++;
        var body = LowerBlock(loop.Body);
        _loopDepth--;
        _scopes.RemoveAt(_scopes.Count - 1);

        var condition = new BinaryExpression(direction > 0 ? BinaryOperator.Less : BinaryOperator.Greater,
            counterReference, end);
        var advance = new Assignment(counterReference, new BinaryExpression(BinaryOperator.Add, counterReference, step));
        statements.Add(new Loop(condition, body, new Block([advance])));
        return new Block(statements);
    }

    private Statement? LowerReturn(GdReturnStmt ret)
    {
        if (_info.ReturnType is GdVoidType)
        {
            if (ret.Value is not null) Diagnostics.Error(ret.Position, "A void function cannot return a value.");
            return new Return(null);
        }

        if (ret.Value is null)
        {
            Diagnostics.Error(ret.Position, $"'{_info.Declaration.Name}' must return a '{_info.ReturnType.Name}'.");
            return null;
        }

        var value = LowerExpression(ret.Value);
        return value is null ? null : new Return(Coerce(value, _info.ReturnType, ret.Position));
    }

    private Expression? LowerCondition(GdExpr source)
    {
        var condition = LowerExpression(source);
        if (condition is null) return null;
        if (condition.Type is not GdScalarType { Kind: GdScalarKind.Bool })
        {
            Diagnostics.Error(source.Position, $"A condition must be 'bool', not '{condition.Type.Name}'.");
        }

        return condition.Ir;
    }

    // ---- Expressions --------------------------------------------------------------------------------------

    /// <summary>Lowers the initializer of a global constant; only literals are allowed.</summary>
    public Typed? LowerConstant(GdExpr source)
    {
        var value = LowerExpression(source);
        if (value is null) return null;
        if (value.Ir is not Literal)
        {
            Diagnostics.Error(source.Position, "A global constant must be a literal value.");
            return null;
        }

        return value;
    }

    public Expression Coerce(Typed value, GdType target, GdPosition position)
    {
        if (GdType.Same(value.Type, target)) return value.Ir;

        if (value.Type is GdScalarType { IsNumeric: true } from && target is GdScalarType { IsNumeric: true } to)
        {
            if (to.Kind == GdScalarKind.Float)
            {
                return value.Ir is IntLiteral literal
                    ? new FloatLiteral(float.Parse(literal.Value, CultureInfo.InvariantCulture))
                    : new NumericConversion(value.Ir, ScalarType.Float32);
            }

            // An int literal adopts uint when it is not negative: `var i: uint = 3`.
            if (to.Kind == GdScalarKind.UInt && value.Ir is IntLiteral { Signed: true } signedLiteral &&
                !signedLiteral.Value.StartsWith('-'))
            {
                return new IntLiteral(signedLiteral.Value, false);
            }

            _ = from;
        }

        Diagnostics.Error(position, $"Cannot convert '{value.Type.Name}' to '{target.Name}'" +
                                    (IsNumeric(value.Type) && IsNumeric(target)
                                        ? "; use an explicit conversion such as int(x), uint(x) or float(x)."
                                        : "."));
        return value.Ir;
    }

    private static bool IsNumeric(GdType type) => type is GdScalarType { IsNumeric: true };

    private Typed? LowerExpression(GdExpr expression)
    {
        switch (expression)
        {
            case GdLiteralExpr literal:
                return LowerLiteral(literal);
            case GdNameExpr name:
                return LowerName(name);
            case GdMemberExpr member:
                return LowerMember(member);
            case GdCallExpr call:
                return LowerCall(call);
            case GdUnaryExpr unary:
                return LowerUnary(unary);
            case GdBinaryExpr binary:
                return LowerBinary(binary);
            case GdTernaryExpr ternary:
                return LowerTernary(ternary);
            default:
                Diagnostics.Error(expression.Position, "Unsupported expression.");
                return null;
        }
    }

    private Typed? LowerLiteral(GdLiteralExpr literal)
    {
        switch (literal.Kind)
        {
            case GdLiteralKind.Int:
                string text = literal.Text;
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    if (!long.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hex) ||
                        hex > int.MaxValue)
                    {
                        Diagnostics.Error(literal.Position, $"The integer literal '{text}' does not fit in 'int'.");
                        return null;
                    }

                    text = hex.ToString(CultureInfo.InvariantCulture);
                }
                else if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                {
                    Diagnostics.Error(literal.Position, $"The integer literal '{text}' does not fit in 'int'.");
                    return null;
                }

                return new Typed(new IntLiteral(text, true), GdScalarType.Int);
            case GdLiteralKind.UInt:
                if (!uint.TryParse(literal.Text, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                {
                    Diagnostics.Error(literal.Position, $"The integer literal '{literal.Text}u' does not fit in 'uint'.");
                    return null;
                }

                return new Typed(new IntLiteral(literal.Text, false), GdScalarType.UInt);
            case GdLiteralKind.Float:
                return new Typed(new FloatLiteral(float.Parse(literal.Text, CultureInfo.InvariantCulture)),
                    GdScalarType.Float);
            case GdLiteralKind.Bool:
                return new Typed(new BoolLiteral(literal.Text == "true"), GdScalarType.Bool);
            default:
                Diagnostics.Error(literal.Position, "Strings are not supported in shaders.");
                return null;
        }
    }

    private Typed? LowerName(GdNameExpr name)
    {
        if (name.Name == "<error>") return null;

        if (Find(name.Name) is { } variable)
        {
            return new Typed(new StorageReference(variable.Storage), variable.Type);
        }

        if (_module.Constants.TryGetValue(name.Name, out var constant))
        {
            return new Typed(constant.Value, constant.Type);
        }

        if (_module.Resources.TryGetValue(name.Name, out var resource))
        {
            return new Typed(new StorageReference(resource.Storage), resource.Type);
        }

        if (_module.Functions.ContainsKey(name.Name) || _module.Structs.ContainsKey(name.Name))
        {
            Diagnostics.Error(name.Position, $"'{name.Name}' is not a value.");
        }
        else
        {
            Diagnostics.Error(name.Position, $"Unknown identifier '{name.Name}'.");
        }

        return null;
    }

    private Typed? LowerMember(GdMemberExpr member)
    {
        var target = LowerExpression(member.Target);
        if (target is null) return null;

        switch (target.Type)
        {
            case GdStructType structure:
                var field = structure.Members.FirstOrDefault(m => m.Name == member.Member);
                if (field is null)
                {
                    Diagnostics.Error(member.Position, $"'{structure.Name}' has no field '{member.Member}'.");
                    return null;
                }

                return new Typed(new StorageStructPropertyReference(target.Ir, field.Name), field.Type);

            case GdVectorType vector:
                int index = member.Member switch { "x" => 0, "y" => 1, "z" => 2, "w" => 3, _ => -1 };
                if (index < 0 || index >= vector.Length)
                {
                    Diagnostics.Error(member.Position, member.Member.Length > 1
                        ? $"Swizzles such as '.{member.Member}' are not supported by the IR yet; read one component at a time."
                        : $"'{vector.Name}' has no component '{member.Member}'.");
                    return null;
                }

                return new Typed(new StorageStructPropertyReference(target.Ir, member.Member), vector.ComponentType);

            default:
                Diagnostics.Error(member.Position, $"'{target.Type.Name}' has no members.");
                return null;
        }
    }

    private Typed? LowerUnary(GdUnaryExpr unary)
    {
        var operand = LowerExpression(unary.Operand);
        if (operand is null) return null;

        switch (unary.Operator)
        {
            case "+" when IsNumeric(operand.Type):
                return operand;
            case "-" when operand.Type is GdScalarType { Kind: GdScalarKind.Int or GdScalarKind.Float }:
                // Fold literals so `-1.0` is a constant, as the Roslyn frontend does.
                return operand.Ir switch
                {
                    FloatLiteral f => new Typed(new FloatLiteral(-f.Value), operand.Type),
                    IntLiteral { Signed: true } i => new Typed(
                        new IntLiteral(i.Value.StartsWith('-') ? i.Value[1..] : "-" + i.Value, true), operand.Type),
                    _ => new Typed(new UnaryExpression(UnaryOperator.Negate, operand.Ir), operand.Type)
                };
            case "!" when operand.Type is GdScalarType { Kind: GdScalarKind.Bool }:
                return new Typed(new UnaryExpression(UnaryOperator.LogicalNot, operand.Ir), operand.Type);
            case "~" when operand.Type is GdScalarType { IsInteger: true }:
                return new Typed(new UnaryExpression(UnaryOperator.BitwiseNot, operand.Ir), operand.Type);
        }

        Diagnostics.Error(unary.Position, operand.Type is GdVectorType
            ? "Operators on vectors are not supported by the IR yet."
            : $"Operator '{unary.Operator}' cannot be applied to '{operand.Type.Name}'.");
        return null;
    }

    private Typed? LowerBinary(GdBinaryExpr binary)
    {
        var left = LowerExpression(binary.Left);
        var right = LowerExpression(binary.Right);
        if (left is null || right is null) return null;
        return LowerBinaryOperation(binary.Operator, left, right, binary.Position, binary.Left, binary.Right);
    }

    private Typed? LowerBinaryOperation(
        string op, Typed left, Typed right, GdPosition position, GdExpr leftSource, GdExpr rightSource)
    {
        if (left.Type is GdVectorType || right.Type is GdVectorType)
        {
            Diagnostics.Error(position, "Operators on vectors are not supported by the IR yet; operate on components.");
            return null;
        }

        if (left.Type is not GdScalarType || right.Type is not GdScalarType)
        {
            Diagnostics.Error(position, $"Operator '{op}' cannot be applied to '{left.Type.Name}' and '{right.Type.Name}'.");
            return null;
        }

        BinaryOperator irOperator;
        switch (op)
        {
            case "&&" or "||":
                if (left.Type is not GdScalarType { Kind: GdScalarKind.Bool } ||
                    right.Type is not GdScalarType { Kind: GdScalarKind.Bool })
                {
                    Diagnostics.Error(position, $"Operator '{op}' needs 'bool' operands.");
                    return null;
                }

                irOperator = op == "&&" ? BinaryOperator.LogicalAnd : BinaryOperator.LogicalOr;
                if (ContainsSequence(right.Ir)) return ShortCircuit(irOperator, left, right);
                return new Typed(new BinaryExpression(irOperator, left.Ir, right.Ir), GdScalarType.Bool);

            case "<<" or ">>":
                if (left.Type is not GdScalarType { IsInteger: true } || right.Type is not GdScalarType { IsInteger: true })
                {
                    Diagnostics.Error(position, $"Operator '{op}' needs integer operands.");
                    return null;
                }

                return new Typed(new BinaryExpression(
                    op == "<<" ? BinaryOperator.ShiftLeft : BinaryOperator.ShiftRight, left.Ir, right.Ir), left.Type);
        }

        var unified = Unify(left, right, position);
        if (unified is null) return null;
        var (l, r, type) = unified.Value;

        bool numeric = type.IsNumeric;
        switch (op)
        {
            case "+" or "-" or "*" or "/" or "%" when numeric:
                irOperator = op switch
                {
                    "+" => BinaryOperator.Add,
                    "-" => BinaryOperator.Subtract,
                    "*" => BinaryOperator.Multiply,
                    "/" => BinaryOperator.Divide,
                    _ => BinaryOperator.Remainder
                };
                return new Typed(new BinaryExpression(irOperator, l, r), type);
            case "&" or "|" or "^" when type.IsInteger:
                irOperator = op switch { "&" => BinaryOperator.BitwiseAnd, "|" => BinaryOperator.BitwiseOr, _ => BinaryOperator.BitwiseXor };
                return new Typed(new BinaryExpression(irOperator, l, r), type);
            case "==" or "!=":
                return new Typed(new BinaryExpression(
                    op == "==" ? BinaryOperator.Equal : BinaryOperator.NotEqual, l, r), GdScalarType.Bool);
            case "<" or "<=" or ">" or ">=" when numeric:
                irOperator = op switch
                {
                    "<" => BinaryOperator.Less,
                    "<=" => BinaryOperator.LessEqual,
                    ">" => BinaryOperator.Greater,
                    _ => BinaryOperator.GreaterEqual
                };
                return new Typed(new BinaryExpression(irOperator, l, r), GdScalarType.Bool);
        }

        Diagnostics.Error(position, $"Operator '{op}' cannot be applied to '{type.Name}'.");
        return null;
    }

    /// <summary>Brings two scalar operands to a common type, adopting the type of a literal when needed.</summary>
    private (Expression Left, Expression Right, GdScalarType Type)? Unify(Typed left, Typed right, GdPosition position)
    {
        var a = (GdScalarType)left.Type;
        var b = (GdScalarType)right.Type;
        if (a.Kind == b.Kind) return (left.Ir, right.Ir, a);

        if (a.IsNumeric && b.IsNumeric)
        {
            if (a.Kind == GdScalarKind.Float) return (left.Ir, Coerce(right, a, position), a);
            if (b.Kind == GdScalarKind.Float) return (Coerce(left, b, position), right.Ir, b);

            // int vs uint: only a signed literal can adopt the other type.
            if (left.Ir is IntLiteral { Signed: true }) return (Coerce(left, b, position), right.Ir, b);
            if (right.Ir is IntLiteral { Signed: true }) return (left.Ir, Coerce(right, a, position), a);
            Diagnostics.Error(position, "Mixing 'int' and 'uint' needs an explicit conversion such as int(x) or uint(x).");
            return null;
        }

        Diagnostics.Error(position, $"Cannot combine '{a.Name}' and '{b.Name}'.");
        return null;
    }

    private Typed ShortCircuit(BinaryOperator op, Typed left, Typed right)
    {
        var result = NewLocal(GdScalarType.Bool, "__shortcircuit");
        var reference = new StorageReference(result);
        Expression runRight = op == BinaryOperator.LogicalAnd
            ? reference
            : new UnaryExpression(UnaryOperator.LogicalNot, reference);
        return new Typed(new SequenceExpression(
            [
                new VariableDeclaration(result, left.Ir),
                new If(runRight, new Assignment(reference, right.Ir), null)
            ],
            reference), GdScalarType.Bool);
    }

    private static bool ContainsSequence(Expression expression) => expression switch
    {
        SequenceExpression => true,
        BinaryExpression b => ContainsSequence(b.Left) || ContainsSequence(b.Right),
        UnaryExpression u => ContainsSequence(u.Operand),
        NumericConversion c => ContainsSequence(c.Operand),
        StorageStructPropertyReference p => ContainsSequence(p.Reference),
        FunctionCallExpression call => call.Arguments.Any(ContainsSequence),
        VectorExpression v => v.Values.Any(ContainsSequence),
        CustomStructExpression s => s.Properties.Any(ContainsSequence),
        _ => false
    };

    private Typed? LowerTernary(GdTernaryExpr ternary)
    {
        var condition = LowerCondition(ternary.Condition);
        var whenTrue = LowerExpression(ternary.WhenTrue);
        var whenFalse = LowerExpression(ternary.WhenFalse);
        if (condition is null || whenTrue is null || whenFalse is null) return null;

        GdType type = whenTrue.Type;
        Expression a = whenTrue.Ir;
        Expression b = whenFalse.Ir;
        if (!GdType.Same(whenTrue.Type, whenFalse.Type))
        {
            if (whenTrue.Type is GdScalarType && whenFalse.Type is GdScalarType)
            {
                var unified = Unify(whenTrue, whenFalse, ternary.Position);
                if (unified is null) return null;
                (a, b, var scalar) = unified.Value;
                type = scalar;
            }
            else
            {
                Diagnostics.Error(ternary.Position,
                    $"The branches of a conditional expression have different types ('{whenTrue.Type.Name}' and '{whenFalse.Type.Name}').");
                return null;
            }
        }

        var result = NewLocal(type, "__select");
        var reference = new StorageReference(result);
        // Only the chosen branch is evaluated, like in GDScript.
        return new Typed(new SequenceExpression(
            [
                new VariableDeclaration(result, null),
                new If(condition, new Assignment(reference, a), new Assignment(reference, b))
            ],
            reference), type);
    }

    // ---- Calls --------------------------------------------------------------------------------------------

    private Typed? LowerCall(GdCallExpr call)
    {
        // Struct.new()
        if (call.Callee is GdMemberExpr { Target: GdNameExpr owner, Member: "new" } &&
            _module.Structs.TryGetValue(owner.Name, out var structure))
        {
            if (call.Arguments.Count != 0)
            {
                Diagnostics.Error(call.Position, $"'{structure.Name}.new()' takes no arguments; assign the fields afterwards.");
                return null;
            }

            var template = (CustomStruct)_module.MakeStorage(structure, structure.Name, StorageKind.Local, null, false, Stage.None);
            return new Typed(new CustomStructExpression(template, []), structure);
        }

        if (call.Callee is not GdNameExpr callee)
        {
            Diagnostics.Error(call.Position, "Only direct function calls are supported.");
            return null;
        }

        string name = callee.Name;

        // Vector constructors and casts.
        if (GdLowerer.BuiltInType(name) is { } builtIn)
        {
            return builtIn switch
            {
                GdVectorType vector => LowerVectorConstructor(vector, call),
                GdScalarType { Kind: GdScalarKind.Bool } => Unsupported(call, "Use a comparison instead of bool()."),
                GdScalarType scalar => LowerCast(scalar, call),
                _ => null
            };
        }

        if (name == "range")
        {
            Diagnostics.Error(call.Position, "range() can only be used as the iterable of a 'for' loop.");
            return null;
        }

        if (Intrinsics.Contains(name) && !_module.Functions.ContainsKey(name))
        {
            Diagnostics.Error(call.Position,
                $"'{name}' is an intrinsic function; the IR has no intrinsic calls yet, so it cannot be compiled.");
            return null;
        }

        if (!ResolveFunctionCall(call, out var function, out var arguments)) return null;
        if (function!.ReturnType is GdVoidType)
        {
            Diagnostics.Error(call.Position, $"'{function.Declaration.Name}' returns void and cannot be used as a value.");
            return null;
        }

        return new Typed(new FunctionCallExpression(_module.Reference(function), arguments!), function.ReturnType);
    }

    private Typed? Unsupported(GdCallExpr call, string message)
    {
        Diagnostics.Error(call.Position, message);
        return null;
    }

    private bool ResolveFunctionCall(GdCallExpr call, out GdFunctionInfo? function, out List<Expression>? arguments)
    {
        function = null;
        arguments = null;

        if (call.Callee is not GdNameExpr callee)
        {
            Diagnostics.Error(call.Position, "Only direct function calls are supported.");
            return false;
        }

        if (!_module.Functions.TryGetValue(callee.Name, out function))
        {
            Diagnostics.Error(call.Position, Intrinsics.Contains(callee.Name)
                ? $"'{callee.Name}' is an intrinsic function; the IR has no intrinsic calls yet, so it cannot be compiled."
                : $"Unknown function '{callee.Name}'.");
            return false;
        }

        if (function.Stage != Stage.None)
        {
            Diagnostics.Error(call.Position, $"Entry point '{callee.Name}' cannot be called from shader code.");
            return false;
        }

        if (ReferenceEquals(function, _info))
        {
            Diagnostics.Error(call.Position, "Recursion is not supported in shaders.");
            return false;
        }

        if (call.Arguments.Count != function.ParameterTypes.Count)
        {
            Diagnostics.Error(call.Position,
                $"'{callee.Name}' takes {function.ParameterTypes.Count} argument(s) but {call.Arguments.Count} were given.");
            return false;
        }

        arguments = new List<Expression>();
        bool ok = true;
        for (int i = 0; i < call.Arguments.Count; i++)
        {
            var argument = LowerExpression(call.Arguments[i]);
            if (argument is null)
            {
                ok = false;
                continue;
            }

            arguments.Add(Coerce(argument, function.ParameterTypes[i], call.Arguments[i].Position));
        }

        return ok;
    }

    private Typed? LowerVectorConstructor(GdVectorType vector, GdCallExpr call)
    {
        if (call.Arguments.Count != vector.Length)
        {
            Diagnostics.Error(call.Position,
                $"{vector.Name}() needs {vector.Length} scalar arguments, but {call.Arguments.Count} were given.");
            return null;
        }

        var values = new List<Expression>();
        foreach (var argument in call.Arguments)
        {
            var value = LowerExpression(argument);
            if (value is null) return null;
            if (value.Type is not GdScalarType)
            {
                Diagnostics.Error(argument.Position,
                    "Vector constructors take scalar components only; combining vectors is not supported by the IR yet.");
                return null;
            }

            values.Add(Coerce(value, vector.ComponentType, argument.Position));
        }

        return new Typed(new VectorExpression(values, vector.Component), vector);
    }

    private Typed? LowerCast(GdScalarType target, GdCallExpr call)
    {
        if (call.Arguments.Count != 1)
        {
            Diagnostics.Error(call.Position, $"{target.Name}() takes exactly one argument.");
            return null;
        }

        var value = LowerExpression(call.Arguments[0]);
        if (value is null) return null;
        if (value.Type is not GdScalarType { IsNumeric: true } source)
        {
            Diagnostics.Error(call.Position, $"Cannot convert '{value.Type.Name}' to '{target.Name}'.");
            return null;
        }

        if (source.Kind == target.Kind) return value;

        // Literal folding keeps `float(1)` a constant.
        if (target.Kind == GdScalarKind.Float && value.Ir is IntLiteral literal)
        {
            return new Typed(new FloatLiteral(float.Parse(literal.Value, CultureInfo.InvariantCulture)), target);
        }

        var scalar = target.Kind switch
        {
            GdScalarKind.Int => ScalarType.Int32,
            GdScalarKind.UInt => ScalarType.UInt32,
            _ => ScalarType.Float32
        };
        return new Typed(new NumericConversion(value.Ir, scalar), target);
    }
}
