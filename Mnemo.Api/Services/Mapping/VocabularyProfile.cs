using AutoMapper;
using Mnemo.Contracts.Entry.Requests;
using Mnemo.Contracts.Vocabulary;
using Mnemo.Contracts.Vocabulary.Requests;
using Mnemo.Data.Entities;
using Mnemo.Shared;

namespace Mnemo.Services.Mapping
{
    public class VocabularyProfile : Profile
    {
        public VocabularyProfile()
        {
            CreateMap<CreateEntryRequest, VocabularyEntry>()
                .IncludeBase<CreateEntryRequest, VocabularyDefinition>();


            CreateMap<Vocabulary, VocabularyResponse>();

            CreateMap<CreateVocabularyRequest, Vocabulary>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => TextNormalizer.NormalizeExample(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => TextNormalizer.NormalizeExample(src.Description)))
                .ForMember(dest => dest.Visibility, opt => opt.MapFrom(src => src.Visibility))
                .ForMember(dest => dest.Entries, opt => opt.Ignore());
        }
    }
}
