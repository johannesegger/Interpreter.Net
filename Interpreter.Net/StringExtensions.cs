using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Interpreter.Net
{
    internal static class StringExtensions
    {
        public static string JoinString(this IEnumerable<string> text, string separator)
        {
            return string.Join(separator, text);
        }

        private static string ChangeFirstCharacter(this string text, Func<string, string> fn)
        {
            if (text == string.Empty)
            {
                return text;
            }
            return fn(text.Substring(0, 1)) + text.Substring(1);
        }

        public static string ToCamelCase(this string text)
        {
            return text.ChangeFirstCharacter(s => s.ToLower());
        }

        public static string ToPascalCase(this string text)
        {
            return text.ChangeFirstCharacter(s => s.ToUpper());
        }
    }
}