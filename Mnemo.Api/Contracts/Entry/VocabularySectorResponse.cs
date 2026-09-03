namespace Mnemo.Contracts.Entry
{
    public class VocabularySectorResponse
    {
        public string Label => StartWord.First() == EndWord.First() ? StartWord.First().ToString() : $"{StartWord.First()}-{EndWord.First()}";
        public string StartWord { get; set; }
        public string EndWord { get; set; }
        public int Count { get; set; }
    }
}
