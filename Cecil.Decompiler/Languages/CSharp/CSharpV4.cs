using System.Collections.Generic;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV4 : CSharpV3
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;

        static CSharpV4()
        {
            CSharpV4.languageSpecificGlobalKeywords = new HashSet<string>(CSharpV3.languageSpecificGlobalKeywords);
            CSharpV4.languageSpecificContextualKeywords = new HashSet<string>(CSharpV3.languageSpecificContextualKeywords);
        }

        public override int Version
        {
            get
            {
                return 4;
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharpV4.languageSpecificGlobalKeywords, CSharpV4.languageSpecificContextualKeywords);
        }

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharpV4.languageSpecificGlobalKeywords);
        }
    }
}
