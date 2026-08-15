namespace Mnemo.Data.Entities
{
    public class VocabularyPackEntry : VocabularyDefinition
    {
        public int Id { get; set; }


        public int VocabularyPackId { get; set; }
        public VocabularyPack VocabularyPack { get; set; }
    }
}
