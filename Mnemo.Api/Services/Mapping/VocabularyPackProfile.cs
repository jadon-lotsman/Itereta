using AutoMapper;
using Mnemo.Contracts.Pack;
using Mnemo.Contracts.Pack.Requests;
using Mnemo.Contracts.Vocabulary.Requests;
using Mnemo.Data.Entities;
using Mnemo.Shared;

namespace Mnemo.Services.Mapping
{
    public class VocabularyPackProfile : Profile
    {
        public VocabularyPackProfile()
        {
            CreateMap<CreateEntryRequest, VocabularyPackEntry>()
                .IncludeBase<CreateEntryRequest, VocabularyDefinition>();


            CreateMap<VocabularyPack, PackResponse>();

            CreateMap<CreatePackRequest, VocabularyPack>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => TextNormalizer.NormalizeExample(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => TextNormalizer.NormalizeExample(src.Description)))
                .ForMember(dest => dest.Visibility, opt => opt.MapFrom(src => src.Visibility))
                .ForMember(dest => dest.PackEntries, opt => opt.Ignore());
        }
    }
}
