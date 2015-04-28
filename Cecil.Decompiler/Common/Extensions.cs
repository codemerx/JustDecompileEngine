using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Telerik.JustDecompiler.External;

namespace Telerik.JustDecompiler.Common
{
    public static class Extensions
    {
        public static bool IsValidIdentifier(this string self)
        {
            if (self == null || self == string.Empty)
            {
                return false;
            }

			string normalizedString;
			try
			{
				normalizedString = self.Normalize();
			}
			catch (ArgumentException)
			{
				// String contains invalid code points created by obfuscators
				return false;
			}

            if (!IsValidIdentifierStartCharacter(normalizedString[0]))
            {
                return false;
            }

            for (int i = 1; i < normalizedString.Length; i++)
            {
                if (!IsValidIdentifierCharacter(normalizedString[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidIdentifierStartCharacter(char c)
        {
            if (c == '_')
            {
                return true;
            }

            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
            }

            return false;
        }

        private static bool IsValidIdentifierCharacter(char c)
        {
            if (IsValidIdentifierStartCharacter(c))
            {
                return true;
            }

            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.Format:
                    return true;
            }

            return false;
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> source)
        {
            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                self.Add(pair);
            }
        }

        /// <summary>
        /// Resolves the given TypeReference to the first overloaded equality (inequality) operator or to Object if there is no such.
        /// </summary>
        /// <param name="type">The TypeReference to be resolved.</param>
        /// <param name="lastResolvedType">Out parameter which is filled with the last resolved type.</param>
        /// <returns>True if there is overloaded operator. False if there is no overloaded operator in the whole inheritance chain. Null if there is not resolved reference.</returns>
        public static bool? ResolveToOverloadedEqualityOperator(TypeReference type, out TypeReference lastResolvedType)
        {
            if (type.IsValueType || type.IsFunctionPointer || type.IsPrimitive || type.IsGenericParameter)
            {
                throw new NotSupportedException();
            }

            lastResolvedType = type;
            TypeDefinition currentTypeDefinition = type.Resolve();
            if (currentTypeDefinition == null)
            {
                return null;
            }

            if (currentTypeDefinition.IsInterface)
            {
                return false;
            }

            while (currentTypeDefinition.Name != "Object")
            {
                if (currentTypeDefinition.Methods.Any(m => m.Name == "op_Equality" || m.Name == "op_Inequality"))
                {
                    return true;
                }

                lastResolvedType = currentTypeDefinition.BaseType;
                TypeDefinition baseTypeDefinition = currentTypeDefinition.BaseType.Resolve();
                if (baseTypeDefinition == null)
                {
                    return null;
                }

                currentTypeDefinition = baseTypeDefinition;
            }

            return false;
        }

        public static string ToString(this FrameworkVersion self, bool includeVersionSign)
        {
            string result = string.Empty;
            switch (self)
            {
                case FrameworkVersion.v1_0:
                    result = "1.0";
                    break;
                case FrameworkVersion.v2_0:
                    result = "2.0";
                    break;
                case FrameworkVersion.v3_0:
                    result = "3.0";
                    break;
                case FrameworkVersion.v3_5:
                    result = "3.5";
                    break;
                case FrameworkVersion.v4_0:
                    result = "4.0";
                    break;
                case FrameworkVersion.v4_5:
                    result = "4.5";
                    break;
                case FrameworkVersion.v4_5_1:
                    result = "4.5.1";
                    break;
                case FrameworkVersion.v4_5_2:
                    result = "4.5.2";
                    break;
                case FrameworkVersion.v4_6:
                    result = "4.6";
                    break;
                case FrameworkVersion.WinRT:
                case FrameworkVersion.Silverlight:
                case FrameworkVersion.WindowsCE:
                case FrameworkVersion.WindowsPhone:
                    return self.ToString();
                default:
                    return string.Empty;
            }

            if (includeVersionSign)
            {
                return "v" + result;
            }
            else
            {
                return result;
            }
        }
    }
}
