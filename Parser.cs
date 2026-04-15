using System.Collections.Generic;
using System.Text;

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
            if (!ForStmt())
            {
                if (_errors.Count == 0 && Current() != null)
                {
                    AddError(Current(), "Ожидалось 'for' в начале программы");
                }
            }
        }

        private bool ForStmt()
        {
            int savedPos = _position;

            if (!Match(TOKEN_FOR, "for"))
            {
                SkipToSync(TOKEN_FOR);
                return false;
            }

            if (!Match(TOKEN_ID, "идентификатор"))
            {
                SkipToSync(TOKEN_IN, TOKEN_COLON, TOKEN_FOR);
                return false;
            }

            if (!Match(TOKEN_IN, "in"))
            {
                SkipToSync(TOKEN_RANGE, TOKEN_FOR);
                return false;
            }

            if (!RangeCall())
            {
                SkipToSync(TOKEN_COLON, TOKEN_FOR);
                return false;
            }

            if (!Match(TOKEN_COLON, ":"))
            {
                SkipToSync(TOKEN_PRINT, TOKEN_FOR);
                return false;
            }

            if (!Block())
            {
                return false;
            }

            return true;
        }

        private bool RangeCall()
        {
            if (!Match(TOKEN_RANGE, "range"))
                return false;

            if (!Match(TOKEN_LPAREN, "("))
                return false;

            if (!Expr())
                return false;

            if (!Match(TOKEN_RPAREN, ")"))
                return false;

            return true;
        }

        private bool Expr()
        {
            if (CurrentCode() == TOKEN_NUM)
            {
                Next();
                return true;
            }
            else if (CurrentCode() == TOKEN_ID)
            {
                Next();
                return true;
            }
            else
            {
                AddError(Current(), "Ожидалось целое число или идентификатор");
                return false;
            }
        }

        private bool PrintCall()
        {
            if (!Match(TOKEN_PRINT, "print"))
                return false;

            if (!Match(TOKEN_LPAREN, "("))
                return false;

            if (!Expr())
                return false;

            if (!Match(TOKEN_RPAREN, ")"))
                return false;

            if (!Match(TOKEN_SEMICOLON, ";"))
                return false;

            return true;
        }

        private bool Block()
        {
            if (CurrentCode() == TOKEN_PRINT)
            {
                return PrintCall();
            }
            else if (CurrentCode() == TOKEN_FOR)
            {
                return ForStmt();
            }
            else
            {
                return true;
            }
        }


        private Lexem Current()
        {
            if (_position < _tokens.Count)
                return _tokens[_position];
            return null;
        }

        private int CurrentCode()
        {
            var cur = Current();
            return cur != null ? cur.Code : -1;
        }

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
                int code = _tokens[_position].Code;
                if (code == TOKEN_WHITESPACE || code == TOKEN_NEWLINE)
                    _position++;
                else
                    break;
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
            else
            {
                string found = cur != null ? $"{cur.Value} ({cur.Type})" : "конец файла";
                AddError(cur, $"Ожидалось '{expectedDesc}', найдено {found}");
                return false;
            }
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

        private void SkipToSync(params int[] syncTokens)
        {
            while (_position < _tokens.Count)
            {
                int code = CurrentCode();
                foreach (int sync in syncTokens)
                {
                    if (code == sync)
                        return;
                }
                if (code == TOKEN_FOR || code == TOKEN_PRINT)
                    return;
                Next();
            }
        }
    }
}