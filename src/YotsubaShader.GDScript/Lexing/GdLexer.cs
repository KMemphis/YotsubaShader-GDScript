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

using System.Text;
using YotsubaShader.GDScript.Diagnostics;

namespace YotsubaShader.GDScript.Lexing;

/// <summary>
/// Turns GDScript source into tokens. Indentation is converted into <see cref="GdTokenKind.Indent"/> and
/// <see cref="GdTokenKind.Dedent"/> tokens; newlines inside parentheses or brackets are ignored.
/// </summary>
public sealed class GdLexer
{
    private static readonly string[] Symbols =
    [
        "<<=", ">>=", "->", ":=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<", ">>", "<=", ">=", "==",
        "!=", "&&", "||", "+", "-", "*", "/", "%", "&", "|", "^", "~", "<", ">", "=", "!", "(", ")", "[", "]",
        ",", ":", ".", "@"
    ];

    private readonly string _source;
    private readonly GdDiagnosticBag _diagnostics;
    private readonly List<GdToken> _tokens = new();
    private readonly Stack<int> _indents = new();
    private int _index;
    private int _line = 1;
    private int _lineStart;
    private int _depth;
    private char? _indentChar;
    private bool _lineHasCode;
    private readonly List<GdComment> _comments = new();

    /// <param name="source">The text to tokenize.</param>
    /// <param name="diagnostics">Receives lexical errors.</param>
    /// <param name="startLine">Line of the first character (used when lexing the text of a comment).</param>
    /// <param name="startColumn">Column of the first character.</param>
    public GdLexer(string source, GdDiagnosticBag diagnostics, int startLine = 1, int startColumn = 1)
    {
        _source = source.Replace("\r\n", "\n").Replace('\r', '\n');
        _diagnostics = diagnostics;
        _indents.Push(0);
        _line = startLine;
        _lineStart = -(startColumn - 1);
    }

    /// <summary>Every comment of the source, in order. Valid after <see cref="Tokenize"/>.</summary>
    public IReadOnlyList<GdComment> Comments => _comments;

    public IReadOnlyList<GdToken> Tokenize()
    {
        bool atLineStart = true;
        while (_index < _source.Length)
        {
            if (atLineStart && _depth == 0)
            {
                if (!ReadIndentation()) continue;
                atLineStart = false;
            }

            char c = _source[_index];
            if (c == '\n')
            {
                if (_depth == 0 && _tokens.Count > 0 && _tokens[^1].Kind != GdTokenKind.Newline)
                {
                    Add(GdTokenKind.Newline, "\\n");
                }

                _index++;
                _line++;
                _lineStart = _index;
                // Inside parentheses a new line continues the expression: it has no indentation of its own.
                atLineStart = _depth == 0;
                _lineHasCode = false;
                continue;
            }

            if (c is ' ' or '\t')
            {
                _index++;
                continue;
            }

            if (c == '#')
            {
                ReadComment(trailing: _lineHasCode);
                continue;
            }

            if (c == '\\' && _index + 1 < _source.Length && _source[_index + 1] == '\n')
            {
                _index += 2;
                _line++;
                _lineStart = _index;
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                ReadIdentifier();
            }
            else if (char.IsDigit(c) || (c == '.' && _index + 1 < _source.Length && char.IsDigit(_source[_index + 1])))
            {
                ReadNumber();
            }
            else if (c is '"' or '\'')
            {
                ReadString(c);
            }
            else
            {
                ReadSymbol();
            }
        }

        if (_tokens.Count > 0 && _tokens[^1].Kind != GdTokenKind.Newline)
        {
            Add(GdTokenKind.Newline, "\\n");
        }

        while (_indents.Count > 1)
        {
            _indents.Pop();
            Add(GdTokenKind.Dedent, "");
        }

        Add(GdTokenKind.EndOfFile, "");
        return _tokens;
    }

    private void ReadComment(bool trailing)
    {
        var position = Here;
        int start = _index;
        while (_index < _source.Length && _source[_index] != '\n') _index++;
        _comments.Add(new GdComment(position, _source[start.._index], trailing));
    }

    private GdPosition Here => new(_line, _index - _lineStart + 1);

    private void Add(GdTokenKind kind, string text)
    {
        _tokens.Add(new GdToken(kind, text, Here));
        if (kind is not (GdTokenKind.Newline or GdTokenKind.Indent or GdTokenKind.Dedent)) _lineHasCode = true;
    }

    /// <summary>Reads leading whitespace. Returns false when the line is blank or comment-only.</summary>
    private bool ReadIndentation()
    {
        int width = 0;
        int start = _index;
        while (_index < _source.Length && _source[_index] is ' ' or '\t')
        {
            char c = _source[_index];
            _indentChar ??= c;
            if (c != _indentChar)
            {
                _diagnostics.Error(Here, "Mixed tabs and spaces in indentation.");
            }

            width += c == '\t' ? 4 : 1;
            _index++;
        }

        if (_index >= _source.Length) return false;
        char next = _source[_index];
        if (next == '\n' || next == '#')
        {
            if (next == '#') ReadComment(trailing: false);
            while (_index < _source.Length && _source[_index] != '\n') _index++;
            if (_index < _source.Length)
            {
                _index++;
                _line++;
                _lineStart = _index;
            }

            return false;
        }

        _ = start;
        if (width > _indents.Peek())
        {
            _indents.Push(width);
            Add(GdTokenKind.Indent, "");
        }
        else
        {
            while (width < _indents.Peek())
            {
                _indents.Pop();
                Add(GdTokenKind.Dedent, "");
            }

            if (width != _indents.Peek())
            {
                _diagnostics.Error(Here, "Inconsistent indentation.");
            }
        }

        return true;
    }

    private void ReadIdentifier()
    {
        var position = Here;
        int start = _index;
        while (_index < _source.Length && (char.IsLetterOrDigit(_source[_index]) || _source[_index] == '_')) _index++;
        _tokens.Add(new GdToken(GdTokenKind.Identifier, _source[start.._index], position));
        _lineHasCode = true;
    }

    private void ReadNumber()
    {
        var position = Here;
        int start = _index;
        bool isFloat = false;

        if (_source[_index] == '0' && _index + 1 < _source.Length && _source[_index + 1] is 'x' or 'X')
        {
            _index += 2;
            while (_index < _source.Length && Uri.IsHexDigit(_source[_index])) _index++;
        }
        else
        {
            while (_index < _source.Length && (char.IsDigit(_source[_index]) || _source[_index] == '_')) _index++;
            if (_index < _source.Length && _source[_index] == '.' &&
                !(_index + 1 < _source.Length && char.IsLetter(_source[_index + 1])))
            {
                isFloat = true;
                _index++;
                while (_index < _source.Length && (char.IsDigit(_source[_index]) || _source[_index] == '_')) _index++;
            }

            if (_index < _source.Length && _source[_index] is 'e' or 'E')
            {
                int save = _index;
                _index++;
                if (_index < _source.Length && _source[_index] is '+' or '-') _index++;
                if (_index < _source.Length && char.IsDigit(_source[_index]))
                {
                    isFloat = true;
                    while (_index < _source.Length && char.IsDigit(_source[_index])) _index++;
                }
                else
                {
                    _index = save;
                }
            }
        }

        string text = _source[start.._index].Replace("_", "");
        // `u` marks an unsigned integer literal (Yotsuba extension; GDScript has no unsigned type).
        if (!isFloat && _index < _source.Length && _source[_index] is 'u' or 'U' &&
            !(_index + 1 < _source.Length && (char.IsLetterOrDigit(_source[_index + 1]) || _source[_index + 1] == '_')))
        {
            _index++;
            text += "u";
        }

        _tokens.Add(new GdToken(isFloat ? GdTokenKind.Float : GdTokenKind.Integer, text, position));
        _lineHasCode = true;
    }

    private void ReadString(char quote)
    {
        var position = Here;
        _index++;
        var text = new StringBuilder();
        while (_index < _source.Length && _source[_index] != quote && _source[_index] != '\n')
        {
            if (_source[_index] == '\\' && _index + 1 < _source.Length)
            {
                _index++;
            }

            text.Append(_source[_index++]);
        }

        if (_index >= _source.Length || _source[_index] != quote)
        {
            _diagnostics.Error(position, "Unterminated string literal.");
        }
        else
        {
            _index++;
        }

        _tokens.Add(new GdToken(GdTokenKind.String, text.ToString(), position));
        _lineHasCode = true;
    }

    private void ReadSymbol()
    {
        foreach (string symbol in Symbols)
        {
            if (string.CompareOrdinal(_source, _index, symbol, 0, symbol.Length) != 0) continue;
            if (symbol is "(" or "[") _depth++;
            if (symbol is ")" or "]") _depth = Math.Max(0, _depth - 1);
            _tokens.Add(new GdToken(GdTokenKind.Symbol, symbol, Here));
            _lineHasCode = true;
            _index += symbol.Length;
            return;
        }

        _diagnostics.Error(Here, $"Unexpected character '{_source[_index]}'.");
        _index++;
    }
}
