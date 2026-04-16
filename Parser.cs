using System.Collections.Generic;

namespace WpfApp1
{
    public class Parser
    {
        private List<Lexem> _tokens;
        private int _position;
        private List<SyntaxError> _errors;

        private const int TOKEN_FOR = 1;
        private const int TOKEN_IN = 2;
        private const int TOKEN_RANGE = 3;
        private const int TOKEN_PRINT = 4;
        private const int TOKEN_ID = 10;
        private const int TOKEN_NUM = 20;
        private const int TOKEN_COLON = 60;
        private const int TOKEN_SEMICOLON = 64;
        private const int TOKEN_LPAREN = 62;
        private const int TOKEN_RPAREN = 63;
        private const int TOKEN_WHITESPACE = 50;
        private const int TOKEN_NEWLINE = 51;

        public List<SyntaxError> Parse(List<Lexem> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _errors = new List<SyntaxError>();
            SkipWhitespace();
            Program();
            return _errors;
        }

        private void Program()
        {
            while (Current() != null)
            {
                int oldPos = _position; 
                if (!ForStmt())
                {
                    SkipToSync(TOKEN_FOR, TOKEN_PRINT);
                    if (_position == oldPos && Current() != null)
                        Next();
                }
                SkipWhitespace();
            }
        }

        private bool ForStmt()
        {
            if (!Match(TOKEN_FOR, "for")) return false;

            if (!Match(TOKEN_ID, "идентификатор"))
            {
                SkipToSync(TOKEN_FOR, TOKEN_PRINT);
                return true;
            }

            if (!Match(TOKEN_IN, "in"))
            {
                SkipToSync(TOKEN_FOR, TOKEN_PRINT);
                return true;
            }

            if (!RangeCall())
            {
                SkipToSync(TOKEN_COLON, TOKEN_FOR, TOKEN_PRINT);
                if (CurrentCode() != TOKEN_COLON) return true;
            }

            if (!Match(TOKEN_COLON, ":"))
            {
                SkipToSync(TOKEN_FOR, TOKEN_PRINT);
                return true;
            }

            if (!Block())
                SkipToSync(TOKEN_FOR, TOKEN_PRINT);

            return true;
        }

        private bool RangeCall()
        {
            if (!Match(TOKEN_RANGE, "range")) return false;
            if (!Match(TOKEN_LPAREN, "(")) return false;

            bool exprOk = Expr();
            if (!exprOk)
                SkipToSync(TOKEN_RPAREN, TOKEN_FOR, TOKEN_PRINT);

            if (!Match(TOKEN_RPAREN, ")")) return false;
            return true;
        }

        private bool Expr()
        {
            if (CurrentCode() == TOKEN_NUM || CurrentCode() == TOKEN_ID)
            {
                Next();
                return true;
            }
            AddError(Current(), "Ожидалось целое число или идентификатор");
            return false;
        }

        private bool PrintCall()
        {
            if (!Match(TOKEN_PRINT, "print")) return false;
            if (!Match(TOKEN_LPAREN, "(")) return false;

            bool exprOk = Expr();
            if (!exprOk)
                SkipToSync(TOKEN_RPAREN, TOKEN_FOR, TOKEN_PRINT);

            if (!Match(TOKEN_RPAREN, ")")) return false;
            if (CurrentCode() == TOKEN_SEMICOLON) Next();
            return true;
        }

        private bool Block()
        {
            bool hasAny = false;
            while (true)
            {
                SkipWhitespace();
                int code = CurrentCode();
                if (code == TOKEN_PRINT)
                {
                    if (!PrintCall())
                        SkipToSync(TOKEN_PRINT, TOKEN_FOR);
                    hasAny = true;
                }
                else if (code == TOKEN_FOR)
                {
                    if (!ForStmt())
                        SkipToSync(TOKEN_PRINT, TOKEN_FOR);
                    hasAny = true;
                }
                else break;
            }
            return hasAny;
        }

        private Lexem Current() => _position < _tokens.Count ? _tokens[_position] : null;
        private int CurrentCode() => Current()?.Code ?? -1;

        private void Next()
        {
            if (_position < _tokens.Count) _position++;
            SkipWhitespace();
        }

        private void SkipWhitespace()
        {
            while (_position < _tokens.Count)
            {
                int c = _tokens[_position].Code;
                if (c == TOKEN_WHITESPACE || c == TOKEN_NEWLINE) _position++;
                else break;
            }
        }

        private bool Match(int expectedCode, string expectedDesc)
        {
            var cur = Current();
            if (cur != null && cur.Code == expectedCode)
            {
                Next();
                return true;
            }
            string found = cur != null ? $"{cur.Value} ({cur.Type})" : "конец файла";
            AddError(cur, $"Ожидалось '{expectedDesc}', найдено {found}");
            return false;
        }

        private void AddError(Lexem token, string description)
        {
            if (token == null)
                _errors.Add(new SyntaxError { ErrorFragment = "EOF", Location = "конец файла", Description = description, Line = -1, Column = -1 });
            else
                _errors.Add(new SyntaxError { ErrorFragment = token.Value, Location = $"строка {token.Line}, позиция {token.StartPos}", Description = description, Line = token.Line, Column = token.StartPos });
        }

        private void SkipToSync(params int[] syncTokens)
        {
            if (_position < _tokens.Count)
            {
                int cur = CurrentCode();
                foreach (int s in syncTokens)
                    if (cur == s) { Next(); return; }
                if (cur == TOKEN_FOR || cur == TOKEN_PRINT) { Next(); return; }
            }

            while (_position < _tokens.Count)
            {
                int c = CurrentCode();
                foreach (int s in syncTokens)
                    if (c == s) return;
                if (c == TOKEN_FOR || c == TOKEN_PRINT) return;
                Next();
            }
        }
    }
}