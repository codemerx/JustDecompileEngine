using System.Collections.Generic;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV5 : CSharpV4
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;

        static CSharpV5()
        {
            CSharpV5.languageSpecificGlobalKeywords = new HashSet<string>(CSharpV4.languageSpecificGlobalKeywords);
            CSharpV5.languageSpecificContextualKeywords = new HashSet<string>(CSharpV4.languageSpecificContextualKeywords);
        }

        public override string Name
        {
            get
            {
                return "C#5";
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharpV5.languageSpecificGlobalKeywords, CSharpV5.languageSpecificContextualKeywords);
        }

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharpV5.languageSpecificGlobalKeywords);
        }
    }
}
