using AutoMapper;
using Mnemo.Contracts.Entry;
using Mnemo.Contracts.Entry.Requests;
using Mnemo.Data.Entities;
using Mnemo.Shared;

namespace Mnemo.Services.Mapping
{
    public class VocabularyEntryProfile : Profile
    {
        public VocabularyEntryProfile()
        {
            CreateMap<CreateEntryRequest, VocabularyEntry>()
                .IncludeBase<CreateEntryRequest, VocabularyDefinition>();

            CreateMap<VocabularyEntry, EntryResponse>()
                .ForMember(dest => dest.PartOfSpeech, opt => opt.MapFrom(src => src.PartOfSpeech.HasValue ? src.PartOfSpeech.Value.ToString() : null))
                .ForMember(dest => dest.CERF, opt => opt.MapFrom(src => src.CEFR.HasValue ? src.CEFR.Value.ToString() : null));

            CreateMap<CreateEntryRequest, VocabularyDefinition>()
                .ForMember(dest => dest.PartOfSpeech, opt => opt.MapFrom(src => src.PartOfSpeech))
                .ForMember(dest => dest.CEFR, opt => opt.MapFrom(src => src.CERF))
                .ForMember(dest => dest.Foreign, opt => opt.MapFrom(src => TextNormalizer.NormalizeForeign(src.Foreign)))
                .ForMember(dest => dest.Transcription, opt => opt.MapFrom(src => src.Transcription != null ? TextNormalizer.NormalizeTranscription(src.Transcription) : null))
                .ForMember(dest => dest.Examples, opt => opt.MapFrom(src => TextNormalizer.NormalizeEnumerable(src.Examples, TextNormalizer.NormalizeExample)))
                .ForMember(dest => dest.Translations, opt => opt.MapFrom(src => TextNormalizer.NormalizeEnumerable(src.Translations, TextNormalizer.NormalizeTranslation)));
        }
    }
}
