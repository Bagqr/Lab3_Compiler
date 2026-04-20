using System.Collections.Generic;

namespace WpfApp1
{
    public class Parser
    {
        private List<Lexem> _tokens;
        private int _position;
        private List<SyntaxError> _errors;

        private bool _failed;      
        private bool _reachedEnd;  

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
            _failed = false;
            _reachedEnd = false;

            SkipWhitespace();

            if (!Match(TOKEN_FOR, "for", critical: true))
                return _errors;

            ForBody();

            if (_reachedEnd)
                return _errors;

            while (Current() != null)
            {
                AddError(Current(), "Лишние символы после завершения программы");
                Next();
            }

            return _errors;
        }

        private void ForBody()
        {
            if (_failed || _reachedEnd) return;

            if (!Match(TOKEN_ID, "идентификатор"))
                SkipTo(TOKEN_IN, TOKEN_RANGE, TOKEN_COLON, TOKEN_PRINT);

            if (_reachedEnd) return;

            if (!Match(TOKEN_IN, "in"))
                SkipTo(TOKEN_RANGE, TOKEN_COLON, TOKEN_PRINT);

            if (_reachedEnd) return;

            RangeCall();

            if (_reachedEnd) return;

            if (!Match(TOKEN_COLON, ":"))
                SkipTo(TOKEN_PRINT);

            if (_reachedEnd) return;

            Block();
        }

        private void RangeCall()
        {
            if (_failed || _reachedEnd) return;

            if (!Match(TOKEN_RANGE, "range"))
                SkipTo(TOKEN_LPAREN, TOKEN_COLON, TOKEN_PRINT);

            if (_reachedEnd) return;

            if (!Match(TOKEN_LPAREN, "("))
                SkipTo(TOKEN_NUM, TOKEN_RPAREN, TOKEN_COLON, TOKEN_PRINT);

            if (_reachedEnd) return;

            if (CurrentCode() == TOKEN_NUM)
                Next();
            else
            {
                AddError(Current(), "Ожидалось целое число");
                SkipTo(TOKEN_RPAREN, TOKEN_COLON, TOKEN_PRINT);
            }

            if (_reachedEnd) return;

            Match(TOKEN_RPAREN, ")");
        }

        private void Block()
        {
            if (_failed || _reachedEnd) return;

            if (!Match(TOKEN_PRINT, "print"))
                SkipTo(TOKEN_LPAREN, TOKEN_SEMICOLON);

            if (_reachedEnd) return;

            if (!Match(TOKEN_LPAREN, "("))
                SkipTo(TOKEN_ID, TOKEN_RPAREN, TOKEN_SEMICOLON);

            if (_reachedEnd) return;

            if (CurrentCode() == TOKEN_ID)
                Next();
            else
            {
                AddError(Current(), "Ожидался идентификатор");
                SkipTo(TOKEN_RPAREN, TOKEN_SEMICOLON);
            }

            if (_reachedEnd) return;

            if (!Match(TOKEN_RPAREN, ")"))
                SkipTo(TOKEN_SEMICOLON);

            if (_reachedEnd) return;

            Match(TOKEN_SEMICOLON, ";");
        }

        private void SkipTo(params int[] syncTokens)
        {
            while (Current() != null)
            {
                int code = CurrentCode();
                foreach (int t in syncTokens)
                    if (code == t) return;

                Next();
            }
        }

        private Lexem Current() => _position < _tokens.Count ? _tokens[_position] : null;
        private int CurrentCode() => Current()?.Code ?? -1;

        private void Next()
        {
            if (_position < _tokens.Count)
                _position++;

            SkipWhitespace();
        }

        private void SkipWhitespace()
        {
            while (_position < _tokens.Count)
            {
                int c = _tokens[_position].Code;
                if (c == TOKEN_WHITESPACE || c == TOKEN_NEWLINE)
                    _position++;
                else
                    break;
            }
        }


        private bool Match(int expectedCode, string expectedDesc, bool critical = false)
        {
            if (_failed || _reachedEnd)
                return false;

            var cur = Current();

            if (cur == null)
            {
                if (!_reachedEnd)
                {
                    AddError(null, $"Ожидалось '{expectedDesc}', найдено конец файла");
                    _reachedEnd = true;
                }

                return false;
            }

            if (cur.Code == expectedCode)
            {
                Next();
                return true;
            }

            AddError(cur, $"Ожидалось '{expectedDesc}', найдено {cur.Value} ({cur.Type})");

            if (critical)
                _failed = true;

            return false;
        }

        private void AddError(Lexem token, string description)
        {
            if (token == null)
            {
                _errors.Add(new SyntaxError
                {
                    ErrorFragment = "EOF",
                    Location = "конец файла",
                    Description = description,
                    Line = -1,
                    Column = -1
                });
            }
            else
            {
                _errors.Add(new SyntaxError
                {
                    ErrorFragment = token.Value,
                    Location = $"строка {token.Line}, позиция {token.StartPos}",
                    Description = description,
                    Line = token.Line,
                    Column = token.StartPos
                });
            }
        }
    }
}
