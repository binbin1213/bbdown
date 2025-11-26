using BBDown.Core;

namespace BBDown
{
    internal class MyOption : BBDownArguments
    {
        //以下仅为兼容旧版本命令行，不建议使用
        public string Aria2cProxy { get; set; } = "";
        public bool OnlyHevc { get; set; }
        public bool OnlyAvc { get; set; }
        public bool OnlyAv1 { get; set; }
        public bool AddDfnSubfix { get; set; }
        public bool NoPaddingPageNum { get; set; }
        public bool BandwithAscending { get; set; }
    }
}