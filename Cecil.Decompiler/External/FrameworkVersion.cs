using JustDecompile.SmartAssembly.Attributes;

namespace Telerik.JustDecompiler.External
{
    [DoNotPrune]
    [DoNotObfuscateType]
    public enum FrameworkVersion
    {
        Unknown,
        v1_0,
        v1_1,
        v2_0,
        v3_0,
        v3_5,
        v4_0,
        v4_5,
        v4_5_1,
        v4_5_2,
        v4_6,
        v4_6_1,
        v4_6_2,
        v4_7,
        v4_7_1,
		Silverlight,
        WindowsPhone,
        WindowsCE,
        NetPortableV4_6,
        WinRT_4_5,
        WinRT_4_5_1,
        UWP,
		NetCoreV2_1,
		NetCoreV2_0,
		NetCoreV1_1,
		NetCoreV1_0,
		XamarinAndroid,
		XamarinIOS
	}
}