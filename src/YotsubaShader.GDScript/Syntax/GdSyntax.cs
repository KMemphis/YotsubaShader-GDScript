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

using YotsubaShader.GDScript.Diagnostics;

namespace YotsubaShader.GDScript.Syntax;

// ---- Expressions --------------------------------------------------------------------------------------------

public abstract record GdExpr(GdPosition Position);

public enum GdLiteralKind { Int, UInt, Float, Bool, String }

public sealed record GdLiteralExpr(GdLiteralKind Kind, string Text, GdPosition Position) : GdExpr(Position);

public sealed record GdNameExpr(string Name, GdPosition Position) : GdExpr(Position);

public sealed record GdMemberExpr(GdExpr Target, string Member, GdPosition Position) : GdExpr(Position);

public sealed record GdCallExpr(GdExpr Callee, IReadOnlyList<GdExpr> Arguments, GdPosition Position) : GdExpr(Position);

public sealed record GdUnaryExpr(string Operator, GdExpr Operand, GdPosition Position) : GdExpr(Position);

public sealed record GdBinaryExpr(string Operator, GdExpr Left, GdExpr Right, GdPosition Position) : GdExpr(Position);

/// <summary><c>whenTrue if condition else whenFalse</c></summary>
public sealed record GdTernaryExpr(GdExpr Condition, GdExpr WhenTrue, GdExpr WhenFalse, GdPosition Position)
    : GdExpr(Position);

// ---- Statements ---------------------------------------------------------------------------------------------

public abstract record GdStmt(GdPosition Position);

public sealed record GdVarStmt(string Name, string? TypeName, GdExpr? Initializer, bool IsConst, GdPosition Position)
    : GdStmt(Position);

/// <summary>Assignment; <see cref="Operator"/> is "=" or a compound operator such as "+=".</summary>
public sealed record GdAssignStmt(GdExpr Target, string Operator, GdExpr Value, GdPosition Position) : GdStmt(Position);

public sealed record GdExprStmt(GdExpr Expression, GdPosition Position) : GdStmt(Position);

public sealed record GdIfStmt(GdExpr Condition, IReadOnlyList<GdStmt> Then, IReadOnlyList<GdStmt>? Else, GdPosition Position)
    : GdStmt(Position);

public sealed record GdWhileStmt(GdExpr Condition, IReadOnlyList<GdStmt> Body, GdPosition Position) : GdStmt(Position);

/// <summary><c>for name in range(arguments): body</c></summary>
public sealed record GdForStmt(string Variable, GdExpr Iterable, IReadOnlyList<GdStmt> Body, GdPosition Position)
    : GdStmt(Position);

public sealed record GdReturnStmt(GdExpr? Value, GdPosition Position) : GdStmt(Position);

public sealed record GdBreakStmt(GdPosition Position) : GdStmt(Position);

public sealed record GdContinueStmt(GdPosition Position) : GdStmt(Position);

public sealed record GdPassStmt(GdPosition Position) : GdStmt(Position);

// ---- Declarations -------------------------------------------------------------------------------------------

public sealed record GdAnnotation(string Name, IReadOnlyList<GdExpr> Arguments, GdPosition Position);

public sealed record GdField(string Name, string TypeName, IReadOnlyList<GdAnnotation> Annotations, GdPosition Position);

public sealed record GdStructDecl(string Name, IReadOnlyList<GdField> Fields, GdPosition Position);

public sealed record GdGlobalVar(
    string Name,
    string? TypeName,
    GdExpr? Initializer,
    bool IsConst,
    IReadOnlyList<GdAnnotation> Annotations,
    GdPosition Position);

public sealed record GdParameter(string Name, string TypeName, IReadOnlyList<GdAnnotation> Annotations, GdPosition Position);

public sealed record GdFunctionDecl(
    string Name,
    IReadOnlyList<GdParameter> Parameters,
    string? ReturnType,
    IReadOnlyList<GdAnnotation> Annotations,
    IReadOnlyList<GdStmt> Body,
    GdPosition Position);

public sealed record GdFile(
    string? ClassName,
    IReadOnlyList<GdStructDecl> Structs,
    IReadOnlyList<GdGlobalVar> Globals,
    IReadOnlyList<GdFunctionDecl> Functions);
