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
using YotsubaShader.GDScript.Lexing;
using YotsubaShader.GDScript.Syntax;

namespace YotsubaShader.GDScript.Parsing;

/// <summary>Recursive-descent parser for the shader subset of GDScript.</summary>
public sealed class GdParser
{
    private static readonly HashSet<string> Unsupported =
        ["match", "await", "signal", "extends", "enum", "static", "lambda", "yield", "assert", "preload", "load"];

    private readonly IReadOnlyList<GdToken> _tokens;
    private readonly GdDiagnosticBag _diagnostics;
    private int _index;

    public GdParser(IReadOnlyList<GdToken> tokens, GdDiagnosticBag diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    public static GdFile Parse(string source, GdDiagnosticBag diagnostics)
    {
        var tokens = new GdLexer(source, diagnostics).Tokenize();
        return new GdParser(tokens, diagnostics).ParseFile();
    }

    private GdToken Current => _tokens[_index];

    private GdToken Peek(int offset = 1) => _tokens[Math.Min(_index + offset, _tokens.Count - 1)];

    private GdToken Advance() => _tokens[Math.Min(_index++, _tokens.Count - 1)];

    private bool AtEnd => Current.Kind == GdTokenKind.EndOfFile;

    private bool Accept(string symbol)
    {
        if (!Current.Is(symbol)) return false;
        _index++;
        return true;
    }

    private bool AcceptKeyword(string keyword)
    {
        if (!Current.IsKeyword(keyword)) return false;
        _index++;
        return true;
    }

    private void Expect(string symbol)
    {
        if (!Accept(symbol)) Fail($"Expected '{symbol}' but found '{Describe(Current)}'.");
    }

    private string ExpectIdentifier(string what)
    {
        if (Current.Kind == GdTokenKind.Identifier) return Advance().Text;
        Fail($"Expected {what} but found '{Describe(Current)}'.");
        return "<missing>";
    }

    private static string Describe(GdToken token) => token.Kind switch
    {
        GdTokenKind.Newline => "end of line",
        GdTokenKind.Indent => "indent",
        GdTokenKind.Dedent => "dedent",
        GdTokenKind.EndOfFile => "end of file",
        _ => token.Text
    };

    private void Fail(string message) => _diagnostics.Error(Current.Position, message);

    /// <summary>Skips to the next line so one syntax error does not cascade.</summary>
    private void Recover()
    {
        while (!AtEnd && Current.Kind is not GdTokenKind.Newline) _index++;
        if (Current.Kind == GdTokenKind.Newline) _index++;
        while (Current.Kind is GdTokenKind.Indent) _index++;
    }

    private void SkipNewlines()
    {
        while (Current.Kind == GdTokenKind.Newline) _index++;
    }

    // ---- File ---------------------------------------------------------------------------------------------

    public GdFile ParseFile()
    {
        string? className = null;
        var structs = new List<GdStructDecl>();
        var globals = new List<GdGlobalVar>();
        var functions = new List<GdFunctionDecl>();

        SkipNewlines();
        while (!AtEnd)
        {
            int before = _index;
            ParseTopLevel(ref className, structs, globals, functions);
            SkipNewlines();
            if (_index == before) Recover();
        }

        return new GdFile(className, structs, globals, functions);
    }

    private void ParseTopLevel(
        ref string? className,
        List<GdStructDecl> structs,
        List<GdGlobalVar> globals,
        List<GdFunctionDecl> functions)
    {
        if (Current.Kind == GdTokenKind.Identifier && Unsupported.Contains(Current.Text))
        {
            Fail($"'{Current.Text}' is not supported in shader scripts.");
            Recover();
            return;
        }

        if (AcceptKeyword("class_name"))
        {
            className = ExpectIdentifier("a class name");
            EndOfLine();
            return;
        }

        var annotations = ParseAnnotations();
        SkipNewlines();

        if (Current.IsKeyword("class"))
        {
            structs.Add(ParseStruct());
        }
        else if (Current.IsKeyword("func"))
        {
            functions.Add(ParseFunction(annotations));
        }
        else if (Current.IsKeyword("var") || Current.IsKeyword("const"))
        {
            globals.Add(ParseGlobal(annotations));
        }
        else
        {
            Fail($"Unexpected '{Describe(Current)}' at the top level.");
            Recover();
        }
    }

    private void EndOfLine()
    {
        if (Current.Kind == GdTokenKind.Newline) _index++;
        else if (!AtEnd && Current.Kind != GdTokenKind.Dedent) Fail($"Unexpected '{Describe(Current)}'.");
    }

    // ---- Annotations --------------------------------------------------------------------------------------

    private List<GdAnnotation> ParseAnnotations()
    {
        var list = new List<GdAnnotation>();
        while (Current.Is("@"))
        {
            var position = Advance().Position;
            string name = ExpectIdentifier("an annotation name");
            var arguments = new List<GdExpr>();
            if (Accept("("))
            {
                if (!Current.Is(")"))
                {
                    do arguments.Add(ParseExpression());
                    while (Accept(","));
                }

                Expect(")");
            }

            list.Add(new GdAnnotation(name, arguments, position));
            // Annotations may sit on their own lines.
            if (Current.Kind == GdTokenKind.Newline && Peek().Is("@")) _index++;
        }

        return list;
    }

    // ---- Declarations -------------------------------------------------------------------------------------

    private GdStructDecl ParseStruct()
    {
        var position = Advance().Position; // class
        string name = ExpectIdentifier("a struct name");
        if (Current.IsKeyword("extends"))
        {
            Fail("'extends' is not supported in shader structs.");
            Recover();
        }
        else
        {
            Expect(":");
            EndOfLineAfterColon();
        }

        var fields = new List<GdField>();
        if (Current.Kind == GdTokenKind.Indent)
        {
            _index++;
            while (Current.Kind != GdTokenKind.Dedent && !AtEnd)
            {
                SkipNewlines();
                if (Current.Kind == GdTokenKind.Dedent || AtEnd) break;
                var fieldAnnotations = ParseAnnotations();
                SkipNewlines();
                if (AcceptKeyword("pass")) { EndOfLine(); continue; }
                if (!Current.IsKeyword("var"))
                {
                    Fail($"Only 'var' fields are allowed in a shader struct, found '{Describe(Current)}'.");
                    Recover();
                    continue;
                }

                var fieldPosition = Advance().Position;
                string fieldName = ExpectIdentifier("a field name");
                Expect(":");
                string typeName = ExpectIdentifier("a type name");
                if (Current.Is("=")) Fail("Field initializers are not supported in shader structs.");
                EndOfLine();
                fields.Add(new GdField(fieldName, typeName, fieldAnnotations, fieldPosition));
            }

            if (Current.Kind == GdTokenKind.Dedent) _index++;
        }
        else
        {
            Fail("A struct needs an indented body.");
        }

        return new GdStructDecl(name, fields, position);
    }

    private GdGlobalVar ParseGlobal(List<GdAnnotation> annotations)
    {
        var position = Current.Position;
        bool isConst = Advance().Text == "const";
        string name = ExpectIdentifier("a variable name");
        string? typeName = null;
        GdExpr? initializer = null;
        if (Accept(":"))
        {
            typeName = ExpectIdentifier("a type name");
        }
        else if (Accept(":="))
        {
            initializer = ParseExpression();
        }

        if (initializer is null && Accept("=")) initializer = ParseExpression();
        EndOfLine();
        return new GdGlobalVar(name, typeName, initializer, isConst, annotations, position);
    }

    private GdFunctionDecl ParseFunction(List<GdAnnotation> annotations)
    {
        var position = Advance().Position; // func
        string name = ExpectIdentifier("a function name");
        var parameters = new List<GdParameter>();
        Expect("(");
        if (!Current.Is(")"))
        {
            do
            {
                var parameterAnnotations = ParseAnnotations();
                var parameterPosition = Current.Position;
                string parameterName = ExpectIdentifier("a parameter name");
                string parameterType = "";
                if (Accept(":")) parameterType = ExpectIdentifier("a type name");
                else Fail($"Parameter '{parameterName}' needs a type.");
                parameters.Add(new GdParameter(parameterName, parameterType, parameterAnnotations, parameterPosition));
            }
            while (Accept(","));
        }

        Expect(")");
        string? returnType = null;
        if (Accept("->")) returnType = ExpectIdentifier("a return type");
        Expect(":");
        var body = ParseBlockAfterColon();
        return new GdFunctionDecl(name, parameters, returnType, annotations, body, position);
    }

    // ---- Statements ---------------------------------------------------------------------------------------

    private void EndOfLineAfterColon()
    {
        if (Current.Kind == GdTokenKind.Newline) _index++;
    }

    private List<GdStmt> ParseBlockAfterColon()
    {
        var statements = new List<GdStmt>();
        if (Current.Kind != GdTokenKind.Newline)
        {
            // Single-line body: `if x: return 1`
            var single = ParseStatement();
            if (single is not null) statements.Add(single);
            return statements;
        }

        _index++;
        SkipNewlines();
        if (Current.Kind != GdTokenKind.Indent)
        {
            Fail("Expected an indented block.");
            return statements;
        }

        _index++;
        while (Current.Kind != GdTokenKind.Dedent && !AtEnd)
        {
            SkipNewlines();
            if (Current.Kind == GdTokenKind.Dedent || AtEnd) break;
            int before = _index;
            var statement = ParseStatement();
            if (statement is not null) statements.Add(statement);
            if (_index == before) Recover();
        }

        if (Current.Kind == GdTokenKind.Dedent) _index++;
        return statements;
    }

    private GdStmt? ParseStatement()
    {
        var token = Current;
        if (token.Kind == GdTokenKind.Identifier && Unsupported.Contains(token.Text))
        {
            Fail($"'{token.Text}' is not supported in shader scripts.");
            Recover();
            return null;
        }

        if (token.IsKeyword("var") || token.IsKeyword("const")) return ParseLocal();
        if (token.IsKeyword("if")) return ParseIf();
        if (token.IsKeyword("while"))
        {
            _index++;
            var condition = ParseExpression();
            Expect(":");
            return new GdWhileStmt(condition, ParseBlockAfterColon(), token.Position);
        }

        if (token.IsKeyword("for"))
        {
            _index++;
            string variable = ExpectIdentifier("a loop variable");
            if (!AcceptKeyword("in")) Fail("Expected 'in' in a for loop.");
            var iterable = ParseExpression();
            Expect(":");
            return new GdForStmt(variable, iterable, ParseBlockAfterColon(), token.Position);
        }

        GdStmt result;
        if (token.IsKeyword("return"))
        {
            _index++;
            GdExpr? value = Current.Kind is GdTokenKind.Newline or GdTokenKind.Dedent or GdTokenKind.EndOfFile
                ? null
                : ParseExpression();
            result = new GdReturnStmt(value, token.Position);
        }
        else if (token.IsKeyword("break")) { _index++; result = new GdBreakStmt(token.Position); }
        else if (token.IsKeyword("continue")) { _index++; result = new GdContinueStmt(token.Position); }
        else if (token.IsKeyword("pass")) { _index++; result = new GdPassStmt(token.Position); }
        else
        {
            var expression = ParseExpression();
            if (Current.Kind == GdTokenKind.Symbol && Current.Text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or
                "&=" or "|=" or "^=" or "<<=" or ">>=")
            {
                string op = Advance().Text;
                result = new GdAssignStmt(expression, op, ParseExpression(), token.Position);
            }
            else
            {
                result = new GdExprStmt(expression, token.Position);
            }
        }

        EndOfLine();
        return result;
    }

    private GdStmt ParseLocal()
    {
        var position = Current.Position;
        bool isConst = Advance().Text == "const";
        string name = ExpectIdentifier("a variable name");
        string? typeName = null;
        GdExpr? initializer = null;
        if (Accept(":="))
        {
            initializer = ParseExpression();
        }
        else
        {
            if (Accept(":")) typeName = ExpectIdentifier("a type name");
            if (Accept("=")) initializer = ParseExpression();
        }

        EndOfLine();
        return new GdVarStmt(name, typeName, initializer, isConst, position);
    }

    private GdStmt ParseIf()
    {
        var position = Advance().Position; // if / elif
        var condition = ParseExpression();
        Expect(":");
        var then = ParseBlockAfterColon();
        List<GdStmt>? otherwise = null;
        SkipNewlines();
        if (Current.IsKeyword("elif"))
        {
            otherwise = [ParseIf()];
        }
        else if (Current.IsKeyword("else"))
        {
            _index++;
            Expect(":");
            otherwise = ParseBlockAfterColon();
        }

        return new GdIfStmt(condition, then, otherwise, position);
    }

    // ---- Expressions --------------------------------------------------------------------------------------

    public GdExpr ParseExpression()
    {
        var value = ParseBinary(0);
        // GDScript ternary: `a if cond else b`
        if (Current.IsKeyword("if"))
        {
            var position = Advance().Position;
            var condition = ParseBinary(0);
            if (!AcceptKeyword("else")) Fail("Expected 'else' in a conditional expression.");
            var whenFalse = ParseExpression();
            return new GdTernaryExpr(condition, value, whenFalse, position);
        }

        return value;
    }

    // Lowest to highest precedence.
    private static readonly string[][] Levels =
    [
        ["or", "||"],
        ["and", "&&"],
        ["==", "!=", "<", "<=", ">", ">="],
        ["|"],
        ["^"],
        ["&"],
        ["<<", ">>"],
        ["+", "-"],
        ["*", "/", "%"]
    ];

    private bool IsOperator(int level, out string op)
    {
        op = Current.Text;
        bool candidate = Current.Kind == GdTokenKind.Symbol || (Current.Kind == GdTokenKind.Identifier && op is "and" or "or");
        return candidate && Levels[level].Contains(op);
    }

    private GdExpr ParseBinary(int level)
    {
        if (level >= Levels.Length) return ParseUnary();
        var left = ParseBinary(level + 1);
        while (IsOperator(level, out string op))
        {
            var position = Advance().Position;
            var right = ParseBinary(level + 1);
            left = new GdBinaryExpr(op switch { "and" => "&&", "or" => "||", _ => op }, left, right, position);
        }

        return left;
    }

    private GdExpr ParseUnary()
    {
        var token = Current;
        if (token.Kind == GdTokenKind.Symbol && token.Text is "-" or "!" or "~" or "+")
        {
            _index++;
            return new GdUnaryExpr(token.Text, ParseUnary(), token.Position);
        }

        if (token.IsKeyword("not"))
        {
            _index++;
            return new GdUnaryExpr("!", ParseUnary(), token.Position);
        }

        return ParsePostfix();
    }

    private GdExpr ParsePostfix()
    {
        var expression = ParsePrimary();
        while (true)
        {
            if (Current.Is("."))
            {
                var position = Advance().Position;
                expression = new GdMemberExpr(expression, ExpectIdentifier("a member name"), position);
            }
            else if (Current.Is("("))
            {
                var position = Advance().Position;
                var arguments = new List<GdExpr>();
                if (!Current.Is(")"))
                {
                    do arguments.Add(ParseExpression());
                    while (Accept(","));
                }

                Expect(")");
                expression = new GdCallExpr(expression, arguments, position);
            }
            else if (Current.Is("["))
            {
                Fail("Indexing is not supported by the IR frontend yet.");
                Recover();
                return expression;
            }
            else
            {
                return expression;
            }
        }
    }

    private GdExpr ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case GdTokenKind.Integer:
                _index++;
                return token.Text.EndsWith('u')
                    ? new GdLiteralExpr(GdLiteralKind.UInt, token.Text[..^1], token.Position)
                    : new GdLiteralExpr(GdLiteralKind.Int, token.Text, token.Position);
            case GdTokenKind.Float:
                _index++;
                return new GdLiteralExpr(GdLiteralKind.Float, token.Text, token.Position);
            case GdTokenKind.String:
                _index++;
                return new GdLiteralExpr(GdLiteralKind.String, token.Text, token.Position);
            case GdTokenKind.Identifier when token.Text is "true" or "false":
                _index++;
                return new GdLiteralExpr(GdLiteralKind.Bool, token.Text, token.Position);
            case GdTokenKind.Identifier:
                _index++;
                return new GdNameExpr(token.Text, token.Position);
            case GdTokenKind.Symbol when token.Text == "(":
                _index++;
                var inner = ParseExpression();
                Expect(")");
                return inner;
        }

        Fail($"Unexpected '{Describe(token)}' in expression.");
        if (!AtEnd && Current.Kind != GdTokenKind.Newline) _index++;
        return new GdNameExpr("<error>", token.Position);
    }
}
