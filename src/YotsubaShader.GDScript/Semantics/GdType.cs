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

using YotsubaShader.GDScript.Syntax;
using YotsubaShader.IR.Intermediate.Types;

namespace YotsubaShader.GDScript.Semantics;

/// <summary>A type in the GDScript shader subset. Compare types with <see cref="Same"/>.</summary>
public abstract record GdType
{
    public abstract string Name { get; }

    public static bool Same(GdType a, GdType b) => a.Name == b.Name;

    public override string ToString() => Name;
}

public sealed record GdVoidType : GdType
{
    public static GdVoidType Instance { get; } = new();

    public override string Name => "void";
}

public enum GdScalarKind { Bool, Int, UInt, Float }

public sealed record GdScalarType(GdScalarKind Kind) : GdType
{
    public static GdScalarType Bool { get; } = new(GdScalarKind.Bool);
    public static GdScalarType Int { get; } = new(GdScalarKind.Int);
    public static GdScalarType UInt { get; } = new(GdScalarKind.UInt);
    public static GdScalarType Float { get; } = new(GdScalarKind.Float);

    public bool IsNumeric => Kind != GdScalarKind.Bool;

    public bool IsInteger => Kind is GdScalarKind.Int or GdScalarKind.UInt;

    public override string Name => Kind switch
    {
        GdScalarKind.Bool => "bool",
        GdScalarKind.Int => "int",
        GdScalarKind.UInt => "uint",
        _ => "float"
    };
}

public sealed record GdVectorType(VectorComponentsType Component, int Length) : GdType
{
    public override string Name => $"Vector{Length}{Component switch
    {
        VectorComponentsType.Int => "i",
        VectorComponentsType.Uint => "u",
        _ => ""
    }}";

    public GdScalarType ComponentType => Component switch
    {
        VectorComponentsType.Int => GdScalarType.Int,
        VectorComponentsType.Uint => GdScalarType.UInt,
        _ => GdScalarType.Float
    };
}

public sealed record GdStructMember(string Name, GdType Type, GdField Field);

public sealed record GdStructType(GdStructDecl Declaration, IReadOnlyList<GdStructMember> Members) : GdType
{
    public override string Name => Declaration.Name;
}
