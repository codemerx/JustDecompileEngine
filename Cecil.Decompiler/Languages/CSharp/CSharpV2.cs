using System.Collections.Generic;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV2 : CSharpV1
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;

        static CSharpV2()
        {
            CSharpV2.languageSpecificGlobalKeywords = new HashSet<string>(CSharpV1.languageSpecificGlobalKeywords);
            CSharpV2.languageSpecificContextualKeywords = new HashSet<string>(CSharpV1.languageSpecificContextualKeywords);
        }

        public override int Version
        {
            get
            {
                return 2;
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharpV2.languageSpecificGlobalKeywords, CSharpV2.languageSpecificContextualKeywords);
        }

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharpV2.languageSpecificGlobalKeywords);
        }
    }
}
