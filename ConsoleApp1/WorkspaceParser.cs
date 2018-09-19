using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace nuget2bazel
{
    public class WorkspaceParser
    {
        private readonly string _toparse;
        private int _pos = 0;

        public WorkspaceParser(string toparse)
        {
            _toparse = toparse;
        }

        public IEnumerable<WorkspaceEntry> Parse()
        {
            return ParseEntries();
        }

        private IEnumerable<WorkspaceEntry> ParseEntries()
        {
            var result = new List<WorkspaceEntry>();
            for (;;)
            {
                var entry = ParseEntry();
                if (entry == null)
                    break;
                result.Add(entry);
            }

            return result;
        }

        private WorkspaceEntry ParseEntry()
        {
            while (!IsEof() && IsWhitespace()) ++_pos;

            if (IsEof())
                return null;

            var result = new WorkspaceEntry();

            RequireToken(Token.NUGET_PACKAGE);
            RequireToken(Token.LPAR);
            RequireAssignment(Token.NAME);
            var package = RequireAssignment(Token.PACKAGE);
            var version = RequireAssignment(Token.VERSION);
            result.PackageIdentity = new PackageIdentity(package, new NuGetVersion(version));
            result.Sha256 = RequireAssignment(Token.SHA256);
            result.CoreLib= RequireAssignment(Token.CORE_LIB);
            result.NetLib = RequireAssignment(Token.NET_LIB);
            result.MonoLib= RequireAssignment(Token.MONO_LIB);
            result.Core_Deps = RequireArray(Token.CORE_DEPS);
            result.Net_Deps = RequireArray(Token.NET_DEPS);
            result.Mono_Deps = RequireArray(Token.MONO_DEPS);
            result.Core_Files = RequireArray(Token.CORE_FILES);
            result.Net_Files = RequireArray(Token.NET_FILES);
            result.Mono_Files = RequireArray(Token.MONO_FILES);
            RequireToken(Token.RPAR);
            return result;
        }

        private string RequireToken(Token required)
        {
            var (token, value) = GetToken();
            if (token != required)
                throw new InvalidOperationException($"Unexpected token {token}, {value}. Expected {required}");

            return value;
        }

        private string RequireAssignment(Token required)
        {
            RequireToken(required);
            RequireToken(Token.EQUAL);
            return RequireToken(Token.STRING);
        }

        private IEnumerable<string> RequireArray(Token required)
        {
            RequireToken(required);
            RequireToken(Token.EQUAL);
            RequireToken(Token.LBRACKET);

            var result = new List<string>();
            for (;;)
            {
                var (token, value) = GetToken();
                if (token == Token.STRING)
                {
                    result.Add(value);
                    continue;
                }

                if (token == Token.RBRACKET)
                    break;
            }

            return result;
        }

        enum Token
        {
            NUGET_PACKAGE,
            NAME,
            PACKAGE,
            VERSION,
            SHA256,
            CORE_LIB,
            NET_LIB,
            MONO_LIB,
            CORE_DEPS,
            NET_DEPS,
            MONO_DEPS,
            LBRACKET,
            RBRACKET,
            LPAR,
            RPAR,
            CORE_FILES,
            NET_FILES,
            MONO_FILES,
            STRING,
            EOF,
            ERROR,
            EQUAL
        }

        private (Token, string) GetToken()
        {
            while (!IsEof() && IsWhitespace()) ++_pos;

            if (IsEof()) return (Token.EOF, null);

            switch (_toparse[_pos])
            {
                case '(':
                    ++_pos;
                    return (Token.LPAR, null);
                case ')':
                    ++_pos;
                    return (Token.RPAR, null);
                case '[':
                    ++_pos;
                    return (Token.LBRACKET, null);
                case ']':
                    ++_pos;
                    return (Token.RBRACKET, null);
                case '=':
                    ++_pos;
                    return (Token.EQUAL, null);
            }

            if (_toparse[_pos] == '\"')
                return GetString();

            int q;
            for (q = _pos; !IsSeparator(_toparse[q]) && q < _toparse.Length; ++q) ;
            var str = _toparse.Substring(_pos, q - _pos);
            _pos = q;

            if (str == "nuget_package") return (Token.NUGET_PACKAGE, null);
            if (str == "name") return (Token.NAME, null);
            if (str == "package") return (Token.PACKAGE, null);
            if (str == "version") return (Token.VERSION, null);
            if (str == "sha256") return (Token.SHA256, null);
            if (str == "core_lib") return (Token.CORE_LIB, null);
            if (str == "net_lib") return (Token.NET_LIB, null);
            if (str == "mono_lib") return (Token.MONO_LIB, null);
            if (str == "core_deps") return (Token.CORE_DEPS, null);
            if (str == "net_deps") return (Token.NET_DEPS, null);
            if (str == "mono_deps") return (Token.MONO_DEPS, null);
            if (str == "core_files") return (Token.CORE_FILES, null);
            if (str == "net_files") return (Token.NET_FILES, null);
            if (str == "mono_files") return (Token.MONO_FILES, null);

            throw new InvalidOperationException($"Unknown token: {str}");
        }

        private (Token, string) GetString()
        {
            ++_pos;
            var q = _pos;

            while (!IsEof() && _toparse[q] != '\"') ++q;
            if (IsEof())
                throw new InvalidOperationException("Expected end of string");

            var result = _toparse.Substring(_pos, q - _pos);
            _pos = q + 1;

            return (Token.STRING, result);
        }

        private bool IsEof()
        {
            return _pos >= _toparse.Length;
        }

        private bool IsWhitespace(char p)
        {
            switch (p)
            {
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                case ',':
                    return true;
            }

            return false;
        }
        private bool IsSeparator(char p)
        {
            switch (p)
            {
                case ' ':
                case '\t':
                case '\n':
                case ',':
                case '(':
                case ')':
                case '[':
                case ']':
                case '=':
                case '\r':
                    return true;
            }

            return false;
        }
        private bool IsWhitespace()
        {
            return IsWhitespace(_toparse[_pos]);
        }
    }
}
